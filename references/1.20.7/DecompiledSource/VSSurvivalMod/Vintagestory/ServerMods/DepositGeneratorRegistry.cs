using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.ServerMods;

public static class DepositGeneratorRegistry
{
	private static Dictionary<string, Type> Generators;

	static DepositGeneratorRegistry()
	{
		Generators = new Dictionary<string, Type>();
		RegisterDepositGenerator<FollowSurfaceDiscGenerator>("disc-followsurface");
		RegisterDepositGenerator<AnywhereDiscGenerator>("disc-anywhere");
		RegisterDepositGenerator<FollowSealevelDiscGenerator>("disc-followsealevel");
		RegisterDepositGenerator<FollowSurfaceBelowDiscGenerator>("disc-followsurfacebelow");
		RegisterDepositGenerator<ChildDepositGenerator>("childdeposit-pointcloud");
		RegisterDepositGenerator<AlluvialDepositGenerator>("disc-alluvialdeposit");
	}

	public static void RegisterDepositGenerator<T>(string code) where T : DepositGeneratorBase
	{
		Generators[code] = typeof(T);
	}

	public static DepositGeneratorBase CreateGenerator(string code, JsonObject attributes, params object[] args)
	{
		if (!Generators.ContainsKey(code))
		{
			return null;
		}
		DepositGeneratorBase generator = Activator.CreateInstance(Generators[code], args) as DepositGeneratorBase;
		attributes.Token.Populate(generator);
		generator.Init();
		return generator;
	}
}
