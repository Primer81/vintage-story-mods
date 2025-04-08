using System;
using System.Collections.Generic;
using System.Linq;
using VSEssentialsMod.Entity.AI.Task;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Essentials;

namespace Vintagestory.GameContent;

public class EntityBehaviorEmotionStates : EntityBehavior
{
	private EmotionState[] availableStates;

	public Dictionary<string, ActiveEmoState> ActiveStatesByCode = new Dictionary<string, ActiveEmoState>();

	private TreeAttribute entityAttr;

	private float healthRel;

	private float tickAccum;

	private EntityPartitioning epartSys;

	private EnumCreatureHostility _enumCreatureHostility;

	private PathfinderTask pathtask;

	private int nopathEmoStateid;

	private long sourceEntityId;

	public EntityBehaviorEmotionStates(Entity entity)
		: base(entity)
	{
		if (entity.Attributes.HasAttribute("emotionstates"))
		{
			entityAttr = entity.Attributes["emotionstates"] as TreeAttribute;
		}
		else
		{
			entity.Attributes["emotionstates"] = (entityAttr = new TreeAttribute());
		}
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		base.Initialize(properties, typeAttributes);
		JsonObject[] availStates = typeAttributes["states"].AsArray();
		availableStates = new EmotionState[availStates.Length];
		int i = 0;
		JsonObject[] array = availStates;
		for (int j = 0; j < array.Length; j++)
		{
			EmotionState state = array[j].AsObject<EmotionState>();
			availableStates[i++] = state;
			if (state.EntityCodes != null)
			{
				state.EntityCodeLocs = state.EntityCodes.Select((string str) => new AssetLocation(str)).ToArray();
			}
		}
		tickAccum = (float)(entity.World.Rand.NextDouble() * 0.33);
		epartSys = entity.Api.ModLoader.GetModSystem<EntityPartitioning>();
		_enumCreatureHostility = entity.World.Config.GetString("creatureHostility") switch
		{
			"aggressive" => EnumCreatureHostility.Aggressive, 
			"passive" => EnumCreatureHostility.Passive, 
			"off" => EnumCreatureHostility.NeverHostile, 
			_ => EnumCreatureHostility.Aggressive, 
		};
	}

	public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
	{
		if (damageSource.Source == EnumDamageSource.Fall && _enumCreatureHostility == EnumCreatureHostility.Passive && _enumCreatureHostility == EnumCreatureHostility.NeverHostile)
		{
			return;
		}
		EntityBehaviorHealth beh = entity.GetBehavior<EntityBehaviorHealth>();
		healthRel = ((beh == null) ? 1f : (beh.Health / beh.MaxHealth));
		Entity damagedBy = damageSource.GetCauseEntity();
		long sourceEntityId = damagedBy?.EntityId ?? 0;
		if (TryTriggerState("alarmherdondamage", sourceEntityId) && damagedBy != null && (entity as EntityAgent).HerdId > 0)
		{
			EmotionState state = availableStates.First((EmotionState s) => s.Code == "alarmherdondamage");
			entity.World.GetNearestEntity(entity.ServerPos.XYZ, state.NotifyRange, state.NotifyRange, delegate(Entity e)
			{
				EntityAgent entityAgent = e as EntityAgent;
				if (e.EntityId != entity.EntityId && entityAgent != null && entityAgent.Alive && entityAgent.HerdId == (entity as EntityAgent).HerdId)
				{
					entityAgent.GetBehavior<EntityBehaviorEmotionStates>().TryTriggerState("aggressiveondamage", sourceEntityId);
				}
				return false;
			});
		}
		if (TryTriggerState("aggressiveondamage", sourceEntityId))
		{
			TryTriggerState("aggressivealarmondamage", sourceEntityId);
		}
		if (TryTriggerState("fleeondamage", sourceEntityId))
		{
			TryTriggerState("fleealarmondamage", sourceEntityId);
		}
	}

	public bool IsInEmotionState(string statecode)
	{
		return ActiveStatesByCode.ContainsKey(statecode);
	}

	public void ClearStates()
	{
		ActiveStatesByCode.Clear();
	}

	public ActiveEmoState GetActiveEmotionState(string statecode)
	{
		ActiveStatesByCode.TryGetValue(statecode, out var state);
		return state;
	}

	public bool TryTriggerState(string statecode, long sourceEntityId)
	{
		return TryTriggerState(statecode, entity.World.Rand.NextDouble(), sourceEntityId);
	}

	public bool TryTriggerState(string statecode, double rndValue, long sourceEntityId)
	{
		bool triggered = false;
		for (int stateid = 0; stateid < availableStates.Length; stateid++)
		{
			EmotionState newstate = availableStates[stateid];
			if (!(newstate.Code != statecode) && !(rndValue > (double)newstate.Chance))
			{
				if (newstate.whenSourceUntargetable)
				{
					TryTarget(stateid, sourceEntityId);
				}
				else if (tryActivateState(stateid, sourceEntityId))
				{
					triggered = true;
				}
			}
		}
		return triggered;
	}

	private void TryTarget(int emostateid, long sourceEntityId)
	{
		if (pathtask == null)
		{
			ICoreAPI api = entity.World.Api;
			PathfindingAsync asyncPathfinder = api.ModLoader.GetModSystem<PathfindingAsync>();
			WaypointsTraverser wptrav = entity.GetBehavior<EntityBehaviorTaskAI>()?.PathTraverser;
			if (wptrav != null)
			{
				pathtask = wptrav.PreparePathfinderTask(entity.ServerPos.AsBlockPos, api.World.GetEntityById(sourceEntityId).ServerPos.AsBlockPos);
				asyncPathfinder.EnqueuePathfinderTask(pathtask);
				nopathEmoStateid = emostateid;
				this.sourceEntityId = sourceEntityId;
			}
		}
	}

	private bool tryActivateState(int stateid, long sourceEntityId)
	{
		EmotionState newstate = availableStates[stateid];
		string statecode = newstate.Code;
		ActiveEmoState activeState = null;
		if (newstate.whenHealthRelBelow < healthRel)
		{
			return false;
		}
		foreach (KeyValuePair<string, ActiveEmoState> val in ActiveStatesByCode)
		{
			if (val.Key == newstate.Code)
			{
				activeState = val.Value;
				continue;
			}
			int activestateid = val.Value.StateId;
			EmotionState activestate = availableStates[activestateid];
			if (activestate.Slot != newstate.Slot)
			{
				continue;
			}
			if (activestate.Priority > newstate.Priority)
			{
				return false;
			}
			ActiveStatesByCode.Remove(val.Key);
			entityAttr.RemoveAttribute(newstate.Code);
			break;
		}
		if (newstate.MaxGeneration < entity.WatchedAttributes.GetInt("generation"))
		{
			return false;
		}
		if (statecode == "aggressivearoundentities" && (activeState != null || !entitiesNearby(newstate)))
		{
			return false;
		}
		float duration = newstate.Duration;
		if (newstate.BelowTempThreshold > -99f && entity.World.BlockAccessor.GetClimateAt(entity.Pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, entity.World.Calendar.TotalDays).Temperature < newstate.BelowTempThreshold)
		{
			duration = newstate.BelowTempDuration;
		}
		float newDuration = 0f;
		if (newstate.AccumType == EnumAccumType.Sum)
		{
			newDuration = activeState?.Duration ?? (0f + duration);
		}
		if (newstate.AccumType == EnumAccumType.Max)
		{
			newDuration = Math.Max(activeState?.Duration ?? 0f, duration);
		}
		if (newstate.AccumType == EnumAccumType.NoAccum)
		{
			newDuration = ((activeState == null || !(activeState.Duration > 0f)) ? duration : (activeState?.Duration ?? 0f));
		}
		if (activeState == null)
		{
			ActiveStatesByCode[newstate.Code] = new ActiveEmoState
			{
				Duration = newDuration,
				SourceEntityId = sourceEntityId,
				StateId = stateid
			};
		}
		else
		{
			activeState.SourceEntityId = sourceEntityId;
		}
		entityAttr.SetFloat(newstate.Code, newDuration);
		return true;
	}

	public override void OnGameTick(float deltaTime)
	{
		if (pathtask != null && pathtask.Finished)
		{
			if (pathtask.waypoints == null)
			{
				tryActivateState(nopathEmoStateid, sourceEntityId);
			}
			pathtask = null;
			nopathEmoStateid = 0;
			sourceEntityId = 0L;
		}
		if ((tickAccum += deltaTime) < 0.33f)
		{
			return;
		}
		tickAccum = 0f;
		if (_enumCreatureHostility == EnumCreatureHostility.Aggressive)
		{
			TryTriggerState("aggressivearoundentities", 0L);
		}
		float nowStressLevel = 0f;
		List<string> codesToRemove = null;
		foreach (KeyValuePair<string, ActiveEmoState> stateAndcode in ActiveStatesByCode)
		{
			string code = stateAndcode.Key;
			ActiveEmoState state = stateAndcode.Value;
			if ((state.Duration -= 10f * deltaTime) <= 0f)
			{
				if (codesToRemove == null)
				{
					codesToRemove = new List<string>();
				}
				codesToRemove.Add(code);
				entityAttr.RemoveAttribute(code);
			}
			else
			{
				nowStressLevel += availableStates[state.StateId].StressLevel;
			}
		}
		if (codesToRemove != null)
		{
			foreach (string s in codesToRemove)
			{
				ActiveStatesByCode.Remove(s);
			}
		}
		float curlevel = entity.WatchedAttributes.GetFloat("stressLevel");
		if (nowStressLevel > 0f)
		{
			entity.WatchedAttributes.SetFloat("stressLevel", Math.Max(curlevel, nowStressLevel));
		}
		else if (curlevel > 0f)
		{
			curlevel = Math.Max(0f, curlevel - deltaTime * 1.25f);
			entity.WatchedAttributes.SetFloat("stressLevel", curlevel);
		}
		if (entity.World.EntityDebugMode)
		{
			entity.DebugAttributes.SetString("emotionstates", string.Join(", ", ActiveStatesByCode.Keys.ToList()));
		}
	}

	private bool entitiesNearby(EmotionState newstate)
	{
		return epartSys.GetNearestEntity(entity.ServerPos.XYZ, newstate.NotifyRange, delegate(Entity e)
		{
			for (int i = 0; i < newstate.EntityCodeLocs.Length; i++)
			{
				if (newstate.EntityCodeLocs[i].Equals(e.Code))
				{
					return e.IsInteractable;
				}
			}
			return false;
		}, EnumEntitySearchType.Creatures) != null;
	}

	public override string PropertyName()
	{
		return "emotionstates";
	}
}
