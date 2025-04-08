using System;
using System.IO;

namespace Vintagestory.API.Config;

public static class GamePaths
{
	public static string AllowedNameChars;

	public static string DataPath;

	public static string CustomLogPath;

	public static string DefaultSaveFilenameWithoutExtension;

	public static string AssetsPath { get; private set; }

	public static string Binaries => AppDomain.CurrentDomain.BaseDirectory;

	public static string BinariesMods => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods");

	public static string Config => DataPath;

	public static string ModConfig => Path.Combine(DataPath, "ModConfig");

	public static string Cache => Path.Combine(DataPath, "Cache");

	public static string Saves => Path.Combine(DataPath, "Saves");

	public static string OldSaves => Path.Combine(DataPath, "OldSaves");

	public static string BackupSaves => Path.Combine(DataPath, "BackupSaves");

	public static string PlayerData => Path.Combine(DataPath, "Playerdata");

	public static string Backups => Path.Combine(DataPath, "Backups");

	public static string Logs
	{
		get
		{
			if (CustomLogPath == null)
			{
				return Path.Combine(DataPath, "Logs");
			}
			return CustomLogPath;
		}
	}

	public static string Macros => Path.Combine(DataPath, "Macros");

	public static string Screenshots => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures, Environment.SpecialFolderOption.DoNotVerify), "Vintagestory");

	public static string Videos => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos, Environment.SpecialFolderOption.DoNotVerify), "Vintagestory");

	public static string DataPathMods => Path.Combine(DataPath, "Mods");

	public static string DataPathServerMods => Path.Combine(DataPath, "ModsByServer");

	static GamePaths()
	{
		AllowedNameChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890_-";
		DefaultSaveFilenameWithoutExtension = "default";
		if (RuntimeEnv.IsDevEnvironment)
		{
			DataPath = AppDomain.CurrentDomain.BaseDirectory;
		}
		else
		{
			DataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify), "VintagestoryData");
		}
		if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets")))
		{
			AssetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
		}
		else
		{
			AssetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "assets");
		}
	}

	public static void EnsurePathExists(string path)
	{
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
	}

	public static void EnsurePathsExist()
	{
		if (!Directory.Exists(Config))
		{
			Directory.CreateDirectory(Config);
		}
		if (!Directory.Exists(Cache))
		{
			Directory.CreateDirectory(Cache);
		}
		if (!Directory.Exists(Saves))
		{
			Directory.CreateDirectory(Saves);
		}
		if (!Directory.Exists(BackupSaves))
		{
			Directory.CreateDirectory(BackupSaves);
		}
		if (!Directory.Exists(PlayerData))
		{
			Directory.CreateDirectory(PlayerData);
		}
		if (!Directory.Exists(Backups))
		{
			Directory.CreateDirectory(Backups);
		}
		if (!Directory.Exists(Logs))
		{
			Directory.CreateDirectory(Logs);
		}
		if (!Directory.Exists(Macros))
		{
			Directory.CreateDirectory(Macros);
		}
		if (!Directory.Exists(DataPathMods))
		{
			Directory.CreateDirectory(DataPathMods);
		}
	}

	public static bool IsValidName(string s)
	{
		if (s.Length < 1 || s.Length > 128)
		{
			return false;
		}
		for (int i = 0; i < s.Length; i++)
		{
			if (!AllowedNameChars.Contains(s[i].ToString()))
			{
				return false;
			}
		}
		return true;
	}

	public static string ReplaceInvalidChars(string filename)
	{
		return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
	}
}
