using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Sanebar
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class SanebarWindow : Window
	{
		SanebarWindow[] sanebarWindows;

		internal WindowInteropHelper this32;
		internal System.Windows.Forms.Screen screenThis;
		//bool isPrimary = false;
		bool isDoubleClick = false;

		static Brush defaultBackground;
		static Brush activeBackground;

		// active window
		static IntPtr hwndActiveWindow;
		static string titleActiveWindow;
		static ImageSource iconActiveWindow;
		static System.Windows.Forms.Screen screenActiveWindow;
		static bool isMaximisedActiveWindow;

		WinAPI.WinEventDelegate winEventDelegate; // Delegate needs to be declared here to avoid being garbage collected

		public SanebarWindow() : this(true)
		{
			
		}

		public SanebarWindow(bool primary = true)
		{
			// Primary Sanebar window handles other Sanebar windows
			if (primary)
			{
				//isPrimary = true;

				WinAPI.DWMCOLORIZATIONPARAMS dwmColors = new WinAPI.DWMCOLORIZATIONPARAMS();
				WinAPI.DwmGetColorizationParameters(ref dwmColors);
				System.Drawing.Color clr = System.Drawing.Color.FromArgb((int)dwmColors.ColorizationColor);
				Color clrAero = Color.FromArgb(clr.A, clr.R, clr.G, clr.B);
				activeBackground = new SolidColorBrush(clrAero);
				defaultBackground = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));

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
				IntPtr m_hhook = WinAPI.SetWinEventHook(WinAPI.EVENT_SYSTEM_FOREGROUND, WinAPI.EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, winEventDelegate, 0, 0, WinAPI.WINEVENT_OUTOFCONTEXT);
				m_hhook = WinAPI.SetWinEventHook(WinAPI.EVENT_OBJECT_LOCATIONCHANGE, WinAPI.EVENT_OBJECT_NAMECHANGE, IntPtr.Zero, winEventDelegate, 0, 0, WinAPI.WINEVENT_OUTOFCONTEXT);
			}
			this.Height = System.Windows.Forms.SystemInformation.CaptionHeight;

			InitializeComponent();
		}

		private void Window_MouseUp(object sender, MouseButtonEventArgs e)
		{
			// TEMP
			if (e.ChangedButton == MouseButton.Right)
				Application.Current.Shutdown();
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
			switch (eventType)
			{
				// Active window changed
				case WinAPI.EVENT_SYSTEM_FOREGROUND:
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

						if (isOwn == false)
						{
							hwndActiveWindow = hwnd;
							Update();
						}
					}
					break;
				// Window name change
				case WinAPI.EVENT_OBJECT_NAMECHANGE:
					{
						if (hwnd == hwndActiveWindow)
						{
							Update(true, false, false);
						}
					}
					break;
				// Window location/size change
				case WinAPI.EVENT_OBJECT_LOCATIONCHANGE:
					{
						if (hwnd == hwndActiveWindow)
						{
							Update(false, false, true);
						}
					}
					break;
			}
		}

		// Change displayed active window title
		private void Update(bool updateTitle = true, bool updateIcon = true, bool updateFocus = true)
		{
			if (updateFocus)
			{
				screenActiveWindow = System.Windows.Forms.Screen.FromHandle(hwndActiveWindow);
				isMaximisedActiveWindow = WinAPI.IsZoomed(hwndActiveWindow);
			}

			if (updateTitle)
			{
				titleActiveWindow = WinAPI.GetWindowTitle(hwndActiveWindow);
				if (string.IsNullOrWhiteSpace(titleActiveWindow))
				{
					titleActiveWindow = "(no title)";
				}
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
				if (updateTitle)
					sanebarWindow.ChangeTitle();
				if (updateIcon)
					sanebarWindow.ChangeIcon();
				if (updateFocus)
					sanebarWindow.ChangeFocus();
			}
		}

		internal void ChangeTitle()
		{
			titleActiveWindowLabel.Content = titleActiveWindow;
		}

		internal void ChangeIcon()
		{
			if (iconActiveWindow == null)
			{
				iconActiveWindowImage.Visibility = System.Windows.Visibility.Hidden;
			}
			else
			{
				iconActiveWindowImage.Source = iconActiveWindow;
				iconActiveWindowImage.Visibility = System.Windows.Visibility.Visible;
			}
			
		}

		internal void ChangeFocus()
		{
			if (screenActiveWindow.DeviceName == screenThis.DeviceName)
			{
				if (isMaximisedActiveWindow)
					this.Background = activeBackground;
				else
					this.Background = defaultBackground;

				this.Opacity = 1;
			}
			else
			{
				this.Background = defaultBackground;
				this.Opacity = 0.75;
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

		private void titleActiveWindowLabel_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Right)
			{
				ShowSystemMenu(e.GetPosition(this));
				e.Handled = true;
			}
		}

		private void iconActiveWindowImage_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (isDoubleClick)
			{
				isDoubleClick = false;
			}
			else if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right)
			{
				ShowSystemMenu(new Point(0, this.ActualHeight));
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
			isDoubleClick = true;
			CloseActiveWindow();
			e.Handled = true;
		}

		private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			ToggleMaximiseActiveWindow();
			e.Handled = true;
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
	}
}
