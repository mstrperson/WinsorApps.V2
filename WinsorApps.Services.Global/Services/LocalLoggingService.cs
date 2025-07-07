using AsyncAwaitBestPractices;
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

        private static string LogFileName = "";

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

            Dictionary<string, byte[]> result = [];

            var logFiles = Directory.GetFiles($"{AppDataPath}{separator}logs")
                .Where(path => path.EndsWith(".log") && File.GetLastWriteTime(path) >= since && File.GetLastWriteTime(path) <= until);

            foreach (var logFile in logFiles)
            {
                var bytes = File.ReadAllBytes(logFile);
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
                parts[^1] = "Downloads";
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

        private static readonly char separator = Path.DirectorySeparatorChar;
        public LocalLoggingService()
        {
            if (!string.IsNullOrEmpty(LogFileName))
                return;

            var now = $"{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";

            if (!Directory.Exists(AppDataPath))
                Directory.CreateDirectory(AppDataPath);

            if (!Directory.Exists($"{AppDataPath}{separator}logs")) 
            { 
                Directory.CreateDirectory($"{AppDataPath}{separator}logs"); 
            }


            LogFileName = $"{AppDataPath}{separator}logs{separator}log_{now}.log";

            CleanUpOldLogs().SafeFireAndForget(e => e.LogException(this));
        }

        private async Task CleanUpOldLogs() => await Task.Run(() => 
        {
            foreach (var fileName in Directory.GetFiles($"{AppDataPath}{separator}logs"))
            {
                if (File.GetCreationTime(fileName).OlderThan(TimeSpan.FromDays(30)))
                {
                    LogMessage(LogLevel.Debug, $"Cleaning Up Old Log File {fileName}");
                    File.Delete(fileName);
                }
            }
        });

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
            if (string.IsNullOrEmpty(LogFileName))
            {
                return;
            }
            try
            {
                using var writer = File.AppendText(LogFileName);
                foreach (var line in messages.Where(msg => !string.IsNullOrEmpty(msg)))
                {
                    writer.WriteLine($"{log}:\t[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\t{line}");
                }
                writer.Flush();
            }
            catch
            {
                Task.Run(async () =>
                {
                    var retryCount = 0;
                    var success = false;
                    while (retryCount < 5 && !success)
                    {

                        await Task.Delay(250);
                        try
                        {
                            using var writer = File.AppendText(LogFileName);
                            foreach (var line in messages.Where(msg => !string.IsNullOrEmpty(msg)))
                            {
                                writer.WriteLine($"{log}:\t[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\t{line}");
                            }
                            writer.Flush();
                            success = true;
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
