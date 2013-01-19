//
// TreeModelListAdapter.cs
//
// Author:
//       Antonius Riha <antoniusriha@gmail.com>
//
// Copyright (c) 2012 Antonius Riha
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using GLib;

namespace Gtk.Tasque
{
	public class TreeModelListAdapter<T> : ListStore, IDisposable where T : INotifyPropertyChanged
	{
		public TreeModelListAdapter (IEnumerable<T> source) : base (typeof (T))
		{
			if (source == null)
				throw new ArgumentNullException ("source");
			var observableSource = source as INotifyCollectionChanged;
			if (observableSource == null)
				throw new ArgumentException ("source must be INotifyCollectionChanged");
			this.source = source;
			observableSource.CollectionChanged += HandleCollectionChanged;
			
			// fill ListStore and register change events from all items in source,
			// since we need to update the TreeView on a by-row basis
			foreach (var item in source) {
				AppendValues (item);
				item.PropertyChanged += HandlePropertyChanged;
			}
		}
		
		public void DisposeLocal ()
		{
			if (disposed)
				return;
			
			foreach (var item in source)
				item.PropertyChanged -= HandlePropertyChanged;
			
			disposed = true;
		}

		void HandlePropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			// get index of changed item
			var item = (T)sender;
			var index = 0;
			foreach (var element in source) {
				if (item.Equals (element))
					break;
				index++;
			}
			TreeIter iter;
			GetIter (out iter, new TreePath (new int [] { index }));
			SetValue (iter, 0, sender);
		}

		void HandleCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			TreeIter iter;
			//FIXME: Only handles add and remove actions
			switch (e.Action) {
			case NotifyCollectionChangedAction.Add:
				var newItem = (T)e.NewItems [0];
				iter = Insert (e.NewStartingIndex);
				SetValues (iter, newItem);
				newItem.PropertyChanged += HandlePropertyChanged;
				break;
			case NotifyCollectionChangedAction.Remove: 
				var oldItem = (T)e.OldItems [0];
				oldItem.PropertyChanged -= HandlePropertyChanged;
				GetIter (out iter, new TreePath (new int [] { e.OldStartingIndex }));
				Remove (ref iter);
				break;
			}
		}
		
		bool disposed;
		IEnumerable<T> source;
	}
}
