//
// TasqueObjectCollection.cs
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
using System.Collections.ObjectModel;
using System.Linq;
using Tasque.Data;

namespace Tasque.Core.Impl
{
	public class TasqueObjectCollection<T, TCore, TContainer, TContainerRepo>
		: ObservableCollection<T>, IBackendDetachable
		where TCore : ITasqueCore
		where T : TCore, ITasqueObject
		where TContainerRepo : ICollectionRepository<TCore, TContainer>
		where TContainer : TasqueObject<TContainerRepo>, IContainer<T>
	{
		const string ItemExistsExMsg = "The specified item exists already.";

		public TasqueObjectCollection (TContainer container)
		{
			if (container == null)
				throw new ArgumentNullException ("container");
			this.container = container;
			IsBackendDetached = container.IsBackendDetached;

			foreach (var item in container.Repository.GetAll (container)) {
				var existingItem = ((IInternalContainee<TContainer, T>)item)
					.Containers.SelectMany (c => c.Items)
					.FirstOrDefault (i => i.Id == item.Id);
				if (existingItem == null)
					Add ((T)item);
				else {
					((IInternalTasqueObject)existingItem).Merge (item);
					Add (existingItem);
				}
			}
		}

		public bool SupportsSharingItemsWithOtherCollections {
			get {
				return container.Repository
					.SupportsSharingItemsWithOtherCollections;
			}
		}

		public bool IsBackendDetached { get; private set; }

		public void AttachBackend (ITasqueObject container)
		{
			IsBackendDetached = false;
			foreach (var item in this)
				((IBackendDetachable)item).AttachBackend (container);
		}

		public void DetachBackend (ITasqueObject container)
		{
			foreach (var item in this)
				((IBackendDetachable)item).DetachBackend (container);
			IsBackendDetached = true;
		}

		protected override void ClearItems ()
		{
			if (!IsBackendDetached)
				container.Repository.ClearAll (container);
			foreach (var item in this) {
				var containee = (IInternalContainee<TContainer, T>)item;
				containee.InternalContainers.Remove (container);
				if (containee.InternalContainers.Count == 0)
					((IBackendDetachable)item).DetachBackend (container);
			}
			base.ClearItems ();
		}

		protected override void InsertItem (int index, T item)
		{
			AddObject (item);
			base.InsertItem (index, item);
		}

		protected override void RemoveItem (int index)
		{
			var oldItem = this [index];
			RemoveObject (oldItem);
			base.RemoveItem (index);
		}

		protected override void SetItem (int index, T item)
		{
			var oldItem = this [index];
			RemoveObject (oldItem);
			AddObject (item);
			base.SetItem (index, item);
		}

		void AddObject (T item)
		{
			if (Contains (item))
				throw new ArgumentException (ItemExistsExMsg, "item");
			if (!IsBackendDetached)
				AddObjectToRepo (item);
			((IInternalContainee<TContainer, T>)item)
				.InternalContainers.Add (container);
		}

		void AddObjectToRepo (T item)
		{
			if (!SupportsSharingItemsWithOtherCollections)
				container.Repository.AddNew (container, item);
			else {
				var itemHasContainers = ((IInternalContainee<TContainer, T>)
				                         item).InternalContainers.Count > 0;
				if (itemHasContainers)
					container.Repository.Add (container, item);
				else
					container.Repository.AddNew (container, item);
			}
			((IBackendDetachable)item).AttachBackend (container);
		}

		void RemoveObject (T item)
		{
			var containee = (IInternalContainee<TContainer, T>)item;
			if (!IsBackendDetached) {
				container.Repository.Remove (container, item);
				if (containee.InternalContainers.Count == 1)
					((IBackendDetachable)item).DetachBackend (container);
			}
			containee.InternalContainers.Remove (container);
		}

		TContainer container;
	}
}
