using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class AiTaskIdle : AiTaskBase
{
	public int minduration;

	public int maxduration;

	public float chance;

	public AssetLocation onBlockBelowCode;

	public long idleUntilMs;

	private bool entityWasInRange;

	private long lastEntityInRangeTestTotalMs;

	public DayTimeFrame[] duringDayTimeFrames;

	private string[] stopOnNearbyEntityCodesExact;

	private string[] stopOnNearbyEntityCodesBeginsWith = new string[0];

	private string targetEntityFirstLetters = "";

	private float stopRange;

	private bool stopOnHurt;

	private EntityPartitioning partitionUtil;

	private bool stopNow;

	private float tamingGenerations = 10f;

	public AiTaskIdle(EntityAgent entity)
		: base(entity)
	{
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		partitionUtil = entity.Api.ModLoader.GetModSystem<EntityPartitioning>();
		minduration = taskConfig["minduration"].AsInt(2000);
		maxduration = taskConfig["maxduration"].AsInt(4000);
		chance = taskConfig["chance"].AsFloat(1.1f);
		string code = taskConfig["onBlockBelowCode"].AsString();
		tamingGenerations = taskConfig["tamingGenerations"].AsFloat(10f);
		if (code != null && code.Length > 0)
		{
			onBlockBelowCode = new AssetLocation(code);
		}
		stopRange = taskConfig["stopRange"].AsFloat();
		stopOnHurt = taskConfig["stopOnHurt"].AsBool();
		duringDayTimeFrames = taskConfig["duringDayTimeFrames"].AsObject<DayTimeFrame[]>();
		string[] codes = taskConfig["stopOnNearbyEntityCodes"].AsArray(new string[1] { "player" });
		List<string> exact = new List<string>();
		List<string> beginswith = new List<string>();
		foreach (string ecode in codes)
		{
			if (ecode.EndsWith('*'))
			{
				beginswith.Add(ecode.Substring(0, ecode.Length - 1));
			}
			else
			{
				exact.Add(ecode);
			}
		}
		stopOnNearbyEntityCodesExact = exact.ToArray();
		stopOnNearbyEntityCodesBeginsWith = beginswith.ToArray();
		string[] array = stopOnNearbyEntityCodesExact;
		foreach (string scode2 in array)
		{
			if (scode2.Length != 0)
			{
				char c2 = scode2[0];
				if (targetEntityFirstLetters.IndexOf(c2) < 0)
				{
					ReadOnlySpan<char> readOnlySpan = targetEntityFirstLetters;
					char reference = c2;
					targetEntityFirstLetters = string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference));
				}
			}
		}
		array = stopOnNearbyEntityCodesBeginsWith;
		foreach (string scode in array)
		{
			if (scode.Length != 0)
			{
				char c = scode[0];
				if (targetEntityFirstLetters.IndexOf(c) < 0)
				{
					ReadOnlySpan<char> readOnlySpan2 = targetEntityFirstLetters;
					char reference = c;
					targetEntityFirstLetters = string.Concat(readOnlySpan2, new ReadOnlySpan<char>(in reference));
				}
			}
		}
		if (maxduration < 0)
		{
			idleUntilMs = -1L;
		}
		else
		{
			idleUntilMs = entity.World.ElapsedMilliseconds + minduration + entity.World.Rand.Next(maxduration - minduration);
		}
		int generation = entity.WatchedAttributes.GetInt("generation");
		float fearReductionFactor = Math.Max(0f, (tamingGenerations - (float)generation) / tamingGenerations);
		if (whenInEmotionState != null)
		{
			fearReductionFactor = 1f;
		}
		stopRange *= fearReductionFactor;
		base.LoadConfig(taskConfig, aiConfig);
		lastEntityInRangeTestTotalMs = entity.World.ElapsedMilliseconds - entity.World.Rand.Next(1500);
	}

	public override bool ShouldExecute()
	{
		long ellapsedMs = entity.World.ElapsedMilliseconds;
		if (cooldownUntilMs < ellapsedMs && entity.World.Rand.NextDouble() < (double)chance)
		{
			if (entity.Properties.Habitat == EnumHabitat.Land && entity.FeetInLiquid)
			{
				return false;
			}
			if (!PreconditionsSatisifed())
			{
				return false;
			}
			if (ellapsedMs - lastEntityInRangeTestTotalMs > 2000)
			{
				entityWasInRange = entityInRange();
				lastEntityInRangeTestTotalMs = ellapsedMs;
			}
			if (entityWasInRange)
			{
				return false;
			}
			if (duringDayTimeFrames != null)
			{
				bool match = false;
				double hourOfDay = (double)(entity.World.Calendar.HourOfDay / entity.World.Calendar.HoursPerDay * 24f) + (entity.World.Rand.NextDouble() * 0.30000001192092896 - 0.15000000596046448);
				int i = 0;
				while (!match && i < duringDayTimeFrames.Length)
				{
					match |= duringDayTimeFrames[i].Matches(hourOfDay);
					i++;
				}
				if (!match)
				{
					return false;
				}
			}
			Block belowBlock = entity.World.BlockAccessor.GetBlockRaw((int)entity.ServerPos.X, (int)entity.ServerPos.InternalY - 1, (int)entity.ServerPos.Z, 1);
			if (!belowBlock.SideSolid[BlockFacing.UP.Index])
			{
				return false;
			}
			if (onBlockBelowCode == null)
			{
				return true;
			}
			Block block = entity.World.BlockAccessor.GetBlockRaw((int)entity.ServerPos.X, (int)entity.ServerPos.InternalY, (int)entity.ServerPos.Z);
			if (!block.WildCardMatch(onBlockBelowCode))
			{
				if (block.Replaceable >= 6000)
				{
					return belowBlock.WildCardMatch(onBlockBelowCode);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		if (maxduration < 0)
		{
			idleUntilMs = -1L;
		}
		else
		{
			idleUntilMs = entity.World.ElapsedMilliseconds + minduration + entity.World.Rand.Next(maxduration - minduration);
		}
		entity.IdleSoundChanceModifier = 0f;
		stopNow = false;
	}

	public override bool ContinueExecute(float dt)
	{
		if (base.rand.NextDouble() < 0.30000001192092896)
		{
			long ellapsedMs = entity.World.ElapsedMilliseconds;
			if (ellapsedMs - lastEntityInRangeTestTotalMs > 1500 && stopOnNearbyEntityCodesExact != null)
			{
				entityWasInRange = entityInRange();
				lastEntityInRangeTestTotalMs = ellapsedMs;
			}
			if (entityWasInRange)
			{
				return false;
			}
			if (duringDayTimeFrames != null)
			{
				bool match = false;
				double hourOfDay = entity.World.Calendar.HourOfDay / entity.World.Calendar.HoursPerDay * 24f;
				int i = 0;
				while (!match && i < duringDayTimeFrames.Length)
				{
					match |= duringDayTimeFrames[i].Matches(hourOfDay);
					i++;
				}
				if (!match)
				{
					return false;
				}
			}
		}
		if (!stopNow)
		{
			if (idleUntilMs >= 0)
			{
				return entity.World.ElapsedMilliseconds < idleUntilMs;
			}
			return true;
		}
		return false;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		entity.IdleSoundChanceModifier = 1f;
	}

	private bool entityInRange()
	{
		if (stopRange <= 0f)
		{
			return false;
		}
		bool found = false;
		partitionUtil.WalkEntities(entity.ServerPos.XYZ, stopRange, delegate(Entity e)
		{
			if (!e.Alive || e.EntityId == entity.EntityId || !e.IsInteractable)
			{
				return true;
			}
			string path = e.Code.Path;
			if (targetEntityFirstLetters.IndexOf(path[0]) < 0)
			{
				return true;
			}
			for (int i = 0; i < stopOnNearbyEntityCodesExact.Length; i++)
			{
				if (path == stopOnNearbyEntityCodesExact[i])
				{
					if (e is EntityPlayer entityPlayer)
					{
						IPlayer player = entity.World.PlayerByUid(entityPlayer.PlayerUID);
						if (player == null || (player.WorldData.CurrentGameMode != EnumGameMode.Creative && player.WorldData.CurrentGameMode != EnumGameMode.Spectator))
						{
							found = true;
							return false;
						}
						return false;
					}
					found = true;
					return false;
				}
			}
			for (int j = 0; j < stopOnNearbyEntityCodesBeginsWith.Length; j++)
			{
				if (path.StartsWithFast(stopOnNearbyEntityCodesBeginsWith[j]))
				{
					found = true;
					return false;
				}
			}
			return true;
		}, EnumEntitySearchType.Creatures);
		return found;
	}

	public override void OnEntityHurt(DamageSource source, float damage)
	{
		if (stopOnHurt)
		{
			stopNow = true;
		}
	}
}
