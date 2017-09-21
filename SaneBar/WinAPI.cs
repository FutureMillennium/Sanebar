using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		public const uint WINEVENT_SKIPOWNPROCESS = 0x0002; // Don't call back for events on installer's process

		public const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B; // hwnd ID idChild is moved/sized item
		public const uint EVENT_OBJECT_NAMECHANGE = 0x800C; // hwnd ID idChild is item w/ name change
		public const uint EVENT_SYSTEM_FOREGROUND = 3;

		public const int OBJID_WINDOW = 0;
		public const int CHILDID_SELF = 0;

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

		[DllImport("user32.dll")]
		public static extern bool IsZoomed(IntPtr hWnd);


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


		public struct DWMCOLORIZATIONPARAMS
		{
			public UInt32 ColorizationColor;
			public UInt32 ColorizationAfterglow;
			public UInt32 ColorizationColorBalance;
			public UInt32 ColorizationAfterglowBalance;
			public UInt32 ColorizationBlurBalance;
			public UInt32 ColorizationGlassReflectionIntensity;
			public UInt32 ColorizationOpaqueBlend;
		}

		[DllImport("dwmapi.dll", EntryPoint = "#127")]
		public static extern void DwmGetColorizationParameters(ref DWMCOLORIZATIONPARAMS dp);


		[StructLayout(LayoutKind.Explicit)]
		public struct WORD
		{
			[FieldOffset(0)]
			public int LongValue;
			[FieldOffset(0)]
			public short LoWord;
			[FieldOffset(2)]
			public short HiWord;

			public WORD(int Lo, int Hi)
			{
			  LongValue = Lo;
			  LoWord = (short)Lo;
			  HiWord = (short)Hi;
			}
		}



		[DllImport("user32.dll")]
		internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

		[StructLayout(LayoutKind.Sequential)]
		internal struct WindowCompositionAttributeData
		{
			public WindowCompositionAttribute Attribute;
			public IntPtr Data;
			public int SizeOfData;
		}

		internal enum WindowCompositionAttribute
		{
			// ...
			WCA_ACCENT_POLICY = 19
			// ...
		}

		internal enum AccentState
		{
			ACCENT_DISABLED = 0,
			ACCENT_ENABLE_GRADIENT = 1,
			ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
			ACCENT_ENABLE_BLURBEHIND = 3,
			ACCENT_INVALID_STATE = 4
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct AccentPolicy
		{
			public AccentState AccentState;
			public int AccentFlags;
			public int GradientColor;
			public int AnimationId;
		}

		internal static void BlurBehind(Window window)
		{
			var windowHelper = new WindowInteropHelper(window);

			var accent = new AccentPolicy();
			var accentStructSize = Marshal.SizeOf(accent);
			accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;

			var accentPtr = Marshal.AllocHGlobal(accentStructSize);
			Marshal.StructureToPtr(accent, accentPtr, false);

			var data = new WindowCompositionAttributeData();
			data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
			data.SizeOfData = accentStructSize;
			data.Data = accentPtr;

			SetWindowCompositionAttribute(windowHelper.Handle, ref data);

			Marshal.FreeHGlobal(accentPtr);
		}


		[StructLayout(LayoutKind.Sequential)]
		internal struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
		}

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);




		[DllImport("user32.dll")]
		internal static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

		internal static Process GetProcessName(IntPtr hwnd)
		{
			uint pid;
			GetWindowThreadProcessId(hwnd, out pid);
			return Process.GetProcessById((int)pid);
		}




		const int MAX_PATH = 260;

		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool DestroyIcon(IntPtr hIcon);

		[StructLayout(LayoutKind.Sequential)]
		public struct SHFILEINFO
		{
			public IntPtr hIcon;
			public IntPtr iIcon;
			public uint dwAttributes;
			[MarshalAs(UnmanagedType.LPStr, SizeConst = MAX_PATH)]
			public string szDisplayName;
			[MarshalAs(UnmanagedType.LPStr, SizeConst = 80)]
			public string szTypeName;
		}

		public enum SHGFI
		{
			SHGFI_ICON = 0x000000100,     // get icon
			SHGFI_DISPLAYNAME = 0x000000200,     // get display name
			SHGFI_TYPENAME = 0x000000400,     // get type name
			SHGFI_ATTRIBUTES = 0x000000800,     // get attributes
			SHGFI_ICONLOCATION = 0x000001000,     // get icon location
			SHGFI_EXETYPE = 0x000002000,     // return exe type
			SHGFI_SYSICONINDEX = 0x000004000,     // get system icon index
			SHGFI_LINKOVERLAY = 0x000008000,     // put a link overlay on icon
			SHGFI_SELECTED = 0x000010000,     // show icon in selected state
			SHGFI_ATTR_SPECIFIED = 0x000020000,     // get only specified attributes
			SHGFI_LARGEICON = 0x000000000,     // get large icon
			SHGFI_SMALLICON = 0x000000001,     // get small icon
			SHGFI_OPENICON = 0x000000002,     // get open icon
			SHGFI_SHELLICONSIZE = 0x000000004,     // get shell size icon
			SHGFI_PIDL = 0x000000008,     // pszPath is a pidl
			SHGFI_USEFILEATTRIBUTES = 0x000000010     // use passed dwFileAttribute
		}

		public static Icon GetFileIcon(string filePath)
		{
			SHFILEINFO info = new SHFILEINFO();

			SHGetFileInfo(filePath, 0, ref info, (uint)Marshal.SizeOf(info), (uint)(SHGFI.SHGFI_ICON | SHGFI.SHGFI_LARGEICON));

			if (info.hIcon == IntPtr.Zero)
				return null;

			Icon icoResult = (Icon)Icon.FromHandle(info.hIcon).Clone();
			DestroyIcon(info.hIcon);
			return icoResult;
		}
	}
}
