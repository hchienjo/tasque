//
// Task.cs
//
// Original header:
// AbstractTask.cs created with MonoDevelop
// User: boyd at 6:52 AM 2/12/2008
//
// Authors:
//       Unknown author
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Tasque.Data;

namespace Tasque.Core.Impl
{
	using NoteCollection =
		TasqueObjectCollection<INote, INoteCore, Task, ITaskRepository>;
	using TaskTaskCollection =
		TasqueObjectCollection<ITask, ITaskCore, Task, ITaskRepository>;

	public class Task : TasqueObject<ITaskRepository>, ITask,
		IInternalContainee<TaskList, Task>, IContainer<Note>,
		IContainer<Task>, IInternalContainee<Task, Task>
	{
		#region Static

		const string CannotBeDiscardedExMsg = "The current backend" +
			"doesn't allow tasks to be discarded.";

		public static Task CreateTask (string id, string text,
			ITaskRepository taskRepo, INoteRepository noteRepo)
		{
			return new Task (text, taskRepo, noteRepo) { Id = id };
		}

		public static Task CreateCompletedTask (
			string id, string text, DateTime completionDate,
			ITaskRepository taskRepo, INoteRepository noteRepo)
		{
			var task = CreateTask (id, text, taskRepo, noteRepo);
			task.State = TaskState.Completed;
			task.CompletionDate = completionDate;
			return task;
		}

		public static Task CreateDiscardedTask (string id, string text,
			ITaskRepository taskRepo, INoteRepository noteRepo)
		{
			var task = CreateTask (id, text, taskRepo, noteRepo);
			if (!task.SupportsDiscarding)
				throw new NotSupportedException (CannotBeDiscardedExMsg);
			task.State = TaskState.Discarded;
			return task;
		}

		#endregion

		public Task (string text, ITaskRepository taskRepo,
		             INoteRepository noteRepo) : base (taskRepo)
		{
			isBackendDetached = true;

			if (NoteSupport != NoteSupport.None) {
				if (noteRepo == null) {
					throw new ArgumentNullException ("noteRepo",
						"Since this task supports adding notes " +
						"an INoteRepository must be provided.");
				}
				this.noteRepo = noteRepo;
			}

			if (text == null)
				throw new ArgumentNullException ("text");
			Text = text;
		}

		#region Properties

		/// <value>
		/// A Task's text will be used to show the task in the main list window.
		/// </value>
		/// <summary>
		/// Gets or sets the text.
		/// </summary>
		public string Text {
			get { return text; }
			set {
				var val = value.Trim ();
				this.SetProperty<string, Task> ("Text", val, text,
					x => text = x, Repository.UpdateText);
			}
		}

		/// <value>
		/// A DueDate of DateTime.MinValue indicates that a due date is not set.
		/// </value>
		/// <summary>
		/// Gets or sets the due date.
		/// </summary>
		public DateTime DueDate {
			get { return dueDate; }
			set {
				this.SetProperty<DateTime, Task> ("DueDate", value, dueDate,
					x => dueDate = x, Repository.UpdateDueDate);
			}
		}

		/// <value>
		/// If set to CompletionDate.MinValue, the task has not been completed.
		/// </value>
		/// <summary>
		/// Gets the completion date.
		/// </summary>
		public DateTime CompletionDate {
			get { return completionDate; }
			private set {
				if (value == completionDate)
					return;

				Logger.Debug ("Setting new task completion date");
				OnPropertyChanging ("CompletionDate");
				completionDate = value;
				OnPropertyChanged ("CompletionDate");
			}
		}

		/// <value>
		/// This is a convenience property which to determine whether a task is
		/// completed.
		/// </value>
		/// <summary>
		/// Gets a value indicating whether this task is completed.
		/// </summary>
		public bool IsComplete { get { return state == TaskState.Completed; } }

		/// <value>
		/// Backends should, by default, set the priority of a task to
		/// TaskPriority.None.
		/// </value>
		/// <summary>
		/// Gets or sets the priority.
		/// </summary>
		public TaskPriority Priority {
			get { return priority; }
			set {
				this.SetProperty<TaskPriority, Task> ("Priority", value,
					priority, x => priority = x, Repository.UpdatePriority);
			}
		}

		/// <value>
		/// The state of the task.
		/// </value>
		/// <summary>
		/// Gets the state.
		/// </summary>
		public TaskState State {
			get { return state; }
			private set {
				if (value == state)
					return;

				Logger.Debug ("Setting new task state");
				OnPropertyChanging ("State");
				state = value;
				OnPropertyChanged ("State");
			}
		}

		public bool SupportsSharingNotesWithOtherTasks {
			get {
				return ((ICollectionRepository<INoteCore, ITaskCore>)
				        Repository).SupportsSharingItemsWithOtherCollections;
			}
		}
		
		/// <summary>
		/// Gets the type of note support of this task/backend.
		/// </summary>
		/// <value>
		/// The note support.
		/// </value>
		public NoteSupport NoteSupport {
			get { return Repository.NoteSupport; }
		}
		
		public INote Note {
			get {
				ThrowOnSingleNoteSupport ();
				return note;
			}
			set {
				ThrowOnSingleNoteSupport ();
				this.SetProperty<INoteCore, Task> (
					"Note", value, note, x => note = (INote)x, Repository.UpdateNote);
			}
		}

		/// <value>
		/// Indicates whether any notes exist in this task.
		/// </value>
		/// <summary>
		/// Gets a value indicating whether this instance has notes.
		/// </summary>
		public bool HasNotes {
			get {
				return NoteSupport != NoteSupport.None &&
					(notes != null ? notes.Count > 0 : false);
			}
		}

		/// <summary>
		/// Gets the notes associated with this task
		/// </summary>
		/// <value>
		/// The notes.
		/// </value>
		public ObservableCollection<INote> Notes {
			get {
				if (NoteSupport == NoteSupport.None) {
					throw new NotSupportedException (
						"This task doesn't support notes.");
				} else if (NoteSupport == NoteSupport.Single) {
					throw new NotSupportedException (
						"This task doesn't support multiple notes. " +
						"Use property Task.Note instead.");
				}
				return notes ?? (notes = new NoteCollection (this));
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance can be discarded.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance can be discarded; otherwise, <c>false</c>.
		/// </value>
		public bool SupportsDiscarding {
			get { return Repository.SupportsDiscarding; }
		}

		public bool SupportsNestedTasks {
			get { return Repository.SupportsNestedTasks; }
		}

		public bool SupportsSharingNestedTasksWithOtherTasks {
			get {
				return ((ICollectionRepository<ITaskCore, ITaskCore>)
				        Repository).SupportsSharingItemsWithOtherCollections;
			}
		}

		public bool HasNestedTasks {
			get {
				return SupportsNestedTasks &&
					(nestedTasks != null ? nestedTasks.Count > 0 : false);
			}
		}

		public ObservableCollection<ITask> NestedTasks {
			get {
				if (!SupportsNestedTasks)
					throw new NotSupportedException ("This task doesn't " +
					                                 "support nested tasks.");
				return nestedTasks ??
					(nestedTasks = new TaskTaskCollection (this));
			}
		}

		public override bool IsBackendDetached {
			get { return isBackendDetached; }
		}

		#endregion // Properties

		public override void AttachBackend (ITasqueObject container)
		{
			isBackendDetached = false;
			if (HasNotes)
				((NoteCollection)Notes).AttachBackend (this);
			if (HasNestedTasks)
				((TaskTaskCollection)NestedTasks).AttachBackend (this);
		}

		public override void DetachBackend (ITasqueObject container)
		{
			var noAttachedContainer = true;
			if (taskListContainers != null) {
				noAttachedContainer = !taskListContainers.Any (
					l => l != container && !l.IsBackendDetached);
			}
			if (noAttachedContainer && taskContainers != null) {
				noAttachedContainer = !taskContainers.Any (
					t => t != container && !t.IsBackendDetached);
			}

			if (noAttachedContainer) {
				if (HasNotes)
					((NoteCollection)Notes).DetachBackend (this);
				if (HasNestedTasks)
					((TaskTaskCollection)NestedTasks).DetachBackend (this);
				isBackendDetached = true;
			}
		}

		public INote CreateNote ()
		{
			return CreateNote (null);
		}

		public INote CreateNote (string text)
		{
			if (NoteSupport == NoteSupport.None)
				throw new NotSupportedException (
					"This task doesn't support notes.");
			var note = new Note (noteRepo) { Text = text };
			if (NoteSupport == NoteSupport.Single)
				Note = note;
			else
				Notes.Add (note);
			return note;
		}

		public ITask CreateNestedTask (string text)
		{
			if (!SupportsNestedTasks)
				throw new NotSupportedException (
					"This task doesn't support nested tasks.");
			var task = new Task (text, Repository, noteRepo);
			NestedTasks.Add (task);
			return task;
		}

		/// <summary>
		/// Activate (Reopen) a task that's Completed.
		/// </summary>
		public void Activate ()
		{
			if (State != TaskState.Completed)
				throw new InvalidOperationException ("Only tasks that have" +
					"been completed can be activated.");

			Logger.Debug ("Task.Activate ()");
			if (!IsBackendDetached)
				Repository.Activate (this);

			if (Activating != null)
				Activating (this, EventArgs.Empty);
			State = TaskState.Active;
			CompletionDate = DateTime.MinValue;
			if (Activated != null)
				Activated (this, EventArgs.Empty);
		}

		/// <summary>
		/// Mark a task as completed.
		/// </summary>
		/// <exception cref="System.InvalidOperationException">
		/// Thrown when the task is not active.
		/// </exception>
		public void Complete ()
		{
			if (State != TaskState.Active)
				throw new InvalidOperationException (
					"Only active tasks can be completed.");

			Logger.Debug ("Task.Complete ()");
			var completionDate = DateTime.Now;
			if (!IsBackendDetached)
				completionDate = Repository.Complete (this, completionDate);

			if (Completing != null)
				Completing (this, EventArgs.Empty);
			State = TaskState.Completed;
			CompletionDate = completionDate;
			if (Completed != null)
				Completed (this, EventArgs.Empty);
		}

		/// <summary>
		/// Discard a task. This method throws, if discarding is not supported.
		/// </summary>
		/// <exception cref="System.NotSupportedException">
		/// thrown when <see cref="CanBeDiscarded"/> is <c>false</c>.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// thrown when <see cref="IsComplete"/> is <c>true</c>.
		/// </exception>
		public void Discard ()
		{
			if (!SupportsDiscarding)
				throw new NotSupportedException (CannotBeDiscardedExMsg);
			if (IsComplete)
				throw new InvalidOperationException ("A complete task" +
				                                     "cannot be discarded.");

			Logger.Debug ("Task.Discard ()");
			if (!IsBackendDetached)
				Repository.Discard (this);
			if (Discarding != null)
				Discarding (this, EventArgs.Empty);
			State = TaskState.Discarded;
			if (Discarded != null)
				Discarded (this, EventArgs.Empty);
		}

		public event EventHandler Completing, Completed, Activating,
			Activated, Discarding, Discarded;

		public override void Refresh ()
		{
			// detach all from backend
//			foreach (var note in Notes)
//				note.DetachBackend ();
//
//			DetachBackend ();
//
//			var notes = Repository.GetNotes (this);
//			Notes.Clear ();
//			foreach (var note in notes)
//				Notes.Add (note);
//			
//			AttachBackend ();
//
//			foreach (var note in Notes)
//				note.AttachBackend ();
			throw new NotImplementedException ();
		}

		public override void Merge (ITasqueCore source)
		{
			var sourceTask = (ITaskCore)source;
			var wasBackendDetached = isBackendDetached;
			isBackendDetached = true;
			DueDate = sourceTask.DueDate;
			Priority = sourceTask.Priority;
			Text = sourceTask.Text;
			isBackendDetached = wasBackendDetached;
		}

		#region Explicit content
		Collection<TaskList> IInternalContainee<TaskList, Task>
			.InternalContainers {
			get { return TaskListContainers; }
		}

		Collection<Task> IInternalContainee<Task, Task>.InternalContainers {
			get { return TaskContainers; }
		}

		IEnumerable<ITaskListCore> IContainee<ITaskListCore>.Containers {
			get { return TaskListContainers; }
		}

		IEnumerable<TaskList> IContainee<TaskList>.Containers {
			get { return TaskListContainers; }
		}

		IEnumerable<ITaskCore> IContainee<ITaskCore>.Containers {
			get { return TaskContainers; }
		}

		IEnumerable<Task> IContainee<Task>.Containers {
			get { return TaskContainers; }
		}

		IEnumerable<Note> IContainer<Note>.Items {
			get {
				IEnumerable<INote> notes = this.notes;
				if (!HasNotes)
					notes = Enumerable.Empty<INote> ();
				foreach (var item in notes)
					yield return (Note)item;
			}
		}

		IEnumerable<Task> IContainer<Task>.Items {
			get {
				IEnumerable<ITask> tasks = nestedTasks;
				if (!HasNestedTasks)
					tasks = Enumerable.Empty<ITask> ();
				foreach (var item in tasks)
					yield return (Task)item;
			}
		}

		IEnumerable<ITaskListCore> ITaskCore.TaskListContainers {
			get { return TaskListContainers; }
		}

		IEnumerable<ITaskCore> ITaskCore.TaskContainers {
			get { return TaskContainers; }
		}
		#endregion

		Collection<TaskList> TaskListContainers {
			get {
				return taskListContainers ?? (
					taskListContainers = new Collection<TaskList> ());
			}
		}

		Collection<Task> TaskContainers {
			get {
				return taskContainers ?? (
					taskContainers = new Collection<Task> ());
			}
		}

		void ThrowOnSingleNoteSupport ()
		{
			if (NoteSupport == NoteSupport.None) {
				throw new NotSupportedException (
					"This task doesn't support notes.");
			} else if (NoteSupport == NoteSupport.Multiple) {
				throw new NotSupportedException (
					"This task supports multiple notes. Use " +
					"property Task.Notes instead.");
			}
		}

		bool isBackendDetached;
		string text;
		DateTime dueDate, completionDate;
		TaskPriority priority;
		TaskState state;
		INote note;
		NoteCollection notes;
		TaskTaskCollection nestedTasks;
		Collection<TaskList> taskListContainers;
		Collection<Task> taskContainers;
		INoteRepository noteRepo;
	}
}
