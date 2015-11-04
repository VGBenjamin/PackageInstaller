param(
	[string]$source = "C:\Projects\Sitecore\PackageInstaller\Sidewalk.SC.PackageInstaller.Client\bin\Debug\",
	[string]$destination = "C:\Projects\Sitecore\PackageInstaller\Sidewalk.PackageInstaller.zip"
)

If(Test-Path "$destination") {
    Remove-Item "$destination"
}    

Add-Type -assembly "System.IO.Compression.FileSystem"
[System.IO.Compression.ZipFile]::CreateFromDirectory("$source", "$destination")