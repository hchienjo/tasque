//
// TasqueObject.cs
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
using System.ComponentModel;
using Tasque.Data;

namespace Tasque.Core.Impl
{
	public abstract class TasqueObject<TRepo> : IInternalTasqueObject,
		IBackendDetachable, IIdEditable<ITasqueObject>, INotifying
		where TRepo : IRepository
	{
		protected TasqueObject (TRepo repository)
		{
			if (repository == null)
				throw new ArgumentNullException ("repository");
			Repository = repository;
		}

		/// <summary>
		/// Gets the identifier. The id is managed by the backend and thus not
		/// editable from the front end.
		/// </summary>
		/// <value>
		/// The identifier.
		/// </value>
		public string Id { get; protected set; }

		public TRepo Repository { get; private set; }

		public abstract bool IsBackendDetached { get; }
		public abstract void DetachBackend (ITasqueObject container);
		public abstract void AttachBackend (ITasqueObject container);
		public abstract void Merge (ITasqueCore source);
		public abstract void Refresh ();

		protected void OnPropertyChanged (string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged (
					this, new PropertyChangedEventArgs (propertyName));
		}
		
		protected void OnPropertyChanging (string propertyName)
		{
			if (PropertyChanging != null)
				PropertyChanging (
					this, new PropertyChangingEventArgs (propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public event PropertyChangingEventHandler PropertyChanging;

		void INotifying.OnPropertyChanged (string propertyName)
		{
			OnPropertyChanged (propertyName);
		}
		
		void INotifying.OnPropertyChanging (string propertyName)
		{
			OnPropertyChanging (propertyName);
		}

		void IIdEditable<ITasqueObject>.SetId (string id)
		{
			Id = id;
		}
	}
}
