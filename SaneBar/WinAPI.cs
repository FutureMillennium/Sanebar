using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Sanebar
{
	class WinAPI
	{
		public const int WS_EX_NOACTIVATE = 0x08000000;
		public const int GWL_EXSTYLE = -20;

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int GetWindowLong(IntPtr hwnd, int index);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);



		public const uint WINEVENT_OUTOFCONTEXT = 0;
		public const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B; // hwnd ID idChild is moved/sized item
		public const uint EVENT_OBJECT_NAMECHANGE = 0x800C; // hwnd ID idChild is item w/ name change
		public const uint EVENT_SYSTEM_FOREGROUND = 3;

		public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

		[DllImport("user32.dll")]
		public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);



		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern int GetWindowTextLength(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

		public static string GetWindowTitle(IntPtr hWnd)
		{
			// Allocate correct string length first
			int length = GetWindowTextLength(hWnd);
			StringBuilder sb = new StringBuilder(length + 1);
			GetWindowText(hWnd, sb, sb.Capacity);
			return sb.ToString();
		}



		public const int SC_MINIMIZE = 0xF020;
		public const int SC_MAXIMIZE = 0xF030;
		public const int SC_RESTORE = 0xF120;
		public const int WM_SYSCOMMAND = 0x0112;
		public const int SC_CLOSE = 0xF060;
		public const int WM_GETSYSMENU = 0x313;

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);

		public const int GCL_HICONSM = -34;
		public const int GCL_HICON = -14;

		public const int ICON_SMALL = 0;
		public const int ICON_BIG = 1;
		public const int ICON_SMALL2 = 2;

		public const int WM_GETICON = 0x7F;

		public static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
		{
			if (IntPtr.Size > 4)
				return GetClassLongPtr64(hWnd, nIndex);
			else
				return new IntPtr(GetClassLongPtr32(hWnd, nIndex));
		}

		[DllImport("user32.dll", EntryPoint = "GetClassLong")]
		public static extern uint GetClassLongPtr32(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
		public static extern IntPtr GetClassLongPtr64(IntPtr hWnd, int nIndex);

		public static Icon GetAppIcon(IntPtr hwnd)
		{
			IntPtr iconHandle = SendMessage(hwnd, WM_GETICON, ICON_SMALL2, 0);
			if (iconHandle == IntPtr.Zero)
				iconHandle = SendMessage(hwnd, WM_GETICON, ICON_SMALL, 0);
			if (iconHandle == IntPtr.Zero)
				iconHandle = SendMessage(hwnd, WM_GETICON, ICON_BIG, 0);
			if (iconHandle == IntPtr.Zero)
				iconHandle = GetClassLongPtr(hwnd, GCL_HICONSM);
			if (iconHandle == IntPtr.Zero)
				iconHandle = GetClassLongPtr(hwnd, GCL_HICON);

			if (iconHandle == IntPtr.Zero)
				return null;

			Icon icon = Icon.FromHandle(iconHandle);

			return icon;
		}

		public static ImageSource ToImageSource(Icon icon)
		{
			ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
				icon.Handle,
				Int32Rect.Empty,
				BitmapSizeOptions.FromEmptyOptions());

			return imageSource;
		}
	}
}
