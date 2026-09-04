[CmdletBinding()]
param([string] $RepositoryRoot = (Get-Location).Path)

$ErrorActionPreference = 'Stop'

$propsPath = Join-Path ([System.IO.Path]::GetFullPath($RepositoryRoot)) 'Directory.Build.props'
if (Test-Path -LiteralPath $propsPath) {
	Remove-Item -LiteralPath $propsPath -Force
	Write-Host "Removed $propsPath"
}
