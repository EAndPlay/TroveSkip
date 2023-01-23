using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace TroveSkip
{
    public sealed class ObservableCollectionEx<T> : ObservableCollection<T> where T : INotifyPropertyChanged
    {
        public ObservableCollectionEx()
        {
            CollectionChanged += ObservableCollectionEx_CollectionChanged;
        }

        private void ObservableCollectionEx_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach(T item in e.OldItems)
                {
                    item.PropertyChanged -= EntityViewModelPropertyChanged;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach(T item in e.NewItems)
                {
                    item.PropertyChanged += EntityViewModelPropertyChanged;
                }     
            }       
        }

        private void EntityViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            OnCollectionChanged(args);
        }
    }
}