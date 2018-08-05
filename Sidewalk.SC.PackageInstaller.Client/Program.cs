using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using log4net;
using NDesk.Options;
using Sidewalk.SC.PackageInstaller.Common;

//using System.Web.Services.Protocols;

namespace Sidewalk.SC.PackageInstaller.Client
{
    /// <summary>
    /// Installer command line utility. Uses NDesk.Options to parse the command line. For more information, please see
    /// http://www.ndesk.org/Options. 
    /// </summary>
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        static int verbosity;

        protected static string connectorFolder = ConfigurationManager.AppSettings["SitecoreConnectorFolder"];

        static readonly FileToDeploy[] FilesToDeploy = {
                new FileToDeploy { SourcePath = @"\Sidewalk.SC.PackageInstaller.Service.dll", TargetPath = @"bin" },
                new FileToDeploy { SourcePath = @"\Sidewalk.SC.PackageInstaller.Common.dll", TargetPath = @"bin" },
                new FileToDeploy { SourcePath = @"\Includes\SitecorePackageInstaller.asmx", TargetPath = "[connectorFolder]" },
                new FileToDeploy { SourcePath = @"\Includes\SitecorePackageInstaller.ashx", TargetPath = "[connectorFolder]" },
                new FileToDeploy { SourcePath = @"\Includes\Web.config", TargetPath = "[connectorFolder]" }
            };

        static void Main(string[] args)
        {
            #region Declare options and installer variables
            try
            {

                // Installer variables

                string packagePath = null;
                string connectorMode = null;
                string sitecoreWebURL = null;
                string sitecoreDeployFolder = null;
                bool show_help = args.Length == 0;

                bool publish = false;
                string publishSourceDb = "master";
                string publishTargetDb = "web";
                string publishLanguage = null;
                string publishRootItem = null;
                string publishMode = null;
                string publishTargets = null;
                bool publishChildrenItems = false;
                bool removeConnector = false;
                bool acceptSsl = false;
                string mergeMode = null;

                // Options declaration
                OptionSet options = new OptionSet()
                {
                    {
                        "p|packagePath=",
                        "The {PACKAGE PATH} is the path to the package. The package must be located in a folder reachable by the web server.\n",
                        v => packagePath = v
                    },
                    {
                        "u|sitecoreUrl=", "The {SITECORE URL} is the url to the root of the Sitecore server.\n",
                        v => sitecoreWebURL = v
                    },
                    {
                        "f|sitecoreDeployFolder=",
                        "The {SITECORE DEPLOY FOLDER} is the UNC path to the Sitecore web root.\n",
                        v => sitecoreDeployFolder = v
                    },
                    {
                        "c|connector=", "The {INSTALLATON MODE} could be tds or sitecore.\n",
                        v => connectorMode = v
                    },

                    {
                        "pb|publish", "Publish some items.\n",
                        v => publish = v != null
                    },
                    {
                        "pbc|publishChildrenItems",
                        "Publish the children items also. Need to be use with the -publish option. If you don't specify this flag you need to specify the paramter -publishRootItem\n",
                        v => publishChildrenItems = v != null
                    },
                    {
                        "pbsdb|publishSourceDb=",
                        "The source database to publish from (master if ommited). Need to be use with the -publish option.\n",
                        v => publishSourceDb = v
                    },
                    {
                        "pbtdb|publishTargetDb=",
                        "The target database to publish to (web if ommited). Need to be use with the -publish option.\n",
                        v => publishTargetDb = v
                    },
                    {
                        "pbl|publishLanguage=",
                        "The language to publish (all if ommited). Need to be use with the -publish option.\n",
                        v => publishLanguage = v
                    },
                    {
                        "pbi|publishRootItem=",
                        "The root item to publish (all if ommited). Need to be use with the -publish option.\n",
                        v => publishRootItem = v
                    },
                    {
                        "pbm|publishMode=",
                        "The publish mode must be one of those values: Full, Incremental, SingleItem, Smart (Full if ommited). Need to be use with the -publish option.\n",
                        v => publishMode = v
                    },
                    {
                        "pbt|publishTargets=",
                        "The publish target separated by a coma if multiple targets. Need to be use with the -publish option.\n",
                        v => publishTargets = v
                    },

                    {
                        "h|help", "Show this message and exit.",
                        v => show_help = v != null
                    },

                    {
                        "rc|removeconnector", "Remove the conenctor after the installation. it will remodify the bin folder so you should consider to let the conenctor for a better performance.",
                        v => removeConnector = v != null
                    },
                    {
                        "ssl", "Accept the self registered ssl certificate",
                        v => acceptSsl = v != null
                    },
                    {
                        "mm|mergeMode=", "Define the merge mode to install the package",
                        v => mergeMode = v
                    }
                };

                #endregion


                // Parse options - exit on error
                List<string> extra;
                try
                {
                    extra = options.Parse(args);
                }
                catch (OptionException e)
                {
                    log.Error($"{e.Message}. Try `packageinstaller --help' for more information.", e);
                    Environment.Exit(100);
                }

                // Display help if one is requested or no parameters are provided
                if (show_help)
                {
                    ShowHelp(options);
                    return;
                }

                #region Validate and process parameters

                bool parameterMissing = false;

                if (connectorMode == null && !publish) // Required except if we are in publish
                {
                    log.Error(
                        "The parameter --connector is required if you are not in publish mode. Try `packageinstaller --help' for more information.");
                    parameterMissing = true;
                }

                if (connectorMode != null &&
                    !(connectorMode.Equals("tds", StringComparison.InvariantCultureIgnoreCase)
                      || connectorMode.Equals("sitecore", StringComparison.InvariantCultureIgnoreCase)))
                {
                    log.Error(
                        $"The parameter --connector must be'tds' or 'sitecore'. Current value is '{connectorMode}'. Try `packageinstaller --help' for more information.");
                    parameterMissing = true;
                }

                if (string.IsNullOrEmpty(packagePath) && !publish && !string.IsNullOrEmpty(connectorMode))
                    //Required except if we are in publish mode and
                {
                    log.Error(
                        $"Package Path is required if you use the -connector parameter. It could be 'tds' or 'sitecore'. Try `packageinstaller --help' for more information.");

                    parameterMissing = true;
                }

                if (string.IsNullOrEmpty(sitecoreWebURL))
                {
                    log.Error("Sitecore Web URL ie required.");

                    parameterMissing = true;
                }

                if (string.IsNullOrEmpty(sitecoreDeployFolder))
                {
                    log.Error("Sitecore Deploy folder is required. Try `packageinstaller --help' for more information.");

                    parameterMissing = true;
                }

                if (publish)
                {
                    if (string.IsNullOrEmpty(publishMode))
                    {
                        log.Error(
                            "The -publishMode parameter is required if you use the flag -publish. Try `packageinstaller --help' for more information.");

                        parameterMissing = true;
                    }
                    else if (!publishMode.Equals("Full", StringComparison.InvariantCultureIgnoreCase)
                             && !publishMode.Equals("Incremental", StringComparison.InvariantCultureIgnoreCase)
                             && !publishMode.Equals("SingleItem", StringComparison.InvariantCultureIgnoreCase)
                             && !publishMode.Equals("Smart", StringComparison.InvariantCultureIgnoreCase))
                    {
                        log.Error(
                            "The publishing mode is not one of the expected values. The value must be one of the following: Full, Incremental, SingleItem, Smart. Try `packageinstaller --help' for more information.");

                        parameterMissing = true;
                    }

                    if (!publishChildrenItems && string.IsNullOrEmpty(publishRootItem))
                    {
                        log.Error(
                            "The paramter -publishRootItem is required if you let the flag -publishChildrenItems to false. Try `packageinstaller --help' for more information.");

                        parameterMissing = true;
                    }
                }

                if (!parameterMissing)
                {
                    if (Directory.Exists(sitecoreDeployFolder))
                    {
                        try
                        {
                            log.Debug($"Initializing update package installation: {packagePath}");
                            if (sitecoreDeployFolder.LastIndexOf(@"\") != sitecoreDeployFolder.Length - 1)
                            {
                                sitecoreDeployFolder = sitecoreDeployFolder + @"\";
                            }

                            if (sitecoreWebURL.LastIndexOf(@"/") != sitecoreWebURL.Length - 1)
                            {
                                sitecoreWebURL = sitecoreWebURL + @"/";
                            }

                            // Install Sitecore connector
                            if (DeploySitecoreConnector(sitecoreDeployFolder))
                            {
                                if (connectorMode.Equals("tds", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    RequestTheHandler(sitecoreWebURL, packagePath, acceptSsl);
                                }
                                else
                                {
                                    using (
                                        ServiceReference.SitecorePackageInstaller service =
                                            new ServiceReference.SitecorePackageInstaller())
                                    {
                                        service.Url = string.Concat(sitecoreWebURL,
                                            ConfigurationManager.AppSettings["SitecoreConnectorFolder"],
                                            "/SitecorePackageInstaller.asmx");
                                        service.Timeout = int.MaxValue;

                                        if (connectorMode != null)
                                        {
                                            log.Debug("Initializing package installation...");
                                            if (connectorMode.Equals("tds", StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                service.InstallTdsPackage(packagePath);
                                            }
                                            else if (connectorMode.Equals("sitecore",
                                                StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                if (mergeMode != null)
                                                {
                                                    if (!mergeMode.ToLower().Equals("merge") && 
                                                        !mergeMode.ToLower().Equals("clear") && 
                                                        !mergeMode.ToLower().Equals("append") && 
                                                        !mergeMode.ToLower().Equals("skip") && 
                                                        !mergeMode.ToLower().Equals("overwrite"))
                                                    {
                                                        log.Error("Merge mode wrong. Accepted modes=> merge, clear, append, overwrite, skip. Default mode overwrite(if mergeMode not provided)");
                                                        Environment.Exit(106);
                                                    }
                                                    mergeMode = mergeMode.ToLower();
                                                }
                                                service.InstallPackage(packagePath, mergeMode);
                                            }
                                            log.Debug("Update package installed successfully.");
                                        }

                                        if (publish)
                                        {
                                            ServiceReference.PublishMode pMode = ServiceReference.PublishMode.Full;

                                            if (publishMode.Equals("Full", StringComparison.InvariantCultureIgnoreCase))
                                                pMode = ServiceReference.PublishMode.Full;
                                            else if (publishMode.Equals("Incremental",
                                                StringComparison.InvariantCultureIgnoreCase))
                                                pMode = ServiceReference.PublishMode.Incremental;
                                            else if (publishMode.Equals("SingleItem",
                                                StringComparison.InvariantCultureIgnoreCase))
                                                pMode = ServiceReference.PublishMode.SingleItem;
                                            else if (publishMode.Equals("Smart",
                                                StringComparison.InvariantCultureIgnoreCase))
                                                pMode = ServiceReference.PublishMode.Smart;

                                            string[] pTargets = null;
                                            if (!string.IsNullOrEmpty(publishTargets))
                                                pTargets = publishTargets.Split(new char[] {','});

                                            log.Debug("Publishing...");
                                            service.Publish(pMode, publishLanguage, pTargets, publishChildrenItems,
                                                publishSourceDb, publishTargetDb, publishRootItem);
                                            log.Debug("Publish successfull");

                                        }
                                    }
                                }
                                if (removeConnector)
                                    RemoveSitecoreConnector(sitecoreDeployFolder);

                            }
                            else
                            {
                                Console.WriteLine("Sitecore connector deployment failed.");
                                WaitIfNotDebug();
                                Environment.Exit(101);
                            }
                        }
                        catch (System.Net.WebException webex)
                        {
                            log.Error(webex.Message, webex);

                            using (WebResponse response = webex.Response)
                            {
                                HttpWebResponse httpResponse = (HttpWebResponse)response;
                                StringBuilder errorMsg = new StringBuilder();
                                errorMsg.AppendLine($"Error code: {httpResponse.StatusCode}");
                                errorMsg.AppendLine($"Server response: ");
  
                                using (Stream data = response.GetResponseStream())
                                using (var reader = new StreamReader(data))
                                {
                                    errorMsg.AppendLine(reader.ReadToEnd());                                   
                                }
                                
                            }

                            WaitIfNotDebug();
                            Environment.Exit(105);
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex.Message, ex);
                
                            WaitIfNotDebug();
                            Environment.Exit(102);
                        }
                    }
                    else
                    {
                        log.Error($"Sitecore Deploy Folder {sitecoreDeployFolder} not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Unexpected exception", ex);
                Environment.Exit(103);
            }

            WaitIfNotDebug();
            #endregion


        }

        private static void WaitIfNotDebug()
        {
#if DEBUG
            log.Info("## END ##");
            Console.ReadKey();
#endif
        }


        private static void RequestTheHandler(string sitecoreWebUrl, string packagePath, bool acceptSsl)
        {
            try
            {
                var handlerUrl = $"{sitecoreWebUrl}{ConfigurationManager.AppSettings["SitecoreConnectorFolder"]}/SitecorePackageInstaller.ashx?package={packagePath}&install=1&upgrade=0&history=";

                log.Info($"Calling the webservice url: {handlerUrl}");

                if(acceptSsl)
                    ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                // Creates an HttpWebRequest with the specified URL. 
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(handlerUrl);
                myHttpWebRequest.Timeout = int.MaxValue;
                myHttpWebRequest.ReadWriteTimeout = int.MaxValue;
                myHttpWebRequest.MaximumResponseHeadersLength = -1;


                // Sends the HttpWebRequest and waits for the response.			
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();

                // Gets the stream associated with the response.
                Stream receiveStream = myHttpWebResponse.GetResponseStream();

                var serializer = new XmlSerializer(typeof(MessageInfo));
                XmlTextReader reader = new XmlTextReader(receiveStream);
                reader.MoveToContent();

                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name == "MessageInfo")
                            {
                                var el = XNode.ReadFrom(reader) as XElement;
                                var message = (MessageInfo)serializer.Deserialize(el.CreateReader());

                                switch (message.Level)
                                {
                                    case "ERROR":
                                        log.Error(message);
                                        break;
                                    case "FATAL":
                                        log.Fatal(message);
                                        break;
                                    case "DEBUG":
                                        log.Debug(message);
                                        break;
                                    default:
                                        log.Info(message);
                                        break;
                                }
                            }
                            break;
                    }
                }
                // Releases the resources of the response.
                myHttpWebResponse.Close();
                // Releases the resources of the Stream.
                reader.Close();
            }
            catch (Exception ex)
            {
                log.Error("An unexpected exception happend", ex);
                throw;
            }
        }

        /// <summary>
        /// Displays the help message
        /// </summary>
        /// <param name="opts"></param>
        static void ShowHelp(OptionSet opts)
        {
            log.Info("Usage: packageinstaller [OPTIONS]");
            log.Info("Installs a sitecore package.");
            log.Info("");
            log.Info("Example to install a sitecore package:");
            log.Info(@"-sitecoreUrl ""http://mysite.com/"" -sitecoreDeployFolder ""C:\inetpub\wwwroot\mysite\Website"" -packagePath ""C:\Package1.zip"" -connector ""sitecore""");
            log.Info("Example to install a sitecore package:");
            log.Info(@"-sitecoreUrl ""http://mysite.com"" -sitecoreDeployFolder ""C:\inetpub\wwwroot\mysite\Website"" -packagePath ""C:\Package1.update"" -connector ""tds""");
            log.Info("");
            log.Info("Options:");

            opts.WriteOptionDescriptions(Console.Out);
        }

        public class FileToDeploy
        {
            public string SourcePath { get; set; }
            public string TargetPath { get; set; }
        }

        /// <summary>
        /// Deploys the 
        /// </summary>
        /// <param name="sitecoreDeployFolder"></param>
        /// <returns></returns>
        static bool DeploySitecoreConnector(string sitecoreDeployFolder)
        {
            log.Debug("Initializing Sitecore connector ...");

            string sourceFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            //Create the conenctor directory if not exist yet
            string sitecoreConnectorDirectory = $@"{sitecoreDeployFolder}\{connectorFolder}";
            if (!Directory.Exists(sitecoreConnectorDirectory))
            {
                Directory.CreateDirectory(sitecoreConnectorDirectory);
            }

            //Deploy the files
            foreach (var fileToDeploy in FilesToDeploy)
            {
                var targetPath = fileToDeploy.TargetPath.Replace("[connectorFolder]", connectorFolder);
                try
                {
                    var source = new FileInfo(sourceFolder + fileToDeploy.SourcePath);
                    if (!source.Exists)
                    {
                        log.Error($"Cannot find the source file {fileToDeploy.SourcePath}");
                        return false;
                    }

                    File.SetAttributes(source.FullName, FileAttributes.Normal);


                    var targetFilePath = $@"{ sitecoreDeployFolder}\{ targetPath}\{source.Name}";
                    var target = new FileInfo(targetFilePath);
                    if (target.Exists && target.LastWriteTimeUtc == source.LastWriteTimeUtc)
                    {
                        log.Debug($"File: {targetFilePath} allready exist and has not been modified since the last install.");
                    }
                    else
                    {
                        File.Copy(source.FullName, targetFilePath, true);
                        log.Debug($"File: {source.FullName} has benn deployed to {targetFilePath}");
                    }

                }
                catch (Exception ex)
                {
                    log.Error($"Error when deploying the file: {fileToDeploy.SourcePath} to {targetPath}", ex);
                    throw;
                }
            }           

            log.Info("Sitecore connector deployed successfully.");

            return true;
        }

        /// <summary>
        /// Removes the sitecore connector from the site
        /// </summary>
        static void RemoveSitecoreConnector(string sitecoreDeployFolder)
        {
            string sitecoreConnectorDirectory = $@"{sitecoreDeployFolder}\{connectorFolder}";
            try
            {
                if (!Directory.Exists(sitecoreConnectorDirectory))
                {
                    Directory.CreateDirectory(sitecoreConnectorDirectory);
                    log.Info($"The sitecore connector has been removed");
                }
            }
            catch (Exception)
            {
                log.Error($"Cannot remove the sitecore connector: {sitecoreConnectorDirectory}");
                throw;
            }
        }
    }

    
}
