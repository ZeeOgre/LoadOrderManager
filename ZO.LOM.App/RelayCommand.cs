using System;
using System.Windows.Input;

namespace ZO.LoadOrderManager
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            //App.LogDebug("RelayCommand created for " + execute.Method.Name);
        }

        public bool CanExecute(object? parameter)
        {
            var result = _canExecute?.Invoke(parameter) ?? true;
            //App.LogDebug($"CanExecute called for {parameter}: {result}");
            return result;
        }
        public void Execute(object? parameter)
        {
            try
            {
                _execute(parameter);
            }
            catch (Exception ex)
            {
                // Log or handle exceptions as needed
                // Example: Logger.LogError(ex);
                throw;
            }
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public class RelayCommandWithParameter : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool>? _canExecute;

        public RelayCommandWithParameter(Action<object> execute, Func<object, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter ?? new object()) ?? true;

        public void Execute(object? parameter)
        {
            try
            {
                _execute(parameter ?? new object());
            }
            catch (Exception ex)
            {
                // Log or handle exceptions as needed
                // Example: Logger.LogError(ex);
                throw;
            }
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter == null || parameter is T)
                return _canExecute?.Invoke((T)parameter) ?? true;
            return false;
        }

        public void Execute(object? parameter)
        {
            // Check for null parameter or incorrect type
            if (parameter == null)
            {
                // Handle null parameter if needed
                _execute(default(T)!); // or use a default value or skip execution
                return;
            }

            // Check if parameter is of the correct type
            if (parameter is T param)
            {
                try
                {
                    _execute(param);
                }
                catch (Exception ex)
                {
                    // Log or handle exceptions as needed
                    throw;
                }
            }
            else
            {
                // Log or handle unexpected type
                App.LogDebug($"Unexpected parameter type: {parameter.GetType().Name}, expected: {typeof(T).Name}");
            }
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
