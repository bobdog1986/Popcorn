using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Akavache;
using CefSharp;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Microsoft.ApplicationInsights.NLogTarget;
using NLog;
using NLog.Config;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Utils;
using Popcorn.Utils.Exceptions;
using Popcorn.ViewModels.Windows;
using Popcorn.Windows;
using Squirrel;
using WPFLocalizeExtension.Engine;

namespace Popcorn
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Splash screen dispatcher
        /// </summary>
        private Dispatcher _splashScreenDispatcher;

        /// <summary>
        /// Watcher
        /// </summary>
        private static Stopwatch WatchStart { get; }
    
        /// <summary>
        /// If first run
        /// </summary>
        private static bool _firstRun;

        /// <summary>
        /// Loading semaphore
        /// </summary>
        private readonly SemaphoreSlim _windowLoadedSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the App class.
        /// </summary>
        static App()
        {
            WatchStart = Stopwatch.StartNew();
            var config = new LoggingConfiguration();
            var target =
                new ApplicationInsightsTarget {InstrumentationKey = Constants.AiKey};
            var rule = new LoggingRule("*", LogLevel.Trace, target);
            config.LoggingRules.Add(rule);
            LogManager.Configuration = config;
            Logger.Info(
                "Popcorn starting...");
            DispatcherHelper.Initialize();
            LocalizeDictionary.Instance.SetCurrentThreadCulture = true;
            BlobCache.ApplicationName = "Popcorn";

            try
            {
                SquirrelAwareApp.HandleEvents(
                    onFirstRun: () =>
                    {
                        // TODO: Must complete welcome.md page before activate this feature
                        //_firstRun = true;
                    });
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
     
        /// <summary>
        /// Initializes a new instance of the App class.
        /// </summary>
        public App()
        {
            DispatcherUnhandledException += AppDispatcherUnhandledException;
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        /// <summary>
        /// On startup, register synchronization context
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AsyncSynchronizationContext.Register();
            var settings = new CefSettings
            {
                BrowserSubprocessPath = $@"{Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName}\CefSharp.BrowserSubprocess"
            };
            Cef.Initialize(settings);
        }

        /// <summary>
        /// Observe unhandled exceptions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            CurrentDomainUnhandledException(sender, new UnhandledExceptionEventArgs(e.Exception, false));
        }

        /// <summary>
        /// Handle unhandled expceptions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            CurrentDomainUnhandledException(sender, new UnhandledExceptionEventArgs(e.Exception, false));
        }

        /// <summary>
        /// When an unhandled exception has been thrown, handle it
        /// </summary>
        /// <param name="sender"><see cref="App"/> instance</param>
        /// <param name="e">DispatcherUnhandledExceptionEventArgs args</param>
        private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            CurrentDomainUnhandledException(sender, new UnhandledExceptionEventArgs(e.Exception, false));
        }

        /// <summary>
        /// When an unhandled exception domain has been thrown, handle it
        /// </summary>
        /// <param name="sender"><see cref="App"/> instance</param>
        /// <param name="e">UnhandledExceptionEventArgs args</param>
        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Logger.Fatal(ex);
                Messenger.Default.Send(
                    new UnhandledExceptionMessage(
                        new PopcornException(LocalizationProviderHelper.GetLocalizedValue<string>("FatalError"))));
            }
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            var splashScreenThread = new Thread(() =>
            {
                var splashScreen = new Windows.SplashScreen();
                _splashScreenDispatcher = splashScreen.Dispatcher;
                _splashScreenDispatcher.ShutdownStarted += (o, args) => splashScreen.Close();
                splashScreen.Show();
                Dispatcher.Run();
            });

            splashScreenThread.SetApartmentState(ApartmentState.STA);
            splashScreenThread.Start();

            var mainWindow = new MainWindow();
            mainWindow.Topmost = true;
            MainWindow = mainWindow;
            mainWindow.Loaded += async (sender2, e2) =>
                await mainWindow.Dispatcher.InvokeAsync(async () =>
                {
                    await _windowLoadedSemaphore.WaitAsync();
                    if (!WatchStart.IsRunning)
                        return;
                    _splashScreenDispatcher.InvokeShutdown();
                    mainWindow.Activate();
                    var vm = mainWindow.DataContext as WindowViewModel;
                    if (vm != null)
                    {
                        vm.InitializeAsyncCommand.Execute(null);
                        if(_firstRun)
                            vm.OpenWelcomeCommand.Execute(null);
                    }
                    WatchStart.Stop();
                    var elapsedStartMs = WatchStart.ElapsedMilliseconds;
                    Logger.Info(
                        $"Popcorn started in {elapsedStartMs} milliseconds.");
                    _windowLoadedSemaphore.Release();
                });

            mainWindow.Show();
        }
    }
}