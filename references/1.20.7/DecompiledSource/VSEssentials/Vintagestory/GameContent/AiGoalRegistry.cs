using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public static class AiGoalRegistry
{
	public static Dictionary<string, Type> GoalTypes;

	public static Dictionary<Type, string> GoalCodes;

	public static Dictionary<string, Type> ActionTypes;

	public static Dictionary<Type, string> ActionCodes;

	public static void RegisterGoal<T>(string code) where T : AiGoalBase
	{
		GoalTypes[code] = typeof(T);
		GoalCodes[typeof(T)] = code;
	}

	public static void RegisterAction<T>(string code) where T : AiActionBase
	{
		ActionTypes[code] = typeof(T);
		ActionCodes[typeof(T)] = code;
	}

	static AiGoalRegistry()
	{
		GoalTypes = new Dictionary<string, Type>();
		GoalCodes = new Dictionary<Type, string>();
		ActionTypes = new Dictionary<string, Type>();
		ActionCodes = new Dictionary<Type, string>();
	}
}
