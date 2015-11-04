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
        InstallMissingModules.ps1 -sitecoreUrl "http://sidewalk.sandbox.local" -sitecoreDeployFolder "C:\inetpub\wwwroot\sidewalk.sandbox\Website" -Verbose
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)][string]$modulesPath = "..\..\..\..\Modules\",
    [Parameter(Mandatory=$false)][string]$allreadyInstalledModulesPath = "..\..\..\..\Modules\allreadyInstalledModules.txt.user",
    [Parameter(Mandatory=$false)][string]$packageInstallerExe = ".\Sidewalk.SC.PackageInstaller.Client\bin\Release\Sidewalk.SC.PackageInstaller.Client.exe",
    [Parameter(Mandatory=$false)][string]$packageInstallerSln = ".\Sidewalk.SC.PackageInstaller.sln",
    [Parameter(Mandatory=$false)][string]$msBuildPath = "C:\Program Files (x86)\MSBuild\14.0\Bin\amd64\msbuild.exe",
    [Parameter(Mandatory=$true)][string]$sitecoreUrl,
    [Parameter(Mandatory=$true)][string]$sitecoreDeployFolder,
    [switch]$whatIf
)

function Get-ScriptDirectory {
    Split-Path -parent $PSCommandPath
}

function BuildSolution {
    try {
        $packageInstallerSlnFullPath = Resolve-Path "$currentDir$packageInstallerSln"
    } catch {
        throw new FileNotFoundException("Cannot find the package installer solution. $($_.Exception.Message)")
    }

    try {
        $msBuildFullPath = Resolve-Path $msBuildPath
    } catch {
        throw new FileNotFoundException("Cannot find the msbuild. $($_.Exception.Message)")
    }

    Write-Verbose -Message "Building the solution. The logs are available here: $packageInstallerSlnFullPath.log"
    & "$msBuildFullPath" "$packageInstallerSlnFullPath" /nologo /m /nr:false /fl /flp:"logfile=$packageInstallerSlnFullPath.log" /p:platform="any cpu" /p:configuration="release" /p:VisualStudioVersion="14.0"
}

$currentDir = Get-ScriptDirectory

try {
    $modulesPathFullPath = Resolve-Path "$currentDir$modulesPath"
} catch {
    throw new FileNotFoundException("The modules path is not found. $($_.Exception.Message)")
}

if(-not (Test-Path "$currentDir$packageInstallerExe")){
    Write-Verbose -Message "The package installer exe is not found."
    BuildSolution
}

try {
    $packageInstallerExeFullPath = Resolve-Path "$currentDir$packageInstallerExe"
} catch {
    throw new FileNotFoundException("Event after the build the package installer exe is still not found the compilation have probably failed. $($_.Exception.Message)")
}

$allreadyInstalledModules = New-Object 'System.Collections.Generic.HashSet[String]'
$allreadyInstalledModulesPathFillPath = "$currentDir$allreadyInstalledModulesPath"

if(Test-Path $allreadyInstalledModulesPathFillPath) {  
    Write-Verbose -Message "Loading the list of allready installed modules: $allreadyInstalledModulesPathFillPath"  
    Get-Content $allreadyInstalledModulesPathFillPath | ForEach-Object { $allreadyInstalledModules.Add($_ ) }
}

foreach($module in Get-ChildItem -Path $modulesPathFullPath.Path -Include @("*.zip","*.update") -Recurse) {
    
    if(-not ($allreadyInstalledModules.Contains($module.Name))) {    
        Write-Verbose -Message "Installing the module: $($module.FullName)"
        
        if($whatIf) {
            Write-Host "WhatIf : Installing the module with the parameters: $packageInstallerExeFullPath -sitecoreUrl $sitecoreUrl -sitecoreDeployFolder $sitecoreDeployFolder -packagePath $($module.FullName) -connector tds"
        } else {
            & "$packageInstallerExeFullPath" -sitecoreUrl $sitecoreUrl -sitecoreDeployFolder $sitecoreDeployFolder -packagePath "$($module.FullName)" -connector "tds"            
        }

        $allreadyInstalledModules.Add($module.Name) | Out-Null
    }
}

Set-Content -Path  $allreadyInstalledModulesPathFillPath -Value ($allreadyInstalledModules | Out-String) -WhatIf:$whatIf