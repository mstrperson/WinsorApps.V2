﻿using AsyncAwaitBestPractices;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Global.Services
{
    public class LocalLoggingService
    {
        public enum LogLevel
        {
            Information = 1,
            Warning = 2,
            Error = 4,
            Debug = 8
        }

        private static Dictionary<LogLevel, string>? LogFileNames;

        public Dictionary<string, byte[]> GetRecentLogs(DateTime since = default, DateTime until = default)
        {
            if (since == default)
            {
                since = DateTime.Today.AddDays(-7);
            }

            if (until == default)
            {
                until = DateTime.Now;
            }

            Dictionary<string, byte[]> result = new Dictionary<string, byte[]>();

            var logFiles = Directory.GetFiles($"{AppDataPath}{separator}logs")
                .Where(path => path.EndsWith(".log") && File.GetLastWriteTime(path) >= since && File.GetLastWriteTime(path) <= until);

            foreach (var logFile in logFiles)
            {
                byte[] bytes = File.ReadAllBytes(logFile);
                result.Add(logFile.Split(separator).Last(), bytes);
            }

            return result;
        }

        public string DownloadsDirectory
        {
            get
            {
                var dir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                var parts = dir.Split(separator);
                parts[parts.Length - 1] = "Downloads";
                dir = Path.Combine(parts);
                if(separator == '/')
                    dir = "/" + dir;

                return dir;
            }
        }

        public static readonly string AppDataPath =
            
            $"{Environment.GetFolderPath(Environment.SpecialFolder.Personal)}{Path.DirectorySeparatorChar}.WinsorApps";
        public static readonly string AppDataPathOld = 
            $"{Environment.GetFolderPath(Environment.SpecialFolder.Personal)}";

        private static char separator = Path.DirectorySeparatorChar;
        public LocalLoggingService()
        {
            if (LogFileNames is not null)
                return;

            var now = $"{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";

            if(!Directory.Exists(AppDataPath))
                Directory.CreateDirectory(AppDataPath);
            
            if (!Directory.Exists($"{AppDataPath}{separator}logs")) { Directory.CreateDirectory($"{AppDataPath}{separator}logs"); }

            LogFileNames ??= new Dictionary<LogLevel, string>()
            {
                {LogLevel.Information, $"{AppDataPath}{separator}logs{separator}info_{now}.log"},
                {LogLevel.Warning, $"{AppDataPath}{separator}logs{separator}warning_{now}.log"},
                {LogLevel.Error, $"{AppDataPath}{separator}logs{separator}error_{now}.log"},
                {LogLevel.Debug, $"{AppDataPath}{separator}logs{separator}debug_{now}.log"},
            };

        }
        public string ValidExecutableType => Environment.OSVersion.Platform == PlatformID.Win32NT ? "exe" : "pkg";
        public string ValidArchitecture =>
            System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == 
            System.Runtime.InteropServices.Architecture.Arm64 ? 
            "arm64" : "x86-64";

        
        

        public string AppStoragePath => 
            $"{AppDataPath}{separator}";

        public void LogError(ErrorRecord error) => LogMessage(LogLevel.Error, error.type, error.error);

        public void LogMessage(LogLevel log, params string[] messages)
        {
            if (LogFileNames is not null)
            {
                try
                {
                    using (var writer = File.AppendText(LogFileNames[log]))
                    {
                        foreach (string line in messages.Where(msg => !string.IsNullOrEmpty(msg)))
                        {
                            writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\t{line}");
                        }
                        writer.Flush();
                    }
                }
                catch
                {
                    Task.Run(async () =>
                    {
                        int retryCount = 0;
                        bool success = false;
                        while (retryCount < 5 && !success)
                        {

                            await Task.Delay(250);
                            try
                            {
                                using (var writer = File.AppendText(LogFileNames[log]))
                                {
                                    foreach (string line in messages.Where(msg => !string.IsNullOrEmpty(msg)))
                                    {
                                        writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\t{line}");
                                    }
                                    writer.Flush();
                                    success = true;
                                }
                            }
                            catch
                            {
                                retryCount++;
                            }
                        }
                    })
                    .SafeFireAndForget();
                }
            }
        }
    }
}
