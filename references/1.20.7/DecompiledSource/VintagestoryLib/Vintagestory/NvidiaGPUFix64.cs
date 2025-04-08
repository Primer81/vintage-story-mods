using System;
using System.Runtime.InteropServices;

namespace Vintagestory;

public static class NvidiaGPUFix64
{
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	private struct OptimusApplication
	{
		public uint version;

		public uint isPredefined;

		public unsafe fixed ushort appName[2048];

		public unsafe fixed ushort userFriendlyName[2048];

		public unsafe fixed ushort launcher[2048];

		public unsafe fixed ushort fileInFolder[2048];
	}

	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	private struct OptimusProfile
	{
		public uint version;

		public unsafe fixed ushort profileName[2048];

		public unsafe uint* gpuSupport;

		public uint isPredefined;

		public uint numOfApps;

		public uint numOfSettings;
	}

	[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
	private struct OptimusSetting
	{
		[FieldOffset(0)]
		public uint version;

		[FieldOffset(8)]
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2048)]
		public string settingName;

		[FieldOffset(4100)]
		public uint settingID;

		[FieldOffset(4104)]
		public uint settingType;

		[FieldOffset(4108)]
		public uint settingLocation;

		[FieldOffset(4112)]
		public uint isCurrentPredefined;

		[FieldOffset(4116)]
		public uint isPredefinedValid;

		[FieldOffset(4120)]
		public uint u32PredefinedValue;

		[FieldOffset(8220)]
		public uint u32CurrentValue;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int CreateSessionDelegate(out nint session);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int CreateApplicationDelegate(nint session, nint profile, ref OptimusApplication application);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int CreateProfileDelegate(nint session, ref OptimusProfile profileInfo, out nint profile);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int DeleteProfileDelegate(nint session, nint profile);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int DestroySessionDelegate(nint session);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private unsafe delegate int EnumApplicationsDelegate(nint session, nint profile, uint startIndex, ref uint appCount, OptimusApplication* allApplications);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int FindProfileByNameDelegate(nint session, [MarshalAs(UnmanagedType.BStr)] string profileName, out nint profile);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int GetProfileInfoDelegate(nint session, nint profile, ref OptimusProfile profileInfo);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int InitializeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int LoadSettingsDelegate(nint session);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int SaveSettingsDelegate(nint session);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int SetSettingDelegate(nint session, nint profile, ref OptimusSetting setting);

	public const int RESULT_NO_CHANGE = 0;

	public const int RESULT_CHANGE = 1;

	public const int RESULT_ERROR = -1;

	private static CreateSessionDelegate CreateSession;

	private static CreateApplicationDelegate CreateApplication;

	private static CreateProfileDelegate CreateProfile;

	private static DeleteProfileDelegate DeleteProfile;

	private static DestroySessionDelegate DestroySession;

	private static EnumApplicationsDelegate EnumApplications;

	private static FindProfileByNameDelegate FindProfileByName;

	private static GetProfileInfoDelegate GetProfileInfo;

	private static InitializeDelegate Initialize;

	private static LoadSettingsDelegate LoadSettings;

	private static SaveSettingsDelegate SaveSettings;

	private static SetSettingDelegate SetSetting;

	[DllImport("kernel32.dll")]
	private static extern nint LoadLibrary(string dllToLoad);

	[DllImport("nvapi64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "nvapi_QueryInterface")]
	private static extern nint QueryInterface(uint offset);

	private static bool CheckForError(int status)
	{
		if (status != 0)
		{
			return true;
		}
		return false;
	}

	private unsafe static bool UnicodeStringCompare(ushort* unicodeString, ushort[] referenceString)
	{
		for (int i = 0; i < 2048; i++)
		{
			if (unicodeString[i] != referenceString[i])
			{
				return false;
			}
		}
		return true;
	}

	private static ushort[] GetUnicodeString(string sourceString)
	{
		ushort[] destinationString = new ushort[2048];
		for (int i = 0; i < 2048; i++)
		{
			if (i < sourceString.Length)
			{
				destinationString[i] = Convert.ToUInt16(sourceString[i]);
			}
			else
			{
				destinationString[i] = 0;
			}
		}
		return destinationString;
	}

	private static bool GetProcs()
	{
		if (IntPtr.Size != 8)
		{
			return false;
		}
		if (LoadLibrary("nvapi64.dll") == IntPtr.Zero)
		{
			return false;
		}
		try
		{
			CreateApplication = Marshal.GetDelegateForFunctionPointer(QueryInterface(1128770014u), typeof(CreateApplicationDelegate)) as CreateApplicationDelegate;
			CreateProfile = Marshal.GetDelegateForFunctionPointer(QueryInterface(3424084072u), typeof(CreateProfileDelegate)) as CreateProfileDelegate;
			CreateSession = Marshal.GetDelegateForFunctionPointer(QueryInterface(110417198u), typeof(CreateSessionDelegate)) as CreateSessionDelegate;
			DeleteProfile = Marshal.GetDelegateForFunctionPointer(QueryInterface(386478598u), typeof(DeleteProfileDelegate)) as DeleteProfileDelegate;
			DestroySession = Marshal.GetDelegateForFunctionPointer(QueryInterface(3671707640u), typeof(DestroySessionDelegate)) as DestroySessionDelegate;
			EnumApplications = Marshal.GetDelegateForFunctionPointer(QueryInterface(2141329210u), typeof(EnumApplicationsDelegate)) as EnumApplicationsDelegate;
			FindProfileByName = Marshal.GetDelegateForFunctionPointer(QueryInterface(2118818315u), typeof(FindProfileByNameDelegate)) as FindProfileByNameDelegate;
			GetProfileInfo = Marshal.GetDelegateForFunctionPointer(QueryInterface(1640853462u), typeof(GetProfileInfoDelegate)) as GetProfileInfoDelegate;
			Initialize = Marshal.GetDelegateForFunctionPointer(QueryInterface(22079528u), typeof(InitializeDelegate)) as InitializeDelegate;
			LoadSettings = Marshal.GetDelegateForFunctionPointer(QueryInterface(928890219u), typeof(LoadSettingsDelegate)) as LoadSettingsDelegate;
			SaveSettings = Marshal.GetDelegateForFunctionPointer(QueryInterface(4240211476u), typeof(SaveSettingsDelegate)) as SaveSettingsDelegate;
			SetSetting = Marshal.GetDelegateForFunctionPointer(QueryInterface(1467863554u), typeof(SetSettingDelegate)) as SetSettingDelegate;
		}
		catch (Exception)
		{
			return false;
		}
		return true;
	}

	private unsafe static bool ContainsApplication(nint session, nint profile, OptimusProfile profileDescriptor, ushort[] unicodeApplicationName, out OptimusApplication application)
	{
		application = default(OptimusApplication);
		if (profileDescriptor.numOfApps == 0)
		{
			return false;
		}
		OptimusApplication[] array = new OptimusApplication[profileDescriptor.numOfApps];
		uint numAppsRead = profileDescriptor.numOfApps;
		fixed (OptimusApplication* allApplicationsPointer = array)
		{
			allApplicationsPointer->version = 147464u;
			if (CheckForError(EnumApplications(session, profile, 0u, ref numAppsRead, allApplicationsPointer)))
			{
				return false;
			}
			for (uint i = 0u; i < numAppsRead; i++)
			{
				if (UnicodeStringCompare(allApplicationsPointer[i].appName, unicodeApplicationName))
				{
					application = allApplicationsPointer[i];
					return true;
				}
			}
		}
		return false;
	}

	public static bool SOP_CheckProfile(string profileName)
	{
		if (!GetProcs() || CheckForError(Initialize()))
		{
			return false;
		}
		if (CheckForError(CreateSession(out var session)))
		{
			return false;
		}
		if (CheckForError(LoadSettings(session)))
		{
			return false;
		}
		GetUnicodeString(profileName);
		nint profile;
		bool result = FindProfileByName(session, profileName, out profile) == 0;
		DestroySession(session);
		return result;
	}

	public static int SOP_RemoveProfile(string profileName)
	{
		int result = 0;
		int status = 0;
		if (!GetProcs() || CheckForError(Initialize()))
		{
			return -1;
		}
		if (CheckForError(CreateSession(out var session)))
		{
			return -1;
		}
		if (CheckForError(LoadSettings(session)))
		{
			return -1;
		}
		GetUnicodeString(profileName);
		nint profile;
		switch (FindProfileByName(session, profileName, out profile))
		{
		case 0:
			if (CheckForError(DeleteProfile(session, profile)) || CheckForError(SaveSettings(session)))
			{
				return -1;
			}
			result = 1;
			break;
		case -163:
			result = 0;
			break;
		default:
			return -1;
		}
		status = DestroySession(session);
		return result;
	}

	public unsafe static int SOP_SetProfile(string profileName, string applicationName)
	{
		int result = 0;
		int status = 0;
		if (!GetProcs() || CheckForError(Initialize()))
		{
			return -1;
		}
		if (CheckForError(CreateSession(out var session)))
		{
			return -1;
		}
		if (CheckForError(LoadSettings(session)))
		{
			return -1;
		}
		ushort[] unicodeProfileName = GetUnicodeString(profileName);
		ushort[] unicodeApplicationName = GetUnicodeString(applicationName);
		status = FindProfileByName(session, profileName, out var profile);
		if (status == -163)
		{
			OptimusProfile newProfileDescriptor = default(OptimusProfile);
			newProfileDescriptor.version = 69652u;
			newProfileDescriptor.isPredefined = 0u;
			for (int j = 0; j < 2048; j++)
			{
				newProfileDescriptor.profileName[j] = unicodeProfileName[j];
			}
			fixed (uint* gpuSupport = new uint[32])
			{
				newProfileDescriptor.gpuSupport = gpuSupport;
				*newProfileDescriptor.gpuSupport = 1u;
			}
			if (CheckForError(CreateProfile(session, ref newProfileDescriptor, out profile)))
			{
				return -1;
			}
			OptimusSetting optimusSetting = default(OptimusSetting);
			optimusSetting.version = 77856u;
			optimusSetting.settingID = 284810369u;
			optimusSetting.u32CurrentValue = 17u;
			if (CheckForError(SetSetting(session, profile, ref optimusSetting)))
			{
				return -1;
			}
			optimusSetting = default(OptimusSetting);
			optimusSetting.version = 77856u;
			optimusSetting.settingID = 274197361u;
			optimusSetting.u32CurrentValue = 1u;
			if (CheckForError(SetSetting(session, profile, ref optimusSetting)))
			{
				return -1;
			}
		}
		else if (CheckForError(status))
		{
			return -1;
		}
		OptimusProfile profileDescriptorManaged = default(OptimusProfile);
		profileDescriptorManaged.version = 69652u;
		if (CheckForError(GetProfileInfo(session, profile, ref profileDescriptorManaged)))
		{
			return -1;
		}
		OptimusApplication applicationDescriptor = default(OptimusApplication);
		if (!ContainsApplication(session, profile, profileDescriptorManaged, GetUnicodeString(applicationName.ToLower()), out applicationDescriptor))
		{
			applicationDescriptor.version = 147464u;
			applicationDescriptor.isPredefined = 0u;
			for (int i = 0; i < 2048; i++)
			{
				applicationDescriptor.appName[i] = unicodeApplicationName[i];
			}
			if (CheckForError(CreateApplication(session, profile, ref applicationDescriptor)) || CheckForError(SaveSettings(session)))
			{
				return -1;
			}
			result = 1;
		}
		status = DestroySession(session);
		return result;
	}
}
