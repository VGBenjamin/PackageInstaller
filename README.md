### The project
This project allow you install the Sitecore packages files in command line. It support both the .zip and .update files.

### Usage
## Install
You need to download and extract the [PackageInstaller zip file](https://github.com/VGBenjamin/PackageInstaller/raw/master/Sidewalk.PackageInstaller.zip) or build the source.

## Arguments
-p, --packagePath=PACKAGE PATH
						 The PACKAGE PATH is the path to the package. The
						   package must be located in a folder reachable by
						   the web server.

-u, --sitecoreUrl=SITECORE URL
						 The SITECORE URL is the url to the root of the
						   Sitecore server.

-f, --sitecoreDeployFolder=SITECORE DEPLOY FOLDER
						 The SITECORE DEPLOY FOLDER is the UNC path to
						   the Sitecore web root.

-c, --connector=INSTALLATON MODE
						 The INSTALLATON MODE could be tds or sitecore.

--pb, --publish        Publish some items.

--pbc, --publishChildrenItems
					 Publish the children items also. Need to be use
					   with the -publish option. If you don't specify
					   this flag you need to specify the paramter -
					   publishRootItem

--pbsdb, --publishSourceDb=VALUE
					 The source database to publish from (master if
					   ommited). Need to be use with the -publish
					   option.

--pbtdb, --publishTargetDb=VALUE
					 The target database to publish to (web if
					   ommited). Need to be use with the -publish
					   option.

--pbl, --publishLanguage=VALUE
					 The language to publish (all if ommited). Need
					   to be use with the -publish option.

--pbi, --publishRootItem=VALUE
					 The root item to publish (all if ommited). Need
					   to be use with the -publish option.

--pbm, --publishMode=VALUE
					 The publish mode must be one of those values:
					   Full, Incremental, SingleItem, Smart (Full if
					   ommited). Need to be use with the -publish
					   option.

--pbt, --publishTargets=VALUE
					 The publish target separated by a coma if
					   multiple targets. Need to be use with the -
					   publish option.

-h, --help                 Show this message and exit.

--rc, --removeconnector
					 Remove the conenctor after the installation. it
					   will remodify the bin folder so you should
					   consider to let the conenctor for a better
					   performance.
--ssl                  Accept the self registered ssl certificate

## Usage Examples
# Install a sitecore package
    Sidewalk.SC.PackageInstaller.Client.exe -sitecoreUrl "http://sc72rev140228" -sitecoreDeployFolder "C:\inetpub\wwwroot\sc72rev140228\Website" -packagePath "C:\temp\TestPkg.zip" -connector "sitecore"

# Install a TDS package
    Sidewalk.SC.PackageInstaller.Client.exe -sitecoreUrl "http://sc72rev140228" -sitecoreDeployFolder "C:\inetpub\wwwroot\sc72rev140228\Website" -packagePath "\Examples\TestPackage.TDS.update" -connector "tds"

### Sources
The dlls of Sitecore are not commited with the sources because of the license. If you need to build you should first copy the dll's of Sitecore into the Libraries folder.
