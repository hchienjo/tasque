using System;

namespace Tasque
{
	public interface INativeApplication : IDisposable
	{
		string ConfDir { get; }

		void Exit (int exitcode);

		void Initialize (string [] args);

		void InitializeIdle ();

		void OpenUrl (string url);

		void QuitMainLoop ();

		void StartMainLoop ();

		event EventHandler Exiting;
	}
}
