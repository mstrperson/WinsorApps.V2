using AsyncAwaitBestPractices;

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

            var logFiles = Directory.GetFiles(AppDataPath)
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

        public static readonly string AppDataPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}";

        private static char separator = Path.DirectorySeparatorChar;
        public LocalLoggingService()
        {
            if (LogFileNames is not null)
                return;

            var now = $"{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";

            if (!Directory.Exists(AppDataPath)) { Directory.CreateDirectory(AppDataPath); }

            if (LastVersionUpdated == DateTime.MinValue)
                LastVersionUpdated = DateTime.Now;

            if(LogFileNames is null)
                LogFileNames = new Dictionary<LogLevel, string>()
                {
                    { LogLevel.Information, $"{AppDataPath}{separator}info_{now}.log" },
                    { LogLevel.Warning, $"{AppDataPath}{separator}warning_{now}.log" },
                    { LogLevel.Error, $"{AppDataPath}{separator}error_{now}.log" },
                    { LogLevel.Debug, $"{AppDataPath}{separator}debug_{now}.log" },
                };
        }
        public string ValidExecutableType => Environment.OSVersion.Platform == PlatformID.Win32NT ? "exe" : "pkg";
        public string ValidArchitecture =>
            System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == 
            System.Runtime.InteropServices.Architecture.Arm64 ? 
            "arm64" : "x86-64";

        private string VersionFilePath => $"{AppDataPath}{separator}version";
        public DateTime LastVersionUpdated
        {
            get
            {
                try
                {
                    if (!File.Exists(VersionFilePath))
                    {
                        LogMessage(LogLevel.Debug, "Version File does not exist...");
                        LastVersionUpdated = DateTime.Now;
                    }
                    return File.GetLastWriteTime(VersionFilePath);
                }
                catch(Exception ex)
                {
                    ex.LogException(this);
                    return DateTime.MaxValue;
                }
            }
            set
            {
                try
                {
                    File.WriteAllText(VersionFilePath, $"{value:F}");
                }
                catch(Exception ex )
                {
                    ex.LogException(this);
                }
            }
        }

        public string AppStoragePath => 
            $"{AppDataPath}{separator}";

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
