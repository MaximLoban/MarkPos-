using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MarkPos.UI.Commands;

public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
    public bool CanExecute(object? _) => canExecute?.Invoke() ?? true;
    public void Execute(object? _) => execute();
}

public sealed class AsyncRelayCommand(Func<Task> execute) : ICommand
{
    private bool _busy;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
    public bool CanExecute(object? _) => !_busy;

    public async void Execute(object? _)
    {
        if (_busy) return;
        _busy = true;
        CommandManager.InvalidateRequerySuggested();
        try { await execute(); }
        finally
        {
            _busy = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}

public sealed class AsyncRelayCommand<T>(Func<T?, Task> execute) : ICommand
{
    private bool _busy;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
    public bool CanExecute(object? _) => !_busy;

    public async void Execute(object? parameter)
    {
        if (_busy) return;
        _busy = true;
        CommandManager.InvalidateRequerySuggested();
        try { await execute(parameter is T t ? t : default); }
        finally
        {
            _busy = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}