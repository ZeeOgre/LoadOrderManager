using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using ZO.LoadOrderManager;
using Timer = System.Timers.Timer;

namespace ZO.LoadOrderManager
{

    public partial class LoadOrderWindowViewModel : INotifyPropertyChanged
    {

        public ICommand EditGroupSetCommand { get; }
        public ICommand RemoveGroupSetCommand { get; }
        public ICommand EditLoadOutCommand { get; }
        public ICommand RemoveLoadOutCommand { get; }
        public ICommand AddNewLoadOutCommand { get; }



        private void ExecuteEditGroupSetCommand(object parameter)
        {
            // Implementation of EditGroupSetCommand
        }

        private bool CanExecuteEditGroupSetCommand(object parameter)
        {
            // Logic to determine if EditGroupSetCommand can execute
            return true;
        }

        private void ExecuteRemoveGroupSetCommand(object parameter)
        {
            // Implementation of RemoveGroupSetCommand
        }

        private bool CanExecuteRemoveGroupSetCommand(object parameter)
        {
            // Logic to determine if RemoveGroupSetCommand can execute
            return true;
        }

        private void ExecuteEditLoadOutCommand(object parameter)
        {
            // Implementation of EditLoadOutCommand
        }

        private bool CanExecuteEditLoadOutCommand(object parameter)
        {
            // Logic to determine if EditLoadOutCommand can execute
            return true;
        }

        private void ExecuteRemoveLoadOutCommand(object parameter)
        {
            // Implementation of RemoveLoadOutCommand
        }

        private bool CanExecuteRemoveLoadOutCommand(object parameter)
        {
            // Logic to determine if RemoveLoadOutCommand can execute
            return true;
        }

        private void ExecuteAddNewLoadOutCommand(object parameter)
        {
            // Implementation of AddNewLoadOutCommand
        }

        private bool CanExecuteAddNewLoadOutCommand(object parameter)
        {
            // Logic to determine if AddNewLoadOutCommand can execute
            return true;
        }
    }
}