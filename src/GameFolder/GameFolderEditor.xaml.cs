using System.IO;
using System.Windows;
using Forms = System.Windows.Forms;

namespace ZO.LoadOrderManager;

public partial class GameFolderEditor : Window
{
    public GameFolder GameFolder { get; }

    public GameFolderEditor(GameFolder gameFolder)
    {
        InitializeComponent();
        GameFolder = gameFolder;
        DisplayNameBox.Text = gameFolder.DisplayName;
        GameRootBox.Text = gameFolder.GameRoot;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog { InitialDirectory = GameRootBox.Text };
        if (dialog.ShowDialog() == Forms.DialogResult.OK) GameRootBox.Text = dialog.SelectedPath;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var root = GameRootBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(DisplayNameBox.Text) || !Directory.Exists(Path.Combine(root, "Data")))
        {
            MessageBox.Show("Enter a display name and select a game root containing a Data folder.", "Invalid game folder");
            return;
        }
        GameFolder.DisplayName = DisplayNameBox.Text.Trim();
        GameFolder.GameRoot = root;
        try { GameFolder.Save(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Unable to save game folder"); return; }
        DialogResult = true;
    }
}
