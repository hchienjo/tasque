//
// PreferencesKeys.cs
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

namespace Tasque
{
	public static class PreferencesKeys
	{
		public const string AuthTokenKey = "AuthToken";
		public const string CurrentBackend = "CurrentBackend";
		public const string InactivateTimeoutKey = "InactivateTimeout";
		public const string SelectedCategoryKey = "SelectedCategory";
		public const string ParseDateEnabledKey = "ParseDateEnabled";
		public const string TodayTaskTextColor = "TodayTaskTextColor";
		public const string OverdueTaskTextColor = "OverdueTaskTextColor";
		
		/// <summary>
		/// A list of category names to show in the TaskWindow when the "All"
		/// category is selected.
		/// </summary>
		public const string HideInAllCategory = "HideInAllCategory";
		public const string ShowCompletedTasksKey = "ShowCompletedTasks";
		public const string UserNameKey = "UserName";
		public const string UserIdKey = "UserID";
		
		/// <summary>
		/// This setting allows a user to specify how many completed tasks to
		/// show in the Completed Tasks Category.  The setting should be one of:
		/// "Yesterday", "Last7Days", "LastMonth", "LastYear", or "All".
		/// </summary>
		public const string CompletedTasksRange = "CompletedTasksRange";
	}
}
