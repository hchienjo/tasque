// 
// StatusIconTray.cs
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
using Tasque;
using Gtk;

namespace Gtk.Tasque
{
	public class StatusIconTray : GtkTray
	{
		public StatusIconTray (GtkApplicationBase application) : base  (application)
		{
			tray = new StatusIcon (Utilities.GetIcon (IconName, 24));
			tray.Visible = true;
			tray.Activate += delegate { ToggleTaskWindowAction.Activate (); };
			tray.PopupMenu += (sender, e) => {
				var popupMenu = Menu;
				popupMenu.ShowAll (); // shows everything
				tray.PresentMenu (popupMenu, (uint)e.Args [0], (uint)e.Args [1]);
			};
		}
		
		protected override void OnTooltipChanged ()
		{
			if (tray != null)
				tray.Tooltip = Tooltip;
		}
		
		StatusIcon tray;
	}
}
