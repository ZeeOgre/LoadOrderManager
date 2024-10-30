using AutoUpdaterDotNET;
using ControlzEx.Theming;
using Microsoft.Win32;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Windows;
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

            try
            {



                App.LogDebug("OnStartup called");

                try
                {
                    base.OnStartup(e);
                    SetProbingPaths();
                    CheckForUpdates(null);

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
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"File not found: {ex.FileName}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }





        public void ApplyCustomTheme(bool isDarkMode)
        {
            var theme = isDarkMode ? ThemeManager.BaseColorDarkConst : ThemeManager.BaseColorLightConst;
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Remove existing Resource Dictionaries related to themes.
                var existingDictionaries = Application.Current.Resources.MergedDictionaries.ToList();
                foreach (var dictionary in existingDictionaries)
                {
                    if (dictionary.Source != null &&
                        (dictionary.Source.OriginalString.Contains("MaterialDesignTheme.Light.xaml") ||
                         dictionary.Source.OriginalString.Contains("MaterialDesignTheme.Dark.xaml") ||
                         dictionary.Source.OriginalString.Contains("MahApps.Metro;component/Styles/Themes/Light.Blue.xaml") ||
                         dictionary.Source.OriginalString.Contains("MahApps.Metro;component/Styles/Themes/Dark.Blue.xaml") ||
                         dictionary.Source.OriginalString.Contains("Themes/ColorsLight.xaml") ||
                         dictionary.Source.OriginalString.Contains("Themes/ColorsDark.xaml")))
                    {
                        _ = Application.Current.Resources.MergedDictionaries.Remove(dictionary);
                    }
                }

                // Load new theme dictionaries based on the selected mode
                var materialDesignResourcePath = isDarkMode
                    ? "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml"
                    : "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml";

                var mahAppsResourcePath = isDarkMode
                    ? "pack://application:,,,/MahApps.Metro;component/Styles/Themes/Dark.Blue.xaml"
                    : "pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Blue.xaml";

                var customColorResourcePath = isDarkMode
                    ? "pack://application:,,,/Themes/ColorsDark.xaml"
                    : "pack://application:,,,/Themes/ColorsLight.xaml";

                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(materialDesignResourcePath) });
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(mahAppsResourcePath) });
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(customColorResourcePath) });

                // Apply MahApps theme
                _ = ThemeManager.Current.ChangeThemeBaseColor(Application.Current, theme);
                _ = ThemeManager.Current.ChangeThemeColorScheme(Application.Current, "Blue");

                // Restart the application to apply the new theme
                //RestartApplication();
            });
        }




        //Helper method to check system's dark mode setting using the registry
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

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ApplyCustomTheme(IsSystemInDarkMode());

                    App.LogDebug("Launching SettingsWindow in settings mode.");
                    var settingsWindow = new SettingsWindow(SettingsLaunchSource.CommandLine);
                    settingsWindow.Closed += (s, e) =>
                    {
                        RestartApplication();
                    };
                    settingsWindow.Show();
                });
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
                _ = Task.Run(() =>
                {
                    try
                    {
                        App.LogDebug("Initializing configuration...");
                        InitializationManager.StartInitialization(nameof(Config));
                        Config.Initialize();
                        InitializationManager.ReportProgress(10, "Configuration initialized");
                        InitializationManager.EndInitialization(nameof(Config));

                        ApplyCustomTheme(Config.Instance.DarkMode);

                        App.LogDebug("Initializing database manager...");
                        InitializationManager.StartInitialization(nameof(DbManager));
                        DbManager.Instance.Initialize();
                        InitializationManager.ReportProgress(20, "Database manager initialized");
                        InitializationManager.EndInitialization(nameof(DbManager));

                        App.LogDebug("Initializing file manager...");
                        InitializationManager.StartInitialization(nameof(FileManager));
                        FileManager.Initialize();
                        InitializationManager.ReportProgress(90, "File manager initialized");
                        InitializationManager.EndInitialization(nameof(FileManager));
                        
                        InitializationManager.PrintInitializingComponents();

                        // Show the main window on the UI thread
                        Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                if (_mainWindow == null)
                                {
                                    App.LogDebug("Creating a new instance of LoadOrderWindow...");
                                    _mainWindow = LoadOrderWindow.Instance;
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
                        _ = Dispatcher.InvokeAsync(() =>
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

        public static void RestartApplication()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var exeName = Process.GetCurrentProcess().MainModule.FileName;
                _ = Process.Start(exeName);
                Application.Current.Shutdown();
            });
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
                using var stream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream);
                writer.WriteLine(logMessage + Environment.NewLine);
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
    public static class ThemeHelper
    {
        public static void ApplyTheme(bool isDarkMode)
        {
            var theme = isDarkMode ? "BaseDark" : "BaseLight";
            var accent = "Blue";
            _ = ThemeManager.Current.ChangeTheme(Application.Current, $"{theme}.{accent}");
        }
    }
}
