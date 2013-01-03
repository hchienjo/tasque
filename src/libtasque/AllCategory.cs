// AllCategory.cs created with MonoDevelop
// User: boyd at 3:45 PM 2/12/2008

using System;
using System.Collections.Generic;
using Mono.Unix;

namespace Tasque
{
	public class AllCategory : ICategory
	{
		// A "set" of categories specified by the user to show when the "All"
		// category is selected in the TaskWindow.  If the list is empty, tasks
		// from all categories will be shown.  Otherwise, only tasks from the
		// specified lists will be shown.
		List<string> categoriesToHide;
		
		public AllCategory (IPreferences preferences)
		{
			if (preferences == null)
				throw new ArgumentNullException ("preferences");
			categoriesToHide =
				preferences.GetStringList (PreferencesKeys.HideInAllCategory);
			categoriesToHide = preferences.GetStringList (PreferencesKeys.HideInAllCategory);
			preferences.SettingChanged += OnSettingChanged;
		}
		
		public string Name
		{
			get { return Catalog.GetString ("All"); }
		}
		
		public bool ContainsTask(ITask task)
		{
			// Filter out tasks based on the user's preferences of which
			// categories should be displayed in the AllCategory.
			ICategory category = task.Category;
			if (category == null)
				return true;
			
			//if (categoriesToHide.Count == 0)
			//	return true;
			
			return (!categoriesToHide.Contains (category.Name));
		}
		
		private void OnSettingChanged (IPreferences preferences, string settingKey)
		{
			if (settingKey.CompareTo (PreferencesKeys.HideInAllCategory) != 0)
				return;
			
			categoriesToHide =
				preferences.GetStringList (PreferencesKeys.HideInAllCategory);
		}
	}
}
