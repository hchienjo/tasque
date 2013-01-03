using System;
using System.Collections.Generic;
using Tasque.Backends;

namespace Tasque
{
	public interface INativeApplication : IDisposable
	{
		IList<IBackend> AvailableBackends { get; }
		IBackend Backend { get; set; }
		TaskGroupModel OverdueTasks { get; }
		TaskGroupModel TodayTasks { get; }
		TaskGroupModel TomorrowTasks { get; }
		string ConfDir { get; }
		IPreferences Preferences { get; }
		void Exit (int exitcode = 0);
		void Initialize (string [] args);
		void ShowPreferences ();
		void ShowAppNotification (string summary, string body);
		event EventHandler Exiting;
		event EventHandler BackendChanged;
	}
}
