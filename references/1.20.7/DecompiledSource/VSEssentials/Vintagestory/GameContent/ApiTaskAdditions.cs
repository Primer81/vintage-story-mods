using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public static class ApiTaskAdditions
{
	public static void RegisterAiTask(this ICoreServerAPI sapi, string code, Type type)
	{
		AiTaskRegistry.Register(code, type);
	}

	public static void RegisterAiTask<T>(this ICoreServerAPI sapi, string code) where T : AiTaskBase
	{
		AiTaskRegistry.Register<T>(code);
	}
}
