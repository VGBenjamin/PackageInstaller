<#
.Synopsis
	Test if a folder exist and create it if required
.Parameter folder
	The folder to ensure
#>
Function Assert-Folder {	
	[CmdletBinding()]
	param
	(
		[Parameter(Mandatory=$true)][string]$folder
	)

	if(!(Test-Path $folder)) {
		Write-Verbose "Create the folder: '$folder'"
		New-Item $folder -Type Directory -force
	}
}

<#
.Synopsis
	Download a remote file to a local folder
.Parameter url
	The url where the originla file is stored
.Parameter targetFile
	The target file path including the filename
#>
Function Get-RemoteFile {
	[CmdletBinding()]
	param
	(
		[Parameter(Mandatory=$true)][string]$url,
		[Parameter(Mandatory=$true)][string]$targetFile
	)

	if (Test-Path $targetFile)	{
		Write-Verbose "The file $targetFile is allready present. Skipping."
	}
	else	{
		$webclient = New-Object System.Net.WebClient
		Write-Verbose "Downloading '$url' to $targetFile"
		try
		{
			$webclient.DownloadFile($url, $targetFile)
		} catch [Exception]
		{
			echo $_.Exception|format-list -force
		}
		Write-Verbose "Download complete!"	
	}
}

<#
.Synopsis
	Unzip a file
.Parameter zipPath
	The zipped file
.Parameter destination
	The where the uncompressed files will be downloaded
.Parameter createDestinationFolderIfNeeded
	The filename to download
#>
Function Expand-Zip {
	[CmdletBinding()]
	param
	(
		[Parameter(Mandatory=$true)][string]$zipPath,
		[Parameter(Mandatory=$true)][string]$destination,
		[switch]$createDestinationFolderIfNeeded
	)
	
	if($createDestinationFolderIfNeeded)	{
		Assert-Folder -folder $destination
	}
	
	#TODO : test if path is empty
	Write-Verbose "Unzipping '$zipPath' to $destination ..."
	$shell_app=new-object -com shell.application 
	$zip_file = $shell_app.namespace($zipPath) 
	$destinationNs = $shell_app.namespace($destination)
	$destinationNs.Copyhere($zip_file.items())
	Write-Verbose "Unzipping done!"
}

 <#
	.SYNOPSIS
		Compares a reference directory with one or more difference directories.

	.DESCRIPTION
		Compare-Directory compares a reference directory with one ore more difference
		directories. Files and directories are compared both on filename and contents
		using a MD5hash.

		Internally, Compare-Object is used to compare the directories. The behavior
		and results of Compare-Directory is similar to Compare-Object.

	.PARAMETER  ReferenceDirectory
		The reference directory to compare one or more difference directories to.

	.PARAMETER  DifferenceDirectory
		One or more directories to compare to the reference directory.

	.PARAMETER Recurse
		Include subdirectories in the comparison.

	.PARAMETER ExcludeFile
		File names to exclude from the comparison.

	.PARAMETER ExcludeDirectory
		Directory names to exclude from the comparison. Directory names are
		relative to the Reference of Difference Directory path

	.PARAMETER ExcludeDifferent
		Displays only the characteristics of compared files that are equal.

	.PARAMETER IncludeEqual
		Displays characteristics of files that are equal. By default, only
		characteristics that differ between the reference and difference files
		are displayed.

	.PARAMETER PassThru
		Passes the objects that differed to the pipeline. By default, this
		cmdlet does not generate any output.

	.EXAMPLE
		Compare-Directory -reference "D:\TEMP\CompareTest\path1" -difference "D:\TEMP\CompareTest\path2" -ExcludeFile "web.config" -recurse

		Compares directories "D:\TEMP\CompareTest\path1" and "D:\TEMP\CompareTest\path2" recursively, excluding "web.config"
		Only differences are shown. Results:

		RelativeBaseName  MD5Hash                          SideIndicator Item
		----------------  -------                          ------------- ----
		bin\site.dll      87A1E6006C2655252042F16CBD7FB41B =>            D:\TEMP\CompareTest\path2\bin\site.dll
		index.html        02BB8A33E1094E547CA41B9E171A267B =>            D:\TEMP\CompareTest\path2\index.html
		index.html        20EE266D1B23BCA649FEC8385E5DA09D <=            D:\TEMP\CompareTest\path1\index.html
		web_2.config      5E6B13B107ED7A921AEBF17F4F8FE7AF <=            D:\TEMP\CompareTest\path1\web_2.config
		bin\site.dll      87A1E6006C2655252042F16CBD7FB41B =>            D:\TEMP\CompareTest\path2\bin\site.dll
		index.html        02BB8A33E1094E547CA41B9E171A267B =>            D:\TEMP\CompareTest\path2\index.html
		index.html        20EE266D1B23BCA649FEC8385E5DA09D <=            D:\TEMP\CompareTest\path1\index.html
		web_2.config      5E6B13B107ED7A921AEBF17F4F8FE7AF <=            D:\TEMP\CompareTest\path1\web_2.config

	.EXAMPLE
		Compare-Directory -reference "D:\TEMP\CompareTest\path1" -difference "D:\TEMP\CompareTest\path2" -ExcludeFile "web.config" -recurse -IncludeEqual

		Compares directories "D:\TEMP\CompareTest\path1" and "D:\TEMP\CompareTest\path2" recursively, excluding "web.config".
		Results include the items that are equal:

		RelativeBaseName    MD5Hash                          SideIndicator Item
		----------------    -------                          ------------- ----
		bin                                                  ==            D:\TEMP\CompareTest\path1\bin
		bin\site2.dll       98B68D681A8D40FA943D90588E94D1A9 ==            D:\TEMP\CompareTest\path1\bin\site2.dll
		bin\site3.dll       9408C4B29F82260CBBA528342CBAA80F ==            D:\TEMP\CompareTest\path1\bin\site3.dll
		bin\site4.dll       0616E1FBE12D468F611F07768D70C2EE ==            D:\TEMP\CompareTest\path1\bin\site4.dll
		...
		bin\site8.dll       87A1E6006C2655252042F16CBD7FB41B =>            D:\TEMP\CompareTest\path2\bin\site8.dll
		index.html          02BB8A33E1094E547CA41B9E171A267B =>            D:\TEMP\CompareTest\path2\index.html
		index.html          20EE266D1B23BCA649FEC8385E5DA09D <=            D:\TEMP\CompareTest\path1\index.html
		web_2.config        5E6B13B107ED7A921AEBF17F4F8FE7AF <=            D:\TEMP\CompareTest\path1\web_2.config

	.EXAMPLE
		Compare-Directory -reference "D:\TEMP\CompareTest\path1" -difference "D:\TEMP\CompareTest\path2" -ExcludeFile "web.config" -recurse -ExcludeDifference

		Compares directories "D:\TEMP\CompareTest\path1" and "D:\TEMP\CompareTest\path2" recursively, excluding "web.config".
		Results only include the files that are equal; different files are excluded from the results.

	.EXAMPLE
		Compare-Directory -reference "D:\TEMP\CompareTest\path1" -difference "D:\TEMP\CompareTest\path2" -ExcludeFile "web.config" -recurse -Passthru

		Compares directories "D:\TEMP\CompareTest\path1" and "D:\TEMP\CompareTest\path2" recursively, excluding "web.config" and returns NO comparison
		results, but the different files themselves!

		FullName
		--------
		D:\TEMP\CompareTest\path2\bin\site3.dll
		D:\TEMP\CompareTest\path2\index.html
		D:\TEMP\CompareTest\path1\index.html
		D:\TEMP\CompareTest\path1\web_2.config

	.LINK
		Compare-Object
#>
function global:Compare-Directory
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$true, position=0, ValueFromPipelineByPropertyName=$true, HelpMessage="The reference directory to compare one or more difference directories to.")]
        [System.IO.DirectoryInfo]$ReferenceDirectory,
 
        [Parameter(Mandatory=$true, position=1, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true, HelpMessage="One or more directories to compare to the reference directory.")]
        [System.IO.DirectoryInfo[]]$DifferenceDirectory,
 
        [Parameter(Mandatory=$false, ValueFromPipelineByPropertyName=$true, HelpMessage="Recurse the directories")]
        [switch]$Recurse,
 
        [Parameter(Mandatory=$false, ValueFromPipelineByPropertyName=$true, HelpMessage="Files to exclude from the comparison")]
        [String[]]$ExcludeFile,
 
        [Parameter(Mandatory=$false, ValueFromPipelineByPropertyName=$true, HelpMessage="Directories to exclude from the comparison")]
        [String[]]$ExcludeDirectory,
 
        [Parameter(Mandatory=$false, ValueFromPipelineByPropertyName=$true, HelpMessage="Displays only the characteristics of compared objects that are equal.")]
        [switch]$ExcludeDifferent,
 
        [Parameter(Mandatory=$false, ValueFromPipelineByPropertyName=$true, HelpMessage="Displays characteristics of files that are equal. By default, only characteristics that differ between the reference and difference files are displayed.")]
        [switch]$IncludeEqual,
 
        [Parameter(Mandatory=$false, ValueFromPipelineByPropertyName=$true, HelpMessage="Passes the objects that differed to the pipeline.")]
        [switch]$PassThru
    )
 
    begin
    {
        function Get-MD5
        {
            [CmdletBinding(SupportsShouldProcess=$false)]
            param
            (
                [Parameter(Mandatory=$true, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true, HelpMessage="file(s) to create hash for")]
                [Alias("File", "Path", "PSPath", "String")]
                [ValidateNotNull()]
                $InputObject
            )
 
            begin
            {
                $cryptoServiceProvider    = [System.Security.Cryptography.MD5CryptoServiceProvider]
                $hashAlgorithm             = new-object $cryptoServiceProvider
            }
 
            process
            {
                $hashByteArray = ""
 
                #$item = Get-Item $InputObject -ErrorAction SilentlyContinue
                #if ($item -is [System.IO.DirectoryInfo])    { throw "Cannot create hash for directory" }
                #if ($item)                                     { $InputObject = $item }
 
                if ($InputObject -is [System.IO.FileInfo])
                {
                    $stream         = $null;
                    $hashByteArray    = $null
 
                    try
                    {
                        $stream                 = $InputObject.OpenRead();
                        $hashByteArray             = $hashAlgorithm.ComputeHash($stream);
                    }
                    finally
                    {
                        if ($stream -ne $null)
                        {
                            $stream.Close();
                        }
                    }
                }
                else
                {
                    $utf8             = new-object -TypeName "System.Text.UTF8Encoding"
                    $hashByteArray     = $hashAlgorithm.ComputeHash($utf8.GetBytes($InputObject.ToString()));
                }
 
                Write-Output ([BitConverter]::ToString($hashByteArray)).Replace("-","")
            }
        }
 
        function Get-Files
        {
            [CmdletBinding(SupportsShouldProcess=$false)]
            param
            (
                [string]$DirectoryPath,
                [String[]]$ExcludeFile,
                [String[]]$ExcludeDirectory,
                [switch]$Recurse
            )
 
            $relativeBasenameIndex = $DirectoryPath.ToString().Length
 
            # Get the files from the first deploypath
            # and ADD the MD5 hash for the file as a property
            # and ADD a filepath relative to the deploypath as a property
            Get-ChildItem -Path $DirectoryPath -Exclude $ExcludeFile -Recurse:$Recurse | foreach {
                $hash = ""
                if (!$_.PSIsContainer) { $hash = Get-MD5 $_    }
 
                # Added two new properties to the DirectoryInfo/FileInfo objects
                $item = $_ |
                    Add-Member -Name "MD5Hash" -MemberType NoteProperty -Value $hash -PassThru |
                    Add-Member -Name "RelativeBaseName" -MemberType NoteProperty -Value ($_.FullName.Substring($relativeBasenameIndex)) -PassThru
 
                # Test for directories and files that need to be excluded because of ExcludeDirectory
                if ($item.PSIsContainer) { $item.RelativeBaseName += "\" }
                if ($ExcludeDirectory | where { $item.RelativeBaseName -like "\$_\*" })
                {
                    Write-Verbose "Ignore item `"$($item.Fullname)`""
                }
                else
                {
                    Write-Verbose "Adding `"$($item.Fullname)`" to result set"
                    Write-Output $item
                }
            }
        }
 
        $referenceDirectoryFiles = Get-Files -DirectoryPath $referenceDirectory -ExcludeFile $ExcludeFile -ExcludeDirectory $ExcludeDirectory -Recurse:$Recurse
    }
 
    process
    {
        if ($DifferenceDirectory -and $referenceDirectoryFiles)
        {
            foreach($nextPath in $DifferenceDirectory)
            {
                $nextDifferenceFiles = Get-Files -DirectoryPath $nextpath -ExcludeFile $ExcludeFile -ExcludeDirectory $ExcludeDirectory -Recurse:$Recurse
 
                ###################################################
                # Compare the contents of the two file/directory arrays and return the results
                $results = @(Compare-Object -ReferenceObject $referenceDirectoryFiles -DifferenceObject $nextDifferenceFiles -ExcludeDifferent:$ExcludeDifferent -IncludeEqual:$IncludeEqual -PassThru:$PassThru -Property RelativeBaseName, MD5Hash)
 
                if (!$PassThru)
                {
                    foreach ($result in $results)
                    {
                        $path         = $ReferenceDirectory                        
                        $pathFiles    = $referenceDirectoryFiles
                        if ($result.SideIndicator -eq "=>")
                        {
                            $path         = $nextPath
                            $pathFiles    = $nextDifferenceFiles
                        }
 
                        # Find the original item in the files array
                        $itemPath = (Join-Path $path $result.RelativeBaseName).ToString().TrimEnd('\')
                        $item = $pathFiles | where { $_.fullName -eq $itemPath }                        
 
                        $result | Add-Member -Name "Item" -MemberType NoteProperty -Value $item
                    }
                }
 
                Write-Output $results
            }
        }
    }
}