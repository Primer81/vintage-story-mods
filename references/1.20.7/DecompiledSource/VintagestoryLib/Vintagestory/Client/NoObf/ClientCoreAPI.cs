using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ClientCoreAPI : APIBase, ICoreClientAPI, ICoreAPI, ICoreAPICommon
{
	internal ClassRegistryAPI instancerapi;

	internal ClientEventAPI eventapi;

	internal RenderAPIGame renderapi;

	internal NetworkAPI networkapi;

	internal ShaderAPI shaderapi;

	internal InputAPI inputapi;

	internal GuiAPI guiapi;

	internal ModLoader modLoader;

	internal ChatCommandApi chatcommandapi;

	public Dictionary<string, Action<LinkTextComponent>> linkProtocols = new Dictionary<string, Action<LinkTextComponent>>();

	private ClientMain game;

	public bool disposed;

	public override ClassRegistry ClassRegistryNative => instancerapi.registry;

	public Dictionary<string, Tag2RichTextDelegate> TagConverters => VtmlUtil.TagConverters;

	public string[] CmdlArguments => ScreenManager.RawCmdLineArgs;

	public EnumAppSide Side => EnumAppSide.Client;

	IEventAPI ICoreAPI.Event => eventapi;

	public IChatCommandApi ChatCommands => chatcommandapi;

	IWorldAccessor ICoreAPI.World => game;

	public IClassRegistryAPI ClassRegistry => instancerapi;

	public IAssetManager Assets => game.Platform.AssetManager;

	public IXPlatformInterface Forms => game.Platform.XPlatInterface;

	public IModLoader ModLoader => modLoader;

	public ILogger Logger => game.Logger;

	public bool IsShuttingDown
	{
		get
		{
			if (!game.Platform.IsShuttingDown)
			{
				return game.disposed;
			}
			return true;
		}
	}

	public bool IsGamePaused => game.IsPaused;

	public long ElapsedMilliseconds => game.Platform.EllapsedMs;

	public long InWorldEllapsedMilliseconds => game.InWorldEllapsedMs;

	public bool HideGuis => !game.ShouldRender2DOverlays;

	public IClientEventAPI Event => eventapi;

	public IAmbientManager Ambient => game.AmbientManager;

	public IRenderAPI Render => renderapi;

	public IShaderAPI Shader => shaderapi;

	public IGuiAPI Gui => guiapi;

	public IInputAPI Input => inputapi;

	public IColorPresets ColorPreset => game.ColorPreset;

	public IMacroManager MacroManager => game.macroManager;

	public ITesselatorManager TesselatorManager => game.TesselatorManager;

	public ITesselatorAPI Tesselator => game.TesselatorManager.Tesselator;

	public IBlockTextureAtlasAPI BlockTextureAtlas => game.BlockAtlasManager;

	public IItemTextureAtlasAPI ItemTextureAtlas => game.ItemAtlasManager;

	public ITextureAtlasAPI EntityTextureAtlas => game.EntityAtlasManager;

	public IClientNetworkAPI Network => networkapi;

	INetworkAPI ICoreAPI.Network => networkapi;

	public IClientWorldAccessor World => game;

	public IEnumerable<object> OpenedGuis => game.OpenedGuis;

	public ISettings Settings => ClientSettings.Inst;

	public IMusicTrack CurrentMusicTrack => game.eventManager?.CurrentTrackSupplier();

	public Dictionary<string, Action<LinkTextComponent>> LinkProtocols => linkProtocols;

	public bool IsSinglePlayer => game.IsSingleplayer;

	public bool OpenedToLan => game.OpenedToLan;

	public bool PlayerReadyFired => game.clientPlayingFired;

	public ClientCoreAPI(ClientMain game)
		: base(game)
	{
		this.game = game;
		instancerapi = new ClassRegistryAPI(game, ClientMain.ClassRegistry);
		eventapi = new ClientEventAPI(game);
		renderapi = new RenderAPIGame(this, game);
		networkapi = new NetworkAPI(game);
		shaderapi = new ShaderAPI(game);
		inputapi = new InputAPI(game);
		guiapi = new GuiAPI(game, this);
		chatcommandapi = new ChatCommandApi(this);
	}

	internal void Dispose()
	{
		renderapi.Dispose();
	}

	public void RegisterEntityClass(string entityClassName, EntityProperties config)
	{
	}

	public void RegisterLinkProtocol(string protocolname, Action<LinkTextComponent> onLinkClicked)
	{
		linkProtocols[protocolname] = onLinkClicked;
	}

	public void SendPacketClient(object packet)
	{
		game.SendPacketClient((Packet_Client)packet);
	}

	public bool RegisterCommand(ClientChatCommand chatcommand)
	{
		return chatcommandapi.RegisterCommand(chatcommand);
	}

	public bool RegisterCommand(string command, string descriptionMsg, string syntaxMsg, ClientChatCommandDelegate handler)
	{
		return chatcommandapi.RegisterCommand(command, descriptionMsg, syntaxMsg, handler, null);
	}

	public void TriggerIngameError(object sender, string errorCode, string text)
	{
		game.eventManager?.TriggerIngameError(sender, errorCode, text);
	}

	public void TriggerIngameDiscovery(object sender, string errorCode, string text)
	{
		game.eventManager?.TriggerIngameDiscovery(sender, errorCode, text);
	}

	public void RegisterEntityRendererClass(string className, Type rendererType)
	{
		ClientMain.ClassRegistry.RegisterEntityRendererType(className, rendererType);
	}

	public void ShowChatMessage(string message)
	{
		game.ShowChatMessage(message);
	}

	public void TriggerChatMessage(string message)
	{
		game.SendMessageToClient(message);
	}

	public void SendChatMessage(string message, int groupId, string data = null)
	{
		game.SendPacketClient(ClientPackets.Chat(groupId, message, data));
	}

	public void SendChatMessage(string message, string data = null)
	{
		game.SendPacketClient(ClientPackets.Chat(game.currentGroupid, message, data));
	}

	public void ShowChatNotification(string message)
	{
		game.ShowChatMessage(message);
	}

	public MusicTrack StartTrack(AssetLocation soundLocation, float priority, EnumSoundType soundType, Action<ILoadedSound> onLoaded = null)
	{
		return game.eventManager?.TrackStarter(soundLocation, priority, soundType, onLoaded);
	}

	public void StartTrack(MusicTrack track, float priority, EnumSoundType soundType, bool playNow = true)
	{
		game.eventManager?.TrackStarterLoaded(track, priority, soundType, playNow);
	}

	public override void RegisterColorMap(ColorMap map)
	{
		game.ColorMaps[map.Code] = map;
	}

	public void PauseGame(bool paused)
	{
		game.PauseGame(paused);
	}
}
