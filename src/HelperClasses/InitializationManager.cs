using System.Windows;

namespace ZO.LoadOrderManager
{

    public static class InitializationManager
    {
        private static readonly HashSet<string> InitializingComponents = new HashSet<string>();
        private static readonly object Lock = new object();
        private static Action<long, string>? _progressCallback;

        public static void StartInitialization(string componentName)
        {
            lock (Lock)
            {
                if (!InitializingComponents.Contains(componentName))
                {
                    _ = InitializingComponents.Add(componentName);
                }
            }
        }

        public static void EndInitialization(string componentName)
        {
            lock (Lock)
            {
                if (InitializingComponents.Contains(componentName))
                {
                    _ = InitializingComponents.Remove(componentName);
                }
            }
        }

        public static bool IsAnyInitializing()
        {
            lock (Lock)
            {
                return InitializingComponents.Count > 0;
            }
        }

        public static void PrintInitializingComponents()
        {
            lock (Lock)
            {
                if (InitializingComponents.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No Components Initializing");
                }
                else
                {
                    foreach (var component in InitializingComponents)
                    {
                        System.Diagnostics.Debug.WriteLine(component);
                    }
                }
            }
        }

        public static void SetProgressCallback(Action<long, string> progressCallback)
        {
            _progressCallback = progressCallback;
        }

        private static readonly Queue<(long progress, string message)> ProgressQueue = new Queue<(long, string)>();

        public static void ReportProgress(long progress, string message)
        {
            if (Application.Current == null)
            {
                // Log or set a breakpoint here to confirm the issue
                Console.WriteLine("Application.Current is null, queuing progress update.");
                ProgressQueue.Enqueue((progress, message));  // Queue the progress update
                return;  // Immediately return to avoid null reference exceptions
            }

            if (Application.Current.Dispatcher == null)
            {
                // Log or set a breakpoint here to detect if this is ever the case
                Console.WriteLine("Application.Current.Dispatcher is null, queuing progress update.");
                ProgressQueue.Enqueue((progress, message));  // Queue the progress update
                return;  // Immediately return to avoid null reference exceptions
            }

            // If Application and Dispatcher are valid, process the progress update
            ProcessQueuedProgress();

            Application.Current.Dispatcher.Invoke(() =>
            {
                _progressCallback?.Invoke(progress, message);
            });
        }

        // Process queued progress updates once Application.Current is ready
        private static void ProcessQueuedProgress()
        {
            while (ProgressQueue.Count > 0)
            {
                var (progress, message) = ProgressQueue.Dequeue();
                _progressCallback?.Invoke(progress, message);
            }
        }

    }
}
