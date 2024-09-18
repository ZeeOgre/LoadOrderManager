using AutoUpdaterDotNET;
using System.Configuration;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Windows;
using ZO.LoadOrderManager.Properties;

namespace ZO.LoadOrderManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class App : Application
    {
        public static bool IsSettingsMode { get; private set; }

        public static string CompanyName { get; }
        public static string ProductName { get; }
        public static string PackageID { get; }
        public static string Version { get; }

        private LoadOrderWindow? _mainWindow;

        static App()
        {
            var assembly = Assembly.GetExecutingAssembly();
            CompanyName = GetAssemblyAttribute<AssemblyCompanyAttribute>(assembly)?.Company ?? "Unknown Company";
            ProductName = GetAssemblyAttribute<AssemblyProductAttribute>(assembly)?.Product ?? "Unknown Product";
            PackageID = GetAssemblyAttribute<AssemblyProductAttribute>(assembly)?.Product ?? "Unknown Product";
            Version = assembly.GetName().Version?.ToString() ?? "0.0.0.0";
        }

        public App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            _ = Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Trace.AutoFlush = true;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            App.LogDebug("OnStartup called");
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            try
            {
                base.OnStartup(e);
                App.LogDebug("Application_Startup called");
                var updateUrl = Settings.Default.UpdateUrl;

                SetProbingPaths();

                App.LogDebug("Verifying local app data files...");
                //Config.VerifyLocalAppDataFiles();

                App.LogDebug("Checking command line arguments...");
                foreach (var arg in e.Args)
                {
                    App.LogDebug($"Argument: {arg}");
                }

                if (Array.Exists(e.Args, arg => arg.Equals("--settings", StringComparison.OrdinalIgnoreCase)))
                {
                    App.LogDebug("--settings argument detected. Entering settings mode.");
                    IsSettingsMode = true;
                    HandleSettingsMode();
                }
                else
                {
                    App.LogDebug("No --settings argument detected. Entering normal mode.");
                    HandleNormalMode();
                }
            }
            catch (Exception ex)
            {
                App.LogDebug($"Exception during startup: {ex.Message}");
                _ = MessageBox.Show($"An error occurred during startup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        public static void LogReaderData(SQLiteDataReader reader)
        {
            var logMessage = new StringBuilder();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                logMessage.AppendFormat("{0} = {1}, ", reader.GetName(i), reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString());
            }
            App.LogDebug(logMessage.ToString());
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            App.LogDebug($"Unhandled exception caught: {e.Exception.Message}");
            _ = MessageBox.Show($"Unhandled exception: {e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // Optionally set this to true to prevent app crash
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                App.LogDebug($"Unhandled domain exception: {ex.Message}");
                _ = MessageBox.Show($"Unhandled domain exception: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                App.LogDebug("Unhandled domain exception occurred.");
                _ = MessageBox.Show("Unhandled domain exception occurred.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HandleSettingsMode()
        {
            try
            {
                Config.InitializeNewInstance();
                App.LogDebug("Launching SettingsWindow in settings mode.");
                var settingsWindow = new SettingsWindow(SettingsLaunchSource.CommandLine);
                settingsWindow.Closed += (s, e) => RestartApplication();
                settingsWindow.Show();
            }
            catch (Exception ex)
            {
                App.LogDebug($"Exception in HandleSettingsMode: {ex.Message}");
                _ = MessageBox.Show($"An error occurred in settings mode: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void HandleNormalMode()
        {
            LoadingWindow loadingWindow = null;

            try
            {
                Dispatcher.Invoke(() =>
                {
                    loadingWindow = new LoadingWindow();
                    loadingWindow.Show();
                });

                InitializationManager.SetProgressCallback((progress, message) =>
                {
                    Dispatcher.Invoke(() => loadingWindow.UpdateProgress(progress, message));
                });

                Task.Run(() =>
                {
                    try
                    {
                        App.LogDebug("Initializing configuration...");
                        InitializationManager.StartInitialization(nameof(Config));
                        Config.Initialize();
                        InitializationManager.ReportProgress(20, "Configuration initialized");
                        InitializationManager.EndInitialization(nameof(Config));
                        App.LogDebug("Configuration initialized.");

                        App.LogDebug("Initializing database manager...");
                        InitializationManager.StartInitialization(nameof(DbManager));
                        DbManager.Instance.Initialize();
                        InitializationManager.ReportProgress(40, "Database manager initialized");
                        InitializationManager.EndInitialization(nameof(DbManager));
                        App.LogDebug("Database manager initialized.");

                        App.LogDebug("Initializing AggLoadInfo from database...");
                        InitializationManager.StartInitialization(nameof(AggLoadInfo));
                        AggLoadInfo.Instance.InitFromDatabase();
                        InitializationManager.ReportProgress(60, "AggLoadInfo initialized from database");
                        InitializationManager.EndInitialization(nameof(AggLoadInfo));
                        App.LogDebug("AggLoadInfo initialized from database.");

                        App.LogDebug("Initializing file manager...");
                        InitializationManager.StartInitialization(nameof(FileManager));
                        FileManager.Initialize();
                        InitializationManager.ReportProgress(80, "File manager initialized");
                        InitializationManager.EndInitialization(nameof(FileManager));
                        App.LogDebug("File manager initialized.");

                        Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                if (_mainWindow == null)
                                {
                                    App.LogDebug("Creating a new instance of LoadOrderWindow...");
                                    _mainWindow = new LoadOrderWindow();
                                }

                                _mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                                _mainWindow.Visibility = Visibility.Visible;
                                _mainWindow.WindowState = WindowState.Normal;

                                App.LogDebug("Attempting to show LoadOrderWindow...");
                                _mainWindow.Show();
                                App.LogDebug("LoadOrderWindow successfully shown.");
                                loadingWindow.UpdateProgress(100, "LoadOrderWindow successfully shown");
                            }
                            catch (Exception ex)
                            {
                                App.LogDebug($"Exception while showing LoadOrderWindow: {ex.Message}");
                                _ = MessageBox.Show($"An error occurred while showing the main window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        App.LogDebug($"Exception in HandleNormalMode: {ex.Message}");
                        _ = MessageBox.Show($"An error occurred in normal mode: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Shutdown();
                    }
                    finally
                    {
                        Dispatcher.Invoke(() =>
                        {
                            loadingWindow?.Close();
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                App.LogDebug($"Exception in HandleNormalMode: {ex.Message}");
                _ = MessageBox.Show($"An error occurred in normal mode: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private async static void RestartApplication()
        {
            App.LogDebug("Preparing to restart application...");
            try
            {
                App.LogDebug("Calling Application.Current.Shutdown()...");
                Application.Current.Shutdown();

                await Task.Delay(1000);

                App.LogDebug("Restarting application...");
                _ = Process.Start(Application.ResourceAssembly.Location);
            }
            catch (Exception ex)
            {
                App.LogDebug($"Exception during restart: {ex.Message}");
            }
        }

        private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            string probingPaths = Settings.Default.ProbingPaths;
            string[] paths = probingPaths.Split(';');

            foreach (string path in paths)
            {
                string assemblyPath = Path.Combine(AppContext.BaseDirectory, path, new AssemblyName(args.Name).Name + ".dll");

                if (File.Exists(assemblyPath))
                {
                    App.LogDebug($"Resolved assembly path: {assemblyPath}");
                    return Assembly.LoadFrom(assemblyPath);
                }
            }

            return null;
        }

        public static void LogDebug(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        {
            string logMessage = $"{Path.GetFileName(filePath)}:{lineNumber} - {memberName}: {message}";
            ActualLogMethod(logMessage);
        }

        private static void ActualLogMethod(string logMessage)
        {
            Console.WriteLine(logMessage);

            bool textLoggingEnabled = bool.TryParse(ConfigurationManager.AppSettings["TextLogging"], out bool result) && result;
            if (textLoggingEnabled)
            {
                string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }
        }

        public static void CheckForUpdates(Window owner)
        {
            try
            {
                AutoUpdater.SetOwner(owner);
                App.LogDebug($"Starting Autoupdate, checking: {Settings.Default.UpdateUrl}");
                AutoUpdater.InstallationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZeeOgre", "LoadOrderManager", "AutoUpdater");
                App.LogDebug($"Autoupdate saving to: {AutoUpdater.InstallationPath}");
                AutoUpdater.ReportErrors = true;
                AutoUpdater.Synchronous = true;
                AutoUpdater.Start(Settings.Default.UpdateUrl);
                App.LogDebug("Autoupdate complete.");
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Error during auto-check: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static T? GetAssemblyAttribute<T>(Assembly assembly) where T : Attribute
        {
            return (T?)Attribute.GetCustomAttribute(assembly, typeof(T));
        }

        private static void SetProbingPaths()
        {
            string probingPaths = Settings.Default.ProbingPaths;
            AppDomain.CurrentDomain.SetData("PROBING_DIRECTORIES", probingPaths);
        }
    }
}
