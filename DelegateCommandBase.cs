using System;
using System.ComponentModel;
using System.Windows.Input;

namespace TroveSkipFramework
{
  public class DelegateCommandBase
  {
    private event EventHandler canExecuteChanged;

    /// <summary>Raises the CanExecuteChanged event.</summary>
    public void RaiseCanExecuteChanged()
    {
      var canExecuteChanged = this.canExecuteChanged;
      canExecuteChanged(this, EventArgs.Empty);
    }

    /// <summary>
    /// An event that is fired when the executable state of this command changes.
    /// Call RaiseCanExecuteChanged to force listeners to update.
    /// </summary>
    public event EventHandler CanExecuteChanged
    {
      add
      {
        CommandManager.RequerySuggested += value;
        canExecuteChanged += value;
      }
      remove
      {
        CommandManager.RequerySuggested -= value;
        canExecuteChanged -= value;
      }
    }

    /// <summary>
    ///  Adds a property upon which this command's CanExecute state depends.
    ///  When the property changes, this command will raise CanExecuteChanged
    /// </summary>
    /// <param name="source">The object instance whose properties will be observed</param>
    /// <param name="propertyName">The property to be observed</param>
    public void CanExecuteDependsOn(INotifyPropertyChanged source, string propertyName) => PropertyChangedEventManager.AddHandler(source, (EventHandler<PropertyChangedEventArgs>) ((_1, _2) => this.RaiseCanExecuteChanged()), propertyName);
  }
}
