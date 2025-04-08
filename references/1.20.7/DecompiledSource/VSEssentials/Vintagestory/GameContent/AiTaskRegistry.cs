using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public static class AiTaskRegistry
{
	public static Dictionary<string, Type> TaskTypes;

	public static Dictionary<Type, string> TaskCodes;

	public static void Register(string code, Type type)
	{
		TaskTypes[code] = type;
		TaskCodes[type] = code;
	}

	public static void Register<T>(string code) where T : AiTaskBase
	{
		TaskTypes[code] = typeof(T);
		TaskCodes[typeof(T)] = code;
	}

	static AiTaskRegistry()
	{
		TaskTypes = new Dictionary<string, Type>();
		TaskCodes = new Dictionary<Type, string>();
		Register("wander", typeof(AiTaskWander));
		Register("lookaround", typeof(AiTaskLookAround));
		Register("meleeattack", typeof(AiTaskMeleeAttack));
		Register("seekentity", typeof(AiTaskSeekEntity));
		Register("fleeentity", typeof(AiTaskFleeEntity));
		Register("stayclosetoentity", typeof(AiTaskStayCloseToEntity));
		Register("getoutofwater", typeof(AiTaskGetOutOfWater));
		Register("idle", typeof(AiTaskIdle));
		Register("seekfoodandeat", typeof(AiTaskSeekFoodAndEat));
		Register("seekblockandlay", typeof(AiTaskSeekBlockAndLay));
		Register("useinventory", typeof(AiTaskUseInventory));
		Register("meleeattacktargetingentity", typeof(AiTaskMeleeAttackTargetingEntity));
		Register("seektargetingentity", typeof(AiTaskSeekTargetingEntity));
		Register("stayclosetoguardedentity", typeof(AiTaskStayCloseToGuardedEntity));
		Register("jealousmeleeattack", typeof(AiTaskJealousMeleeAttack));
		Register("jealousseekentity", typeof(AiTaskJealousSeekEntity));
		Register("gotoentity", typeof(AiTaskGotoEntity));
		Register("lookatentity", typeof(AiTaskLookAtEntity));
	}
}
