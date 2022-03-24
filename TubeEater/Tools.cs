using MaterialDesignThemes.Wpf;
using NLog;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TubeEater
{
    public class Tools
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// プロセス実行
        /// </summary>
        /// <param name="target"></param>
        public static void StartProcess(string? target)
        {
            _logger.Debug(MethodBase.GetCurrentMethod()?.Name);
            if (target == null) return;

            try
            {
                // Windows
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var psi = new ProcessStartInfo
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        FileName = "powershell.exe",
                        Arguments = $"Start-Process -FilePath \"{target.Replace("&", "^&")}\"",
                    };
                    _logger.Info($"{psi.FileName} {psi.Arguments}");
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ERROR: Can't start process.");
            }
        }

        /// <summary>
        /// フォルダを開く
        /// </summary>
        /// <param name="path"></param>
        public static void OpenFolder(string? path)
        {
            _logger.Debug(MethodBase.GetCurrentMethod()?.Name);
            if (path == null) return;

            try
            {
                // Windows
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "Explorer.exe",
                        Arguments = $"\"{path}\""
                    };
                    _logger.Info($"{psi.FileName} {psi.Arguments}");
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ERROR: Can't start process.");
            }
        }
    }
}
