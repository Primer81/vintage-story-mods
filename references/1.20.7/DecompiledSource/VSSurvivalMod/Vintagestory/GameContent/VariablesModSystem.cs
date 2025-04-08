using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class VariablesModSystem : ModSystem
{
	public VariableData VarData;

	public ICoreAPI Api;

	protected ICoreServerAPI sapi;

	public event OnDialogueControllerInitDelegate OnDialogueControllerInit;

	public override void Start(ICoreAPI api)
	{
		Api = api;
		api.Network.RegisterChannel("variable").RegisterMessageType<VariableData>();
		api.ChatCommands.GetOrCreate("debug").BeginSubCommand("clearvariables").HandleWith(cmdClearVariables);
		OnDialogueControllerInit += setDefaultVariables;
	}

	private TextCommandResult cmdClearVariables(TextCommandCallingArgs args)
	{
		VarData.GlobalVariables = new EntityVariables();
		VarData.PlayerVariables = new Dictionary<string, EntityVariables>();
		VarData.GroupVariables = new Dictionary<string, EntityVariables>();
		return TextCommandResult.Success("Variables cleared");
	}

	public void SetVariable(Entity callingEntity, EnumActivityVariableScope scope, string name, string value)
	{
		switch (scope)
		{
		case EnumActivityVariableScope.Entity:
		{
			ITreeAttribute tree2 = callingEntity.WatchedAttributes.GetTreeAttribute("variables");
			if (tree2 == null)
			{
				tree2 = (ITreeAttribute)(callingEntity.WatchedAttributes["variables"] = new TreeAttribute());
			}
			tree2[name] = new StringAttribute(value);
			break;
		}
		case EnumActivityVariableScope.Global:
			VarData.GlobalVariables[name] = value;
			break;
		case EnumActivityVariableScope.Group:
		{
			string groupCode = callingEntity.WatchedAttributes.GetString("groupCode");
			EntityVariables variables2 = null;
			if (!VarData.GroupVariables.TryGetValue(groupCode, out variables2))
			{
				variables2 = (VarData.GroupVariables[groupCode] = new EntityVariables());
			}
			variables2[name] = value;
			break;
		}
		case EnumActivityVariableScope.Player:
		{
			string uid2 = (callingEntity as EntityPlayer).Player.PlayerUID;
			EntityVariables variables = null;
			if (!VarData.PlayerVariables.TryGetValue(uid2, out variables))
			{
				variables = (VarData.PlayerVariables[uid2] = new EntityVariables());
			}
			variables[name] = value;
			break;
		}
		case EnumActivityVariableScope.EntityPlayer:
		{
			string uid = (callingEntity as EntityPlayer).Player.PlayerUID;
			ITreeAttribute tree = callingEntity.WatchedAttributes.GetTreeAttribute("variables");
			if (tree == null)
			{
				tree = (ITreeAttribute)(callingEntity.WatchedAttributes["variables"] = new TreeAttribute());
			}
			tree[uid + "-" + name] = new StringAttribute(value);
			break;
		}
		}
	}

	public void SetPlayerVariable(string playerUid, string name, string value)
	{
		EntityVariables variables = null;
		if (!VarData.PlayerVariables.TryGetValue(playerUid, out variables))
		{
			variables = (VarData.PlayerVariables[playerUid] = new EntityVariables());
		}
		variables[name] = value;
	}

	public string GetVariable(EnumActivityVariableScope scope, string name, Entity callingEntity)
	{
		switch (scope)
		{
		case EnumActivityVariableScope.Entity:
		{
			ITreeAttribute tree2 = callingEntity.WatchedAttributes.GetTreeAttribute("variables");
			if (tree2 != null)
			{
				return (tree2[name] as StringAttribute)?.value;
			}
			return null;
		}
		case EnumActivityVariableScope.Global:
			return VarData.GlobalVariables[name];
		case EnumActivityVariableScope.Group:
		{
			string groupCode = callingEntity.WatchedAttributes.GetString("groupCode");
			EntityVariables variables2 = null;
			if (!VarData.GroupVariables.TryGetValue(groupCode, out variables2))
			{
				return null;
			}
			return variables2[name];
		}
		case EnumActivityVariableScope.Player:
		{
			string uid2 = (callingEntity as EntityPlayer).Player.PlayerUID;
			EntityVariables variables = null;
			if (!VarData.PlayerVariables.TryGetValue(uid2, out variables))
			{
				return null;
			}
			return variables[name];
		}
		case EnumActivityVariableScope.EntityPlayer:
		{
			string uid = (callingEntity as EntityPlayer).Player.PlayerUID;
			ITreeAttribute tree = callingEntity.WatchedAttributes.GetTreeAttribute("variables");
			if (tree != null)
			{
				return (tree[uid + "-" + name] as StringAttribute)?.value;
			}
			return null;
		}
		default:
			return null;
		}
	}

	public string GetPlayerVariable(string playerUid, string name)
	{
		EntityVariables variables = null;
		if (!VarData.PlayerVariables.TryGetValue(playerUid, out variables))
		{
			return null;
		}
		return variables[name];
	}

	private void setDefaultVariables(VariableData data, EntityPlayer playerEntity, EntityAgent npcEntity)
	{
		if (!data.PlayerVariables.TryGetValue(playerEntity.PlayerUID, out var vars))
		{
			EntityVariables entityVariables2 = (data.PlayerVariables[playerEntity.PlayerUID] = new EntityVariables());
			vars = entityVariables2;
		}
		vars["characterclass"] = playerEntity.WatchedAttributes.GetString("characterClass");
	}

	public void OnControllerInit(EntityPlayer playerEntity, EntityAgent npcEntity)
	{
		if (VarData == null)
		{
			playerEntity.Api.Logger.Warning("Variable system has not received initial state from server, may produce wrong dialogue for state-dependent cases eg. Treasure Hunter trader.");
			VarData = new VariableData();
		}
		this.OnDialogueControllerInit?.Invoke(VarData, playerEntity, npcEntity);
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		api.Network.GetChannel("variable").SetMessageHandler<VariableData>(onDialogueData);
	}

	private void onDialogueData(VariableData dlgData)
	{
		VarData = dlgData;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.PlayerJoin += Event_PlayerJoin;
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
		sapi.Network.GetChannel("variable").SendPacket(VarData, byPlayer);
	}

	private void Event_GameWorldSave()
	{
		sapi.WorldManager.SaveGame.StoreData("dialogueData", SerializerUtil.Serialize(VarData));
	}

	private void Event_SaveGameLoaded()
	{
		VarData = sapi.WorldManager.SaveGame.GetData("dialogueData", new VariableData());
	}
}
