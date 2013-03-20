//
// TaskList.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Tasque.Data;

namespace Tasque.Core.Impl
{
	using TaskListTaskCollection =
		TasqueObjectCollection<ITask, ITaskCore, TaskList, ITaskListRepository>;

	public class TaskList : TasqueObject<ITaskListRepository>, ITaskList,
		ICollection<ITask>, IContainer<Task>, INotifyCollectionChanged
	{
		const string ItemExistsExMsg = "The specified Task exists already.";

		public static TaskList CreateTaskList (
			string id, string name, ITaskListRepository taskListRepo,
			ITaskRepository taskRepo, INoteRepository noteRepo)
		{
			return new TaskList (name, taskListRepo, taskRepo, noteRepo) {
				Id = id
			};
		}

		public static ITaskListCore CreateSmartTaskList (
			string id, string name, ITaskListRepository taskListRepo)
		{
			return new TaskList (name, taskListRepo) { Id = id };
		}

		public TaskList (string name, ITaskListRepository taskListRepo,
			ITaskRepository taskRepo, INoteRepository noteRepo)
			: base (taskListRepo)
		{
			if (taskRepo == null)
				throw new ArgumentNullException ("taskRepo");
			this.taskRepo = taskRepo;
			this.noteRepo = noteRepo;

			isBackendDetached = true;
			InitName (name);
		}

		public TaskList (string name, ITaskListRepository taskListRepo,
		                 ITaskRepository taskRepo)
			: this (name, taskListRepo, taskRepo, null) {}

		TaskList (string name, ITaskListRepository taskListRepo)
			: base (taskListRepo)
		{
			isBackendDetached = true;
			ListType = TaskListType.Smart;
			InitName (name);
		}

		public int Count { get { return Tasks.Count; } }

		public bool IsReadOnly {
			get { return ListType == TaskListType.Smart; }
		}

		public bool SupportsSharingTasksWithOtherTaskLists {
			get { return Repository.SupportsSharingItemsWithOtherCollections; }
		}

		public bool CanChangeName {
			get { return Repository.CanChangeName (this); }
		}

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>
		/// The name.
		/// </value>
		/// <exception cref="System.InvalidOperationException">
		/// thrown when <see cref="CanChangeName"/> is <c>false</c>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// thrown when the value is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// thrown when the name is an empty string or consists only of white
		/// space characters.
		/// </exception>
		public string Name {
			get { return name; }
			set {
				if (!CanChangeName)
					throw new InvalidOperationException ("Cannot change " +
						"the name because CanChangeName is false.");
				if (value == null)
					throw new ArgumentNullException ("name");
				if (string.IsNullOrWhiteSpace (value))
					throw new ArgumentException (
						"Must not be empty or white space", "name");

				this.SetProperty<string, TaskList> ("Name", value,
					name, x => name = x, Repository.UpdateName);
			}
		}

		public TaskListType ListType { get; private set; }

		public override bool IsBackendDetached {
			get { return isBackendDetached; }
		}

		public override void AttachBackend (ITasqueObject container)
		{
			isBackendDetached = false;
			Tasks.AttachBackend (this);
		}

		public override void DetachBackend (ITasqueObject container)
		{
			Tasks.DetachBackend (this);
			isBackendDetached = true;
		}
		
		/// <summary>
		/// Creates a new task and adds it to this list.
		/// </summary>
		/// <returns>
		/// The task.
		/// </returns>
		/// <param name='name'>
		/// The text of the task.
		/// </param>
		public ITask CreateTask (string text)
		{
			ThrowIfIsReadOnly ();
			var task = new Task (text, taskRepo, noteRepo);
			Tasks.Add (task);
			return task;
		}

		public void Add (ITask item)
		{
			if (!IsBackendDetached)
				ThrowIfIsReadOnly ();
			Tasks.Add (item);
		}

		public void Clear ()
		{
			if (!IsBackendDetached)
				ThrowIfIsReadOnly ();
			Tasks.Clear ();
		}

		public bool Contains (ITask item)
		{
			return Tasks.Contains (item);
		}

		public void CopyTo (ITask [] array, int arrayIndex)
		{
			Tasks.CopyTo (array, arrayIndex);
		}

		public IEnumerator<ITask> GetEnumerator ()
		{
			return Tasks.GetEnumerator ();
		}

		public bool Remove (ITask item)
		{
			if (!IsBackendDetached)
				ThrowIfIsReadOnly ();
			return Tasks.Remove (item);
		}

		public override void Refresh ()
		{
//			// detach all from backend
//			foreach (var task in this) {
//				foreach (var note in task.Notes)
//					note.DetachBackend ();
//				task.DetachBackend ();
//			}
//			DetachBackend ();
//			
//			var tasks = Repository.GetTasks (this);
//			Clear ();
//			foreach (var task in Tasks)
//				Add (task);
//			
//			AttachBackend ();
//			foreach (var task in this) {
//				task.AttachBackend ();
//				foreach (var note in task.Notes)
//					note.AttachBackend ();
//			}
		}

		public override void Merge (ITasqueCore source)
		{
			var sourceTaskList = (ITaskListCore)source;
			var wasBackendDetached = isBackendDetached;
			isBackendDetached = true;
			if (CanChangeName)
				Name = sourceTaskList.Name;
			isBackendDetached = wasBackendDetached;
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged {
			add { Tasks.CollectionChanged += value; }
			remove { Tasks.CollectionChanged -= value; }
		}

		#region Explicit content
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		IEnumerable<Task> IContainer<Task>.Items {
			get {
				foreach (var item in this)
					yield return (Task)item;
			}
		}
		#endregion

		TaskListTaskCollection Tasks {
			get {
				return tasks ?? (tasks = new TaskListTaskCollection (this));
			}
		}
		
		void InitName (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			if (string.IsNullOrWhiteSpace (name))
				throw new ArgumentException (
					"Must not be empty or white space", "name");
			this.name = name;
		}

		void ThrowIfIsReadOnly ()
		{
			if (IsReadOnly)
				throw new InvalidOperationException (
					"This collection is read-only.");
		}

		bool isBackendDetached;
		string name;
		TaskListTaskCollection tasks;
		INoteRepository noteRepo;
		ITaskRepository taskRepo;
	}
}
