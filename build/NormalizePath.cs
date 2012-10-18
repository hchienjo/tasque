// 
// NormalizePath.cs
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
using IO = System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Tasque.Build
{
	public class NormalizePath : Task
	{
		[Required]
		[Output]
		public string Path { get; set; }

		public override bool Execute ()
		{
			try {
				Path = InternalNormalizePath (Path);
			} catch (Exception ex) {
				Log.LogErrorFromException (ex, true);
				return false;
			}
			return true;
		}

		internal static string InternalNormalizePath (string path)
		{
			if (path == null)
				throw new ArgumentNullException ("path");

			if (IO.Path.IsPathRooted (path))
				return IO.Path.GetFullPath (path).TrimEnd (IO.Path.DirectorySeparatorChar);

			var basePath = Environment.CurrentDirectory;
			var targetPath = IO.Path.GetFullPath (path);
			path = GetRelPath.InternalGetRelPath (basePath, targetPath);
			if (string.IsNullOrWhiteSpace (path))
				path = ".";
			return path;
		}
	}
}
