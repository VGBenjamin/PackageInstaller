Example of commands:
====================

Install a sitecore package:
---------------------------
  Sidewalk.SC.PackageInstaller.Client.exe -sitecoreUrl "http://sitecore650" -sitecoreDeployFolder "C:\inetpub\wwwroot\sitecore650\Website" -packagePath "C:\temp\TestPkg.zip" -connector "sitecore"

Install a TDS package:
----------------------
  Sidewalk.SC.PackageInstaller.Client.exe -sitecoreUrl "http://sc72rev140228" -sitecoreDeployFolder "C:\inetpub\wwwroot\sc72rev140228\Website" -packagePath "\Examples\TestPackage.TDS.update" -connector "tds"

Publish every templates:
------------------------
  Sidewalk.SC.PackageInstaller.Client.exe -sitecoreUrl "http://sitecore650" -sitecoreDeployFolder "C:\inetpub\wwwroot\sitecore650\Website" -publish -publishRootItem "/sitecore/templates" -publishChildrenItems -publishMode "Smart"