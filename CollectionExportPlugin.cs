using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;
using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Plugins;
using Hearthstone_Deck_Tracker.Utility.Logging;
using Newtonsoft.Json;

namespace CollectionExportPlugin
{
	public sealed class CollectionExportPlugin : IPlugin
	{
		public string Name => "Collection Exporter";
		public string Description => "Export collection to JSON (HSReplay format).";
		public string ButtonText => "Export + Open Folder";
		public string Author => "Collection Export Plugin";
		public Version Version => new Version(1, 0, 0);
		public System.Windows.Controls.MenuItem MenuItem { get; } = BuildMenu();

		public void OnLoad()
		{
			Log.Info("CollectionExportPlugin loaded.");
		}

		public void OnUnload()
		{
			Log.Info("CollectionExportPlugin unloaded.");
		}

		public void OnButtonPress()
		{
			_ = ExportCollectionAsync(openFolderAfterExport: true, exportFileOverride: string.Empty);
		}

		public void OnUpdate()
		{
		}

		private static System.Windows.Controls.MenuItem BuildMenu()
		{
			var root = new System.Windows.Controls.MenuItem { Header = "Collection Exporter" };

			var exportItem = new System.Windows.Controls.MenuItem { Header = "Export to Default Folder" };
			exportItem.Click += (_, _) => _ = ExportCollectionAsync(openFolderAfterExport: false, exportFileOverride: string.Empty);

			var exportToItem = new System.Windows.Controls.MenuItem { Header = "Export To File..." };
			exportToItem.Click += (_, _) =>
			{
				var selected = PromptForFile(GetDefaultFileName(string.Empty));
				if(!string.IsNullOrWhiteSpace(selected))
					_ = ExportCollectionAsync(openFolderAfterExport: false, exportFileOverride: selected);
			};

			var openFolderItem = new System.Windows.Controls.MenuItem { Header = "Open Export Folder" };
			openFolderItem.Click += (_, _) => OpenFolder(GetExportDirectory(string.Empty));

			root.Items.Add(exportItem);
			root.Items.Add(exportToItem);
			root.Items.Add(openFolderItem);
			return root;
		}

		private static async Task ExportCollectionAsync(bool openFolderAfterExport, string exportFileOverride)
		{
			if(!Core.Game.IsRunning)
			{
				Log.Warn("Collection export aborted: Hearthstone is not running.");
				ShowError("Hearthstone is not running. Please launch Hearthstone and try again.");
				return;
			}

			var collection = await CollectionHelpers.Hearthstone.GetCollection();
			if(collection == null)
			{
				Log.Warn("Collection export aborted: collection not loaded. Visit the in-game collection screen and try again.");
				ShowError("Collection not loaded. Open the in-game Collection screen and try again.");
				return;
			}

			try
			{
				string filePath;
				if(!string.IsNullOrWhiteSpace(exportFileOverride))
				{
					filePath = exportFileOverride;
					var exportDir = Path.GetDirectoryName(filePath);
					if(!string.IsNullOrEmpty(exportDir))
						Directory.CreateDirectory(exportDir);
				}
				else
				{
					var exportDir = GetExportDirectory(string.Empty);
					Directory.CreateDirectory(exportDir);
					filePath = Path.Combine(exportDir, GetDefaultFileName(collection.BattleTag));
				}

			var json = SerializeToJson(collection);
			File.WriteAllText(filePath, json);

				Log.Info($"Collection exported to {filePath}");

				if(openFolderAfterExport)
					OpenFolder(Path.GetDirectoryName(filePath) ?? GetExportDirectory(string.Empty));
			}
			catch(Exception ex)
			{
				Log.Error(ex);
				ShowError($"Export failed: {ex.Message}");
			}
		}

		private static string GetExportDirectory(string overrideDir)
		{
			if(!string.IsNullOrWhiteSpace(overrideDir))
				return overrideDir;

			return Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"HearthstoneDeckTracker",
				"CollectionExports");
		}

		private static string PromptForFile(string defaultFileName)
		{
			return FileSavePicker.PickFile("Save collection as", defaultFileName);
		}

		private static string SerializeToJson(object value)
		{
			return JsonConvert.SerializeObject(value, Formatting.Indented);
		}

		private static void ShowError(string message)
		{
			var app = Application.Current;
			if(app?.Dispatcher != null)
				app.Dispatcher.Invoke(() => MessageBox.Show(message, "Collection Exporter", MessageBoxButton.OK, MessageBoxImage.Warning));
			else
				MessageBox.Show(message, "Collection Exporter", MessageBoxButton.OK, MessageBoxImage.Warning);
		}

		private static string GetDefaultFileName(string battleTag)
		{
			var safeTag = string.IsNullOrWhiteSpace(battleTag) ? "unknown" : battleTag.Replace('#', '_');
			var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
			return $"collection-{safeTag}-{timestamp}.json";
		}

		private static void OpenFolder(string folderPath)
		{
			try
			{
				Process.Start("explorer.exe", folderPath);
			}
			catch(Exception ex)
			{
				Log.Warn($"Could not open export folder: {ex.Message}");
			}
		}
	}

	internal static class FileSavePicker
	{
		private const uint FOS_FORCEFILESYSTEM = 0x00000040;
		private const uint FOS_OVERWRITEPROMPT = 0x00000002;
		private const uint SIGDN_FILESYSPATH = 0x80058000;
		private static readonly Guid CLSID_FileSaveDialog = new Guid("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B");

		public static string PickFile(string title, string defaultFileName)
		{
			var dialog = (IFileSaveDialog)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_FileSaveDialog));
			dialog.SetTitle(title);

			dialog.GetOptions(out var options);
			options |= FOS_FORCEFILESYSTEM | FOS_OVERWRITEPROMPT;
			dialog.SetOptions(options);
			dialog.SetFileName(defaultFileName);
			dialog.SetFileTypes(1, new[] { new COMDLG_FILTERSPEC { pszName = "JSON files", pszSpec = "*.json" } });

			var hr = dialog.Show(IntPtr.Zero);
			if(hr != 0)
			{
				Marshal.ReleaseComObject(dialog);
				return string.Empty;
			}

			dialog.GetResult(out var item);
			item.GetDisplayName(SIGDN_FILESYSPATH, out var path);

			Marshal.ReleaseComObject(item);
			Marshal.ReleaseComObject(dialog);

			return path ?? string.Empty;
		}
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	internal struct COMDLG_FILTERSPEC
	{
		public string pszName;
		public string pszSpec;
	}

	[ComImport]
	[Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IFileOpenDialog
	{
		[PreserveSig] int Show(IntPtr parent);
		void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
		void SetFileTypeIndex(uint iFileType);
		void GetFileTypeIndex(out uint piFileType);
		void Advise(IntPtr pfde, out uint pdwCookie);
		void Unadvise(uint dwCookie);
		void SetOptions(uint fos);
		void GetOptions(out uint pfos);
		void SetDefaultFolder(IShellItem psi);
		void SetFolder(IShellItem psi);
		void GetFolder(out IShellItem ppsi);
		void GetCurrentSelection(out IShellItem ppsi);
		void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
		void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
		void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
		void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
		void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
		void GetResult(out IShellItem ppsi);
		void AddPlace(IShellItem psi, uint fdap);
		void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
		void Close(int hr);
		void SetClientGuid(ref Guid guid);
		void ClearClientData();
		void SetFilter(IntPtr pFilter);
	}

	[ComImport]
	[Guid("84BCCD23-5FDE-4CDB-AEA4-AF64B83D78AB")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IFileSaveDialog
	{
		[PreserveSig] int Show(IntPtr parent);
		void SetFileTypes(uint cFileTypes, [MarshalAs(UnmanagedType.LPArray)] COMDLG_FILTERSPEC[] rgFilterSpec);
		void SetFileTypeIndex(uint iFileType);
		void GetFileTypeIndex(out uint piFileType);
		void Advise(IntPtr pfde, out uint pdwCookie);
		void Unadvise(uint dwCookie);
		void SetOptions(uint fos);
		void GetOptions(out uint pfos);
		void SetDefaultFolder(IShellItem psi);
		void SetFolder(IShellItem psi);
		void GetFolder(out IShellItem ppsi);
		void GetCurrentSelection(out IShellItem ppsi);
		void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
		void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
		void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
		void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
		void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
		void GetResult(out IShellItem ppsi);
		void AddPlace(IShellItem psi, uint fdap);
		void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
		void Close(int hr);
		void SetClientGuid(ref Guid guid);
		void ClearClientData();
		void SetFilter(IntPtr pFilter);
		void SetSaveAsItem(IShellItem psi);
		void SetProperties(IntPtr pStore);
		void SetCollectedProperties(IntPtr pList, int fAppendDefault);
		void GetProperties(out IntPtr ppStore);
		void ApplyProperties(IShellItem psi, IntPtr pStore, IntPtr hwnd, IntPtr pSink);
	}

	[ComImport]
	[Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IShellItem
	{
		void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
		void GetParent(out IShellItem ppsi);
		void GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
		void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
		void Compare(IShellItem psi, uint hint, out int piOrder);
	}
}
