using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ZO.LoadOrderManager
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;

            field = value;
            //Debug.WriteLine($"SETPROPERTY: Property '{propertyName}' changed to: {value}");
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
