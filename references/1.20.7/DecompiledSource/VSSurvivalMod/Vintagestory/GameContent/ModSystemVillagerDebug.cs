using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods.WorldEdit;

namespace Vintagestory.GameContent;

public class ModSystemVillagerDebug : ModSystem
{
	private ICoreServerAPI sapi;

	private ICoreClientAPI capi;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		CommandArgumentParsers parsers = api.ChatCommands.Parsers;
		api.ChatCommands.GetOrCreate("dev").BeginSub("talk").WithArgs(parsers.Entities("entity"), parsers.OptionalWord("talk type"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
			{
				if (e == args.Caller.Entity)
				{
					return TextCommandResult.Success("Ignoring removal of caller");
				}
				EntityBehaviorConversable behavior = e.GetBehavior<EntityBehaviorConversable>();
				if (behavior != null)
				{
					if (args.Parsers[1].IsMissing)
					{
						StringBuilder stringBuilder = new StringBuilder();
						foreach (object current in Enum.GetValues(typeof(EnumTalkType)))
						{
							if (stringBuilder.Length > 0)
							{
								stringBuilder.Append(", ");
							}
							stringBuilder.Append(current);
						}
						return TextCommandResult.Success(stringBuilder.ToString());
					}
					if (Enum.TryParse<EnumTalkType>(args[1] as string, ignoreCase: true, out var result))
					{
						behavior.TalkUtil.Talk(result);
					}
				}
				return TextCommandResult.Success("Ok, executed");
			}))
			.EndSub()
			.BeginSub("pro")
			.WithArgs(parsers.Entities("entity"))
			.BeginSub("reload")
			.HandleWith(proReload)
			.EndSub()
			.EndSub();
		base.StartClientSide(api);
	}

	private TextCommandResult proReload(TextCommandCallingArgs args)
	{
		capi.Assets.Reload(AssetCategory.config);
		capi.ModLoader.GetModSystem<HumanoidOutfits>().Reload();
		return OnEach(args, delegate(EntityDressedHumanoid edh, Dictionary<string, WeightedCode[]> pro)
		{
			edh.MarkShapeModified();
			return TextCommandResult.Success("Ok reloaded.");
		});
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		CommandArgumentParsers parsers = api.ChatCommands.Parsers;
		api.ChatCommands.GetOrCreate("dev").BeginSub("astardebug").WithArgs(api.ChatCommands.Parsers.Entities("entity"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
			{
				if (e == args.Caller.Entity)
				{
					return TextCommandResult.Success("Ignoring removal of caller");
				}
				EntityBehaviorActivityDriven behavior = e.GetBehavior<EntityBehaviorActivityDriven>();
				if (behavior != null)
				{
					bool flag = !behavior.ActivitySystem.wppathTraverser.PathFindDebug;
					behavior.ActivitySystem.wppathTraverser.PathFindDebug = flag;
					return TextCommandResult.Success("Astar debug now " + (flag ? "on" : "off"));
				}
				return TextCommandResult.Success("Entity is lacking EntityBehaviorActivityDriven");
			}))
			.EndSub()
			.BeginSub("pro")
			.WithArgs(parsers.Entities("entity"))
			.BeginSub("freeze")
			.WithArgs(parsers.OptionalBool("on"))
			.HandleWith(cmdFreeze)
			.EndSub()
			.BeginSub("unfreeze")
			.WithArgs(parsers.OptionalBool("on"))
			.HandleWith(cmdUnFreeze)
			.EndSub()
			.BeginSub("set")
			.WithArgs(parsers.Word("slot"), parsers.All("codes"))
			.HandleWith(proSet)
			.EndSub()
			.BeginSub("naked")
			.HandleWith(proNaked)
			.EndSub()
			.BeginSub("clear")
			.WithArgs(parsers.Word("slot"))
			.HandleWith(proClear)
			.EndSub()
			.BeginSub("add")
			.WithArgs(parsers.Word("slot"), parsers.Word("code"))
			.HandleWith(proAdd)
			.EndSub()
			.BeginSub("remove")
			.HandleWith(proRemove)
			.EndSub()
			.BeginSub("export")
			.HandleWith(proExport)
			.EndSub()
			.BeginSub("test")
			.HandleWith(proTest)
			.EndSub()
			.EndSub()
			.BeginSub("dress")
			.WithArgs(api.ChatCommands.Parsers.Entities("entity"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
			{
				if (e == args.Caller.Entity)
				{
					return TextCommandResult.Success("Ignoring caller");
				}
				if (e is EntityDressedHumanoid entityDressedHumanoid)
				{
					StringBuilder stringBuilder = new StringBuilder();
					for (int i = 0; i < entityDressedHumanoid.OutfitCodes.Length; i++)
					{
						stringBuilder.AppendLine(entityDressedHumanoid.OutfitSlots[i] + ": " + entityDressedHumanoid.OutfitCodes[i]);
					}
					return TextCommandResult.Success("Current outfit:\n" + stringBuilder.ToString());
				}
				return TextCommandResult.Success("Ok, entity removed");
			}))
			.EndSub();
	}

	private TextCommandResult cmdUnFreeze(TextCommandCallingArgs args)
	{
		return freeze(args, freeze: false);
	}

	private TextCommandResult cmdFreeze(TextCommandCallingArgs args)
	{
		bool freeze = args.Parsers[1].IsMissing || (bool)args[1];
		return this.freeze(args, freeze);
	}

	private TextCommandResult freeze(TextCommandCallingArgs args, bool freeze)
	{
		return CmdUtil.EntityEach(args, delegate(Entity e)
		{
			if (e == args.Caller.Entity)
			{
				return TextCommandResult.Success("Ignoring caller");
			}
			EntityBehaviorTaskAI behavior = e.GetBehavior<EntityBehaviorTaskAI>();
			e.GetBehavior<EntityBehaviorActivityDriven>()?.ActivitySystem.PauseAutoSelection(freeze);
			if (freeze)
			{
				if (behavior != null)
				{
					behavior.TaskManager.OnShouldExecuteTask += TaskManager_OnShouldExecuteTask;
				}
			}
			else if (behavior != null)
			{
				behavior.TaskManager.OnShouldExecuteTask -= TaskManager_OnShouldExecuteTask;
			}
			return TextCommandResult.Success("Ok. Freeze set");
		});
	}

	private bool TaskManager_OnShouldExecuteTask(IAiTask t)
	{
		return false;
	}

	private TextCommandResult proSet(TextCommandCallingArgs args)
	{
		string slot = (string)args[1];
		string values = args.Parsers[2].GetValue() as string;
		WeightedCode[] wcodes = (from v in values.Split(" ")
			select new WeightedCode
			{
				Code = v,
				Weight = 1f
			}).ToArray();
		return OnEach(args, delegate(EntityDressedHumanoid edh, Dictionary<string, WeightedCode[]> pro)
		{
			pro[slot] = wcodes;
			edh.LoadOutfitCodes();
			return TextCommandResult.Success("ok, slot " + slot + " set to " + values);
		});
	}

	private TextCommandResult proNaked(TextCommandCallingArgs args)
	{
		return OnEach(args, delegate(EntityDressedHumanoid edh, Dictionary<string, WeightedCode[]> pro)
		{
			SlotAlloc[] bySlot = sapi.ModLoader.GetModSystem<HumanoidOutfits>().GetConfig(edh.OutfitConfigFileName).BySlot;
			foreach (SlotAlloc slotAlloc in bySlot)
			{
				pro[slotAlloc.Code] = new WeightedCode[0];
			}
			edh.LoadOutfitCodes();
			return TextCommandResult.Success("ok, all slots cleared");
		});
	}

	private TextCommandResult proClear(TextCommandCallingArgs args)
	{
		string slot = (string)args[1];
		return OnEach(args, delegate(EntityDressedHumanoid edh, Dictionary<string, WeightedCode[]> pro)
		{
			pro[slot] = new WeightedCode[0];
			edh.LoadOutfitCodes();
			return TextCommandResult.Success("ok, slot " + slot + " cleared");
		});
	}

	private TextCommandResult proAdd(TextCommandCallingArgs args)
	{
		string slot = (string)args[1];
		string value = (string)args[2];
		return OnEach(args, delegate(EntityDressedHumanoid edh, Dictionary<string, WeightedCode[]> pro)
		{
			if (pro[slot].FirstOrDefault((WeightedCode wc) => wc.Code == value) != null)
			{
				return TextCommandResult.Error("Value " + value + " already exists in slot " + slot);
			}
			pro[slot] = pro[slot].Append(new WeightedCode
			{
				Code = value
			});
			edh.LoadOutfitCodes();
			return TextCommandResult.Success("ok, " + value + " added to slot " + slot);
		});
	}

	private TextCommandResult proRemove(TextCommandCallingArgs args)
	{
		string slot = (string)args[1];
		string value = (string)args[2];
		return OnEach(args, delegate(EntityDressedHumanoid edh, Dictionary<string, WeightedCode[]> pro)
		{
			WeightedCode[] array = pro[slot].Where((WeightedCode v) => v.Code != value).ToArray();
			if (pro[slot].Length != array.Length)
			{
				edh.LoadOutfitCodes();
				return TextCommandResult.Success("ok, " + value + " removed from slot " + slot);
			}
			return TextCommandResult.Error("Value " + value + " not present in " + slot);
		});
	}

	private TextCommandResult proExport(TextCommandCallingArgs args)
	{
		StringBuilder sb = new StringBuilder();
		TextCommandResult result = OnEach(args, delegate(EntityDressedHumanoid edh, Dictionary<string, WeightedCode[]> pro)
		{
			sb.AppendLine(JsonUtil.ToString(edh.partialRandomOutfitsOverride));
			return TextCommandResult.Success();
		});
		sapi.Network.GetChannel("worldedit").SendPacket(new CopyToClipboardPacket
		{
			Text = sb.ToString()
		}, args.Caller.Player as IServerPlayer);
		return result;
	}

	private TextCommandResult proTest(TextCommandCallingArgs args)
	{
		return OnEach(args, delegate(EntityDressedHumanoid edh, Dictionary<string, WeightedCode[]> pro)
		{
			edh.LoadOutfitCodes();
			return TextCommandResult.Success("ok, reloaded");
		});
	}

	private TextCommandResult OnEach(TextCommandCallingArgs args, DressedEntityEachDelegate dele)
	{
		return CmdUtil.EntityEach(args, delegate(Entity e)
		{
			if (e == args.Caller.Entity)
			{
				return TextCommandResult.Success("Ignoring caller");
			}
			return (e is EntityDressedHumanoid entity) ? dele(entity, getOrCreatePro(e)) : TextCommandResult.Success("Ok, entity removed");
		});
	}

	private Dictionary<string, WeightedCode[]> getOrCreatePro(Entity entity)
	{
		EntityDressedHumanoid edh = entity as EntityDressedHumanoid;
		if (edh.partialRandomOutfitsOverride == null)
		{
			edh.partialRandomOutfitsOverride = entity.Properties.Attributes["partialRandomOutfits"].AsObject<Dictionary<string, WeightedCode[]>>();
			if (edh.partialRandomOutfitsOverride == null)
			{
				edh.partialRandomOutfitsOverride = new Dictionary<string, WeightedCode[]>();
			}
		}
		return edh.partialRandomOutfitsOverride;
	}
}
