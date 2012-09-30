// 
// GetRelPath.cs
//  
// Author:
//       Antonius Riha <antoniusriha@gmail.com>
// 
// Copyright (c) 2012 Antonius Riha
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
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Tasque.Build
{
	public class GetRelPath : Task
	{
		[Required]
		public string FromPath { get; set; }

		[Required]
		public string ToPath { get; set; }

		[Output]
		public string RelativePath { get; set; }

		public override bool Execute ()
		{
			try {
				RelativePath = InternalGetRelPath (FromPath, ToPath);
			} catch (Exception ex) {
				Log.LogErrorFromException (ex, true);
				return false;
			}
			return true;
		}

        internal static string InternalGetRelPath (string fromPath, string toPath)
		{
			if (fromPath == null)
				throw new ArgumentNullException ("fromPath");
			if (toPath == null)
				throw new ArgumentNullException ("toPath");

			// normalize
			fromPath = NormalizePath.InternalNormalizePath (Path.GetFullPath (fromPath));
			toPath = NormalizePath.InternalNormalizePath (Path.GetFullPath (toPath));

			var basePathPieces = fromPath.Split (Path.DirectorySeparatorChar);
			var targetPathPieces = toPath.Split (Path.DirectorySeparatorChar);

			var max = Math.Min (basePathPieces.Length, targetPathPieces.Length);
			int i = 0;
			for (; i < max; i++)
				if (basePathPieces [i] != targetPathPieces [i])
					break;

			StringBuilder fromBranch = new StringBuilder (), toBranch = new StringBuilder ();
			for (int j = i; j < basePathPieces.Length; j++)
				fromBranch.Append (".." + Path.DirectorySeparatorChar.ToString ());
			for (int j = i; j < targetPathPieces.Length; j++)
				toBranch.Append (targetPathPieces [j] + Path.DirectorySeparatorChar.ToString ());

			var toBranchString = toBranch.ToString ();
			var result = fromBranch.Append (toBranchString).ToString ();
			return result.TrimEnd (Path.DirectorySeparatorChar);
        }
	}
}
