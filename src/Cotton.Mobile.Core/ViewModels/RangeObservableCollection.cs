// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.ViewModels
{
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;

    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        public void ReplaceWith(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            List<T> replacement = items is List<T> list ? list : items.ToList();
            CheckReentrancy();

            Items.Clear();
            foreach (T item in replacement)
            {
                Items.Add(item);
            }

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
