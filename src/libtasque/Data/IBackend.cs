//
// IBackend.cs
//
// Author:
//       Antonius Riha <antoniusriha@gmail.com>
//
// Copyright (c) 2013 Antonius Riha
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
using Mono.Addins;

namespace Tasque.Data
{
	[TypeExtensionPoint (ExtensionAttributeType =
	                     typeof (BackendExtensionAttribute))]
	public interface IBackend : IDisposable,
		IRepositoryProvider<INoteRepository>,
		IRepositoryProvider<ITaskListRepository>,
		IRepositoryProvider<ITaskRepository>
	{
		bool IsConfigured { get; }
		bool IsInitialized { get; }
		IBackendPreferences Preferences { get; }

		/// <summary>
		/// Initializes the backend.
		/// </summary>
		/// <param name='preferences'>
		/// An object to access Tasque preferences.
		/// </param>
		/// <exception cref="T:Tasque.BackendInitializationException">
		/// thrown when the initialization of the backend fails.
		/// </exception>
		void Initialize (IPreferences preferences);

		/// <summary>
		/// Gets all task lists populated with tasks, which in turn are
		/// populated with notes and possibly nested tasks.
		/// </summary>
		/// <returns>
		/// The lists.
		/// </returns>
		/// <exception cref="T:Tasque.TransactionException">
		/// thrown when the transaction failed to commit on the backend
		/// </exception>
		IEnumerable<ITaskListCore> GetAll ();

		ITaskListCore GetBy (string id);
		
		/// <summary>
		/// Create the specified task list on the backend.
		/// </summary>
		/// <param name='item'>
		/// The list to create.
		/// </param>
		/// <exception cref="T:Tasque.TransactionException">
		/// thrown when the transaction failed to commit on the backend
		/// </exception>
		void Create (ITaskListCore taskList);
		
		/// <summary>
		/// Delete the specified task list.
		/// </summary>
		/// <param name='item'>
		/// The list.
		/// </param>
		/// <exception cref="T:Tasque.TransactionException">
		/// thrown when the transaction failed to commit on the backend
		/// </exception>
		void Delete (ITaskListCore taskList);

		event EventHandler Disposed;
		event EventHandler Initialized;
		event EventHandler NeedsConfiguration;
	}
}
