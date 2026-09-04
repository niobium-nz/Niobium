[CmdletBinding()]
param(
	[string] $RepositoryRoot = (Get-Location).Path,
	[Parameter(Mandatory)] [string] $BaseVersion,
	[Parameter(Mandatory)] [string] $RunNumber,
	[string] $SelectedProjectPathsJson = '[]'
)

$ErrorActionPreference = 'Stop'

$resolvedRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$propsPath = Join-Path $resolvedRoot 'Directory.Build.props'
if (Test-Path -LiteralPath $propsPath) {
	throw "Refusing to overwrite existing file: $propsPath"
}

$selectedProjectPaths = @($SelectedProjectPathsJson | ConvertFrom-Json)
$selectedFullPaths = @($selectedProjectPaths | ForEach-Object {
	[System.IO.Path]::GetFullPath((Join-Path $resolvedRoot ([string] $_)))
} | Sort-Object -Unique)

$document = [System.Xml.XmlDocument]::new()
$project = $document.CreateElement('Project')
$null = $document.AppendChild($project)
$project.SetAttribute('TreatAsLocalProperty', 'BUILD_BUILDNUMBER')

$defaults = $document.CreateElement('PropertyGroup')
$defaultVersion = $document.CreateElement('BUILD_BUILDNUMBER')
$defaultVersion.InnerText = "$BaseVersion.0"
$null = $defaults.AppendChild($defaultVersion)
$null = $project.AppendChild($defaults)

foreach ($selectedPath in $selectedFullPaths) {
	$propertyGroup = $document.CreateElement('PropertyGroup')
	$escapedPath = $selectedPath.Replace("'", "''")
	$propertyGroup.SetAttribute('Condition', "`$([System.String]::Copy('`$(MSBuildProjectFullPath)').Equals('$escapedPath', System.StringComparison.OrdinalIgnoreCase))")
	$selectedVersion = $document.CreateElement('BUILD_BUILDNUMBER')
	$selectedVersion.InnerText = "$BaseVersion.$RunNumber"
	$null = $propertyGroup.AppendChild($selectedVersion)
	$null = $project.AppendChild($propertyGroup)
}

$settings = [System.Xml.XmlWriterSettings]::new()
$settings.Indent = $true
$settings.OmitXmlDeclaration = $true
$settings.Encoding = [System.Text.UTF8Encoding]::new($false)
$writer = [System.Xml.XmlWriter]::Create($propsPath, $settings)
try {
	$document.Save($writer)
}
finally {
	$writer.Dispose()
}

Write-Host "Generated $propsPath with $($selectedFullPaths.Count) project-specific version override(s)."
