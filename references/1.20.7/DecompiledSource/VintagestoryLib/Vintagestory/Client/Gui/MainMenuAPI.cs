using System;
using System.Collections.Generic;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client.Gui;

public class MainMenuAPI : ICoreClientAPI, ICoreAPI, ICoreAPICommon
{
	private ScreenManager screenManager;

	internal MainMenuInputAPI inputapi;

	internal MainMenuRenderAPI renderapi;

	internal MainMenuGuiAPI guiapi;

	internal MainMenuEventApi eventapi;

	public IRenderAPI Render => renderapi;

	public IInputAPI Input => inputapi;

	public IGuiAPI Gui => guiapi;

	public bool IsShuttingDown => screenManager.GamePlatform.IsShuttingDown;

	public long ElapsedMilliseconds => screenManager.GamePlatform.EllapsedMs;

	public IAssetManager Assets => screenManager.GamePlatform.AssetManager;

	public ILogger Logger => screenManager.GamePlatform.Logger;

	public ISettings Settings => ClientSettings.Inst;

	public Dictionary<string, Tag2RichTextDelegate> TagConverters => VtmlUtil.TagConverters;

	public IColorPresets ColorPreset => null;

	public bool IsGamePaused
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public bool HideGuis
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public IAmbientManager Ambient
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public IClientEventAPI Event => eventapi;

	public ITesselatorManager TesselatorManager
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public ITesselatorAPI Tesselator
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public ITesselatorAPI TesselatorThreadSafe
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public IBlockTextureAtlasAPI BlockTextureAtlas
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public IItemTextureAtlasAPI ItemTextureAtlas
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public ITextureAtlasAPI EntityTextureAtlas
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public IShaderAPI Shader
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public IClientNetworkAPI Network
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public IClientWorldAccessor World
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public bool MouseWorldInteractAnyway
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public OrderedDictionary<string, HotKey> HotKeys
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public string[] CmdlArguments
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public EnumAppSide Side => EnumAppSide.Client;

	public IClassRegistryAPI ClassRegistry
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public IModLoader ModLoader => screenManager.modloader;

	public Dictionary<string, object> ObjectCache
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public string DataBasePath
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	IEventAPI ICoreAPI.Event => eventapi;

	IWorldAccessor ICoreAPI.World
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public IEnumerable<object> OpenedGuis
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public long InWorldEllapsedMilliseconds
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public IXPlatformInterface Forms => screenManager.GamePlatform.XPlatInterface;

	public IMusicTrack CurrentMusicTrack
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public Dictionary<string, Action<LinkTextComponent>> LinkProtocols { get; set; }

	INetworkAPI ICoreAPI.Network
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public IMacroManager MacroManager
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public bool IsSinglePlayer
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public bool PlayerReadyFired
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public IChatCommandApi ChatCommands
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public bool OpenedToLan
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public MainMenuAPI(ScreenManager screenManager)
	{
		this.screenManager = screenManager;
		inputapi = new MainMenuInputAPI(screenManager);
		renderapi = new MainMenuRenderAPI(screenManager);
		guiapi = new MainMenuGuiAPI(screenManager, this);
		eventapi = new MainMenuEventApi();
		ClientSettings.Inst.AddWatcher("guiScale", delegate(float val)
		{
			RuntimeEnv.GUIScale = val;
		});
		RuntimeEnv.GUIScale = ClientSettings.GUIScale;
	}

	public int ApplyColorMapOnRgba(int tintIndex, int color, int posX, int posY, int posZ, bool fipRb)
	{
		throw new NotImplementedException();
	}

	public string GetOrCreateDataPath(string foldername)
	{
		throw new NotImplementedException();
	}

	public int GetRandomBlockPixel(ushort blockId, int textureSubId)
	{
		throw new NotImplementedException();
	}

	public int GetBlockPixelAt(ushort blockId, int textureSubId, float px, float py)
	{
		throw new NotImplementedException();
	}

	public int GetRandomItemPixel(int itemId, int textureSubId)
	{
		throw new NotImplementedException();
	}

	public void RegisterBlockBehaviorClass(string className, Type blockBehaviorType)
	{
		throw new NotImplementedException();
	}

	public void RegisterBlockEntityBehaviorClass(string className, Type blockBehaviorType)
	{
		throw new NotImplementedException();
	}

	public void RegisterBlockClass(string className, Type blockType)
	{
		throw new NotImplementedException();
	}

	public void RegisterBlockEntityClass(string className, Type blockentityType)
	{
		throw new NotImplementedException();
	}

	public bool RegisterCommand(ClientChatCommand chatcommand)
	{
		throw new NotImplementedException();
	}

	public bool RegisterCommand(string command, string descriptionMsg, string syntaxMsg, ClientChatCommandDelegate handler)
	{
		throw new NotImplementedException();
	}

	public void RegisterCropBehavior(string className, Type type)
	{
		throw new NotImplementedException();
	}

	public void RegisterEntity(string className, Type entity)
	{
		throw new NotImplementedException();
	}

	public void RegisterEntityBehaviorClass(string className, Type entityBehavior)
	{
		throw new NotImplementedException();
	}

	public void RegisterEntityClass(string entityClassName, EntityProperties config)
	{
		throw new NotImplementedException();
	}

	public void RegisterEntityRendererClass(string className, Type rendererType)
	{
		throw new NotImplementedException();
	}

	public void RegisterHotKey(string hotkeyCode, string name, Keys key, HotkeyType type = HotkeyType.CharacterControls, bool altPressed = false, bool ctrlPressed = false, bool shiftPressed = false)
	{
		throw new NotImplementedException();
	}

	public void RegisterItemClass(string className, Type itemType)
	{
		throw new NotImplementedException();
	}

	public void RegisterMountable(string className, GetMountableDelegate mountableInstancer)
	{
		throw new NotImplementedException();
	}

	public void TriggerChatMessage(string message)
	{
		throw new NotImplementedException();
	}

	public void SendChatMessage(string message, int groupId, string data = null)
	{
		throw new NotImplementedException();
	}

	public void SendChatMessage(string message, string data = null)
	{
		throw new NotImplementedException();
	}

	public void SetHotKeyHandler(string hotkeyCode, ActionConsumable<KeyCombination> handler)
	{
		throw new NotImplementedException();
	}

	public void ShowChatMessage(string message)
	{
		throw new NotImplementedException();
	}

	public void SendPacketClient(object packetClient)
	{
		throw new NotImplementedException();
	}

	public int ApplyColorMapOnRgba(int tintIndex, int color, int rain, int temp, bool flipRb = true)
	{
		throw new NotImplementedException();
	}

	public void TriggerIngameError(object sender, string errorCode, string text)
	{
		throw new NotImplementedException();
	}

	public void ShowChatNotification(string message)
	{
		throw new NotImplementedException();
	}

	public MusicTrack StartTrack(AssetLocation soundLocation, float priority, EnumSoundType soundType, Action<ILoadedSound> onLoaded = null)
	{
		throw new NotImplementedException();
	}

	public void RegisterLinkProtocol(string protocolname, Action<LinkTextComponent> onLinkClicked)
	{
		throw new NotImplementedException();
	}

	public void StoreModConfig<T>(T jsonSerializeableData, string filename)
	{
		throw new NotImplementedException();
	}

	public T LoadModConfig<T>(string filename)
	{
		throw new NotImplementedException();
	}

	public void RegisterColorMap(ColorMap map)
	{
		throw new NotImplementedException();
	}

	public void TriggerIngameDiscovery(object sender, string errorCode, string text)
	{
		throw new NotImplementedException();
	}

	public void RegisterCollectibleBehaviorClass(string className, Type blockBehaviorType)
	{
		throw new NotImplementedException();
	}

	public T RegisterRecipeRegistry<T>(string recipeRegistryCode) where T : RecipeRegistryBase
	{
		throw new NotImplementedException();
	}

	public void ResolveBlockColorMaps()
	{
		throw new NotImplementedException();
	}

	public void PauseGame(bool paused)
	{
		throw new NotImplementedException();
	}

	public void StoreModConfig(JsonObject jobj, string filename)
	{
		throw new NotImplementedException();
	}

	public JsonObject LoadModConfig(string filename)
	{
		throw new NotImplementedException();
	}

	public void StartTrack(MusicTrack track, float priority, EnumSoundType soundType, bool playnow = true)
	{
		throw new NotImplementedException();
	}
}
