[CmdletBinding()]
param(
	[string] $RepositoryRoot = (Get-Location).Path,
	[string[]] $ChangedFiles,
	[switch] $BuildAllProjects,
	[string] $BuildAllProjectsInput = $env:BUILD_ALL_PROJECTS,
	[string] $EventName = $env:GITHUB_EVENT_NAME,
	[string] $BaseSha,
	[string] $HeadSha = $env:GITHUB_SHA,
	[string] $GitHubOutput = $env:GITHUB_OUTPUT,
	[switch] $PassThru
)

$ErrorActionPreference = 'Stop'

function Get-NormalizedRelativePath {
	param(
		[Parameter(Mandatory)] [string] $Root,
		[Parameter(Mandatory)] [string] $Path
	)

	return [System.IO.Path]::GetRelativePath($Root, $Path).Replace('\', '/')
}

function Get-NuGetProjects {
	param([Parameter(Mandatory)] [string] $Root)

	return @(Get-ChildItem -LiteralPath $Root -Recurse -Filter *.csproj -File | ForEach-Object {
		$projectFile = $_
		[xml] $projectXml = Get-Content -LiteralPath $projectFile.FullName
		$packageIdNode = $projectXml.SelectSingleNode('//PackageId')
		$isPackableNode = $projectXml.SelectSingleNode('//IsPackable')
		$projectReferences = @($projectXml.SelectNodes('//ProjectReference') | ForEach-Object {
			if (-not [string]::IsNullOrWhiteSpace($_.Include)) {
				$referencePath = [System.IO.Path]::GetFullPath((Join-Path $projectFile.DirectoryName $_.Include))
				Get-NormalizedRelativePath -Root $Root -Path $referencePath
			}
		})

		[PSCustomObject]@{
			Path = Get-NormalizedRelativePath -Root $Root -Path $projectFile.FullName
			Directory = Get-NormalizedRelativePath -Root $Root -Path $projectFile.DirectoryName
			PackageId = if ($null -eq $packageIdNode -or [string]::IsNullOrWhiteSpace($packageIdNode.InnerText)) { $projectFile.BaseName } else { $packageIdNode.InnerText }
			IsPackable = -not ($null -ne $isPackableNode -and $isPackableNode.InnerText.Trim().Equals('false', [System.StringComparison]::OrdinalIgnoreCase))
			ProjectReferences = $projectReferences
		}
	})
}

function Get-ChangedFilesFromGit {
	param(
		[Parameter(Mandatory)] [string] $Root,
		[string] $WorkflowEventName,
		[string] $WorkflowBaseSha,
		[string] $WorkflowHeadSha
	)

	Push-Location $Root
	try {
		if ($WorkflowEventName -eq 'pull_request') {
			if ([string]::IsNullOrWhiteSpace($WorkflowBaseSha) -or [string]::IsNullOrWhiteSpace($WorkflowHeadSha)) {
				throw 'Pull request change detection requires both base and head commit SHAs.'
			}

			& git fetch --no-tags origin $WorkflowBaseSha --depth=1
			if ($LASTEXITCODE -ne 0) { throw "Failed to fetch base commit $WorkflowBaseSha." }
			& git fetch --no-tags origin $WorkflowHeadSha --depth=1
			if ($LASTEXITCODE -ne 0) { throw "Failed to fetch head commit $WorkflowHeadSha." }
		}
		else {
			if ([string]::IsNullOrWhiteSpace($WorkflowBaseSha) -or $WorkflowBaseSha -eq ('0' * 40)) {
				$WorkflowBaseSha = & git rev-list --max-parents=0 HEAD | Select-Object -First 1
				if ($LASTEXITCODE -ne 0) { throw 'Failed to determine the repository root commit.' }
			}
		}

		Write-Host "Comparing changes from $WorkflowBaseSha to $WorkflowHeadSha"
		$files = @(& git diff --name-only $WorkflowBaseSha $WorkflowHeadSha)
		if ($LASTEXITCODE -ne 0) { throw 'Failed to determine changed files.' }
		return @($files | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
	}
	finally {
		Pop-Location
	}
}

function Select-ChangedNuGetProjects {
	param(
		[Parameter(Mandatory)] [object[]] $Projects,
		[string[]] $Files = @(),
		[switch] $SelectAll
	)

	$projectMap = @{}
	$dependentsByProject = @{}
	foreach ($project in $Projects) {
		$projectMap[$project.Path] = $project
		$dependentsByProject[$project.Path] = New-Object System.Collections.Generic.List[string]
	}

	foreach ($project in $Projects) {
		foreach ($reference in $project.ProjectReferences) {
			if ($dependentsByProject.ContainsKey($reference)) {
				$dependentsByProject[$reference].Add($project.Path)
			}
		}
	}

	$directlyChanged = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
	if ($SelectAll) {
		foreach ($project in $Projects) { $null = $directlyChanged.Add($project.Path) }
	}
	else {
		foreach ($file in $Files) {
			$normalizedFile = $file.Replace('\', '/')
			foreach ($project in $Projects) {
				$directory = if ($project.Directory -eq '.') { '' } else { $project.Directory.TrimEnd('/') }
				if ($directory -and $normalizedFile.StartsWith("$directory/", [System.StringComparison]::OrdinalIgnoreCase)) {
					$null = $directlyChanged.Add($project.Path)
				}
			}
		}
	}

	$selected = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
	$projectsToVisit = New-Object System.Collections.Generic.Queue[string]
	foreach ($projectPath in $directlyChanged) { $projectsToVisit.Enqueue($projectPath) }

	while ($projectsToVisit.Count -gt 0) {
		$projectPath = $projectsToVisit.Dequeue()
		if (-not $selected.Add($projectPath)) { continue }

		foreach ($dependent in $dependentsByProject[$projectPath]) {
			$projectsToVisit.Enqueue($dependent)
		}
	}

	$selectedPaths = @($selected | Sort-Object)
	$packageIds = @($selectedPaths | ForEach-Object {
		$project = $projectMap[$_]
		if ($project.IsPackable) { $project.PackageId }
	} | Sort-Object -Unique)

	return [PSCustomObject]@{
		HasChanges = $packageIds.Count -gt 0
		PackageIds = $packageIds
		SelectedProjectPaths = $selectedPaths
	}
}

function Write-WorkflowOutput {
	param(
		[Parameter(Mandatory)] [string] $OutputPath,
		[Parameter(Mandatory)] $Result
	)

	"has_changes=$($Result.HasChanges.ToString().ToLowerInvariant())" | Out-File -FilePath $OutputPath -Encoding utf8 -Append
	"package_ids=$([string]::Join(',', $Result.PackageIds))" | Out-File -FilePath $OutputPath -Encoding utf8 -Append
	"selected_project_paths=$($Result.SelectedProjectPaths | ConvertTo-Json -Compress -AsArray)" | Out-File -FilePath $OutputPath -Encoding utf8 -Append
}

$resolvedRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$projects = Get-NuGetProjects -Root $resolvedRoot
$shouldBuildAll = $BuildAllProjects -or ($EventName -eq 'workflow_dispatch' -and $BuildAllProjectsInput -eq 'true')

if (-not $shouldBuildAll -and $null -eq $ChangedFiles) {
	$ChangedFiles = Get-ChangedFilesFromGit -Root $resolvedRoot -WorkflowEventName $EventName -WorkflowBaseSha $BaseSha -WorkflowHeadSha $HeadSha
}

$effectiveChangedFiles = if ($null -eq $ChangedFiles) { [string[]] @() } else { [string[]] @($ChangedFiles) }
$effectiveChangedFiles | ForEach-Object { Write-Host "Changed: $_" }
$result = Select-ChangedNuGetProjects -Projects $projects -Files $effectiveChangedFiles -SelectAll:$shouldBuildAll

Write-Host "Projects selected for versioning: $([string]::Join(', ', $result.SelectedProjectPaths))"
Write-Host "Package IDs selected for publishing: $([string]::Join(', ', $result.PackageIds))"

if (-not [string]::IsNullOrWhiteSpace($GitHubOutput)) {
	Write-WorkflowOutput -OutputPath $GitHubOutput -Result $result
}

if ($PassThru) {
	return $result
}
