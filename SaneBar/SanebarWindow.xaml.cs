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
		WindowInteropHelper this32;
		IntPtr hwndActiveWindow;
		private string titleActiveWindow;
		WinAPI.WinEventDelegate winEventDelegate;

		public SanebarWindow() : this(true)
		{
			
		}

		public SanebarWindow(bool primary = true)
		{
			// Primary Sanebar window handles other Sanebar windows
			if (primary)
			{
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
					}
					else
					{
						// Set size
						sanebarWindows[i] = new SanebarWindow(false);
						sanebarWindows[i].Top = screen.WorkingArea.Top;
						sanebarWindows[i].Left = screen.WorkingArea.Left;
						sanebarWindows[i].Width = screen.WorkingArea.Right - screen.WorkingArea.Left;
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
						if (hwnd != this32.Handle)
						{
							hwndActiveWindow = hwnd;
							ChangeTitle();
							
						}
					}
					break;
				// Window name change
				case WinAPI.EVENT_OBJECT_NAMECHANGE:
					{
						if (hwnd == hwndActiveWindow)
						{
							ChangeTitle();
						}
					}
					break;
				// Window location/size change
				case WinAPI.EVENT_OBJECT_LOCATIONCHANGE:
				{
					
				}
					break;
			}
		}

		// Change displayed active window title
		private void ChangeTitle()
		{
			titleActiveWindow = WinAPI.GetWindowTitle(hwndActiveWindow);
			if (string.IsNullOrWhiteSpace(titleActiveWindow))
			{
				titleActiveWindow = "(no title)";
			}

			foreach (SanebarWindow sanebarWindow in sanebarWindows)
			{
				sanebarWindow.ChangeTitle(titleActiveWindow);
			}
		}

		internal void ChangeTitle(string title)
		{
			currentTitleLabel.Content = title;
		}
	}
}
