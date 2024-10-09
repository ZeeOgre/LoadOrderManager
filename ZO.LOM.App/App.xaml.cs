using AutoUpdaterDotNET;
using Microsoft.Win32;
using System.Configuration;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Windows;
using Windows.UI.ViewManagement;
using ZO.LoadOrderManager.Properties;

namespace ZO.LoadOrderManager
{
    /// <summary>
    /// longeraction logic for App.xaml
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

            try
            {
                base.OnStartup(e);
                SetProbingPaths();

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

        // Apply the ModernWpf theme
        //public void ApplyModernTheme()
        //{
        //    Application.Current.Dispatcher.Invoke(() =>
        //    {
        //        ModernWpf.ThemeManager.Current.ApplicationTheme = Config.Instance.DarkMode ? ModernWpf.ApplicationTheme.Dark : ModernWpf.ApplicationTheme.Light;
        //    });
        //}


        public void ApplyCustomTheme(bool? isDarkMode)
        {
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            ResourceDictionary colorDict = new ResourceDictionary();

            if (isDarkMode == null)
            {
                isDarkMode = IsSystemInDarkMode(); // Use system settings
            }

            // Load appropriate color dictionary
            if (isDarkMode == true)
            {
                colorDict.Source = new Uri($"/{assemblyName};component/Themes/ColorsDark.xaml", UriKind.Relative);
            }
            else
            {
                colorDict.Source = new Uri($"/{assemblyName};component/Themes/ColorsLight.xaml", UriKind.Relative);
            }

            // Clear existing dictionaries and load the new one
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(colorDict);

            // Also add the shared style dictionary (common between both modes)
            ResourceDictionary styleDict = new ResourceDictionary
            {
                Source = new Uri($"/{assemblyName};component/Themes/CommonStyle.xaml", UriKind.Relative)
            };
            Application.Current.Resources.MergedDictionaries.Add(styleDict);
        }




        // Helper method to check system's dark mode setting using the registry
        private bool IsSystemInDarkMode()
        {
            const string registryKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string registryValue = "AppsUseLightTheme";

            object value = Registry.GetValue(registryKey, registryValue, null);

            if (value != null && value is int intValue)
            {
                return intValue == 0; // 0 means dark mode, 1 means light mode
            }

            // Default to light mode if unable to detect
            return false;
        }


        private void HandleSettingsMode()
        {
            try
            {
                Config.InitializeNewInstance();
                //ApplyModernTheme();
                //ApplyCustomTheme(Config.Instance.DarkMode);

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
                // Show the loading window on the UI thread
                Dispatcher.Invoke(() =>
                {
                    loadingWindow = new LoadingWindow();
                    loadingWindow.Show();
                });

                // Set progress callback
                InitializationManager.SetProgressCallback((progress, message) =>
                {
                    try
                    {
                        if (Application.Current != null && Application.Current.Dispatcher != null && !Application.Current.Dispatcher.HasShutdownStarted)
                        {
                            App.LogDebug($"Setting progress: {progress} - {message}");
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                loadingWindow?.UpdateProgress(progress, message);
                            });
                        }
                    }
                    catch (TaskCanceledException ex)
                    {
                        App.LogDebug($"Task canceled during progress update: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        App.LogDebug($"Unexpected exception during progress update: {ex.Message}");
                    }
                });

                // Run initialization tasks in a background thread
                Task.Run(() =>
                {
                    try
                    {
                        App.LogDebug("Initializing configuration...");
                        InitializationManager.StartInitialization(nameof(Config));
                        Config.Initialize();
                        InitializationManager.ReportProgress(20, "Configuration initialized");
                        InitializationManager.EndInitialization(nameof(Config));

                        //ApplyModernTheme();
                        //ApplyCustomTreeViewTheme(Config.Instance.DarkMode);
                        //ApplyCustomTheme(null);

                        App.LogDebug("Initializing database manager...");
                        InitializationManager.StartInitialization(nameof(DbManager));
                        DbManager.Instance.Initialize();
                        InitializationManager.ReportProgress(40, "Database manager initialized");
                        InitializationManager.EndInitialization(nameof(DbManager));

                        App.LogDebug("Initializing file manager...");
                        InitializationManager.StartInitialization(nameof(FileManager));
                        FileManager.Initialize();
                        InitializationManager.ReportProgress(80, "File manager initialized");
                        InitializationManager.EndInitialization(nameof(FileManager));

                        // Show the main window on the UI thread
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
                                loadingWindow?.UpdateProgress(100, "LoadOrderWindow successfully shown");
                            }
                            catch (Exception ex)
                            {
                                App.LogDebug($"Exception while showing LoadOrderWindow: {ex.Message}");
                                _ = MessageBox.Show($"An error occurred while showing the main window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        });
                    }
                    catch (TaskCanceledException ex)
                    {
                        App.LogDebug($"Task canceled: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        App.LogDebug($"Exception in HandleNormalMode: {ex.Message}");
                        Dispatcher.Invoke(() =>
                        {
                            _ = MessageBox.Show($"An error occurred in normal mode: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            Shutdown();
                        });
                    }
                    finally
                    {
                        // Close the loading window on the UI thread
                        Dispatcher.InvokeAsync(() =>
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

        public static void LogDebug(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] long lineNumber = 0, [CallerMemberName] string memberName = "")
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
                using (var stream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine(logMessage + Environment.NewLine);
                }
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

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogDebug($"Unhandled domain exception: {ex.Message}");
                _ = MessageBox.Show($"Unhandled domain exception: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                LogDebug("Unhandled domain exception occurred.");
                _ = MessageBox.Show("Unhandled domain exception occurred.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogDebug($"Unhandled exception caught: {e.Exception.Message}");
            _ = MessageBox.Show($"Unhandled exception: {e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // Optionally set this to true to prevent app crash
        }


    }
}
