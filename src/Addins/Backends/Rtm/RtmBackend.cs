//
// RtmBackend.cs
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
using System.Collections.Generic;
using Tasque.Data;
using Tasque.Utils;

namespace Tasque.Backends.Rtm
{
	[BackendExtension ("Remember the Milk")]
	public class RtmBackend : IBackend
	{
		public bool IsConfigured { get; private set; }

		public bool IsInitialized { get; private set; }

		public IBackendPreferences Preferences {
			get { return new RtmPreferencesWidget (this, preferences); }
		}

		public TasqueObjectFactory Factory { get; private set; }
		
		public RtmNet.Rtm Rtm { get; private set; }

		public RtmTaskListRepository TaskListRepo { get; private set; }

		public string Timeline { get; private set; }

		public IEnumerable<ITaskListCore> GetAll ()
		{
			yield return allList;

			var lists = Rtm.ListsGetList ();
			foreach (var list in lists.listCollection) {
				ITaskListCore taskList;
				if (list.Smart == 1)
					taskList = Factory.CreateSmartTaskList (list.ID,
					                                        list.Name);
				else
					taskList = Factory.CreateTaskList (list.ID, list.Name);
				yield return taskList;
			}
		}

		public ITaskListCore GetBy (string id)
		{
			throw new NotImplementedException ();
		}

		public void Create (ITaskListCore taskList)
		{
			throw new NotImplementedException ();
		}

		public void Delete (ITaskListCore taskList)
		{
			throw new NotImplementedException ();
		}

		public void Initialize (IPreferences preferences)
		{
			if (preferences == null)
				throw new ArgumentNullException ("preferences");
			this.preferences = preferences;

			// *************************************
			// AUTHENTICATION to Remember The Milk
			// *************************************
			string authToken = preferences.Get (PreferencesKeys.AuthTokenKey);
			if (authToken != null) {
				Logger.Debug ("Found AuthToken, checking credentials...");
				try {
					Rtm = new RtmNet.Rtm (ApiKey, SharedSecret, authToken);
					rtmAuth = Rtm.AuthCheckToken (authToken);
					Timeline = Rtm.TimelineCreate ();
					Logger.Debug ("RTM Auth Token is valid!");
					Logger.Debug ("Setting configured status to true");
					IsConfigured = true;
				} catch (RtmNet.RtmApiException e) {
					preferences.Set (PreferencesKeys.AuthTokenKey, null);
					preferences.Set (PreferencesKeys.UserIdKey, null);
					preferences.Set (PreferencesKeys.UserNameKey, null);
					Rtm = null;
					rtmAuth = null;
					Logger.Error ("Exception authenticating, reverting "
					              + e.Message);
				} catch (RtmNet.ResponseXmlException e) {
					Rtm = null;
					rtmAuth = null;
					Logger.Error ("Cannot parse RTM response. " +
						"Maybe the service is down. " + e.Message);
				} catch (RtmNet.RtmWebException e) {
					Rtm = null;
					rtmAuth = null;
					Logger.Error ("Not connected to RTM, maybe proxy: #{0}",
					              e.Message);
				} catch (System.Net.WebException e) {
					Rtm = null;
					rtmAuth = null;
					Logger.Error ("Problem connecting to internet: #{0}",
					              e.Message);
				}
			}

			if (Rtm == null) {
				Rtm = new RtmNet.Rtm (ApiKey, SharedSecret);
				if (NeedsConfiguration != null)
					NeedsConfiguration (this, EventArgs.Empty);
				return;
			}

			FinishInitialization ();
		}

		public void FinishedAuth ()
		{
			rtmAuth = Rtm.AuthGetToken (frob);
			if (rtmAuth != null) {
				preferences.Set (PreferencesKeys.AuthTokenKey, rtmAuth.Token);
				if (rtmAuth.User != null) {
					preferences.Set (PreferencesKeys.UserNameKey,
					                 rtmAuth.User.Username);
					preferences.Set (PreferencesKeys.UserIdKey,
					                 rtmAuth.User.UserId);
				}
			}
			
			var authToken = preferences.Get (PreferencesKeys.AuthTokenKey);
			if (authToken != null) {
				Logger.Debug ("Found AuthToken, checking credentials...");
				try {
					Rtm = new RtmNet.Rtm (ApiKey, SharedSecret, authToken);
					rtmAuth = Rtm.AuthCheckToken (authToken);
					Timeline = Rtm.TimelineCreate ();
					Logger.Debug ("RTM Auth Token is valid!");
					Logger.Debug ("Setting configured status to true");
					IsConfigured = true;
					FinishInitialization ();
				} catch (Exception e) {
					Rtm = null;
					rtmAuth = null;				
					Logger.Error ("Exception authenticating, reverting"
					              + e.Message);
				}
			}
		}
		
		public string GetAuthUrl ()
		{
			frob = Rtm.AuthGetFrob ();
			string url = Rtm.AuthCalcUrl (frob, RtmNet.AuthLevel.Delete);
			return url;
		}

		public string EncodeTaskId (string taskSeriesId, string taskId)
		{
			return taskSeriesId + " " + taskId;
		}

		public void DecodeTaskId (ITaskCore task, out string taskSeriesId,
		                          out string taskId)
		{
			var ids = task.Id.Split ();
			taskSeriesId = ids [0];
			taskId = ids [1];
		}

		public void Dispose ()
		{
			if (disposed)
				return;

			Rtm = null;
			IsInitialized = false;
			disposed = true;
		}

		public event EventHandler Disposed, Initialized, NeedsConfiguration;

		#region Explicit content
		INoteRepository IRepositoryProvider<INoteRepository>.Repository {
			get { return noteRepo; }
		}
		
		ITaskListRepository IRepositoryProvider<ITaskListRepository>
			.Repository {
			get { return TaskListRepo; }
		}
		
		ITaskRepository IRepositoryProvider<ITaskRepository>.Repository {
			get { return taskRepo; }
		}
		#endregion

		void FinishInitialization ()
		{
			allList = new AllList (preferences);

			TaskListRepo = new RtmTaskListRepository (this);
			taskRepo = new RtmTaskRepository (this);
			noteRepo = new RtmNoteRepository (this);
			
			Factory = new TasqueObjectFactory (
				TaskListRepo, taskRepo, noteRepo);
			
			IsInitialized = true;
			if (Initialized != null)
				Initialized (null, null);
		}

		IPreferences preferences;
		AllList allList;

		INoteRepository noteRepo;
		ITaskRepository taskRepo;

		const string ApiKey = "b29f7517b6584035d07df3170b80c430",
			SharedSecret = "93eb5f83628b2066";

		string frob;
		RtmNet.Auth rtmAuth;

		bool disposed;
	}
}
