<#
    .SYNOPSIS
        Install all the sitecore modules who are not installed yet. And save the list of those modules to avoid to reinstall it next time.
 
    .PARAMETER  modulesPath
        The folder who contains the modules to install
 
    .PARAMETER  allreadyInstalledModulesPath
        The folder who contains the file .txt.user with the allready installed modules

    .PARAMETER  packageInstallerExe
        The path to the file Sidewalk.SC.PackageInstaller.Client.exe

    .PARAMETER  packageInstallerSln
        The path to the file \Sidewalk.SC.PackageInstaller.sln used to build the solution only if the Sidewalk.SC.PackageInstaller.Client.exe is not found

    .PARAMETER  msBuildPath
        The path to the file msbuild.exe used to build the solution only if the Sidewalk.SC.PackageInstaller.Client.exe is not found

    .PARAMETER  sitecoreUrl
        The sitecore url where the modules needs to be installed

    .PARAMETER  sitecoreDeployFolder
        The sitecore Website folder where the modules need to be installed
 
    .EXAMPLE
        InstallMissingModules.ps1 -sitecoreUrl "http://mysitecore.sandbox.local" -sitecoreDeployFolder "C:\inetpub\wwwroot\mysitecore.sandbox\Website" -Verbose
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)][string]$modulesPath = "..\..\..\Modules\",
    [Parameter(Mandatory=$false)][string]$allreadyInstalledModulesPath = "..\..\..\Modules\allreadyInstalledModules.txt.user",
    [Parameter(Mandatory=$true)][string]$sitecoreUrl,
    [Parameter(Mandatory=$true)][string]$sitecoreDeployFolder,
    [switch]$whatIf
)

$fileSystemModulePath = "$currentDir\FileSystem.psm1"

function Get-ScriptDirectory {
    Split-Path -parent $PSCommandPath
}

$currentDir = Get-ScriptDirectory

try {
    $modulesPathFullPath = Resolve-Path $modulesPath
	Write-Verbose -Message "Module directory: $($modulesPathFullPath.Path)"
} catch {
    throw new FileNotFoundException("The modules path is not found. $($_.Exception.Message)")
}
$packageInstallerExe = "$currentDir\PackageInstaller\SC.PackageInstaller.Client.exe"

if(-not (Test-Path $packageInstallerExe)){
    Write-Verbose -Message "The package installer exe is not found."    

    #Download the zip file from github
    $modulePath = Resolve-Path $fileSystemModulePath

    Import-Module $modulePath.Path
    $zipPath = "$currentDir\PackageInstaller.zip"    
    Get-RemoteFile -url "https://github.com/VGBenjamin/PackageInstaller/raw/master/Sidewalk.PackageInstaller.zip" -targetFile $zipPath -Verbose:$PSBoundParameters['Verbose']

    Write-Verbose -Message "Extracting the zipfile"
    try
	{
		Expand-Zip -zipPath $zipPath -destination "$currentDir\PackageInstaller\" -Verbose:$PSBoundParameters['Verbose'] -createDestinationFolderIfNeeded
	} catch [Exception]
	{
		echo $_.Exception|format-list -force
	}    
}

$allreadyInstalledModules = New-Object 'System.Collections.Generic.HashSet[String]'
$allreadyInstalledModulesPathFillPath = "$currentDir$allreadyInstalledModulesPath"

if(Test-Path $allreadyInstalledModulesPathFillPath) {  
    Write-Verbose -Message "Loading the list of allready installed modules: $allreadyInstalledModulesPathFillPath"  
    Get-Content $allreadyInstalledModulesPathFillPath | ForEach-Object { $allreadyInstalledModules.Add($_ ) | Out-Null } 

    Write-Verbose -Message "Modules allready installed: $allreadyInstalledModules"
}

foreach($module in Get-ChildItem -Path $modulesPathFullPath.Path -Include @("*.zip","*.update") -Recurse) {
    
    if(-not ($allreadyInstalledModules.Contains($module.Name))) {    
        Write-Verbose -Message "Installing the module: $($module.FullName)"
        
        if($whatIf) {
            Write-Host "WhatIf : Installing the module with the parameters: $packageInstallerExe -sitecoreUrl $sitecoreUrl -sitecoreDeployFolder $sitecoreDeployFolder -packagePath $($module.FullName) -connector tds"
        } else {
            try {
                & "$packageInstallerExe" -sitecoreUrl $sitecoreUrl -sitecoreDeployFolder $sitecoreDeployFolder -packagePath "$($module.FullName)" -connector "tds" | Out-String           
                $allreadyInstalledModules.Add($module.Name) | Out-Null
            } catch [Exception]
	        {
		        echo $_.Exception|format-list -force
	        }
        }

        
    }
}

Set-Content -Path  $allreadyInstalledModulesPathFillPath -Value ($allreadyInstalledModules | Out-String) -WhatIf:$whatIf