// CompletedTaskGroup.cs created with MonoDevelop
// User: boyd at 12:34 AM 3/1/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Mono.Unix;
using Gtk;
using Gtk.Tasque;
using Tasque.Core;
using Tasque.Utils;

namespace Tasque
{
	public enum ShowCompletedRange : uint
	{
		Yesterday = 0,
		Last7Days,
		LastMonth,
		LastYear,
		All
	}
	
	public class CompletedTaskGroup : TaskGroup
	{
		ITaskList selectedTaskList;
		HScale rangeSlider;
		ShowCompletedRange currentRange;
		
		public CompletedTaskGroup (string groupName, DateTime rangeStart,
		                           DateTime rangeEnd, ICollection<ITask> tasks, GtkApplicationBase application)
			: base (groupName, rangeStart, rangeEnd, new CompletedTasksSortModel(tasks), application)
		{
			var preferences = application.Preferences;
			// Only hide this group, when ShowCompletedTasks is unset in prefs.
			// If it's set, don't hide this group when it's empty because then the range
			// slider won't appear and the user won't be able to customize the range.
			HideWhenEmpty = !preferences.GetBool (PreferencesKeys.ShowCompletedTasksKey);
			
			// track changes in prefs for ShowCompletedTasks setting
			preferences.SettingChanged += (prefs, key) => {
				switch (key) {
				case PreferencesKeys.ShowCompletedTasksKey:
					HideWhenEmpty = !prefs.GetBool (PreferencesKeys.ShowCompletedTasksKey);
					
					// refresh ui
					var cat = GetSelectedTaskList ();
					if (cat != null)
						Refilter (cat);
					break;
				case PreferencesKeys.SelectedTaskListKey:
					selectedTaskList = GetSelectedTaskList ();
					Refilter (selectedTaskList);
					break;
				}
			};
			
			CreateRangeSlider ();
			UpdateDateRanges ();
		}
		
		/// <summary>
		/// Create and show a slider (HScale) that will allow the user to
		/// customize how far in the past to show completed items.
		/// </summary>
		private void CreateRangeSlider ()
		{
			// There are five (5) different values allowed here:
			// "Yesterday", "Last7Days", "LastMonth", "LastYear", or "All"
			// Create the slider with 5 distinct "stops"
			rangeSlider = new HScale (0, 4, 1);
			rangeSlider.SetIncrements (1, 1);
			rangeSlider.WidthRequest = 100;
			rangeSlider.DrawValue = true;
			
			// TODO: Set the initial value and range
			string rangeStr =
				Application.Preferences.Get (PreferencesKeys.CompletedTasksRange);
			if (rangeStr == null) {
				// Set a default value of All
				rangeStr = ShowCompletedRange.All.ToString ();
				Application.Preferences.Set (PreferencesKeys.CompletedTasksRange,
											 rangeStr);
			}
			
			currentRange = ParseRange (rangeStr);
			rangeSlider.Value = (double)currentRange;
			rangeSlider.FormatValue += OnFormatRangeSliderValue;
			rangeSlider.ValueChanged += OnRangeSliderChanged;
			rangeSlider.Show ();
			
			this.ExtraWidget = rangeSlider;
		}

		protected override TreeModel CreateModel (DateTime rangeStart, DateTime rangeEnd,
		                                          ICollection<ITask> tasks)
		{
			Model = new CompletedTaskGroupModel (rangeStart, rangeEnd,
			                                     tasks, Application.Preferences);
			return new TreeModelListAdapter<ITask> (Model);
		}
		
		protected ITaskList GetSelectedTaskList ()
		{
			ITaskList foundTaskList = null;
			
			string cat = Application.Preferences.Get (
				PreferencesKeys.SelectedTaskListKey);
			if (cat != null) {
				var model = Application.BackendManager.TaskLists;
				foundTaskList = model.FirstOrDefault (c => c.Name == cat);
			}
			
			return foundTaskList;
		}
		
		private void OnRangeSliderChanged (object sender, EventArgs args)
		{
			ShowCompletedRange range = (ShowCompletedRange)(uint)rangeSlider.Value;
			
			// If the value is different than what we already have, adjust it in
			// the UI and set the preference.
			if (range == this.currentRange)
				return;
			
			this.currentRange = range;
			Application.Preferences.Set (PreferencesKeys.CompletedTasksRange,
										 range.ToString ());
			
			UpdateDateRanges ();
		}
		
		private void OnFormatRangeSliderValue (object sender,
											   FormatValueArgs args)
		{
			ShowCompletedRange range = (ShowCompletedRange)args.Value;
			args.RetVal = GetTranslatedRangeValue (range);
		}
		
		private ShowCompletedRange ParseRange (string rangeStr)
		{
			switch (rangeStr) {
			case "Yesterday":
				return ShowCompletedRange.Yesterday;
			case "Last7Days":
				return ShowCompletedRange.Last7Days;
			case "LastMonth":
				return ShowCompletedRange.LastMonth;
			case "LastYear":
				return ShowCompletedRange.LastYear;
			}
			
			// If the string doesn't match for some reason just return the
			// default, which is All.
			return ShowCompletedRange.All;
		}
		
		private string GetTranslatedRangeValue (ShowCompletedRange range)
		{
			switch (range) {
			case ShowCompletedRange.Yesterday:
				return Catalog.GetString ("Yesterday");
			case ShowCompletedRange.Last7Days:
				return Catalog.GetString ("Last 7 Days");
			case ShowCompletedRange.LastMonth:
				return Catalog.GetString ("Last Month");
			case ShowCompletedRange.LastYear:
				return Catalog.GetString ("Last Year");
			}
			
			return Catalog.GetString ("All");
		}
		
		private void UpdateDateRanges ()
		{
			DateTime date = DateTime.MinValue;
			DateTime today = DateTime.Now;
			
			switch (currentRange) {
			case ShowCompletedRange.Yesterday:
				date = today.AddDays (-1);
				date = new DateTime (date.Year, date.Month, date.Day,
									 0, 0, 0);
				break;
			case ShowCompletedRange.Last7Days:
				date = today.AddDays (-7);
				date = new DateTime (date.Year, date.Month, date.Day,
									 0, 0, 0);
				break;
			case ShowCompletedRange.LastMonth:
				date = today.AddMonths (-1);
				date = new DateTime (date.Year, date.Month, date.Day,
									 0, 0, 0);
				break;
			case ShowCompletedRange.LastYear:
				date = today.AddYears (-1);
				date = new DateTime (date.Year, date.Month, date.Day,
									 0, 0, 0);
				break;
			}
			
			this.TimeRangeStart = date;
		}
	}
	
	/// <summary>
	/// The purpose of this class is to allow the CompletedTaskGroup to show
	/// completed tasks in reverse order (i.e., most recently completed tasks
	/// at the top of the list).
	/// </summary>
	class CompletedTasksSortModel : ObservableCollection<ITask>
	{
		public CompletedTasksSortModel (ICollection<ITask> childModel)
		{
			completionDateComparer = new TaskCompletionDateComparer ();
			originalTasks = childModel;
			((INotifyCollectionChanged)childModel).CollectionChanged += HandleCollectionChanged;
			foreach (var item in originalTasks)
				AddTask (item);
		}
		
		#region Private Methods
		void AddTask (ITask task)
		{
			var index = Count;
			var valIdx = this.Select ((val, idx) => new { val, idx })
				.FirstOrDefault (t => CompareTasksSortFunc (t.val, task) > 0);
			if (valIdx != null)
				index = valIdx.idx;
			
			Insert (index, task);
		}
		
		void HandleCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			//FIXME: Only add and remove actions are expected
			switch (e.Action) {
			case NotifyCollectionChangedAction.Add:
				AddTask ((ITask)e.NewItems [0]);
				break;
			case NotifyCollectionChangedAction.Remove:
				var task = ((ITask)e.OldItems [0]);
				Remove (task);
				break;
			}
		}
		
		int CompareTasksSortFunc (ITask x, ITask y)
		{
			if (x == null || y == null)
				return 0;
			
			// Reverse the logic with the '-' so it's in reverse
			return -(completionDateComparer.Compare (x, y));
		}
		#endregion // Private Methods
		
		ICollection<ITask> originalTasks;
		TaskCompletionDateComparer completionDateComparer;
	}
}
