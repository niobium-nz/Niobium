[CmdletBinding()]
param(
	[string] $RepositoryRoot = (Get-Location).Path,
	[Parameter(Mandatory)] [string] $PackageIds,
	[Parameter(Mandatory)] [string] $PackageVersion,
	[string] $NuGetFeedUrl,
	[string] $NuGetApiKey,
	[switch] $DiscoverOnly
)

$ErrorActionPreference = 'Stop'

$published = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
foreach ($packageId in @($PackageIds -split ',' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
	$expectedNames = @("$packageId.$PackageVersion.nupkg", "$packageId.$PackageVersion.snupkg")
	$packages = @(Get-ChildItem -LiteralPath $RepositoryRoot -Recurse -File | Where-Object {
		$_.DirectoryName -match "[\\/]bin[\\/]Release(?:[\\/]|$)" -and $expectedNames -contains $_.Name
	})

	$mainPackage = @($packages | Where-Object { $_.Extension -eq '.nupkg' })
	if ($mainPackage.Count -eq 0) {
		throw "No package artifact found for $packageId version $PackageVersion."
	}

	if ($DiscoverOnly) {
		$packages.FullName
		continue
	}

	if ([string]::IsNullOrWhiteSpace($NuGetFeedUrl) -or [string]::IsNullOrWhiteSpace($NuGetApiKey)) {
		throw 'NuGetFeedUrl and NuGetApiKey are required when publishing packages.'
	}

	foreach ($package in $packages) {
		if ($published.Add($package.FullName)) {
			Write-Host "Publishing $($package.Name)"
			& dotnet nuget push $package.FullName --source $NuGetFeedUrl --api-key $NuGetApiKey
			if ($LASTEXITCODE -ne 0) {
				throw "Failed to publish $($package.Name)."
			}
		}
	}
}

Write-Host "Published $($published.Count) package artifact(s)."
