//
// ICollectionRepository.cs
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
using System.Collections.Generic;

namespace Tasque.Data
{
	public interface ICollectionRepository<T, in TContainer> : IRepository
		where T : ITasqueCore
		where TContainer : ITasqueCore
	{
		/// <summary>
		/// Determines whether the specified collection instance can share
		/// items with other collections or if an item belongs to only one
		/// collection instance at most. NOTE: If this returns false for an
		/// instance (or for all instances of the type), the mehtod
		/// <see cref="Add (T, TContainer)"/> will never be called for this
		/// instance (or for all instances of the type). Instead, each addition
		/// of a new item will be made through
		/// <see cref="AddNew (T, TContainer)"/>.
		/// </summary>
		/// <returns>
		/// <c>true</c> if the specified instance can share items with other
		/// collections; otherwise, <c>false</c>.
		/// </returns>
		bool SupportsSharingItemsWithOtherCollections { get; }

		IEnumerable<T> GetAll (TContainer container);
		T GetBy (TContainer container, string id);
		
		/// <summary>
		/// Add an existing item to the container.
		/// </summary>
		/// <param name='item'>
		/// Item.
		/// </param>
		/// <param name='container'>
		/// Container.
		/// </param>
		void Add (TContainer container, T item);
		
		/// <summary>
		/// Add a new item to the container.
		/// </summary>
		/// <param name='item'>
		/// Item.
		/// </param>
		/// <param name='container'>
		/// Container.
		/// </param>
		void AddNew (TContainer container, T item);
		void Remove (TContainer container, T item);
		void ClearAll (TContainer container);
	}
}
