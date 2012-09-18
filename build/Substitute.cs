// 
// Substitute.cs
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
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Tasque.Build
{
	public class Substitute : Task
	{
		[Required]
		public ITaskItem [] DestinationFiles { get; set; }
		
		[Required]
		public ITaskItem [] SourceFiles { get; set; }
		
		[Required]
		public ITaskItem [] Substitutions { get; set; }
		
		public override bool Execute ()
		{
			try {
				CheckItems ();
				ParseSubstitutions ();
				
				for (int i = 0; i < SourceFiles.Length; i++) {
					var source = File.ReadAllText (SourceFiles [i].GetMetadata ("FullPath"));
					foreach (var subst in substitutions) {
						string value = subst [1];
						if (subst [2] == "path")
							value = File.ReadAllText (subst [1]);
						source = source.Replace (subst [0], value);
					}
					var path = DestinationFiles [i].GetMetadata ("FullPath");
					var dirPath = Path.GetDirectoryName (path);
					if (!Directory.Exists (dirPath))
						Directory.CreateDirectory (dirPath);
					File.WriteAllText (path, source);
				}
			} catch (Exception ex) {
				Log.LogErrorFromException (ex, true);
				return false;
			}
			return true;
		}
		
		void CheckItems ()
		{
			if (DestinationFiles.Length != SourceFiles.Length)
				throw new Exception ("The number of SourceFiles must be equal to " +
					"the number of DestinationFiles.");
		}
		
		void ParseSubstitutions ()
		{
			/* 
			 * <pattern>|<value>|<valueType> where <pattern> and
			 * <valueType> must not contain "|"
			 * 
			 * <valueType> is one of the following:
			 * 	text	- the <value> should be inserted directly
			 * 	path	- the <value> represents a path to a *text* file of which
			 * the contents will be the substitution value.
			 * 
			 * pattern can be any pattern, but usually is @pattern@
			 */
			
			// parse in files last to first, so that later added items
			// override more general earlier items.
			for (int i = Substitutions.Length - 1; i >= 0; i--) {
				var line = Substitutions [i].ItemSpec;
				var subst = new string [3];
				var firstDelimiter = line.IndexOf ('|');
				var lastDelimiter = line.LastIndexOf ('|');
				subst [0] = line.Substring (0, firstDelimiter);
				subst [1] = line.Substring (firstDelimiter + 1, lastDelimiter - firstDelimiter - 1);
				subst [2] = line.Substring (lastDelimiter + 1);
				substitutions.Add (subst);
			}
		}
		
		List<string []> substitutions = new List<string[]> ();
	}
}
