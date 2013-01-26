// Task.cs created with MonoDevelop
// User: boyd at 8:50 PM 2/10/2008

using System;
using RtmNet;
using System.Collections.Generic;

namespace Tasque.Backends.Rtm
{
	public class RtmTask : AbstractTask
	{
		private RtmBackend rtmBackend;
		private TaskState state;
		private RtmCategory category;
		private List<INote> notes;		

		TaskSeries taskSeries;
		Task task;
		
		/// <summary>
		/// Constructor that is created from an RTM Task Series
		/// </summary>
		/// <param name="taskSeries">
		/// A <see cref="TaskSeries"/>
		/// </param>
		public RtmTask(TaskSeries taskSeries, Task task, RtmBackend be, string listID)
		{
			this.taskSeries = taskSeries;
			this.rtmBackend = be;
			this.category = be.GetCategory(listID);
			this.task = task;
			
			if(CompletionDate == DateTime.MinValue )
				state = TaskState.Active;
			else
				state = TaskState.Completed;
			notes = new List<INote>();

			if (taskSeries.Notes.NoteCollection != null) {
				foreach(Note note in taskSeries.Notes.NoteCollection) {
					RtmNote rtmNote = new RtmNote(note);
					notes.Add(rtmNote);
				}
			}
		}
		
		#region Public Properties
		/// <value>
		/// Gets the id of the task
		/// </value>
		public override string Id
		{
			get { return task.TaskID; }
		}

		/// <value>
		/// Holds the name of the task
		/// </value>		
		public override string Name
		{
			get { return taskSeries.Name; }
			set {
				if (value != null) {
					OnPropertyChanging ("Name");
					taskSeries.Name = value.Trim ();
					rtmBackend.UpdateTaskName(this);
					OnPropertyChanged ("CompletionDate");
				}
			}
		}
		
		/// <value>
		/// Due Date for the task
		/// </value>
		public override DateTime DueDate
		{
			get { return task.Due; }
			set {
				OnPropertyChanging ("DueDate");
				task.Due = value;
				rtmBackend.UpdateTaskDueDate(this);
				OnPropertyChanged ("CompletionDate");
			}
		}
		

		/// <value>
		/// Due Date for the task
		/// </value>
		public string DueDateString
		{
			get {
				// Return the due date in UTC format
				string format = "yyyy-MM-ddTHH:mm:ssZ";
				string dateString = task.Due.ToUniversalTime ().ToString (format);
				return dateString;
			}
		}

		
		/// <value>
		/// Completion Date for the task
		/// </value>
		public override DateTime CompletionDate
		{
			get { return task.Completed; }
			set {
				OnPropertyChanging ("CompletionDate");
				task.Completed = value;
				OnPropertyChanged ("CompletionDate");
			}
		}
		
		/// <value>
		/// Returns if the task is complete
		/// </value>
		public override bool IsComplete
		{
			get { return state == TaskState.Completed; }
		}
		
		/// <value>
		/// Holds the priority of the task
		/// </value>
		public override TaskPriority Priority
		{
			get { 
				switch (task.Priority) {
					default:
					case "N":
						return TaskPriority.None;
					case "1":
						return TaskPriority.High;
					case "2":
						return TaskPriority.Medium;
					case "3":
						return TaskPriority.Low;
				}
			}
			set {
				OnPropertyChanging ("Priority");
				switch (value) {
					default:
					case TaskPriority.None:
						task.Priority = "N";
						break;
					case TaskPriority.High:
						task.Priority = "1";
						break;
					case TaskPriority.Medium:
						task.Priority = "2";
						break;
					case TaskPriority.Low:
						task.Priority = "3";
						break;
				}
				rtmBackend.UpdateTaskPriority(this);
				OnPropertyChanged ("CompletionDate");
			}
		}
		
		public string PriorityString
		{
			get { return task.Priority; }
		}		
		
		
		/// <value>
		/// Returns if the task has any notes
		/// </value>
		public override bool HasNotes
		{
			get { return (notes.Count > 0); }
		}
		
		/// <value>
		/// Returns if the task supports multiple notes
		/// </value>
		public override NoteSupport NoteSupport
		{
			get { return NoteSupport.Multiple; }
		}
		
		/// <value>
		/// Holds the current state of the task
		/// </value>
		public override TaskState State
		{
			get { return state; }
		}
		
		/// <value>
		/// Returns the category object for this task
		/// </value>
		public override ICategory Category
		{
			get { return category; } 
			set {
				OnPropertyChanging ("Category");
				RtmCategory rtmCategory = value as RtmCategory;
				rtmBackend.MoveTaskCategory(this, rtmCategory.ID);
				OnPropertyChanged ("CompletionDate");
			}
		}
		
		/// <value>
		/// Returns the notes associates with this task
		/// </value>
		public override List<INote> Notes
		{
			get { return notes; }
		}
		
		/// <value>
		/// Holds the current RtmBackend for this task
		/// </value>
		public RtmBackend RtmBackend
		{
			get { return this.rtmBackend; }
		}
		
		public string ID
		{
			get {return taskSeries.TaskID; }
		}
		
		public string SeriesTaskID
		{
			get { return taskSeries.TaskID; }
		}
		
		public string TaskTaskID
		{
			get { return task.TaskID; }
		}
		
		public string ListID
		{
			get { return category.ID; }
		}
		#endregion // Public Properties
		
		#region Public Methods
		/// <summary>
		/// Activates the task
		/// </summary>
		public override void Activate ()
		{
			Logger.Debug("Activating Task: " + Name);
			SetState (TaskState.Active);
			CompletionDate = DateTime.MinValue;
		}
		
		/// <summary>
		/// Completes the task
		/// </summary>
		public override void Complete ()
		{
			Logger.Debug("Completing Task: " + Name);
			SetState (TaskState.Completed);
			if(CompletionDate == DateTime.MinValue)
				CompletionDate = DateTime.Now;
		}
		
		/// <summary>
		/// Deletes the task
		/// </summary>
		public override void Delete ()
		{
			SetState (TaskState.Deleted);
			rtmBackend.DeleteTask (this);
		}
		
		/// <summary>
		/// Adds a note to a task
		/// </summary>
		/// <param name="note">
		/// A <see cref="INote"/>
		/// </param>
		public override INote CreateNote(string text)
		{
			RtmNote rtmNote;
			
			rtmNote = rtmBackend.CreateNote(this, text);
			notes.Add(rtmNote);
			
			return rtmNote;
		}
		
		/// <summary>
		/// Deletes a note from a task
		/// </summary>
		/// <param name="note">
		/// A <see cref="INote"/>
		/// </param>
		public override void DeleteNote(INote note)
		{
			RtmNote rtmNote = (note as RtmNote);
			
			foreach(RtmNote lRtmNote in notes) {
				if(lRtmNote.ID == rtmNote.ID) {
					notes.Remove(lRtmNote);
					break;
				}
			}
			rtmBackend.DeleteNote(this, rtmNote);
		}		

		/// <summary>
		/// Deletes a note from a task
		/// </summary>
		/// <param name="note">
		/// A <see cref="INote"/>
		/// </param>
		public override void SaveNote(INote note)
		{		
			rtmBackend.SaveNote(this, (note as RtmNote));
		}		

		#endregion // Public Methods

		void SetState (TaskState value)
		{
			if (value == state)
				return;
			OnPropertyChanging ("State");
			state = value;
			OnPropertyChanged ("State");
		}
	}
}
