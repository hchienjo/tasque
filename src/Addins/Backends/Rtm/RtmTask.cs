// Task.cs created with MonoDevelop
// User: boyd at 8:50 PM 2/10/2008

using System;
using RtmNet;
using System.Collections.Generic;

namespace Tasque.Backends.Rtm
{
	public class RtmTask : Task
	{
		private RtmBackend rtmBackend;
		private RtmList list;
		private List<INote> notes;

		TaskSeries taskSeries;
		RtmNet.Task task;
		
		/// <summary>
		/// Constructor that is created from an RTM Task Series
		/// </summary>
		/// <param name="taskSeries">
		/// A <see cref="TaskSeries"/>
		/// </param>
		public RtmTask(TaskSeries taskSeries, RtmNet.Task task, RtmBackend be, string listID)
		{
			this.taskSeries = taskSeries;
			this.rtmBackend = be;
			this.list = be.GetTaskList(listID);
			this.task = task;

			Id = task.TaskID;
			Name = taskSeries.Name;
			DueDate = task.Due;
			CompletionDate = task.Completed;
			if (CompletionDate == DateTime.MinValue)
				State = TaskState.Active;
			else
				State = TaskState.Completed;
			Priority = GetPriority ();
			notes = new List<INote>();

			if (taskSeries.Notes.NoteCollection != null) {
				foreach(var note in taskSeries.Notes.NoteCollection) {
					RtmNote rtmNote = new RtmNote(note);
					notes.Add(rtmNote);
				}
			}
		}
		
		#region Public Properties

		protected override void OnNameChanged ()
		{
			taskSeries.Name = Name;
			rtmBackend.UpdateTaskName (this);
			base.OnNameChanged ();
		}

		protected override void OnDueDateChanged ()
		{
			task.Due = DueDate;
			rtmBackend.UpdateTaskDueDate (this);
			base.OnDueDateChanged ();
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

		protected override void OnCompletionDateChanged ()
		{
			task.Completed = CompletionDate;
			base.OnCompletionDateChanged ();
		}

		protected override void OnPriorityChanged ()
		{
			switch (Priority) {
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
			rtmBackend.UpdateTaskPriority (this);
			base.OnPriorityChanged ();
		}
		
		public string PriorityString
		{
			get { return task.Priority; }
		}

		public override NoteSupport NoteSupport {
			get { return NoteSupport.Multiple; }
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
			get { return list.ID; }
		}
		#endregion // Public Properties
		
		#region Public Methods
		protected override void OnActivated ()
		{
			rtmBackend.UpdateTaskActive (this);
			base.OnActivated ();
		}

		protected override void OnCompleted ()
		{
			rtmBackend.UpdateTaskCompleted (this);
			base.OnCompleted ();
		}

		protected override void OnDeleted ()
		{
			rtmBackend.DeleteTask (this);
			base.OnDeleted ();
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

		TaskPriority GetPriority ()
		{
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

		#endregion // Public Methods
	}
}
