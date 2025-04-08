using System;
using System.IO;
using System.Reflection;
using Vintagestory.API.Config;

namespace Vintagestory.Common;

public static class AssemblyResolver
{
	private static readonly string[] AssemblySearchPaths = new string[4]
	{
		GamePaths.Binaries,
		Path.Combine(GamePaths.Binaries, "Lib"),
		GamePaths.BinariesMods,
		GamePaths.DataPathMods
	};

	public static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
	{
		string dllName = new AssemblyName(args.Name).Name + ".dll";
		string assemblyPath = null;
		try
		{
			string[] assemblySearchPaths = AssemblySearchPaths;
			for (int i = 0; i < assemblySearchPaths.Length; i++)
			{
				assemblyPath = Path.Combine(assemblySearchPaths[i], dllName);
				if (File.Exists(assemblyPath))
				{
					return Assembly.LoadFrom(assemblyPath);
				}
			}
			return null;
		}
		catch (Exception ex)
		{
			throw new Exception($"Failed to load assembly '{args.Name}' from '{assemblyPath}'", ex);
		}
	}
}
