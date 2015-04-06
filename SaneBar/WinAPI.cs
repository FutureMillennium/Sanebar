using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
	}
}
