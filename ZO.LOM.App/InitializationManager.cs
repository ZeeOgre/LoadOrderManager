public static class InitializationManager
{
    private static readonly HashSet<string> InitializingComponents = new HashSet<string>();
    private static readonly object Lock = new object();
    private static Action<int, string>? _progressCallback;

    public static void StartInitialization(string componentName)
    {
        lock (Lock)
        {
            InitializingComponents.Add(componentName);
        }
    }

    public static void EndInitialization(string componentName)
    {
        lock (Lock)
        {
            InitializingComponents.Remove(componentName);
        }
    }

    public static bool IsAnyInitializing()
    {
        lock (Lock)
        {
            return InitializingComponents.Count > 0;
        }
    }

    public static void SetProgressCallback(Action<int, string> progressCallback)
    {
        _progressCallback = progressCallback;
    }

    public static void ReportProgress(int progress, string message)
    {
        _progressCallback?.Invoke(progress, message);
    }
}