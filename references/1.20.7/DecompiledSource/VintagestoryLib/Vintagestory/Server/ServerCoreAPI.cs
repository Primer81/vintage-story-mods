using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class ServerCoreAPI : APIBase, ICoreServerAPI, ICoreAPI, ICoreAPICommon
{
	public class TreeGenWrapper : ITreeGenerator
	{
		public GrowTreeDelegate dele;

		public void GrowTree(IBlockAccessor blockAccessor, BlockPos pos, TreeGenParams treegenParams, IRandom random)
		{
			dele(blockAccessor, pos, treegenParams);
		}
	}

	internal ServerEventAPI eventapi;

	internal ServerAPI serverapi;

	internal WorldAPI worldapi;

	internal ClassRegistryAPI classregistryapi;

	internal NetworkAPI networkapi;

	internal ModLoader modLoader;

	internal ChatCommandApi commandapi;

	private ServerMain server;

	public override ClassRegistry ClassRegistryNative => server.ClassRegistryInt;

	public string[] CmdlArguments => server.RawCmdLineArgs;

	public IChatCommandApi ChatCommands => commandapi;

	public EnumAppSide Side => EnumAppSide.Server;

	IEventAPI ICoreAPI.Event => eventapi;

	IWorldAccessor ICoreAPI.World => server;

	public IClassRegistryAPI ClassRegistry => classregistryapi;

	public IAssetManager Assets => server.AssetManager;

	public IModLoader ModLoader => modLoader;

	public IPermissionManager Permissions => server.PlayerDataManager;

	public IGroupManager Groups => server.PlayerDataManager;

	public IPlayerDataManager PlayerData => server.PlayerDataManager;

	public IServerEventAPI Event => eventapi;

	public IWorldManagerAPI WorldManager => worldapi;

	public IServerAPI Server => serverapi;

	public IServerNetworkAPI Network => networkapi;

	INetworkAPI ICoreAPI.Network => networkapi;

	public IServerWorldAccessor World => server;

	public ILogger Logger => ServerMain.Logger;

	public ServerCoreAPI(ServerMain server)
		: base(server)
	{
		this.server = server;
		eventapi = new ServerEventAPI(server);
		serverapi = new ServerAPI(server);
		worldapi = new WorldAPI(server);
		classregistryapi = new ClassRegistryAPI(server, ServerMain.ClassRegistry);
		networkapi = new NetworkAPI(server);
		commandapi = new ChatCommandApi(this);
	}

	public void RegisterEntityClass(string entityClassName, EntityProperties config)
	{
		server.EntityTypesByCode[config.Code] = config;
		config.Id = server.EntityTypesByCode.Count;
		server.entityTypesCached = null;
		server.entityCodesCached = null;
	}

	public void SendIngameError(IServerPlayer player, string code, string message = null, params object[] langparams)
	{
		server.SendIngameError(player, code, message, langparams);
	}

	public void SendIngameDiscovery(IServerPlayer player, string code, string message = null, params object[] langparams)
	{
		server.SendIngameDiscovery(player, code, message, langparams);
	}

	public void SendMessage(IPlayer player, int groupid, string message, EnumChatType chatType, string data = null)
	{
		server.SendMessage((IServerPlayer)player, groupid, message, chatType, data);
	}

	public void SendMessageToGroup(int groupid, string message, EnumChatType chatType, string data = null)
	{
		server.SendMessageToGroup(groupid, message, chatType, null, data);
	}

	public void BroadcastMessageToAllGroups(string message, EnumChatType chatType, string data = null)
	{
		server.BroadcastMessageToAllGroups(message, chatType, data);
	}

	public void RegisterItem(Item item)
	{
		server.RegisterItem(item);
	}

	public void RegisterBlock(Block block)
	{
		server.RegisterBlock(block);
	}

	public void RegisterCraftingRecipe(GridRecipe recipe)
	{
		server.GridRecipes.Add(recipe);
	}

	public void RegisterTreeGenerator(AssetLocation generatorCode, ITreeGenerator gen)
	{
		server.TreeGeneratorsByTreeCode[generatorCode] = gen;
	}

	public void RegisterTreeGenerator(AssetLocation generatorCode, GrowTreeDelegate dele)
	{
		server.TreeGeneratorsByTreeCode[generatorCode] = new TreeGenWrapper
		{
			dele = dele
		};
	}

	public override void RegisterColorMap(ColorMap map)
	{
		server.ColorMaps[map.Code] = map;
	}

	public bool RegisterCommand(ServerChatCommand chatcommand)
	{
		return commandapi.RegisterCommand(chatcommand);
	}

	public bool RegisterCommand(string command, string descriptionMsg, string syntaxMsg, ServerChatCommandDelegate handler, string requiredPrivilege = null)
	{
		return commandapi.RegisterCommand(command, descriptionMsg, syntaxMsg, handler, requiredPrivilege);
	}

	public void HandleCommand(IServerPlayer player, string message)
	{
		string command = message.Split(new char[1] { ' ' })[0].Replace("/", "");
		command = command.ToLowerInvariant();
		string argument = ((message.IndexOf(' ') < 0) ? "" : message.Substring(message.IndexOf(' ') + 1));
		commandapi.Execute(command, player, GlobalConstants.CurrentChatGroup, argument);
	}

	public void InjectConsole(string message)
	{
		server.ReceiveServerConsole(message);
	}

	public void TriggerOnAssetsFirstLoaded()
	{
		server.ModEventManager.OnAssetsFirstLoaded();
	}
}
