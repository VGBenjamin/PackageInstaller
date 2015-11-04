using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using log4net;
using log4net.Repository.Hierarchy;
using log4net.spi;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Update;
using Sitecore.Update.Installer;
using Sitecore.Update.Installer.Exceptions;
using Sitecore.Update.Installer.Installer.Utils;
using Sitecore.Update.Installer.Utils;
using Sitecore.Update.Metadata;
using Sitecore.Update.Utils;
using Sitecore.Web;
using Sidewalk.SC.PackageInstaller.Common;

namespace Sidewalk.SC.PackageInstaller.Service
{
    public class SitecorePackageInstallerHandler : IHttpHandler, ILog
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SitecorePackageInstallerHandler));

        public ILogger Logger { get; }

        private List<string> logEntries;
        private int commandsProcessedCount;
        private int countOfCommands;

        public SitecorePackageInstallerHandler()
        {
            this.Logger = new RootLogger(Level.ALL);
        }
        
        /// <summary>
        /// Installs a Sitecore Update Package.
        /// </summary>
        /// <param name="path">A path to a package that is reachable by the web server</param>
        public string InstallTdsPackage(string path)
        {
            using (new ShutdownGuard())
            {
                logEntries = new List<string>();
                PackageInstallationInfo installationInfo = new PackageInstallationInfo
                {
                    Mode = InstallMode.Update,
                    Action = UpgradeAction.Upgrade,
                    Path = path
                };

                string historyPath = null;
                List<ContingencyEntry> entries = null;
                try
                {            
                    var action = (installationInfo.Action == UpgradeAction.Preview) ? "Analyzing" : "Installing";
                    this.WriteMessage($"{action} package: {installationInfo.Path}", null, Level.INFO, false);
            
                    entries = UpdateHelper.Install(installationInfo, this, out historyPath);
                }
                catch (PostStepInstallerException exception)
                {
                    entries = exception.Entries;
                    historyPath = exception.HistoryPath;
                    foreach (var entry in entries)
                    {
                        this.WriteMessage(entry.Message, exception, Level.ERROR);
                    }
                    throw exception;
                }
                finally
                {
                    //UpdateHelper.SaveInstallationMessages(entries, historyPath);
                }
                return historyPath;
            }
        }

        #region WriteMessages
        protected void WriteMessage(object message, Exception ex, Level level)
        {
            this.WriteMessage(message, ex, level, true);
        }

        protected void WriteMessage(object message, Exception ex, Level level, bool isCommandText)
        {
            var messageInfo = new MessageInfo()
            {
                Level = level.Name,
                Message = message.ToString(),
                Date = DateTime.Now
            };
            
            if (ex != null)
            {
                messageInfo.Exception = new MessageInfoException()
                {
                    ErrorMessage = ex.Message,
                    Source = ex.Source,
                    StackTrace = ex.StackTrace
                };
            }

            if (isCommandText && ((this.commandsProcessedCount < this.countOfCommands) || (this.countOfCommands <= 0)))
            {
                double a = (++this.commandsProcessedCount * 100.0) / ((this.countOfCommands == 0) ? ((double)1) : ((double)this.countOfCommands));

                messageInfo.Progress = new MessageInfoProgress()
                {
                    Percentage = Math.Round(a),
                    Processed = commandsProcessedCount,
                    TotalToProcess = countOfCommands
                };

                log.Info($"Progress: ({ messageInfo.Progress.Processed}/{ messageInfo.Progress.TotalToProcess} - { messageInfo.Progress.Percentage}%)");
            }
            else
            {
                if (level == Level.FATAL)
                    log.Fatal(message, ex);
                else if (level == Level.ERROR)
                    log.Error(message, ex);
                else if (level == Level.DEBUG)
                    log.Debug(message);
                else
                    log.Info(message);
            }

            var memoryStream = MessageToXmlString(messageInfo);

            HttpContext.Current.Response.Write(Utf8ByteArrayToString(memoryStream.ToArray()));
            HttpContext.Current.Response.Flush();
        }

        private static MemoryStream MessageToXmlString(MessageInfo messageInfo)
        {
            using (var memoryStream = new MemoryStream())
            {
                var xs = new XmlSerializer(typeof (MessageInfo));
                var settings = new XmlWriterSettings() {OmitXmlDeclaration = true, Encoding = Encoding.UTF8};
                using (var xmlTextWriter = XmlWriter.Create(memoryStream, settings))
                {
                    xs.Serialize(xmlTextWriter, messageInfo);
                    return memoryStream;
                }
            }
        }

        private static string Utf8ByteArrayToString(byte[] characters)
        {
            var encoding = new UTF8Encoding();
            string constructedString = encoding.GetString(characters);
            return constructedString;
        }

        public void Info(object message)
        {
            this.WriteMessage(message, null, Level.INFO);
        }
        public void Info(object message, Exception t)
        {
            this.WriteMessage(message, t, Level.INFO);
        }
        public void Warn(object message)
        {
            this.WriteMessage(message, null, Level.WARN);
        }
        public void Warn(object message, Exception t)
        {
            this.WriteMessage(message, t, Level.WARN);
        }
        public void Debug(object message)
        {
            this.WriteMessage(message, null, Level.DEBUG);
        }
        public void Debug(object message, Exception t)
        {
            this.WriteMessage(message, t, Level.DEBUG);
        }
        public void Error(object message, Exception t)
        {
            this.WriteMessage(message, t, Level.ERROR);
        }
        public void Error(object message)
        {
            this.WriteMessage(message, null, Level.ERROR);
        }
        public void Fatal(object message)
        {
            this.WriteMessage(message, null, Level.FATAL);
        }
        public void Fatal(object message, Exception t)
        {
            this.WriteMessage(message, t, Level.FATAL);
        }

        public bool IsDebugEnabled => true;
        public bool IsInfoEnabled => true;
        public bool IsWarnEnabled => true;
        public bool IsErrorEnabled => true;
        public bool IsFatalEnabled => true;

        #endregion

  

        public static MetadataView GetMetadata(string packagePath, out string error)
        {
            Assert.IsNotNull(packagePath, "package path");
            error = string.Empty;
            MetadataView metadataFromCache = null;
            
            try
            {
                metadataFromCache = UpdateHelper.LoadMetadata(packagePath);
            }
            catch
            {
            }

            if ((metadataFromCache == null) || string.IsNullOrEmpty(metadataFromCache.PackageName))
            {
                error = Translate.Text("The package \"{0}\" could not be loaded.<p>The file is not an update package.</p>", new object[] { packagePath });
                return null;
            }
            
            return metadataFromCache;
        }



        public void ProcessRequest(HttpContext context)
        {
            try
            {                
                HttpContext.Current.Response.Write("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                HttpContext.Current.Response.Write("<Response>");
                HttpContext.Current.Response.Flush();

                this.ExtractQueryParameters();
                if (string.IsNullOrEmpty(this.PackagePath))
                {
                    string msg = $"The package \"{PackagePath}\" not found.";
                    Fatal(msg, new ArgumentNullException(msg));
                }
                else
                {
                    string str;
                    MetadataView metadata = GetMetadata(this.PackagePath, out str);
                    if ((metadata == null) || !string.IsNullOrEmpty(str))
                    {
                        string msg = $"The package \"{PackagePath}\" could not be loaded.<p>The file is not an update package.";
                        Fatal(msg, new ArgumentNullException(msg));
                    }
                    else
                    {
                        this.countOfCommands = metadata.CommandsCount;
                        string packageInfos = $@"Package name: {metadata.PackageName}
Package version: {metadata.Version} (revision: {metadata.Revision})
Author: {metadata.PackageName}
Publisher: {metadata.Publisher}
Readme: {metadata.Readme}
Comment: {metadata.Comment}";                        

                        Info(packageInfos);
                        this.InstallTdsPackage(this.PackagePath);
                    }
                }
            }
            catch (Exception exception)
            {
                Error("Unknow error", exception);
            }
            finally
            {
                HttpContext.Current.Response.Write("</Response>");
                HttpContext.Current.Response.Flush();
            }
        }


        protected virtual void ExtractQueryParameters()
        {
            string queryString = WebUtil.GetQueryString("package");
            if (!string.IsNullOrEmpty(queryString))
            {
                string packagePath = queryString;
                if (!string.IsNullOrEmpty(packagePath) && !packagePath.Contains(@"\"))
                {
                    packagePath = UpdateHelper.GetPackagePath(queryString);
                }
                if (!string.IsNullOrEmpty(packagePath) && File.Exists(packagePath))
                {
                    this.PackagePath = packagePath;
                }
            }
            this.InstallMode = (WebUtil.GetQueryString("install") == "1") ? InstallMode.Install : InstallMode.Update;
            this.UpgradeAction = (WebUtil.GetQueryString("upgrade") == "1") ? UpgradeAction.Upgrade : UpgradeAction.Preview;
            string str3 = WebUtil.GetQueryString("history");
            if (!string.IsNullOrEmpty(str3))
            {
                this.InstallationHistoryRoot = UpdateHelper.GetHistoryPath(HttpContext.Current.Server.UrlDecode(str3));
            }
        }

        public string InstallationHistoryRoot { get; set; }

        public string PackagePath { get; set; }

        public UpgradeAction UpgradeAction { get; set; }

        public InstallMode InstallMode { get; set; }

        public bool IsReusable { get; private set; }
    }


}
