using NLog;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace TubeEater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private Mutex? _mutex;
        public static string AsmName { get; private set; } = (Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()).GetName().Name ?? @"TubeEater";

        public App()
        {
            _logger.Debug(MethodBase.GetCurrentMethod()?.Name);
            try
            {
                _mutex = new Mutex(false, @$"Global\{AsmName}");
                if (!_mutex.WaitOne(0, false))
                {
                    _logger.Warn($"既に {AsmName} は実行中のため、起動を中止します。");
                    _mutex.Dispose();
                    Shutdown(-1);
                }
            }
            catch (AbandonedMutexException ex)
            {
                _logger.Error(ex, "多重起動の抑制に失敗しました。");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _logger.Debug(MethodBase.GetCurrentMethod()?.Name);
            base.OnExit(e);
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
    }
}
