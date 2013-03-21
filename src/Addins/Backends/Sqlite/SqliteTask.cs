// SqliteTask.cs created with MonoDevelop
// User: boyd at 8:50 PM 2/10/2008

using System;
using Tasque;
using System.Collections.Generic;
using Mono.Data.Sqlite;

namespace Tasque.Backends.Sqlite
{
	public class SqliteTask : Task
	{
		private SqliteBackend backend;

		public SqliteTask(SqliteBackend backend, string name)
		{
			this.backend = backend;
			Logger.Debug("Creating New Task Object : {0} (id={1})", name, Id);
			name = backend.SanitizeText (name);
			Name = name;
			string command = String.Format("INSERT INTO Tasks (Name, DueDate, CompletionDate, Priority, State, Category, ExternalID) values ('{0}','{1}', '{2}','{3}', '{4}', '{5}', '{6}'); SELECT last_insert_rowid();", 
								name, Database.FromDateTime (DueDate), Database.FromDateTime (CompletionDate),
								0, (int)State, 0, string.Empty);
			Id = backend.Database.ExecuteScalar (command).ToString ();
		}

		public SqliteTask (SqliteBackend backend, int id, string name,
		                   long dueDate, long completionDate, int priority, int state)
		{
			this.backend = backend;
			Id = id.ToString ();
			Name = name;
			DueDate = Database.ToDateTime (dueDate);
			CompletionDate = Database.ToDateTime (completionDate);
			Priority = (TaskPriority)priority;
			State = (TaskState)state;
		}
		
		#region Public Properties

		protected override void OnNameChanging (ref string newName)
		{
			newName = backend.SanitizeText (newName);
			base.OnNameChanging (ref newName);
		}

		protected override void OnNameChanged ()
		{
			var cmd = string.Format ("UPDATE Tasks set Name='{0}' where ID='{1}'", Name, Id);
			backend.Database.ExecuteScalar (cmd);
			base.OnNameChanged ();
		}

		protected override void OnDueDateChanged ()
		{
			var lngDueDate = Database.FromDateTime (DueDate);
			var cmd = string.Format ("UPDATE Tasks set DueDate='{0}' where ID='{1}'", lngDueDate, Id);
			backend.Database.ExecuteScalar (cmd);
			base.OnDueDateChanged ();
		}

		protected override void OnCompletionDateChanged ()
		{
			var lngCompletionDate = Database.FromDateTime (CompletionDate);
			var cmd = string.Format ("UPDATE Tasks set CompletionDate='{0}' where ID='{1}'", lngCompletionDate, Id);
			backend.Database.ExecuteScalar (cmd);
			base.OnCompleted ();
		}

		protected override void OnPriorityChanged ()
		{
			var intPriority = (int)Priority;
			var cmd = string.Format ("UPDATE Tasks set Priority='{0}' where ID='{1}'", intPriority, Id);
			backend.Database.ExecuteScalar (cmd);
			base.OnPriorityChanged ();
		}
		
		public override NoteSupport NoteSupport {
			get { return NoteSupport.Multiple; }
		}

		protected override void OnStateChanged ()
		{
			var intState = (int)State;
			var command = string.Format ("UPDATE Tasks set State='{0}' where ID='{1}'", intState, Id);
			backend.Database.ExecuteScalar (command);
			base.OnStateChanged ();
		}
		
		public override List<INote> Notes
		{
			get {
				List<INote> notes = new List<INote>();

				string command = String.Format("SELECT ID, Text FROM Notes WHERE Task='{0}'", Id);
				SqliteCommand cmd = backend.Database.Connection.CreateCommand();
				cmd.CommandText = command;
				SqliteDataReader dataReader = cmd.ExecuteReader();
				while(dataReader.Read()) {
					int taskId = dataReader.GetInt32(0);
					string text = dataReader.GetString(1);
					notes.Add (new SqliteNote (taskId, text));
				}

				return notes;
			}
		}		
		
		#endregion // Public Properties
		
		#region Public Methods

		protected override void OnDeleted ()
		{
			backend.DeleteTask (this);
			base.OnDeleted ();
		}
		
		public override INote CreateNote(string text)
		{
			Logger.Debug("Creating New Note Object : {0} (id={1})", text, Id);
			text = backend.SanitizeText (text);
			string command = String.Format("INSERT INTO Notes (Task, Text) VALUES ('{0}','{1}'); SELECT last_insert_rowid();", Id, text);
			int taskId = Convert.ToInt32 (backend.Database.ExecuteScalar(command));

			return new SqliteNote (taskId, text);
		}
		
		public override void DeleteNote(INote note)
		{
			SqliteNote sqNote = (note as SqliteNote);

 			string command = String.Format("DELETE FROM Notes WHERE ID='{0}'", sqNote.ID);
			backend.Database.ExecuteScalar(command);
		}

		public override void SaveNote(INote note)
		{
			SqliteNote sqNote = (note as SqliteNote);

			string text = backend.SanitizeText (sqNote.Text);
			string command = String.Format("UPDATE Notes SET Text='{0}' WHERE ID='{1}'", text, sqNote.ID);
			backend.Database.ExecuteScalar(command);
		}

		#endregion // Public Methods
	}
}
