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
		Preferences Preferences { get; }
		void Exit (int exitcode);
		void Quit ();
		void Initialize (string [] args);
		void QuitMainLoop ();
		void ShowPreferences ();
		void StartMainLoop ();
		event EventHandler Exiting;
		event EventHandler BackendChanged;
	}
}
