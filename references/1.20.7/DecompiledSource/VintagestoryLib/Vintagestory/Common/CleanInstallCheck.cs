using System;
using System.IO;
using Vintagestory.API.Config;

namespace Vintagestory.Common;

public class CleanInstallCheck
{
	public static bool IsCleanInstall()
	{
		if (RuntimeEnv.IsDevEnvironment)
		{
			return true;
		}
		string basePath = AppDomain.CurrentDomain.BaseDirectory;
		if (!File.Exists(Path.Combine(basePath, "assets", "survival", "itemtypes", "bag", "backpack.json")))
		{
			bool num = File.Exists(Path.Combine(basePath, "assets", "version-1.20.7.txt"));
			string[] strings = Directory.GetFiles(Path.Combine(basePath, "assets"), "version-*.txt", SearchOption.TopDirectoryOnly);
			if (num)
			{
				return strings.Length == 1;
			}
			return false;
		}
		return false;
	}
}
