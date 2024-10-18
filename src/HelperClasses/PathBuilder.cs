using System.IO;

namespace ZO.LoadOrderManager
{
    public static class PathBuilder
    {
        //public static readonly string RepoFolder = Config.Instance.RepoFolder;
        //public static readonly string BackupFolder = Path.Combine(RepoFolder, "BACKUP");
        //public static readonly string ModStagingFolder = Config.Instance.ModStagingFolder;
        //public static readonly List<string> ValidStages = Config.Instance.ModStages.ToList();

        //public static string BuildPath(string PluginName, string? stage = null, bool isBackup = false, bool isDeploy = false)
        //{
        //    if (isBackup && isDeploy)
        //    {
        //        throw new ArgumentException("isBackup and isDeploy cannot both be true.");
        //    }

        //    if (stage != null && !ValidStages.Contains(stage))
        //    {
        //        throw new ArgumentException($"Invalid stage: {stage}");
        //    }

        //    if (stage != null && stage.StartsWith("#") && !isBackup)
        //    {
        //        throw new ArgumentException($"Stage {stage} is reserved for backup only.");
        //    }

        //    if (PluginName == null)
        //    {
        //        throw new ArgumentNullException(nameof(PluginName));
        //    }

        //    if (stage == null && !isDeploy)
        //    {
        //        // Return source path
        //        string sourceStage = ValidStages.FirstOrDefault(s => s.StartsWith("*")) ?? throw new InvalidOperationException("No source stage found.");
        //        return Path.Combine(RepoFolder, sourceStage.TrimStart('*'), PluginName);
        //    }

        //    if (isBackup)
        //    {
        //        return Path.Combine(BackupFolder, PluginName, stage.TrimStart('#'));
        //    }

        //    if (isDeploy)
        //    {
        //        return Path.Combine(ModStagingFolder, PluginName);
        //    }

        //    return Path.Combine(RepoFolder, stage, PluginName);
        //}

        //public static string GetBackupFolder(string PluginName)
        //{
        //    return Path.Combine(BackupFolder, PluginName);
        //}

        //public static string GetModStagingFolder(string PluginName)
        //{
        //    return Path.Combine(ModStagingFolder, PluginName);
        //}

        //public static string GetModSourceBackupFolder(string PluginName)
        //{
        //    string sourceStage = ValidStages.FirstOrDefault(s => s.StartsWith("*")) ?? throw new InvalidOperationException("No source stage found.");
        //    return Path.Combine(BackupFolder, PluginName, sourceStage.TrimStart('*'));
        //}

        //public static string GetDeployBackupFolder(string PluginName)
        //{
        //    return Path.Combine(BackupFolder, PluginName, "DEPLOYED");
        //}

        //public static string GetPackageDestination(string PluginName)
        //{
        //    return Path.Combine(RepoFolder, "NEXUS", PluginName);
        //}

        //public static string GetPackageBackup(string PluginName)
        //{
        //    return Path.Combine(BackupFolder, PluginName, "NEXUS");
        //}

        //public static string GetModStageFolder(string PluginName, string stage)
        //{
        //    return Path.Combine(RepoFolder, stage, PluginName);
        //}

        //public static string GetModStageBackupFolder(string PluginName, string stage)
        //{
        //    return Path.Combine(BackupFolder, PluginName, stage.TrimStart('#', '*'));
        //}

        public static string GetRelativePath(string relativeTo, string path)
        {
            // Ensure the relativeTo path ends with a directory separator
            if (!relativeTo.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                relativeTo += Path.DirectorySeparatorChar;
            }

            var uri = new Uri(relativeTo);
            var pathUri = new Uri(path);

            if (uri.Scheme != pathUri.Scheme)
            {
                return path; // Path can't be made relative.
            }

            Uri relativeUri = uri.MakeRelativeUri(pathUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (pathUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }
    }
}

