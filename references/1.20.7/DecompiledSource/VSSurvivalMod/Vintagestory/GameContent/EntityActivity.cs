using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class EntityActivity : IEntityActivity
{
	public int currentActionIndex = -1;

	private EntityActivitySystem vas;

	public double origPriority;

	[JsonProperty]
	public int Slot { get; set; }

	[JsonProperty]
	public double Priority { get; set; } = 1.0;


	[JsonProperty]
	public string Name { get; set; }

	[JsonProperty]
	public string Code { get; set; }

	[JsonProperty]
	public IActionCondition[] Conditions { get; set; } = new IActionCondition[0];


	[JsonProperty]
	public IEntityAction[] Actions { get; set; } = new IEntityAction[0];


	[JsonProperty]
	public EnumConditionLogicOp ConditionsOp { get; set; } = EnumConditionLogicOp.AND;


	public IEntityAction CurrentAction
	{
		get
		{
			if (currentActionIndex >= 0)
			{
				return Actions[currentActionIndex];
			}
			return null;
		}
	}

	public bool Finished { get; set; }

	public EntityActivity()
	{
	}

	public EntityActivity(EntityActivitySystem vas)
	{
		this.vas = vas;
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
		if (Code == null || Code.Length == 0)
		{
			Code = Name;
		}
		if (Actions != null)
		{
			IEntityAction[] actions = Actions;
			for (int i = 0; i < actions.Length; i++)
			{
				actions[i].OnLoaded(vas);
			}
		}
		if (Conditions != null)
		{
			IActionCondition[] conditions = Conditions;
			for (int i = 0; i < conditions.Length; i++)
			{
				conditions[i].OnLoaded(vas);
			}
		}
		origPriority = Priority;
	}

	public void Cancel()
	{
		CurrentAction?.Cancel();
		currentActionIndex = -1;
		Finished = true;
		Priority = origPriority;
	}

	public void Start()
	{
		Finished = false;
		currentActionIndex = 0;
		CurrentAction.Start(this);
		if (vas.Debug)
		{
			vas.Entity.World.Logger.Debug("ActivitySystem entity {0}, starting new Activity - {1}", vas.Entity.EntityId, Name);
			vas.Entity.World.Logger.Debug("starting next action {0}", CurrentAction?.Type);
		}
	}

	public void Finish()
	{
		CurrentAction?.Finish();
		Priority = origPriority;
	}

	public void Pause()
	{
		CurrentAction?.Pause();
	}

	public void Resume()
	{
		CurrentAction?.Resume();
	}

	public void OnTick(float dt)
	{
		if (CurrentAction == null)
		{
			return;
		}
		CurrentAction.OnTick(dt);
		if (!CurrentAction.IsFinished())
		{
			return;
		}
		CurrentAction.Finish();
		if (currentActionIndex < Actions.Length - 1)
		{
			currentActionIndex++;
			CurrentAction.Start(this);
			if (vas.Debug)
			{
				vas.Entity.World.Logger.Debug("ActivitySystem entity {0}, starting next Action - {1}", vas.Entity.EntityId, CurrentAction?.Type);
			}
		}
		else
		{
			currentActionIndex = -1;
			Finished = true;
		}
	}

	public void LoadState(ITreeAttribute tree)
	{
		IStorableTypedComponent[] actions = Actions;
		loadState(actions, tree, "action");
		actions = Conditions;
		loadState(actions, tree, "condition");
	}

	public void StoreState(ITreeAttribute tree)
	{
		if (Actions != null)
		{
			IStorableTypedComponent[] actions = Actions;
			storeState(actions, tree, "action");
		}
		if (Conditions != null)
		{
			IStorableTypedComponent[] actions = Conditions;
			storeState(actions, tree, "condition");
		}
	}

	public void storeState(IStorableTypedComponent[] elems, ITreeAttribute tree, string key)
	{
		for (int i = 0; i < elems.Length; i++)
		{
			TreeAttribute atree = new TreeAttribute();
			elems[i].StoreState(atree);
			atree.SetString("type", elems[i].Type);
			tree[key + i] = atree;
		}
	}

	public void loadState(IStorableTypedComponent[] elems, ITreeAttribute tree, string key)
	{
		for (int i = 0; i < elems.Length; i++)
		{
			ITreeAttribute atree = tree.GetTreeAttribute(key + i);
			if (atree != null)
			{
				elems[i].LoadState(atree);
				continue;
			}
			break;
		}
	}

	public override string ToString()
	{
		return base.ToString();
	}

	public EntityActivity Clone()
	{
		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.All
		};
		EntityActivity entityActivity = JsonUtil.ToObject<EntityActivity>(JsonConvert.SerializeObject(this, Formatting.Indented, settings), "", settings);
		entityActivity.OnLoaded(vas);
		return entityActivity;
	}
}
