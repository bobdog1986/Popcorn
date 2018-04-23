using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Akavache;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Threading;
using Ignite.SharpNetSH;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;
using Microsoft.Owin.Hosting;
using NetFwTypeLib;
using NLog;
using Popcorn.Helpers;
using Popcorn.Initializers;
using Popcorn.Services.Server;
using Popcorn.Services.User;
using Popcorn.Utils;
using Popcorn.ViewModels;
using Popcorn.ViewModels.Windows;
using Popcorn.Windows;
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
        private Stopwatch WatchStart { get; set; }

        /// <summary>
        /// Loading semaphore
        /// </summary>
        private readonly SemaphoreSlim _windowLoadedSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// The disposable local OWIN server
        /// </summary>
        private IDisposable _localServer;

        static App()
        {
            DispatcherHelper.Initialize();
            LocalizeDictionary.Instance.SetCurrentThreadCulture = true;
            BlobCache.ApplicationName = "Popcorn";
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
        protected override async void OnStartup(StartupEventArgs e)
        {
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new PopcornApplicationInsightsInitializer());
            var builder = TelemetryConfiguration.Active.TelemetryProcessorChainBuilder;
            builder.UseAdaptiveSampling(1);
            builder.Build();
            await ApplicationInsightsHelper.Initialize();
            base.OnStartup(e);
            WatchStart = Stopwatch.StartNew();
            Logger.Info(
                "Popcorn starting...");
            Unosquare.FFME.MediaElement.FFmpegDirectory = Constants.FFmpegPath;

            try
            {
                var userService = SimpleIoc.Default.GetInstance<IUserService>();
                await userService.GetUser();
                await Task.Run(async () =>
                {
                    var netsh = new NetSH(new Utils.CommandLineHarness());
                    var showResponse = netsh.Http.Show.UrlAcl(Constants.ServerUrl);
                    if (showResponse.ResponseObject.Count == 0)
                    {
                        if (!Helper.IsAdministrator())
                        {
                            RestartAsAdmin();
                        }
                        else
                        {
                            RegisterUrlAcl();
                        }
                    }

                    if (!await FirewallRuleExists("Popcorn Server"))
                    {
                        if (!Helper.IsAdministrator())
                        {
                            RestartAsAdmin();
                        }
                        else
                        {
                            await RegisterFirewallRule();
                        }
                    }

                    _localServer = WebApp.Start<Startup>(Constants.ServerUrl);
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private static void RestartAsAdmin()
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                var args = string.Empty;
                var cmd = Environment.GetCommandLineArgs();
                if (cmd.Any())
                {
                    args = string.Join(" ", cmd);
                }

                var info = new ProcessStartInfo(
                    Assembly.GetEntryAssembly().Location)
                {
                    Verb = "runas",
                    Arguments = args
                };

                var process = new Process
                {
                    StartInfo = info
                };

                process.Start();
                Current.Shutdown();
            });
        }

        private async Task<bool> FirewallRuleExists(string ruleName)
        {
            var tcs = new TaskCompletionSource<bool>();
            await Task.Run(() =>
            {
                try
                {
                    Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                    INetFwPolicy2 fwPolicy2 = (INetFwPolicy2) Activator.CreateInstance(tNetFwPolicy2);
                    foreach (INetFwRule rule in fwPolicy2.Rules)
                    {
                        if (rule.Name.IndexOf(ruleName, StringComparison.Ordinal) != -1)
                        {
                            tcs.TrySetResult(true);
                        }
                    }

                    tcs.TrySetResult(false);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    tcs.TrySetResult(false);
                }
            });

            return await tcs.Task;
        }

        private static async Task<bool> RegisterFirewallRule()
        {
            var tcs = new TaskCompletionSource<bool>();
            await Task.Run(() =>
            {
                try
                {
                    INetFwRule firewallRule = (INetFwRule) Activator.CreateInstance(
                        Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    firewallRule.Description = "Enables Popcorn server.";
                    firewallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
                    firewallRule.Enabled = true;
                    firewallRule.InterfaceTypes = "All";
                    firewallRule.Name = "Popcorn Server";
                    firewallRule.Protocol = (int) NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
                    firewallRule.LocalPorts = Constants.ServerPort.ToString();
                    INetFwPolicy2 firewallPolicy = (INetFwPolicy2) Activator.CreateInstance(
                        Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
                    firewallPolicy.Rules.Add(firewallRule);
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    tcs.TrySetResult(false);
                }
            });

            return await tcs.Task;
        }

        private static void RegisterUrlAcl()
        {
            try
            {
                var username = Environment.GetEnvironmentVariable("USERNAME");
                var domain = Environment.GetEnvironmentVariable("USERDOMAIN");
                var netsh = new NetSH(new Utils.CommandLineHarness());
                netsh.Http.Add.UrlAcl(Constants.ServerUrl, $"{domain}\\{username}", true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
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
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            _localServer?.Dispose();
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

            var mainWindow = new MainWindow {Topmost = true};
            MainWindow = mainWindow;
            mainWindow.Loaded += async (sender2, e2) =>
                await mainWindow.Dispatcher.InvokeAsync(async () =>
                {
                    await _windowLoadedSemaphore.WaitAsync();
                    if (!WatchStart.IsRunning)
                        return;
                    _splashScreenDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                    mainWindow.Activate();
                    if (mainWindow.DataContext is WindowViewModel vm)
                    {
                        vm.InitializeAsyncCommand.Execute(null);
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