using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ActivityEditorSystem : ModSystem
{
	private ICoreClientAPI capi;

	private ICoreServerAPI sapi;

	private GuiDialogActivityCollections dlg;

	public OrderedDictionary<AssetLocation, EntityActivityCollection> collections = new OrderedDictionary<AssetLocation, EntityActivityCollection>();

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		api.Network.RegisterChannel("activityEditor").RegisterMessageType<ApplyConfigPacket>().RegisterMessageType<ActivityCollectionsJsonPacket>();
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		api.Network.GetChannel("activityEditor").SetMessageHandler<ActivityCollectionsJsonPacket>(storeActivityCollectionPacket);
		capi = api;
		api.ChatCommands.GetOrCreate("dev").WithPreCondition((TextCommandCallingArgs args) => (api.World.Player.WorldData.CurrentGameMode != EnumGameMode.Creative) ? TextCommandResult.Error("Only available in creative mode") : TextCommandResult.Success()).BeginSub("aedit")
			.HandleWith(onCmdAedit)
			.BeginSub("cv")
			.HandleWith(clearVisualizer)
			.EndSub()
			.EndSub();
	}

	private TextCommandResult clearVisualizer(TextCommandCallingArgs args)
	{
		GuiDialogActivity.visualizer?.Dispose();
		GuiDialogActivity.visualizer = null;
		return TextCommandResult.Success("ok!");
	}

	private void storeActivityCollectionPacket(ActivityCollectionsJsonPacket packet)
	{
		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.All
		};
		foreach (string collection2 in packet.Collections)
		{
			EntityActivityCollection collection = JsonUtil.ToObject<EntityActivityCollection>(collection2, "", settings);
			if (collection != null)
			{
				saveCollection(collection);
			}
		}
	}

	private static void saveCollection(EntityActivityCollection collection)
	{
		string path = Path.Combine(GamePaths.AssetsPath, "survival", "config", "activitycollections", GamePaths.ReplaceInvalidChars(collection.Name) + ".json");
		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.All
		};
		string json = JsonConvert.SerializeObject(collection, Formatting.Indented, settings);
		File.WriteAllText(path, json);
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Network.GetChannel("activityEditor").SetMessageHandler<ApplyConfigPacket>(onClientPacket).SetMessageHandler(delegate(IServerPlayer player, ActivityCollectionsJsonPacket packet)
		{
			if (!player.HasPrivilege(Privilege.controlserver))
			{
				player.SendMessage(GlobalConstants.GeneralChatGroup, "No privilege to save activity collections to server", EnumChatType.CommandError);
			}
			else
			{
				storeActivityCollectionPacket(packet);
				api.Network.GetChannel("activityEditor").BroadcastPacket(packet, player);
			}
		});
		api.ChatCommands.GetOrCreate("dev").BeginSub("aee").WithArgs(api.ChatCommands.Parsers.Entities("target entity"))
			.BeginSub("unmount")
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
			{
				(e as EntityAgent)?.TryUnmount();
				return TextCommandResult.Success();
			}))
			.EndSub()
			.BeginSub("startanim")
			.WithArgs(api.ChatCommands.Parsers.Word("anim"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
			{
				e.StartAnimation(args[1] as string);
				return TextCommandResult.Success();
			}))
			.EndSub()
			.BeginSub("runa")
			.WithArgs(api.ChatCommands.Parsers.All("activity code"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
			{
				EntityBehaviorActivityDriven behavior3 = e.GetBehavior<EntityBehaviorActivityDriven>();
				return (behavior3 != null) ? (behavior3.ActivitySystem.StartActivity(args[1] as string) ? TextCommandResult.Success("Acitivty started") : TextCommandResult.Error("Target entity has no such activity")) : TextCommandResult.Error("Target entity has no EntityBehaviorActivityDriven");
			}))
			.EndSub()
			.BeginSub("pause")
			.WithArgs(api.ChatCommands.Parsers.Bool("paused"))
			.HandleWith(onCmdPause)
			.EndSub()
			.BeginSub("stop")
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
			{
				EntityBehaviorActivityDriven behavior2 = e.GetBehavior<EntityBehaviorActivityDriven>();
				return (behavior2 != null) ? (behavior2.ActivitySystem.CancelAll() ? TextCommandResult.Success("Acitivties stopped") : TextCommandResult.Error("No activity was running")) : TextCommandResult.Error("Target entity has no EntityBehaviorActivityDriven");
			}))
			.EndSub()
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
			{
				EntityBehaviorActivityDriven behavior = e.GetBehavior<EntityBehaviorActivityDriven>();
				if (behavior != null)
				{
					Dictionary<int, IEntityActivity>.ValueCollection values = behavior.ActivitySystem.ActiveActivitiesBySlot.Values;
					if (values.Count == 0)
					{
						return TextCommandResult.Success("No active activities");
					}
					return TextCommandResult.Success("Active activities: " + string.Join(", ", values));
				}
				return TextCommandResult.Error("Target entity has no EntityBehaviorActivityDriven");
			}));
	}

	private void Event_SaveGameLoaded()
	{
		if (sapi.World.Config.GetBool("syncActivityCollections"))
		{
			sapi.Event.PlayerJoin += Event_PlayerJoin;
		}
	}

	private void Event_PlayerJoin(IServerPlayer player)
	{
		new JsonSerializerSettings().TypeNameHandling = TypeNameHandling.All;
		sapi.Assets.Reload(AssetCategory.config);
		List<IAsset> many = sapi.Assets.GetMany("config/activitycollections/");
		ActivityCollectionsJsonPacket packet = new ActivityCollectionsJsonPacket
		{
			Collections = new List<string>()
		};
		collections.Clear();
		foreach (IAsset file in many)
		{
			packet.Collections.Add(file.ToText());
		}
		sapi.Network.GetChannel("activityEditor").SendPacket(packet, player);
	}

	private TextCommandResult onCmdPause(TextCommandCallingArgs args)
	{
		bool paused = (bool)args[1];
		return CmdUtil.EntityEach(args, delegate(Entity e)
		{
			EntityBehaviorActivityDriven behavior = e.GetBehavior<EntityBehaviorActivityDriven>();
			if (behavior != null)
			{
				behavior.ActivitySystem.PauseAutoSelection(paused);
				if (paused)
				{
					behavior.ActivitySystem.CancelAll();
				}
				return TextCommandResult.Success(paused ? "Activity selection paused" : "Activity selection resumed");
			}
			return TextCommandResult.Error("Target entity has no EntityBehaviorActivityDriven");
		});
	}

	private void onClientPacket(IServerPlayer fromPlayer, ApplyConfigPacket packet)
	{
		Entity e = sapi.World.GetEntityById(packet.EntityId);
		if (e == null)
		{
			fromPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "No such entity id loaded", EnumChatType.Notification);
			return;
		}
		EntityBehaviorActivityDriven ebh = e.GetBehavior<EntityBehaviorActivityDriven>();
		if (ebh == null)
		{
			fromPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "This entity is lacking the ActivityDriven behavior", EnumChatType.Notification);
			return;
		}
		sapi.Assets.Reload(AssetCategory.config);
		if (ebh.load(packet.ActivityCollectionName))
		{
			fromPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "ActivityCollection loaded on entity " + e.EntityId, EnumChatType.Notification);
		}
		else
		{
			fromPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "Failed to load ActivityCollection", EnumChatType.Notification);
		}
	}

	private TextCommandResult onCmdAedit(TextCommandCallingArgs args)
	{
		if (dlg == null)
		{
			dlg = new GuiDialogActivityCollections(capi);
			dlg.OnClosed += Dlg_OnClosed;
			dlg.TryOpen();
		}
		return TextCommandResult.Success();
	}

	private void Dlg_OnClosed()
	{
		dlg = null;
	}
}
