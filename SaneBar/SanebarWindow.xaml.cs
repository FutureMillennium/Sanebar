using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Sanebar.WinAPI;

namespace Sanebar
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class SanebarWindow : Window
	{
		internal const string APP_TITLE = "Sanebar";

		SanebarWindow[] sanebarWindows;

		internal WindowInteropHelper this32;
		internal System.Windows.Forms.Screen screenThis;
		internal bool isCollapsed = false;
		bool isPrimary = false;
		bool isDoubleClick = false;
		internal static DispatcherTimer hideQuickLaunchTimer;
		static System.Windows.Forms.NotifyIcon notifyIcon;

		internal static Brush defaultBackground;
		static Brush activeBackground;

		static QuickLaunch quickLaunch;
		static MenuWindow menuWindow;
		internal static SanebarWindow primarySanebarWindow;

		static System.Collections.Specialized.StringCollection exceptionList;
		internal static bool isHidden = false;

		// active window
		static IntPtr hwndActiveWindow;
		static string titleActiveWindow;
		static ImageSource iconActiveWindow;
		static System.Windows.Forms.Screen screenActiveWindow;
		static bool isMaximisedActiveWindow;
		static RECT rectActiveWindow;
		static Process processActive;

		WinAPI.WinEventDelegate winEventDelegate; // Delegate needs to be declared here to avoid being garbage collected

		public SanebarWindow() : this(true)
		{
			
		}

		public SanebarWindow(bool primary = true)
		{
			// Primary Sanebar window handles other Sanebar windows
			if (primary)
			{
				isPrimary = true;
				primarySanebarWindow = this;

				WinAPI.DWMCOLORIZATIONPARAMS dwmColors = new WinAPI.DWMCOLORIZATIONPARAMS();
				WinAPI.DwmGetColorizationParameters(ref dwmColors);
				System.Drawing.Color clr = System.Drawing.Color.FromArgb((int)dwmColors.ColorizationColor);
				Color clrAero = Color.FromArgb(clr.A, clr.R, clr.G, clr.B);
				//Color clrAero = Color.FromArgb(255, clr.R, clr.G, clr.B);
				activeBackground = new SolidColorBrush(clrAero);
				defaultBackground = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));

				quickLaunch = new QuickLaunch();

				// Create a Sanebar window for each monitor
				sanebarWindows = new SanebarWindow[System.Windows.Forms.Screen.AllScreens.Length];
				sanebarWindows[0] = this;
				int i = 1;

				foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
				{
					if (screen.Primary == true)
					{
						// Set size
						this.Top = screen.WorkingArea.Top;
						this.Left = screen.WorkingArea.Left;
						this.Width = screen.WorkingArea.Right - screen.WorkingArea.Left;
						screenThis = screen;
					}
					else
					{
						// Set size
						sanebarWindows[i] = new SanebarWindow(false);
						sanebarWindows[i].Top = screen.WorkingArea.Top;
						sanebarWindows[i].Left = screen.WorkingArea.Left;
						sanebarWindows[i].Width = screen.WorkingArea.Right - screen.WorkingArea.Left;
						sanebarWindows[i].screenThis = screen;
						sanebarWindows[i].Show();
						i++;
					}
				}

				winEventDelegate = new WinAPI.WinEventDelegate(WinEventProc);

                // EVENT_SYSTEM_FOREGROUND
                IntPtr m_hhook = WinAPI.SetWinEventHook(WinAPI.EVENT_SYSTEM_FOREGROUND, WinAPI.EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, winEventDelegate, 0, 0, WinAPI.WINEVENT_OUTOFCONTEXT | WinAPI.WINEVENT_SKIPOWNPROCESS);

                // EVENT_OBJECT_LOCATIONCHANGE, EVENT_OBJECT_NAMECHANGE
                m_hhook = WinAPI.SetWinEventHook(WinAPI.EVENT_OBJECT_LOCATIONCHANGE, WinAPI.EVENT_OBJECT_NAMECHANGE, IntPtr.Zero, winEventDelegate, 0, 0, WinAPI.WINEVENT_OUTOFCONTEXT | WinAPI.WINEVENT_SKIPOWNPROCESS);

				// EVENT_SYSTEM_MINIMIZEEND
				m_hhook = WinAPI.SetWinEventHook(WinAPI.EVENT_SYSTEM_MINIMIZEEND, WinAPI.EVENT_SYSTEM_MINIMIZEEND, IntPtr.Zero, winEventDelegate, 0, 0, WinAPI.WINEVENT_OUTOFCONTEXT | WinAPI.WINEVENT_SKIPOWNPROCESS);
			}

			exceptionList = Properties.Settings.Default.ExceptionList;
			if (exceptionList == null)
				exceptionList = new System.Collections.Specialized.StringCollection();

			InitializeComponent();

			this.Height = System.Windows.Forms.SystemInformation.CaptionHeight;
		}

		private void Window_MouseUp(object sender, MouseButtonEventArgs e)
		{
            if (e.ChangedButton == MouseButton.Middle)
            {
                //quickLaunch.Hide();
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
				var pos = e.GetPosition(this);
				pos.X += this.Left;
				pos.Y += this.Top;

				MenuShow(pos);
			}
		}

		private void Window_SourceInitialized(object sender, EventArgs e)
		{
			this32 = new WindowInteropHelper(this);

			// can’t get focus (WS_EX_NOACTIVATE)
			int exStyle = WinAPI.GetWindowLong(this32.Handle, WinAPI.GWL_EXSTYLE);
			WinAPI.SetWindowLong(this32.Handle, WinAPI.GWL_EXSTYLE, exStyle | WinAPI.WS_EX_NOACTIVATE);
		}

		// Windows event
		public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
		{
			if (isHidden == false
				&& hwnd != IntPtr.Zero && idObject == OBJID_WINDOW && idChild == CHILDID_SELF)
				switch (eventType)
			{
				// Active window changed
				case WinAPI.EVENT_SYSTEM_FOREGROUND:
				case WinAPI.EVENT_SYSTEM_MINIMIZEEND:
					{
						if (hwndActiveWindow != hwnd)
						{
							bool isOwn = false;

							foreach (SanebarWindow sanebarWindow in sanebarWindows)
							{
								if (hwnd == sanebarWindow.this32.Handle)
								{
									isOwn = true;
									break;
								}
							}

							if (isOwn == false
								&& (menuWindow == null
									|| menuWindow.this32 == null
									|| menuWindow.this32.Handle != hwnd)
								&& (quickLaunch == null
									|| quickLaunch.this32 == null
									|| quickLaunch.this32.Handle != hwnd))
							{
								hwndActiveWindow = hwnd;
								Update(true, true, true, true);
							}
						}
                    }
					break;
				// Window name change
				case WinAPI.EVENT_OBJECT_NAMECHANGE:
					{
						if (hwnd == hwndActiveWindow)
						{
							Update(true, false, false, false);
						}
					}
					break;
				// Window location/size change
				case WinAPI.EVENT_OBJECT_LOCATIONCHANGE:
					{
						if (hwnd != IntPtr.Zero && hwnd == hwndActiveWindow)
						{
							Update(false, false, true, false);
						}
					}
					break;
			}
		}

		// Change displayed active window title
		private void Update(bool updateTitle, bool updateIcon, bool updateFocus, bool processUpdate)
		{
			if (processUpdate)
			{
				processActive = WinAPI.GetProcessName(hwndActiveWindow);
			}

			if (updateFocus) // screenActiveWindow == null || 
			{
				screenActiveWindow = System.Windows.Forms.Screen.FromHandle(hwndActiveWindow);
				WinAPI.GetWindowRect(hwndActiveWindow, ref rectActiveWindow); // @TODO if false?
				isMaximisedActiveWindow = WinAPI.IsZoomed(hwndActiveWindow);
			}

			if (updateTitle)
			{
				titleActiveWindow = WinAPI.GetWindowTitle(hwndActiveWindow);
			}

			if (updateIcon)
			{
				System.Drawing.Icon icon = WinAPI.GetAppIcon(hwndActiveWindow);
				if (icon != null)
				{
					iconActiveWindow = WinAPI.ToImageSource(icon);
				}
				else
				{
					iconActiveWindow = null;
				}
			}

			foreach (SanebarWindow sanebarWindow in sanebarWindows)
			{
				if (updateFocus)
					sanebarWindow.ChangeFocus();
				if (updateTitle)
					sanebarWindow.ChangeTitle();
				if (updateIcon)
					sanebarWindow.ChangeIcon();
			}
		}

		internal void ChangeTitle()
		{
			if (screenActiveWindow.Equals(screenThis))
			{
				titleActiveWindowLabel.Content = titleActiveWindow;
			}
		}

		internal void ChangeIcon()
		{
			if (screenActiveWindow.Equals(screenThis))
			{
				if (iconActiveWindow == null)
				{
					iconActiveWindowImage.Visibility = System.Windows.Visibility.Hidden;
				}
				else
				{
					iconActiveWindowImage.Source = iconActiveWindow;
					if (isCollapsed == false)
						iconActiveWindowImage.Visibility = System.Windows.Visibility.Visible;
				}
			}
		}

		internal void ChangeFocus()
		{
			if (screenActiveWindow.Equals(screenThis))
			{
				if (rectActiveWindow.Left == screenThis.Bounds.Left
					&& rectActiveWindow.Top == screenThis.Bounds.Top
					&& rectActiveWindow.Right == screenThis.Bounds.Right
					&& rectActiveWindow.Bottom == screenThis.Bounds.Bottom)
				{
					if (this.Visibility == Visibility.Visible)
						this.Visibility = Visibility.Collapsed;
				}
				else
				{
					if (this.Visibility == Visibility.Collapsed)
						this.Visibility = Visibility.Visible;

					if (isMaximisedActiveWindow)
					{
						if (this.Background.Equals(activeBackground) == false)
							this.Background = activeBackground;
						maxButton.Content = "\xE923";

						if (exceptionList.IndexOf(processActive.ProcessName) != -1)
						{
							CollapsedChange(true);
						}
						else
						{
							CollapsedChange(false);
						}
					}
					else
					{
						if (this.Background.Equals(defaultBackground) == false)
							this.Background = defaultBackground;
						maxButton.Content = "\xE922";

						CollapsedChange(false);
					}

					if (this.Opacity != 1)
						this.Opacity = 1;
				}
			}
			else
			{
				if (this.Background.Equals(defaultBackground) == false)
					this.Background = defaultBackground;

				if (this.Opacity != 0.5)
					this.Opacity = 0.5;
			}
		}

		private void closeButton_Click(object sender, RoutedEventArgs e)
		{
			CloseActiveWindow();
		}

		private void CloseActiveWindow()
		{
			WinAPI.SendMessage(hwndActiveWindow, WinAPI.WM_SYSCOMMAND, WinAPI.SC_CLOSE, 0);
		}

		/*private void titleActiveWindowLabel_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Right)
			{
				ShowSystemMenu(e.GetPosition(this));
				e.Handled = true;
			}
		}*/

		private void iconActiveWindowImage_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left && isDoubleClick)
			{
				isDoubleClick = false;
			}
			else if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right)
			{
				var p = new Point();
				p = cIconActiveWindowControl.TranslatePoint(p, this);
				p.Y = this.Height;
				ShowSystemMenu(p);
				e.Handled = true;
			}
		}

		private void ShowSystemMenu(Point pos)
		{
			WinAPI.WORD word = new WinAPI.WORD((short)(pos.X + screenThis.Bounds.Left), (short)(pos.Y + screenThis.Bounds.Top));
			WinAPI.SendMessage(hwndActiveWindow, WinAPI.WM_GETSYSMENU, 0, word.LongValue);
		}

		private void ContentControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				isDoubleClick = true;
				CloseActiveWindow();
				e.Handled = true;
			}
		}

		private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				if (isDoubleClick == false)
					ToggleMaximiseActiveWindow();
				e.Handled = true;
			}
		}

		private void ToggleMaximiseActiveWindow()
		{
			if (isMaximisedActiveWindow)
			{
				WinAPI.SendMessage(hwndActiveWindow, WinAPI.WM_SYSCOMMAND, WinAPI.SC_RESTORE, 0);
				//return false;
			}
			else
			{
				WinAPI.SendMessage(hwndActiveWindow, WinAPI.WM_SYSCOMMAND, WinAPI.SC_MAXIMIZE, 0);
				//return true;
			}
		}

		private void Window_Drop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				/*string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				if (files.Length > 0)
				{
					OpenFile(files[0]);
				}*/
			}
		}

		private void Window_DragLeave(object sender, DragEventArgs e)
		{
			if (quickLaunch.IsVisible)
			{
				if (hideQuickLaunchTimer == null)
				{
					hideQuickLaunchTimer = new DispatcherTimer();
					hideQuickLaunchTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
					hideQuickLaunchTimer.Tick += hideQuickLaunchTimer_Tick;
				}

				hideQuickLaunchTimer.Start();
			}
		}

		void hideQuickLaunchTimer_Tick(object sender, EventArgs e)
		{
			if (quickLaunch.IsVisible)
			{
				quickLaunch.Hide();
			}
			hideQuickLaunchTimer.Stop();
		}

		private void Window_DragEnter(object sender, DragEventArgs e)
		{
			var a = e.Data.GetFormats();
			if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.UnicodeText)) // @TODO "UniformResourceLocator"?
			{
				if (hideQuickLaunchTimer != null)
					hideQuickLaunchTimer.Stop();
				if (quickLaunch.IsVisible == false)
				{
					ShowQuickLaunch(e.GetPosition(this));
					quickLaunch.CaptureMouse();
				}
			}
		}

		private void ShowQuickLaunch(Point pos)
		{
			quickLaunch.Left = this.Left + pos.X - (quickLaunch.Width / 2);
			if (quickLaunch.Left < this.Left)
				quickLaunch.Left = this.Left;
			else if (quickLaunch.Left + quickLaunch.Width > this.Left + this.Width)
				quickLaunch.Left = this.Left + this.Width - quickLaunch.Width;
			quickLaunch.Top = this.Top + this.Height;
			quickLaunch.Show();
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Middle)
			{
				QuickLaunchShowAndTrap(e.GetPosition(this));
			}
		}

		private void QuickLaunchShowAndTrap(Point pos)
		{
			quickLaunch.prevCursorPosition = System.Windows.Forms.Cursor.Position;

			ShowQuickLaunch(pos);

			quickLaunch.Activate();
			System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)(quickLaunch.Left + quickLaunch.Width / 2), (int)(quickLaunch.Top + quickLaunch.Height / 2));
			System.Windows.Forms.Cursor.Clip = new System.Drawing.Rectangle((int)quickLaunch.Left, (int)quickLaunch.Top, (int)quickLaunch.Width, (int)quickLaunch.Height);
			quickLaunch.CaptureMouse();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			BlurBehind(this);
		}

		private void minButton_Click(object sender, RoutedEventArgs e)
		{
			WinAPI.SendMessage(hwndActiveWindow, WinAPI.WM_SYSCOMMAND, WinAPI.SC_MINIMIZE, 0);
		}

		private void maxButton_Click(object sender, RoutedEventArgs e)
		{
			ToggleMaximiseActiveWindow();
		}

		void CollapsedChange(bool isCollapseTo)
		{
			if (isCollapsed != isCollapseTo)
			{
				isCollapsed = isCollapseTo;

				if (isCollapsed)
				{
					this.Width = 5;
					quickLaunchButton.Width = 5;
					//iconActiveWindowImage.Visibility = Visibility.Hidden;
					//closeButton.Visibility = Visibility.Hidden;
					rightButtonsStackPanel.Visibility = Visibility.Hidden;
				}
				else
				{
					//iconActiveWindowImage.Visibility = Visibility.Visible;
					//closeButton.Visibility = Visibility.Visible;
					rightButtonsStackPanel.Visibility = Visibility.Visible;
					quickLaunchButton.Width = 40;
					this.Width = screenThis.Bounds.Width;
				}
			}
		}

		internal void ExceptionChange()
		{
			if (processActive != null)
			{
				int index = exceptionList.IndexOf(processActive.ProcessName);

				if (index != -1)
				{
					exceptionList.RemoveAt(index);
				}
				else
				{
					exceptionList.Add(processActive.ProcessName);
				}

				foreach (SanebarWindow sanebarWindow in sanebarWindows)
				{
					sanebarWindow.ChangeFocus();
				}
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (isPrimary)
			{
				if (notifyIcon != null)
					notifyIcon.Visible = false;

				Properties.Settings.Default.ExceptionList = exceptionList;
				Properties.Settings.Default.Save();
			}
		}

		internal void HideAllToggle()
		{
			if (isHidden == true)
			{
				isHidden = false;

				notifyIcon.Visible = false;

				foreach (SanebarWindow sanebarWindow in sanebarWindows)
				{
					sanebarWindow.Visibility = Visibility.Visible;
				}
			}
			else
			{
				isHidden = true;

				if (notifyIcon == null)
				{
					notifyIcon = new System.Windows.Forms.NotifyIcon()
					{
						Icon = Properties.Resources.Sanebar,
						Text = APP_TITLE,
					};

					notifyIcon.MouseDoubleClick += NotifyIcon_MouseDoubleClick;
					notifyIcon.MouseUp += NotifyIcon_MouseUp;
				}

				notifyIcon.Visible = true;

				foreach (SanebarWindow sanebarWindow in sanebarWindows)
				{
					sanebarWindow.Visibility = Visibility.Collapsed;
				}
			}
		}

		private void NotifyIcon_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			//if (e.Button == System.Windows.Forms.MouseButtons.Right)
			{
				var pos = System.Windows.Forms.Cursor.Position;

				MenuShow(new Point(pos.X, pos.Y));
			}
		}

		private void NotifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			HideAllToggle();
		}

		void MenuShow(Point pos)
		{
			if (menuWindow == null)
				menuWindow = new MenuWindow();

			if (processActive != null)
			{
				// @TODO @Speed Don't do this again if the active window hasn't changed
				if (exceptionList.IndexOf(processActive.ProcessName) != -1)
					menuWindow.hideCheckbox.IsChecked = true;
				else
					menuWindow.hideCheckbox.IsChecked = false;
				//menuWindow.hideCheckbox.Content = "Hide when on " + processActive.MainModule.FileVersionInfo.FileDescription;
				menuWindow.appNameRun.Text = processActive.MainModule.FileVersionInfo.FileDescription;
			}

			menuWindow.Prepare();

			if (pos.X + menuWindow.Width > screenThis.Bounds.Right)
				menuWindow.Left = pos.X - menuWindow.Width;
			else
				menuWindow.Left = pos.X;

			if (pos.Y + menuWindow.Height > screenThis.Bounds.Bottom)
				menuWindow.Top = pos.Y - menuWindow.Height;
			else
				menuWindow.Top = pos.Y;
			
			menuWindow.Show();
			menuWindow.Activate();
		}

		private void quickLaunchButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				QuickLaunchShowAndTrap(new Point());
				e.Handled = true;
			}
		}

		private void menuButton_Click(object sender, RoutedEventArgs e)
		{
			var p = new Point();
			p = menuButton.TranslatePoint(p, this);
			p.X += this.Left + menuButton.Width;
			p.Y = this.Top + this.Height;

			MenuShow(p);
		}
	}
}
