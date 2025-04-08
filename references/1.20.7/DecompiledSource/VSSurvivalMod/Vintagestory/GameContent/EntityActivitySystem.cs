using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Essentials;

namespace Vintagestory.GameContent;

public class EntityActivitySystem
{
	public List<IEntityActivity> AvailableActivities = new List<IEntityActivity>();

	public Dictionary<int, IEntityActivity> ActiveActivitiesBySlot = new Dictionary<int, IEntityActivity>();

	public PathTraverserBase linepathTraverser;

	public WaypointsTraverser wppathTraverser;

	public EntityAgent Entity;

	private float accum;

	private BlockPos activityOffset;

	private bool pauseAutoSelection;

	private bool clearDelay;

	public string Code { get; set; }

	public bool Debug { get; set; } = ActivityModSystem.Debug;


	public BlockPos ActivityOffset
	{
		get
		{
			if (activityOffset == null)
			{
				activityOffset = Entity.WatchedAttributes.GetBlockPos("importOffset", new BlockPos(Entity.Pos.Dimension));
			}
			return activityOffset;
		}
		set
		{
			activityOffset = value;
			Entity.WatchedAttributes.SetBlockPos("importOffset", activityOffset);
		}
	}

	public EntityActivitySystem(EntityAgent entity)
	{
		Entity = entity;
	}

	public bool StartActivity(string code, float priority = 9999f, int slot = -1)
	{
		int index = AvailableActivities.IndexOf((IEntityActivity item) => item.Code == code);
		if (index < 0)
		{
			return false;
		}
		IEntityActivity activity = AvailableActivities[index];
		if (slot < 0)
		{
			slot = activity.Slot;
		}
		if (priority < 0f)
		{
			priority = (float)activity.Priority;
		}
		if (ActiveActivitiesBySlot.TryGetValue(activity.Slot, out var activeAct))
		{
			if (activeAct.Priority > (double)priority)
			{
				return false;
			}
			activeAct.Cancel();
		}
		ActiveActivitiesBySlot[activity.Slot] = activity;
		activity.Priority = priority;
		activity.Start();
		return true;
	}

	public bool CancelAll()
	{
		bool stopped = false;
		foreach (IEntityActivity val in ActiveActivitiesBySlot.Values)
		{
			if (val != null)
			{
				val.Cancel();
				stopped = true;
			}
		}
		return stopped;
	}

	public void PauseAutoSelection(bool paused)
	{
		pauseAutoSelection = paused;
	}

	public void Pause()
	{
		foreach (IEntityActivity value in ActiveActivitiesBySlot.Values)
		{
			value?.Pause();
		}
	}

	public void Resume()
	{
		foreach (IEntityActivity value in ActiveActivitiesBySlot.Values)
		{
			value?.Resume();
		}
	}

	public void ClearNextActionDelay()
	{
		clearDelay = true;
	}

	public void OnTick(float dt)
	{
		linepathTraverser.OnGameTick(dt);
		wppathTraverser.OnGameTick(dt);
		accum += dt;
		if ((double)accum < 0.25 && !clearDelay)
		{
			return;
		}
		clearDelay = false;
		foreach (int key in ActiveActivitiesBySlot.Keys)
		{
			IEntityActivity activity2 = ActiveActivitiesBySlot[key];
			if (activity2 == null)
			{
				continue;
			}
			if (activity2.Finished)
			{
				activity2.Finish();
				Entity.Attributes.SetString("lastActivity", activity2.Code);
				if (Debug)
				{
					Entity.World.Logger.Debug("ActivitySystem entity {0} activity {1} has finished", Entity.EntityId, activity2.Name);
				}
				ActiveActivitiesBySlot.Remove(key);
			}
			else
			{
				activity2.OnTick(accum);
				Entity.World.FrameProfiler.Mark("behavior-activitydriven-tick-" + activity2.Code);
			}
		}
		accum = 0f;
		if (!pauseAutoSelection)
		{
			foreach (IEntityActivity activity in AvailableActivities)
			{
				int slot = activity.Slot;
				if (ActiveActivitiesBySlot.TryGetValue(slot, out var activeActivity) && activeActivity != null && activeActivity.Priority >= activity.Priority)
				{
					continue;
				}
				bool execute = activity.ConditionsOp == EnumConditionLogicOp.AND;
				for (int i = 0; i < activity.Conditions.Length; i++)
				{
					if (!execute && activity.ConditionsOp != 0)
					{
						break;
					}
					IActionCondition obj = activity.Conditions[i];
					bool ok = obj.ConditionSatisfied(Entity);
					if (obj.Invert)
					{
						ok = !ok;
					}
					if (activity.ConditionsOp == EnumConditionLogicOp.OR)
					{
						if (Debug && ok)
						{
							Entity.World.Logger.Debug("ActivitySystem entity {0} activity condition {1} is satisfied, will execute {2}", Entity.EntityId, activity.Conditions[i].Type, activity.Name);
						}
						execute = execute || ok;
					}
					else
					{
						execute = execute && ok;
					}
				}
				if (execute)
				{
					ActiveActivitiesBySlot.TryGetValue(slot, out var act);
					act?.Cancel();
					ActiveActivitiesBySlot[slot] = activity;
					activity?.Start();
				}
			}
		}
		if (!Entity.World.EntityDebugMode)
		{
			return;
		}
		StringBuilder sb = new StringBuilder();
		foreach (KeyValuePair<int, IEntityActivity> val in ActiveActivitiesBySlot)
		{
			sb.Append(val.Key + ": " + val.Value.Name + "/" + val.Value.CurrentAction?.Type);
		}
		Entity.DebugAttributes.SetString("activities", sb.ToString());
	}

	public bool Load(AssetLocation activityCollectionPath)
	{
		linepathTraverser = new StraightLineTraverser(Entity);
		wppathTraverser = new WaypointsTraverser(Entity);
		AvailableActivities.Clear();
		ActiveActivitiesBySlot.Clear();
		if (activityCollectionPath == null)
		{
			return false;
		}
		IAsset file = Entity.Api.Assets.TryGet(activityCollectionPath.WithPathPrefixOnce("config/activitycollections/").WithPathAppendixOnce(".json"));
		if (file == null)
		{
			Entity.World.Logger.Error(string.Concat("Unable to load activity file ", activityCollectionPath, " not such file found"));
			return false;
		}
		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.All
		};
		EntityActivityCollection coll = file.ToObject<EntityActivityCollection>(settings);
		AvailableActivities.AddRange(coll.Activities);
		coll.OnLoaded(this);
		return true;
	}

	public void StoreState(TreeAttribute attributes, bool forClient)
	{
		if (!forClient)
		{
			storeStateActivities(attributes, "executingActions", ActiveActivitiesBySlot.Values);
		}
	}

	public void LoadState(TreeAttribute attributes, bool forClient)
	{
		if (forClient)
		{
			return;
		}
		ActiveActivitiesBySlot.Clear();
		foreach (IEntityActivity val in loadStateActivities(attributes, "executingActions"))
		{
			ActiveActivitiesBySlot[val.Slot] = val;
		}
	}

	private void storeStateActivities(TreeAttribute attributes, string key, IEnumerable<IEntityActivity> activities)
	{
		ITreeAttribute ctree = (ITreeAttribute)(attributes[key] = new TreeAttribute());
		int i = 0;
		foreach (IEntityActivity activity in activities)
		{
			ITreeAttribute attr = new TreeAttribute();
			activity.StoreState(attr);
			ctree["activitiy" + i++] = attr;
		}
	}

	private IEnumerable<IEntityActivity> loadStateActivities(TreeAttribute attributes, string key)
	{
		List<IEntityActivity> activities = new List<IEntityActivity>();
		ITreeAttribute ctree = attributes.GetTreeAttribute(key);
		if (ctree == null)
		{
			return activities;
		}
		int i = 0;
		while (i < 200)
		{
			ITreeAttribute tree = ctree.GetTreeAttribute("activity" + i++);
			if (tree == null)
			{
				break;
			}
			EntityActivity activity = new EntityActivity();
			activity.LoadState(tree);
			activities.Add(activity);
		}
		return activities;
	}
}
