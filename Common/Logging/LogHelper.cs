using System;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using log4net.Config;

namespace Common.Logging
{
    public static class LogHelper
    {
        public static bool HasInitializedLogging;

        public static ILog GetLogger(string name)
        {
            return LogManager.GetLogger(Assembly.GetEntryAssembly(), name);
        }

        public static void InitializeFileAndElkLogging(string logFileName)
        {
            InitializeLogging(logFileName);
        }

        public static void InitializeElkOnlyLogging()
        {
            InitializeLogging(null);
        }

        private static void InitializeLogging(string logFileName)
        {
            if (HasInitializedLogging)
                return;
            HasInitializedLogging = true;

            logFileName = logFileName == null
                ? Path.Combine(Helper.GetGlobalCementDirectory(), "log", "log")
                : Path.Combine(Helper.CurrentWorkspace, Helper.CementDirectory, "log", logFileName);
            Environment.SetEnvironmentVariable("logfilename", logFileName);
            GlobalContext.Properties["logfilename"] = logFileName;

            var logConfig = Path.Combine(Helper.GetCementInstallDirectory(), "dotnet", "log.config.xml");
            if (!File.Exists(logConfig))
            {
                ConsoleWriter.WriteError($"{logConfig} not found.");
                return;
            }
            //var repo = LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));
            var repo = LogManager.GetRepository(Assembly.GetEntryAssembly());
            repo.Properties["logfilename"] = logFileName;
            XmlConfigurator.ConfigureAndWatch(repo, new FileInfo(logConfig));
        }

        public static void SaveLog(string log)
        {
            try
            {
                var file = Path.Combine(Helper.GetCementInstallDirectory(), "log.log");
                if (!File.Exists(file))
                    File.Create(file).Close();
                File.AppendAllText(file, "\n" + log);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static void SendSavedLog()
        {
            try
            {
                var file = Path.Combine(Helper.GetCementInstallDirectory(), "log.log");
                if (!File.Exists(file))
                    File.Create(file).Close();
                var lines = File.ReadAllLines(file).Where(l => l.Length > 0).ToList();
                File.WriteAllText(file, "");

                var log = LogManager.GetLogger(typeof(LogHelper));
                log = new PrefixAppender("SAVED-LOG", log);

                foreach (var line in lines)
                {
                    log.Debug(line);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}