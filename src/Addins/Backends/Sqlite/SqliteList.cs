// SqliteCategory.cs created with MonoDevelop
// User: boyd at 9:06 AM 2/11/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using Tasque;

namespace Tasque.Backends.Sqlite
{
	public class SqliteList : TaskList
	{
		private int id;
		SqliteBackend backend;
		
		public int ID
		{
			get { return id; }
		}

		public override bool IsReadOnly { get { return false; } }
		
		public string ExternalID
		{
			get {
				string command = String.Format("SELECT ExternalID FROM Categories where ID='{0}'", id);
				return backend.Database.GetSingleString(command);
			}
			set {
				string command = String.Format("UPDATE Categories set ExternalID='{0}' where ID='{0}'", value, id);
				backend.Database.ExecuteScalar(command);
			}
		}
		
		public SqliteList (SqliteBackend backend, string name)
		{
			this.backend = backend;
			string command = String.Format("INSERT INTO Categories (Name, ExternalID) values ('{0}', '{1}'); SELECT last_insert_rowid();", name, string.Empty);
			this.id = Convert.ToInt32 (backend.Database.ExecuteScalar(command));
			Name = name;
			//Logger.Debug("Inserted taskList named: {0} with id {1}", name, id);
		}
		
		public SqliteList (SqliteBackend backend, int id, string name)
		{
			this.backend = backend;
			this.id = id;
			Name = name;
		}

		protected override void OnNameChanging (ref string newName)
		{
			newName = backend.SanitizeText (newName);
			base.OnNameChanging (ref newName);
		}
		
		protected override void OnNameChanged ()
		{
			var cmd = string.Format ("UPDATE Categories set Name='{0}' where ID='{0}'", Name, id);
			backend.Database.ExecuteNonQuery (cmd);
			base.OnNameChanged ();
		}

		protected override void OnAdded (Task newTask)
		{
			var cmd = string.Format ("UPDATE Tasks set Category='{0}' where ID='{1}'", id, newTask.Id);
			backend.Database.ExecuteNonQuery (cmd);
			base.OnAdded (newTask);
		}

		protected override void OnRemoved (Task oldTask)
		{
			// set cat to 1 for now, since this is always called in conjunction with Add
			var cmd = string.Format ("UPDATE Tasks set Category='{0}' where ID='{1}'", 1, oldTask.Id);
			backend.Database.ExecuteNonQuery (cmd);
			base.OnRemoved (oldTask);
		}
	}
}
