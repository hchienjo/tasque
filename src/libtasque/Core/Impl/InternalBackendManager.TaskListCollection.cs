//
// InternalBackendManager.TaskListCollection.cs
//
// Author:
//       Antonius Riha <antoniusriha@gmail.com>
//
// Copyright (c) 2013 Antonius Riha
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Tasque.Data;

namespace Tasque.Core.Impl
{
	public partial class InternalBackendManager
	{
		public class TaskListCollection : ObservableCollection<ITaskList>
		{
			const string ItemExistsExMsg = "The specified TaskList exists already.";

			public TaskListCollection ()
			{
				tasks = new ObservableCollection<ITask> ();
				Tasks = new ReadOnlyObservableCollection<ITask> (tasks);
			}

			public bool IsLoaded { get; private set; }

			public ReadOnlyObservableCollection<ITask> Tasks { get; private set; }

			public void LoadTaskLists (IBackend backend)
			{
				if (backend == null)
					throw new ArgumentNullException ("backend");
				this.backend = backend;

				if (IsLoaded)
					UnloadTaskLists ();
				IsLoaded = true;

				var i = 0;
				foreach (var item in backend.GetAll ())
					AddList (i++, (ITaskList)item, true);

				// enable backend propagation on all objects
				foreach (var list in this) {
					if (list.ListType == TaskListType.Regular)
						((IBackendDetachable)list).AttachBackend (null);
				}
			}

			public void UnloadTaskLists ()
			{
				if (!IsLoaded)
					return;
				IsLoaded = false;

				// disable backend propagation on all objects
				foreach (var list in this) {
					if (list.ListType == TaskListType.Regular)
						((IBackendDetachable)list).DetachBackend (null);
				}

				foreach (var item in this)
					RemoveList (0, item, true);
			}
			
			protected override void ClearItems ()
			{
				ThrowIfNotLoaded ();

				foreach (var item in this) {
					backend.Delete (item);
					item.CollectionChanged -= HandleTaskListChanged;
				}
				tasks.Clear ();
				base.ClearItems ();
			}
			
			protected override void InsertItem (int index, ITaskList item)
			{
				ThrowIfNotLoaded ();
				AddList (index, item, false);
			}
			
			protected override void RemoveItem (int index)
			{
				ThrowIfNotLoaded ();
				var oldList = this [index];
				RemoveList (index, oldList, false);
			}
			
			protected override void SetItem (int index, ITaskList item)
			{
				ThrowIfNotLoaded ();
				if (Contains (item))
					throw new ArgumentException (ItemExistsExMsg, "item");

				var oldList = this [index];
				backend.Delete (oldList);
				item.CollectionChanged -= HandleTaskListChanged;
				foreach (var task in item)
					RemoveTask (task);

				backend.Create (item);
				foreach (var task in item)
					AddTask (task);
				item.CollectionChanged += HandleTaskListChanged;

				base.SetItem (index, item);
			}

			void AddList (int index, ITaskList item, bool isLoading)
			{
				if (Contains (item))
					throw new ArgumentException (ItemExistsExMsg, "item");

				if (!isLoading) {
					backend.Create (item);
					((IBackendDetachable)item).AttachBackend (null);
				}

				foreach (var task in item)
					AddTask (task);
				item.CollectionChanged += HandleTaskListChanged;
				base.InsertItem (index, item);
			}

			void RemoveList (int index, ITaskList item, bool isUnloading)
			{
				if (!isUnloading) {
					((IBackendDetachable)item).DetachBackend (null);
					backend.Delete (item);
				}

				item.CollectionChanged -= HandleTaskListChanged;
				foreach (var task in item)
					RemoveTask (task);
				base.RemoveItem (index);
			}

			void HandleTaskListChanged (object sender, NotifyCollectionChangedEventArgs e)
			{
				switch (e.Action) {
				case NotifyCollectionChangedAction.Add:
					AddTask (e.NewItems [0] as ITask);
					break;
				case NotifyCollectionChangedAction.Remove:
					RemoveTask (e.OldItems [0] as ITask);
					break;
				case NotifyCollectionChangedAction.Replace:
					RemoveTask (e.OldItems [0] as ITask);
					AddTask (e.NewItems [0] as ITask);
					break;
				case NotifyCollectionChangedAction.Reset:
					foreach (var task in sender as ITaskList)
						RemoveTask (task);
					break;
				}
			}

			void AddTask (ITask task)
			{
				if (!tasks.Contains (task))
					tasks.Add (task);
			}

			void RemoveTask (ITask task)
			{
				if (this.Count (l => l.Contains (task)) == 1)
					tasks.Remove (task);
			}

			void ThrowIfNotLoaded ()
			{
				if (!IsLoaded)
					throw new InvalidOperationException ("This method can" +
						"only be called, when IsLoaded is true.");
			}
			
			ObservableCollection<ITask> tasks;
			IBackend backend;
		}
	}
}
