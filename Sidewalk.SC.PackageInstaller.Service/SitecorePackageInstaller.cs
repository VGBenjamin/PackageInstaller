using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Web.Services;
using System.Xml;
using log4net;
using log4net.Config;
using Sitecore.Data;
using Sitecore.Data.Engines;
using Sitecore.Data.Managers;
using Sitecore.Data.Proxies;
using Sitecore.Exceptions;
using Sitecore.Publishing;
using Sitecore.SecurityModel;
using Sitecore.Update;
using Sitecore.Update.Installer;
using Sitecore.Update.Installer.Exceptions;
using Sitecore.Update.Installer.Installer.Utils;
using Sitecore.Update.Installer.Utils;
using Sitecore.Update.Utils;

namespace Sidewalk.SC.PackageInstaller.Service
{
    [WebService(Namespace = "http://sitecoreblog.blogspot.be/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class SitecorePackageInstaller : System.Web.Services.WebService
    {
        /// <summary>
        /// Installs a Sitecore Package.
        /// </summary>
        /// <param name="path">A path to a package that is reachable by the web server</param>
        [WebMethod(Description = "Installs a Sitecore Package.")]
        public void InstallPackage(string path)
        {
            // Use default logger
            ILog log = LogManager.GetLogger("root");
            XmlConfigurator.Configure((XmlElement)ConfigurationManager.GetSection("log4net"));

            FileInfo pkgFile = new FileInfo(path);

            if (!pkgFile.Exists)
                throw new ClientAlertException($"Cannot access path '{path}'. Please check path setting.");

            Sitecore.Context.SetActiveSite("shell");
            using (new SecurityDisabler())
            {
                using (new ProxyDisabler())
                {
                    using (new SyncOperationContext())
                    {
                        Sitecore.Install.Framework.IProcessingContext context = new Sitecore.Install.Framework.SimpleProcessingContext(); // 
                        Sitecore.Install.Items.IItemInstallerEvents events =
                            new Sitecore.Install.Items.DefaultItemInstallerEvents(new Sitecore.Install.Utils.BehaviourOptions(Sitecore.Install.Utils.InstallMode.Overwrite, Sitecore.Install.Utils.MergeMode.Undefined));
                        context.AddAspect(events);
                        Sitecore.Install.Files.IFileInstallerEvents events1 = new Sitecore.Install.Files.DefaultFileInstallerEvents(true);
                        context.AddAspect(events1);
                        var inst = new Sitecore.Install.Installer();
                        inst.InstallPackage(Sitecore.MainUtil.MapPath(path), context);
                    }
                }
            }            
        }

        /// <summary>
        /// Installs a Sitecore Update Package.
        /// </summary>
        /// <param name="path">A path to a package that is reachable by the web server</param>
        [WebMethod(Description = "Installs a TDS Update Package.")]
        public string InstallTdsPackage(string path)
        {            
            // Use default logger
            var log = LogManager.GetLogger("root");
            XmlConfigurator.Configure((XmlElement)ConfigurationManager.GetSection("log4net"));

            using (new ShutdownGuard())
            {
                List<string> logEntries = new List<string>();
                PackageInstallationInfo installationInfo =  new PackageInstallationInfo {
                                                                Mode = InstallMode.Update,
                                                                Action = UpgradeAction.Upgrade,
                                                                Path = path
                                                            };

                string historyPath = null;
                List<ContingencyEntry> entries = null;
                try
                {
                    log.Info($"Installing package: {installationInfo.Path}");
                    UpdateHelper.Install(installationInfo, log, out historyPath);
                }
                catch (PostStepInstallerException exception)
                {
                    foreach (var entry in exception.Entries)
                    {
                        log.Error(entry.Message);
                    }
                    throw exception;
                }
                finally
                {
                }
                return historyPath;
            }
        }

        /// <summary>
        /// Publish some sitecore items
        /// </summary>                
        /// <param name="mode">The publish mode</param>
        /// <param name="languageName">The name of the language</param>
        /// <param name="publishingTargets">teh publish targets</param>
        /// <param name="deep">Publish the children items</param>
        /// <param name="sourceDatabaseName">The source database (master if ommited)</param>
        /// <param name="targetDatabaseName">The target database (web if ommited)</param>
        /// <param name="rootItemPath">The root item for the publish (all if ommited)</param>
        [WebMethod(Description = "Publish some sitecore items.")]
        public void Publish(PublishMode mode, string languageName, List<string> publishingTargets, bool deep, string sourceDatabaseName = "master", string targetDatabaseName = "web", string rootItemPath = null)
        {
            //TODO : This option doesn't work yet

            using (new SecurityDisabler())
            {
                var sourceDatabase = Database.GetDatabase(sourceDatabaseName);
                var targetDatabase = Database.GetDatabase(targetDatabaseName);
                var language = string.IsNullOrEmpty(languageName) ? null : LanguageManager.GetLanguage(languageName);
                var rootItem = rootItemPath == null ? null : sourceDatabase.SelectSingleItem(rootItemPath);

                Sitecore.Publishing.PublishOptions publishOptions = new Sitecore.Publishing.PublishOptions(sourceDatabase,
                                                        targetDatabase,
                                                        mode,
                                                        language,
                                                        System.DateTime.Now/*, publishingTargets*/);  // Create a publisher with the publishoptions
        

                // The publishOptions determine the source and target database,
                // the publish mode and language, and the publish date                
                var publisher = new Sitecore.Publishing.Publisher(publishOptions);

                // Choose where to publish from
                publisher.Options.RootItem = rootItem;

                // Publish children as well?
                publisher.Options.Deep = deep;

                // Do the publish!
                publisher.Publish();
            }
        }
    }
}
