using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZO.LoadOrderManager
{
    public class LoadOrderItemViewModel : INotifyPropertyChanged
    {
        private string displayName;
        private bool isEnabled;
        private Plugin pluginData;
        private int? groupID;

        public string DisplayName
        {
            get => displayName;
            set
            {
                if (displayName != value)
                {
                    displayName = value;
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public ObservableCollection<LoadOrderItemViewModel> Children { get; set; } = new ObservableCollection<LoadOrderItemViewModel>();

        public EntityType EntityType { get; set; }

        public Plugin PluginData
        {
            get => pluginData;
            set
            {
                if (pluginData != value)
                {
                    pluginData = value;
                    OnPropertyChanged(nameof(PluginData));
                    GroupID = pluginData?.GroupID;
                }
            }
        }

        public int? GroupID
        {
            get => groupID;
            set
            {
                if (groupID != value)
                {
                    groupID = value;
                    OnPropertyChanged(nameof(GroupID));
                }
            }
        }

        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                if (isEnabled != value)
                {
                    isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ModGroup? FindModGroup(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                // Handle empty or whitespace displayName
                return null;
            }

            // Split the displayName to get groupName and description
            var parts = displayName.Split('|');
            if (parts.Length < 2)
            {
                // If only one part, search by groupName only
                var groupName = displayName.Trim();
                return AggLoadInfo.Instance.Groups.FirstOrDefault(g =>
                    string.Equals(g.GroupName?.Trim(), groupName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                var groupName = parts[0].Trim();
                var description = parts[1].Trim();

                // Search for the ModGroup using groupName and description
                return AggLoadInfo.Instance.Groups.FirstOrDefault(g =>
                    string.Equals(g.GroupName?.Trim(), groupName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(g.Description?.Trim(), description, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

}