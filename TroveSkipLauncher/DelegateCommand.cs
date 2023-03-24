using System;
using System.Diagnostics;
using System.Windows.Input;

namespace TroveSkipLauncher
{
  public sealed class DelegateCommand : DelegateCommandBase, ICommand
  {
    private readonly Action _execute;
    private readonly Predicate<object> _canExecute;

    /// <summary>
    /// to initialize the DelegateCommand results in a command that can always execute.
    /// </summary>
    /// <param name="execute">The action to run when the command is executed.</param>
    public DelegateCommand(Action execute) : this(execute, null) {}

    /// <param name="execute">The action to run when the command is executed.</param>
    /// <param name="canExecute">The function to evaluate whether this command is executable.  If this
    /// parameter is null, the command is always executable.</param>
    public DelegateCommand(Action execute, Predicate<object> canExecute)
    {
      _execute = execute;
      _canExecute = canExecute;
    }

    /// <summary>
    /// Gets a value indicating whether this command is executable.
    /// </summary>
    /// <param name="parameter">A parameter to pass to the canExecute delegate (specified in the constructor).</param>
    /// <returns>True if the command is executable or if the canExecute given during initialization was null.</returns>
    [DebuggerStepThrough]
    public bool CanExecute(object parameter)
    {
      var canExecute = this._canExecute;
      return canExecute == null || canExecute(parameter);
    }

    /// <summary>Executes the command.</summary>
    /// <param name="parameter">A parameter to pass to the execute delegate (specified in the constructor).</param>
    public void Execute(object parameter) => _execute();
  }
}