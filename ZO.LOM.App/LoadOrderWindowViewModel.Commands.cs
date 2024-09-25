namespace ZO.LOM.App
{
    public partial class LoadOrderWindowViewModel
    {
        // Command properties
        public ICommand SaveCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand ImportPluginsCommand { get; }
        public ICommand SaveAsNewLoadoutCommand { get; }
        public ICommand OpenGameFolderCommand { get; }
        public ICommand OpenGameSaveFolderCommand { get; }
        public ICommand EditPluginsCommand { get; }
        public ICommand EditContentCatalogCommand { get; }
        public ICommand ImportContextCatalogCommand { get; }
        public ICommand SavePluginsCommand { get; }
        public ICommand EditHighlightedItemCommand { get; }
        public ICommand OpenAppDataFolderCommand { get; }
        public ICommand OpenGameLocalAppDataCommand { get; }
        public ICommand SettingsWindowCommand { get; }
        public ICommand ImportFromYamlCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand SaveLoadOutCommand { get; }
        public ICommand OpenGameSettingsCommand { get; }
        public ICommand OpenPluginEditorCommand { get; }
        public ICommand OpenGroupEditorCommand { get; }
        public ICommand RefreshDataCommand { get; }
        public ICommand CopyTextCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand ChangeGroupCommand { get; }
        public ICommand ToggleEnableCommand { get; }

        // Command methods
        private void Save(object? parameter) { }
        private void MoveUp() { }
        private void MoveDown() { }
        private void ImportPlugins(AggLoadInfo? aggLoadInfo = null, string? pluginsFile = null) { }
        private void SaveAsNewLoadout() { }
        private void OpenGameFolder() { }
        private void OpenGameSaveFolder() { }
        private void EditPlugins() { }
        private void EditContentCatalog() { }
        private void ImportContextCatalog() { }
        private void SavePlugins() { }
        private void EditHighlightedItem() { }
        private void OpenAppDataFolder() { }
        private void OpenGameLocalAppData() { }
        private void SettingsWindow() { }
        private void ImportFromYaml() { }
        private void OpenGameSettings() { }
        private void OpenPluginEditor() { }
        private void OpenGroupEditor() { }
        private void RefreshData() { }
        private void CopyText() { }
        private void Delete() { }
        private void ChangeGroup(object parameter) { }
        private void ToggleEnable(object parameter) { }
    }
}
