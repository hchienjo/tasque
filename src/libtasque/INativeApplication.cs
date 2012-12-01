using System;
using System.Collections.Generic;
using Tasque.Backends;

namespace Tasque
{
	public interface INativeApplication : IDisposable
	{
		IList<IBackend> AvailableBackends { get; }
		IBackend Backend { get; set; }
		string ConfDir { get; }
		Preferences Preferences { get; }
		void Exit (int exitcode);
		void Initialize (string [] args);
		void OpenUrl (string url);
		void QuitMainLoop ();
		void StartMainLoop ();
		event EventHandler Exiting;
	}
}
