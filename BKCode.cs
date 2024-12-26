//FileMenuUtil2.ShowShellContextMenur(action.Subtitle);
using static HakeQuick.Helpers.FileMenuUtil.API;

ContextMenuWrapper cmw = new ContextMenuWrapper();
cmw.OnQueryMenuItems += (QueryMenuItemsEventHandler)delegate (object s, QueryMenuItemsEventArgs args)
{
				args.ExtraMenuItems = new string[] { "Edit Dock Entry", "Remove Dock Entry", "---" };

				args.GrayedItems = new string[] { "delete", "rename", "cut", "copy" };
				args.HiddenItems = new string[] { "link" };
				args.DefaultItem = 1;
};
cmw.OnAfterPopup += (AfterPopupEventHandler)delegate (object s, AfterPopupEventArgs args)
{
				//Messenger.Default.Send<ShellContextMenuMessage>(ShellContextMenuMessage.Closed());
};

//Messenger.Default.Send<ShellContextMenuMessage>(ShellContextMenuMessage.Opened());
Debug.WriteLine(File.Exists(action.Subtitle) + "");
try
{
				//FileSystemInfoEx[] files = new[] { FileInfoEx.FromString(@action.Subtitle) };
				FileSystemInfoEx[] files = new[] { FileInfoEx.FromString(@"c:\windows\notepad.exe") };
				//int[] position = Win32Mouse.GetMousePosition();
				string command = cmw.Popup(files, new System.Drawing.Point(p.X, p.Y));

				// Handle the click on the 'ExtraMenuItems'.
				switch (command)
				{
					case "Edit Dock Entry":
						//Messenger.Default.Send<ApplicationMessage>(ApplicationMessage.Edit(application));
						break;
					case "Remove Dock Entry":
						//Messenger.Default.Send<ApplicationMessage>(ApplicationMessage.Remove(application));
						break;
				}
				e.Handled = true; // Don't open the normal context menu.
}
catch (Exception ex)
{
				Debug.Print("Problem displaying shell context menu: {0}", ex);
}


			//ListViewItem.Focus();
			//System.Windows.Controls.ListView item = sender as System.Windows.Controls.ListView;
			//if (item != null && item.SelectedItem != null)
			//{
			//	//MessageBox.Show(item.SelectedIndex.ToString());
			//}
			//e.Handled = true;


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;
using System.Windows.Forms;
using System.Windows;
using Shell32;
using System.IO;
using System.Runtime.InteropServices;
using System;
using System.Runtime.InteropServices.ComTypes;
//using SharpShell.Interop;

namespace HakeQuick.Helpers
{

	//����IShellFolder�ӿ�
	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("000214E6-0000-0000-C000-000000000046")]
	public interface IShellFolder
	{
		void ParseDisplayName(IntPtr hwnd, IBindCtx pbc, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, out uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
		//void EnumObjects(IntPtr hwnd, uint grfFlags, out IEnumIDList ppenumIDList);
		void BindToObject(IntPtr pidl, IBindCtx pbc, [In] ref Guid riid, out IntPtr ppv);
		void BindToStorage(IntPtr pidl, IBindCtx pbc, [In] ref Guid riid, out IntPtr ppv);
		void CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
		void CreateViewObject(IntPtr hwndOwner, [In] ref Guid riid, out IntPtr ppv);
		void GetAttributesOf(uint cidl, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, ref uint rgfInOut);
		void GetUIObjectOf(IntPtr hwndOwner, uint cidl, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, [In] ref Guid riid, IntPtr rgfReserved, out IntPtr ppv);
		void GetDisplayNameOf(IntPtr pidl, uint uFlags, out IntPtr pName);
		void SetNameOf(IntPtr hwnd, IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] string pszName, uint uFlags, out IntPtr ppidlOut);
	}

	//����IContextMenu�ӿ�
	//[ComImport]
	//[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	//[Guid("000214e4-0000-0000-c000-000000000046")]
	//public interface IContextMenu
	//{
	//	[PreserveSig]
	//	int QueryContextMenu(IntPtr hMenu, uint iMenu, uint idCmdFirst, uint idCmdLast, uint uFlags);
	//	void InvokeCommand(ref ShellAPI.CMINVOKECOMMANDINFOEX info);
	//	void GetCommandString(uint idcmd, uint uflags, uint reserved, [MarshalAs(UnmanagedType.LPStr)] StringBuilder commandstring, int cch);
	//}

	//����CMINVOKECOMMANDINFOEX�ṹ��
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct CMINVOKECOMMANDINFOEX
	{
		public int cbSize;
		public uint fMask;
		public IntPtr hwnd;
		public IntPtr lpVerb;
		[MarshalAs(UnmanagedType.LPStr)]
		public string lpParameters;
		[MarshalAs(UnmanagedType.LPStr)]
		public string lpDirectory;
		public int nShow;
		public uint dwHotKey;
		public IntPtr hIcon;
		[MarshalAs(UnmanagedType.LPStr)]
		public string lpTitle;
		public IntPtr lpVerbW;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string lpParametersW;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string lpDirectoryW;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string lpTitleW;
		public Point ptInvoke;
	}

	//���峣��
	public static class ShellAPI
	{
		public const int MAX_PATH = 260;
		public const uint SEE_MASK_INVOKEIDLIST = 12;
		public const uint SW_SHOWNORMAL = 1;
		public const uint CMF_DEFAULTONLY = 1;
		public const uint GCS_VERBW = 4;
		public const int S_OK = 0;
		public static Guid IID_IContextMenu = new Guid("000214e4-0000-0000-c000-000000000046");
		public static Guid IID_IShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
	}

	//���帨������
	public static class ShellHelper
	{
		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		public static extern int SHGetDesktopFolder(out IShellFolder ppshf);

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		public static extern int SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string pszName, IntPtr pbc, out IntPtr ppidl, uint sfgaoIn, out uint psfgaoOut);

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		public static extern int SHBindToParent(IntPtr pidl, [In] ref Guid riid, out IntPtr ppv, out IntPtr ppidlLast);

		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		public static extern IntPtr GetMenu(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern IntPtr GetSubMenu(IntPtr hMenu, int nPos);

		[DllImport("user32.dll")]
		public static extern int GetMenuItemCount(IntPtr hMenu);

		[DllImport("user32.dll")]
		public static extern int TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

		[DllImport("user32.dll")]
		public static extern bool DestroyMenu(IntPtr hMenu);
	}

	public class FileMenuUtil2
	{




		//����������
		public static void ShowShellContextMenur(string fileName)
		{
			//��ȡ�ļ���
			//string fileName = args[0];
			//��ȡ����IShellFolder
			IShellFolder desktopFolder;
			ShellHelper.SHGetDesktopFolder(out desktopFolder);
			//�����ļ�����PIDL
			IntPtr filePidl;
			uint attr;
			ShellHelper.SHParseDisplayName(fileName, IntPtr.Zero, out filePidl, 0, out attr);
			//�󶨵���Ŀ¼��IShellFolder
			//IntPtr parentPidl;
			//IShellFolder parentFolder;
			//ShellHelper.SHBindToParent(filePidl, ref ShellAPI.IID_IShellFolder, out parentFolder, out parentPidl);

			IntPtr parentPidl;
			IntPtr parentFolderPtr; //�޸�ΪIntPtr
			ShellHelper.SHBindToParent(filePidl, ref ShellAPI.IID_IShellFolder, out parentFolderPtr, out parentPidl); //�޸�Ϊout IntPtr
			IShellFolder parentFolder = (IShellFolder)Marshal.GetTypedObjectForIUnknown(parentFolderPtr, typeof(IShellFolder)); //ʹ��Marshalת��ΪIShellFolder
																																//��ȡIContextMenu�ӿ�
			IntPtr contextMenuPtr;
			//IContextMenu contextMenu;
			parentFolder.GetUIObjectOf(IntPtr.Zero, 1, new IntPtr[] { parentPidl }, ref ShellAPI.IID_IContextMenu, IntPtr.Zero, out contextMenuPtr);
			//contextMenu = (IContextMenu)Marshal.GetTypedObjectForIUnknown(contextMenuPtr, typeof(IContextMenu));
			//��ȡ�˵�
			IntPtr menu = ShellHelper.GetMenu(ShellHelper.GetForegroundWindow());
			IntPtr subMenu = ShellHelper.GetSubMenu(menu, 0);
			int count = ShellHelper.GetMenuItemCount(subMenu);
			//contextMenu.QueryContextMenu(subMenu, (uint)count, 0, 0x7FFF, (CMF)ShellAPI.CMF_DEFAULTONLY);
			//�����˵�
			int cmd = ShellHelper.TrackPopupMenuEx(subMenu, 0x100, 100, 100, ShellHelper.GetForegroundWindow(), IntPtr.Zero);
			//���ݷ���ֵ��������
			if (cmd > 0)
			{
				StringBuilder verb = new StringBuilder(ShellAPI.MAX_PATH);
				//contextMenu.GetCommandString((int)(uint)cmd, (GCS)ShellAPI.GCS_VERBW, 0, verb, verb.Capacity);
				CMINVOKECOMMANDINFOEX info = new CMINVOKECOMMANDINFOEX();
				info.cbSize = Marshal.SizeOf(info);
				info.fMask = ShellAPI.SEE_MASK_INVOKEIDLIST;
				info.hwnd = ShellHelper.GetForegroundWindow();
				info.lpVerb = Marshal.StringToHGlobalAnsi(verb.ToString());
				info.lpVerbW = Marshal.StringToHGlobalUni(verb.ToString());
				info.nShow = (int)ShellAPI.SW_SHOWNORMAL;
				//info.lpDirectoryW = Marshal.StringToHGlobalUni(Path.GetDirectoryName(fileName));
				//contextMenu.InvokeCommand(ref info);
			}
			//�ͷ���Դ
			ShellHelper.DestroyMenu(menu);
			//Marshal.ReleaseComObject(contextMenu);
			Marshal.ReleaseComObject(parentFolder);
			Marshal.ReleaseComObject(desktopFolder);
			Marshal.FreeCoTaskMem(filePidl);
			Marshal.FreeCoTaskMem(parentPidl);
		}
	}
}


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;
using System.Windows.Forms;
using System.Windows;
using Shell32;
using System.IO;
using System.Runtime.InteropServices;
namespace HakeQuick.Helpers
{

	public class FileMenuUtil
	{
		// ���� SHELLEXECUTEINFO �ṹ��
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct SHELLEXECUTEINFO
		{
			public int cbSize;
			public uint fMask;
			public IntPtr hwnd;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string lpVerb;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string lpFile;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string lpParameters;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string lpDirectory;
			public int nShow;
			public IntPtr hInstApp;
			public IntPtr lpIDList;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string lpClass;
			public IntPtr hkeyClass;
			public uint dwHotKey;
			public IntPtr hIcon;
			public IntPtr hProcess;
		}

		// ���� ShellExecuteEx ����
		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

		public static void ShowFileContentMenu(string path)
		{
			// ����һ�� SHELLEXECUTEINFO ʵ��
			SHELLEXECUTEINFO sei = new SHELLEXECUTEINFO();
			sei.cbSize = Marshal.SizeOf(sei);
			sei.fMask = 0x00000040; // SEE_MASK_INVOKEIDLIST
			sei.hwnd = IntPtr.Zero;
			sei.lpVerb = "properties"; // ��������Ϊ�Ҽ��˵�
									   //sei.lpVerb = "properties"; // ��������Ϊ�Ҽ��˵�
			sei.lpFile = @path; // �ļ�·��
			sei.nShow = 1; // SW_SHOWNORMAL
			sei.hInstApp = IntPtr.Zero;

			// ���� ShellExecuteEx ����
			bool result = ShellExecuteEx(ref sei);
			if (result)
			{
				Debug.WriteLine("�ɹ���ʾ�Ҽ��˵�");
			}
			else
			{
				Debug.WriteLine("��ʾ�Ҽ��˵�ʧ��");
			}
		}






		//����һЩ�����ͽṹ��
		public static class API
		{
			public const uint CMD_FIRST = 1;
			public const uint CMD_LAST = 30000;

			public const uint TPM_RETURNCMD = 0x0100;

			public enum CMF : uint
			{
				NORMAL = 0x00000000,
				DEFAULTONLY = 0x00000001,
				VERBSONLY = 0x00000002,
				EXPLORE = 0x00000004,
				NOVERBS = 0x00000008,
				CANRENAME = 0x00000010,
				NODEFAULT = 0x00000020,
				INCLUDESTATIC = 0x00000040,
				EXTENDEDVERBS = 0x00000100,
				RESERVED = 0xffff0000
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct POINT
			{
				public int x;
				public int y;

				public POINT(int x, int y)
				{
					this.x = x;
					this.y = y;
				}
			}

			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
			public struct CMINVOKECOMMANDINFOEX
			{
				public int cbSize;
				public uint fMask;
				public IntPtr hwnd;
				public IntPtr lpVerb;
				[MarshalAs(UnmanagedType.LPStr)]
				public string lpParameters;
				[MarshalAs(UnmanagedType.LPStr)]
				public string lpDirectory;
				public int nShow;
				public uint dwHotKey;
				public IntPtr hIcon;
				[MarshalAs(UnmanagedType.LPStr)]
				public string lpTitle;
				public IntPtr lpVerbW;
				[MarshalAs(UnmanagedType.LPWStr)]
				public string lpParametersW;
				[MarshalAs(UnmanagedType.LPWStr)]
				public string lpDirectoryW;
				[MarshalAs(UnmanagedType.LPWStr)]
				public string lpTitleW;
				public POINT ptInvoke;
			}

			[DllImport("user32.dll")]
			public static extern IntPtr CreatePopupMenu();

			[DllImport("user32.dll")]
			public static extern bool DestroyMenu(IntPtr hMenu);

			[DllImport("user32.dll")]
			public static extern uint TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hWnd, IntPtr tpmParams);
		}

		//����һЩ�ӿ�
		[ComImport]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[Guid("000214E6-0000-0000-C000-000000000046")]
		public interface IShellFolder
		{
			void ParseDisplayName(IntPtr hwnd, IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, out uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
			void EnumObjects(IntPtr hwnd, uint grfFlags, out IEnumIDList ppenumIDList);
			void BindToObject(IntPtr pidl, IntPtr pbc, [In] ref Guid riid, out IShellFolder ppv);
			void BindToStorage(IntPtr pidl, IntPtr pbc, [In] ref Guid riid, out IntPtr ppv);
			void CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
			void CreateViewObject(IntPtr hwndOwner, [In] ref Guid riid, out IntPtr ppv);
			void GetAttributesOf(uint cidl, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, ref uint rgfInOut);
			void GetUIObjectOf(IntPtr hwndOwner, uint cidl, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, [In] ref Guid riid, IntPtr rgfReserved, out IntPtr ppv);
			void GetDisplayNameOf(IntPtr pidl, uint uFlags, out IntPtr pName);
			void SetNameOf(IntPtr hwnd, IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] string pszName, uint uFlags, out IntPtr ppidlOut);
		}

		[ComImport]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[Guid("000214F4-0000-0000-C000-000000000046")]
		public interface IContextMenu
		{
			[PreserveSig]
			int QueryContextMenu(IntPtr hMenu, uint iMenu, uint idCmdFirst, uint idCmdLast, uint uFlags);
			void InvokeCommand(ref API.CMINVOKECOMMANDINFOEX info);
			void GetCommandString(uint idCmd, uint uFlags, uint pwReserved, [MarshalAs(UnmanagedType.LPStr)] StringBuilder pszName, uint cchMax);
		}

		[ComImport]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[Guid("000214F2-0000-0000-C000-000000000046")]
		public interface IEnumIDList
		{
			[PreserveSig]
			int Next(uint celt, out IntPtr rgelt, out uint pceltFetched);
			void Skip(uint celt);
			void Reset();
			void Clone(out IEnumIDList ppenum);
		}


		[DllImport("shell32.dll")]
		static extern int SHGetDesktopFolder(out IShellFolder ppshf);


		//����һ���࣬������װ IShellFolder �� IContextMenu �Ĳ���
		public class ShellHelper
		{
			//��ȡ����� IShellFolder �ӿ�
			public static IShellFolder GetDesktopFolder()
			{
				IShellFolder folder;
				Guid iidShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
				SHGetDesktopFolder(out folder);
				return folder;
			}

			//����·����ȡ IShellFolder �ӿ�
			public static IShellFolder GetShellFolder(string path)
			{
				IShellFolder folder = GetDesktopFolder();
				uint eaten;
				uint attributes = 0;
				IntPtr pidl;
				folder.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, path, out eaten, out pidl, ref attributes);
				Guid iidShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
				folder.BindToObject(pidl, IntPtr.Zero, ref iidShellFolder, out folder);
				return folder;
			}

			//����·����ȡ IContextMenu �ӿ�
			public static IContextMenu GetContextMenu(string path)
			{
				IShellFolder folder = GetShellFolder(Path.GetDirectoryName(path));
				uint eaten;
				uint attributes = 0;
				IntPtr pidl;
				folder.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, path, out eaten, out pidl, ref attributes);
				IntPtr[] pidls = new IntPtr[] { pidl };
				IntPtr contextMenuPtr;
				Guid iidContextMenu = new Guid("000214F4-0000-0000-C000-000000000046");
				folder.GetUIObjectOf(IntPtr.Zero, 1, pidls, ref iidContextMenu, IntPtr.Zero, out contextMenuPtr);
				IContextMenu contextMenu = (IContextMenu)Marshal.GetObjectForIUnknown(contextMenuPtr);
				return contextMenu;
			}

			//���������Ĳ˵�
			public static void ShowContextMenu(string path, Point location)
			{
				IContextMenu contextMenu = GetContextMenu(path);
				IntPtr menu = API.CreatePopupMenu();
				contextMenu.QueryContextMenu(menu, 0, API.CMD_FIRST, API.CMD_LAST, (uint)(API.CMF.NORMAL | API.CMF.EXPLORE));
				uint cmd = API.TrackPopupMenuEx(menu, API.TPM_RETURNCMD, (int)location.X, (int)location.Y, Form.ActiveForm.Handle, IntPtr.Zero);
				if (cmd >= API.CMD_FIRST)
				{
					API.CMINVOKECOMMANDINFOEX invoke = new API.CMINVOKECOMMANDINFOEX();
					invoke.cbSize = Marshal.SizeOf(typeof(API.CMINVOKECOMMANDINFOEX));
					invoke.lpVerb = (IntPtr)(cmd - 1);
					invoke.lpDirectory = string.Empty;
					invoke.fMask = 0;
					invoke.ptInvoke = new API.POINT((int)location.X, (int)location.Y);
					invoke.nShow = 1;
					contextMenu.InvokeCommand(ref invoke);
				}
				API.DestroyMenu(menu);
			}
		}
	}
}
