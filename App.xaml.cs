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
            Version = Settings.Default.version ?? "0.0.0.0";
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
            base.OnStartup(e);
            try
            {
                LogDebug("OnStartup called");
                SetProbingPaths();
			
                // Determine if we're in settings mode or normal mode
                if (e.Args.Contains("--settings"))
                {
                    LogDebug("--settings argument detected. Entering settings mode.");
                    IsSettingsMode = true;
                    HandleSettingsMode();
                }
                else
                {
                    LogDebug("No --settings argument detected. Entering normal mode.");
                    HandleNormalMode();
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Exception during startup: {ex.Message}");
                MessageBox.Show($"An error occurred during startup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }


        }



        public void ApplyCustomTheme(bool isDarkMode)
        {
            var theme = isDarkMode ? ThemeManager.BaseColorDarkConst : ThemeManager.BaseColorLightConst;

            if (Application.Current != null && Application.Current.Dispatcher != null)
            {
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
                });
            }
            else
            {
                // Log or handle the case where Application.Current is null
                LogDebug("Application.Current is null. Cannot apply custom theme.");
            }
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

                    // Use ShowDialog to wait for the window to be closed
                    var result = settingsWindow.ShowDialog();
                    App.LogDebug($"Settings window dialog result: {result}");
                    
                    if (result == true)  // If DialogResult was set to true (e.g., Save was clicked)
                    {
                        App.LogDebug("Settings saved successfully.");
                        RestartApplication();
                        //Shutdown();
                        //HandleNormalMode();  // Transition to normal mode
                    }
                    else
                    {
                        App.LogDebug("Settings window closed without saving.");
                        // Only shut down if there is no configuration to proceed with
                        if (Config.Instance == null)
                        {
                            App.LogDebug("Shutting down due to missing or invalid configuration.");
                            Shutdown();  // App can't continue without valid settings
                        }
                        else
                        {
                            App.LogDebug("Proceeding without re-launching normal mode.");
                        }
                    }
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
                Dispatcher.Invoke(() =>
                {
                    // Show the loading window
                    loadingWindow = new LoadingWindow();
                    loadingWindow.Show();
                });

                // Set progress callback to update the loading window
                InitializationManager.SetProgressCallback((progress, message) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        loadingWindow?.UpdateProgress(progress, message);
                    });
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
                        if (Config.Instance.AutoCheckForUpdates) CheckForUpdates(null);

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
                    if (_mainWindow == null)
                    {
                        LogDebug("Creating a new instance of LoadOrderWindow...");
                        _mainWindow = LoadOrderWindow.Instance;
                    }

                    _mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    _mainWindow.Show();

                    LogDebug("LoadOrderWindow successfully shown.");
                    loadingWindow?.UpdateProgress(100, "LoadOrderWindow successfully shown");
                });
            }

                    catch (Exception ex)
                    {
                        LogDebug($"Exception in HandleNormalMode: {ex.Message}");
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"An error occurred in normal mode: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            Shutdown();
                        });
                    }
                    finally
                    {
                        // Ensure the loading window is closed after initialization
                        Dispatcher.Invoke(() =>
                        {
                            loadingWindow?.Close();
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                LogDebug($"Exception in HandleNormalMode: {ex.Message}");
                MessageBox.Show($"An error occurred in normal mode: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        public static void RestartDialog(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var result = MessageBox.Show(message, "Restart Required", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    RestartApplication();
                }
            });
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

            // Always log to the debug output when logging is off
            Debug.WriteLine(logMessage);  // This will write to the Debug Console in Visual Studio

            bool textLoggingEnabled = bool.TryParse(ConfigurationManager.AppSettings["TextLogging"], out bool result) && result;

            if (textLoggingEnabled)
            {
                // Log to a file when text logging is enabled
                LogFileMethod(logMessage);
            }
            else
            {
                // Log to the console when text logging is disabled (optional)
                Console.WriteLine(logMessage);
            }
        }

        private static void LogFileMethod(string logMessage)
        {
            Console.WriteLine(logMessage);  // Output to console, in case it's needed

            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
            using var stream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream);
            writer.WriteLine(logMessage + Environment.NewLine);
        }


        public static void CheckForUpdates(Window owner)
        {
            try
            {
                AutoUpdater.SetOwner(owner);
                App.LogDebug($"Starting Autoupdate, checking: {Settings.Default.UpdateUrl}");
                AutoUpdater.InstallationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZeeOgre", "LoadOrderManager", "AutoUpdater");
                App.LogDebug($"Autoupdate saving to: {AutoUpdater.InstallationPath}");
                AutoUpdater.ReportErrors = false;
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
