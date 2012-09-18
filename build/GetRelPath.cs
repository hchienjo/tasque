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
				var fromPath = Path.GetFullPath (FromPath);
				var toPath = Path.GetFullPath (ToPath);
				RelativePath = MakeRelPath (fromPath, toPath);
			} catch (Exception ex) {
				Log.LogErrorFromException (ex, true);
				return false;
			}
			return true;
		}

        string MakeRelPath (string fromPath, string toPath)
		{
			if (!Path.IsPathRooted (fromPath) || !Path.IsPathRooted (toPath))
				throw new Exception ("All paths must be absolute.");

			// normalize
			fromPath = Path.GetFullPath (fromPath).TrimEnd (Path.DirectorySeparatorChar);
			toPath = Path.GetFullPath (toPath).TrimEnd (Path.DirectorySeparatorChar);
			
			var fromPieces = fromPath.Split (Path.DirectorySeparatorChar);
			var toPieces = toPath.Split (Path.DirectorySeparatorChar);

			var fromBranch = new StringBuilder ();
			var toBranch = new StringBuilder ();
			var root = new StringBuilder ();

			var longerPieces = fromPieces.Length > toPieces.Length ? fromPieces : toPieces;

			var isRootFinished = false;
			for (int i = 0; i < longerPieces.Length; i++) {
				if ((i < fromPieces.Length && i < toPieces.Length) && !isRootFinished
				    && fromPieces [i] == toPieces [i])
					root.Append (longerPieces [i] + Path.DirectorySeparatorChar);
				else {
					isRootFinished = true;

					if (i < fromPieces.Length)
						fromBranch.Append (".." + Path.DirectorySeparatorChar.ToString ());

					if (i < toPieces.Length)
						toBranch.Append (toPieces [i] + (i == toPieces.Length - 1 ? ""
							: Path.DirectorySeparatorChar.ToString ()));
				}
			}

			return fromBranch.ToString () + toBranch.ToString ();
        }
	}
}
