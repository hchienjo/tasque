// RtmCategory.cs created with MonoDevelop
// User: boyd at 9:06 AM 2/11/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//
using Tasque;
using RtmNet;

namespace Tasque.Backends.Rtm
{
	public class RtmList : TaskList
	{
		public RtmList (RtmBackend backend, List list)
		{
			if (backend == null)
				throw new System.ArgumentNullException ("backend");
			if (list == null)
				throw new System.ArgumentNullException ("list");
			this.backend = backend;
			this.list = list;
			Name = list.Name;
		}

		public string ID
		{
			get { return list.ID; }
		}

		public override bool IsReadOnly { get { return false; } }
    
		public int Deleted
		{
			get { return list.Deleted; }
		}

		public int Locked
		{
			get { return list.Locked; }
		}
    
		public int Archived
		{
			get { return list.Archived; }
		}

		public int Position
		{
			get { return list.Position; }
		}

		public int Smart
		{
			get { return list.Smart; }
		}

		protected override void OnAdded (Task newTask)
		{
			backend.MoveTaskTaskList ((RtmTask)newTask, ID);
			base.OnAdded (newTask);
		}

		RtmBackend backend;
		List list;
	}
}
