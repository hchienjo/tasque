// 
// Program.cs
//  
// Author:
//       Antonius Riha <antoniusriha@gmail.com>
// 
// Copyright (c) 2012-2013 Antonius Riha
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
using System.Runtime.InteropServices;
using System.Text;
using Mono.Unix.Native;
using Gtk.Tasque;

namespace Tasque
{
	class Program
	{
		#region Set process name
		// From Banshee: Hyena/ApplicationContext.cs
		[DllImport ("libc")] // Linux
		static extern int prctl (int option, byte [] arg2, IntPtr arg3, IntPtr arg4, IntPtr arg5);

		[DllImport ("libc")] // BSD
		static extern void setproctitle (byte [] fmt, byte [] str_arg);

		static void SetProcessName (string name)
		{
			if (Environment.OSVersion.Platform != PlatformID.Unix)
				return;
			
			try {
				var retVal = prctl (15 /* PR_SET_NAME */, Encoding.ASCII.GetBytes (name + "\0"),
				                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				if (retVal != 0)
					throw new ApplicationException ("Error setting process name: " +
					                                Stdlib.GetLastError ());
			} catch (EntryPointNotFoundException) {
				setproctitle (Encoding.ASCII.GetBytes ("%s\0"), Encoding.ASCII.GetBytes (name + "\0"));
			}
		}
		#endregion

		static void Main (string[] args)
		{
			try {
				lock (lockObject) {
					if (application != null)
						return;

					// Fix process name not set on Unix
					SetProcessName ("Tasque");
#if WIN
					application = new GtkWinApplication (args);
#else
					application = new GtkLinuxApplication (args);
#endif
				}
			} catch (Exception e) {
				Logger.Debug ("Exception is: {0}", e);
				application.Exit (-1);
			} finally {
				lock (lockObject) {
					if (application != null)
						application.Dispose ();
				}
			}
		}
		
		static GtkApplicationBase application;
		static object lockObject = new object ();
	}
}
