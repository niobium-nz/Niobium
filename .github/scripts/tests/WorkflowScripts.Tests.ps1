$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$scriptRoot = Split-Path -Parent $PSScriptRoot
$detector = Join-Path $scriptRoot 'Detect-ChangedNuGetProjects.ps1'
$versionGenerator = Join-Path $scriptRoot 'New-ProjectVersionProps.ps1'
$cleanup = Join-Path $scriptRoot 'Remove-ProjectVersionProps.ps1'
$testCount = 0

function Assert-Equal {
	param($Expected, $Actual, [string] $Message)
	$script:testCount++
	if ($Expected -ne $Actual) {
		throw "$Message Expected: '$Expected'; actual: '$Actual'."
	}
}

function Assert-SequenceEqual {
	param([string[]] $Expected, [string[]] $Actual, [string] $Message)
	Assert-Equal -Expected ([string]::Join('|', $Expected)) -Actual ([string]::Join('|', $Actual)) -Message $Message
}

function New-TestProject {
	param(
		[Parameter(Mandatory)] [string] $Root,
		[Parameter(Mandatory)] [string] $Name,
		[string[]] $References = @(),
		[bool] $IsPackable = $true
	)

	$directory = Join-Path $Root $Name
	$null = New-Item -ItemType Directory -Path $directory
	$referenceXml = [string]::Join([Environment]::NewLine, @($References | ForEach-Object { "    <ProjectReference Include=`"..\$_\$_.csproj`" />" }))
	$content = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
	<TargetFramework>net10.0</TargetFramework>
	<IsPackable>$($IsPackable.ToString().ToLowerInvariant())</IsPackable>
	<Version Condition=" '`$(BUILD_BUILDNUMBER)' != '' ">`$(BUILD_BUILDNUMBER)</Version>
  </PropertyGroup>
  <ItemGroup>
$referenceXml
  </ItemGroup>
</Project>
"@
	Set-Content -LiteralPath (Join-Path $directory "$Name.csproj") -Value $content
	Set-Content -LiteralPath (Join-Path $directory 'Class.cs') -Value "namespace $Name; public class Class { }"
}

function Invoke-Detection {
	param([string] $Root, [string[]] $ChangedFiles, [switch] $BuildAllProjects)
	return & $detector -RepositoryRoot $Root -ChangedFiles $ChangedFiles -BuildAllProjects:$BuildAllProjects -GitHubOutput '' -PassThru
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "NiobiumWorkflowTests-$([guid]::NewGuid())"
$null = New-Item -ItemType Directory -Path $tempRoot
try {
	New-TestProject -Root $tempRoot -Name C
	New-TestProject -Root $tempRoot -Name B -References C
	New-TestProject -Root $tempRoot -Name A -References B

	$result = Invoke-Detection -Root $tempRoot -ChangedFiles 'C/Class.cs'
	Assert-SequenceEqual -Expected @('A/A.csproj', 'B/B.csproj', 'C/C.csproj') -Actual $result.SelectedProjectPaths -Message 'Changing C should select C and all transitive dependents.'
	Assert-SequenceEqual -Expected @('A', 'B', 'C') -Actual $result.PackageIds -Message 'Changing C should publish all packages in the reverse closure.'

	$result = Invoke-Detection -Root $tempRoot -ChangedFiles 'B/Class.cs'
	Assert-SequenceEqual -Expected @('A/A.csproj', 'B/B.csproj') -Actual $result.SelectedProjectPaths -Message 'Changing B should select B and A, not dependency C.'
	Assert-SequenceEqual -Expected @('A', 'B') -Actual $result.PackageIds -Message 'Changing B should publish A and B.'

	$result = Invoke-Detection -Root $tempRoot -ChangedFiles 'A/Class.cs'
	Assert-SequenceEqual -Expected @('A/A.csproj') -Actual $result.SelectedProjectPaths -Message 'Changing A should select only A.'

	$result = Invoke-Detection -Root $tempRoot -ChangedFiles 'README.md'
	Assert-Equal -Expected $false -Actual $result.HasChanges -Message 'Repository-level files should not select packages.'

	$result = Invoke-Detection -Root $tempRoot -ChangedFiles @() -BuildAllProjects
	Assert-SequenceEqual -Expected @('A/A.csproj', 'B/B.csproj', 'C/C.csproj') -Actual $result.SelectedProjectPaths -Message 'Build-all should select every project.'

	Remove-Item -LiteralPath (Join-Path $tempRoot 'A') -Recurse -Force
	Remove-Item -LiteralPath (Join-Path $tempRoot 'B') -Recurse -Force
	Remove-Item -LiteralPath (Join-Path $tempRoot 'C') -Recurse -Force
	New-TestProject -Root $tempRoot -Name C
	New-TestProject -Root $tempRoot -Name B -References C -IsPackable $false
	New-TestProject -Root $tempRoot -Name A -References B

	$result = Invoke-Detection -Root $tempRoot -ChangedFiles 'C/Class.cs'
	Assert-SequenceEqual -Expected @('A/A.csproj', 'B/B.csproj', 'C/C.csproj') -Actual $result.SelectedProjectPaths -Message 'Traversal should pass through a non-packable intermediary.'
	Assert-SequenceEqual -Expected @('A', 'C') -Actual $result.PackageIds -Message 'Non-packable projects should not be published.'

	& $versionGenerator -RepositoryRoot $tempRoot -BaseVersion '4.0' -RunNumber '15' -SelectedProjectPathsJson ($result.SelectedProjectPaths | ConvertTo-Json -Compress -AsArray)
	$propsPath = Join-Path $tempRoot 'Directory.Build.props'
	[xml] $props = Get-Content -LiteralPath $propsPath
	Assert-Equal -Expected 'BUILD_BUILDNUMBER' -Actual $props.Project.TreatAsLocalProperty -Message 'Version property must be project-local.'
	Assert-Equal -Expected '4.0.0' -Actual $props.Project.PropertyGroup[0].BUILD_BUILDNUMBER -Message 'Unselected projects should use the baseline version.'
	$conditionalGroups = @($props.Project.PropertyGroup | Where-Object { $_.HasAttribute('Condition') })
	Assert-Equal -Expected 3 -Actual $conditionalGroups.Count -Message 'Each selected project should have a version override.'
	Assert-SequenceEqual -Expected @('4.0.15', '4.0.15', '4.0.15') -Actual @($conditionalGroups | ForEach-Object BUILD_BUILDNUMBER) -Message 'Selected projects should use the run version.'

	$overwriteFailed = $false
	try {
		& $versionGenerator -RepositoryRoot $tempRoot -BaseVersion '4.0' -RunNumber '16' -SelectedProjectPathsJson '[]'
	}
	catch {
		$overwriteFailed = $true
	}
	Assert-Equal -Expected $true -Actual $overwriteFailed -Message 'Version generation should not overwrite an existing props file.'

	& $cleanup -RepositoryRoot $tempRoot
	Assert-Equal -Expected $false -Actual (Test-Path -LiteralPath $propsPath) -Message 'Cleanup should remove generated props.'

	Write-Host "All $testCount workflow script assertions passed."
}
finally {
	Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
