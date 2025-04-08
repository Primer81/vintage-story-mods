using System;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

/// <summary>
/// Contains some client specific events you can hook into
/// </summary>
public interface IClientEventAPI : IEventAPI
{
	/// <summary>
	/// Called when a chat message was received
	/// </summary>
	event ChatLineDelegate ChatMessage;

	/// <summary>
	/// Called before a chat message is sent to the server
	/// </summary>
	event ClientChatLineDelegate OnSendChatMessage;

	/// <summary>
	/// Called when a player joins. The Entity of the player might be null if out of range!
	/// </summary>
	event PlayerEventDelegate PlayerJoin;

	/// <summary>
	/// Called whenever a player disconnects (timeout, leave, disconnect, kick, etc.). The Entity of the player might be null if out of range!
	/// </summary>
	event PlayerEventDelegate PlayerLeave;

	/// <summary>
	/// Called when the player dies
	/// </summary>
	event PlayerEventDelegate PlayerDeath;

	/// <summary>
	/// Fired when a player is ready to join but awaits any potential mod-user interaction, such as a character selection screen
	/// </summary>
	event IsPlayerReadyDelegate IsPlayerReady;

	/// <summary>
	/// Called when a players entity got in range
	/// </summary>
	event PlayerEventDelegate PlayerEntitySpawn;

	/// <summary>
	/// Called whenever a players got out of range
	/// </summary>
	event PlayerEventDelegate PlayerEntityDespawn;

	/// <summary>
	/// When the game was paused/resumed (only in single player)
	/// </summary>
	event OnGamePauseResume PauseResume;

	/// <summary>
	/// When the player wants to leave the world to go back to the main menu
	/// </summary>
	event Action LeaveWorld;

	/// <summary>
	/// When the player left the world to go back to the main menu
	/// </summary>
	event Action LeftWorld;

	/// <summary>
	/// When a player block has been modified. OldBlock param may be null!
	/// </summary>
	event BlockChangedDelegate BlockChanged;

	/// <summary>
	/// When player tries to modify a block
	/// </summary>
	event TestBlockAccessDelegate TestBlockAccess;

	/// <summary>
	/// Fired before a player changes their active slot (such as selected hotbar slot).
	/// Allows for the event to be cancelled depending on the return value.
	/// Note: Not called when the server forcefully changes active slot.
	/// </summary>
	event Vintagestory.API.Common.Func<ActiveSlotChangeEventArgs, EnumHandling> BeforeActiveSlotChanged;

	/// <summary>
	/// Fired after a player changes their active slot (such as selected hotbar slot).
	/// </summary>
	event Action<ActiveSlotChangeEventArgs> AfterActiveSlotChanged;

	/// <summary>
	/// Fired when something fires an ingame error
	/// </summary>
	event IngameErrorDelegate InGameError;

	/// <summary>
	/// Fired when something triggers a discovery event, such as the lore system
	/// </summary>
	event IngameDiscoveryDelegate InGameDiscovery;

	/// <summary>
	/// Fired when the GuiColorsPreset client setting is changed, since meshes may need to be redrawn
	/// </summary>
	event Action ColorsPresetChanged;

	/// <summary>
	/// Fired when server assets were received and all texture atlases have been created, also all sounds loaded
	/// </summary>
	event Action BlockTexturesLoaded;

	/// <summary>
	/// Fired when the player tries to reload the shaders (happens when graphics settings are changed)
	/// </summary>
	event ActionBoolReturn ReloadShader;

	/// <summary>
	/// Called when textures got reloaded
	/// </summary>
	event Action ReloadTextures;

	/// <summary>
	/// Called when the client received the level finalize packet from the server
	/// </summary>
	event Action LevelFinalize;

	/// <summary>
	/// Called when shapes got reloaded
	/// </summary>
	event Action ReloadShapes;

	/// <summary>
	/// Called when the hotkeys are changed
	/// </summary>
	event Action HotkeysChanged;

	/// <summary>
	/// Provides low level access to the mouse down event. If e.Handled is set to true, the event will not be handled by the game
	/// </summary>
	event MouseEventDelegate MouseDown;

	/// <summary>
	/// Provides low level access to the mouse up event. If e.Handled is set to true, the event will not be handled by the game
	/// </summary>
	event MouseEventDelegate MouseUp;

	/// <summary>
	/// Provides low level access to the mouse move event. If e.Handled is set to true, the event will not be handled by the game
	/// </summary>
	event MouseEventDelegate MouseMove;

	/// <summary>
	/// Provides low level access to the key down event. If e.Handled is set to true, the event will not be handled by the game
	/// </summary>
	event KeyEventDelegate KeyDown;

	/// <summary>
	/// Provides low level access to the key up event. If e.Handled is set to true, the event will not be handled by the game
	/// </summary>
	event KeyEventDelegate KeyUp;

	/// <summary>
	/// Fired when the user drags&amp;drops a file into the game window
	/// </summary>
	event FileDropDelegate FileDrop;

	/// <summary>
	/// Registers a rendering handler to be called during every render frame
	/// </summary>
	/// <param name="renderer"></param>
	/// <param name="renderStage"></param>
	/// <param name="profilingName">If set, the frame profile will record the frame cost for this renderer</param>
	void RegisterRenderer(IRenderer renderer, EnumRenderStage renderStage, string profilingName = null);

	/// <summary>
	/// Removes a previously registered rendering handler.
	/// </summary>
	/// <param name="renderer"></param>
	/// <param name="renderStage"></param>
	void UnregisterRenderer(IRenderer renderer, EnumRenderStage renderStage);

	/// <summary>
	/// Registers a custom itemstack renderer for given collectible object. If none is registered, the default renderer is used. For render target gui, the gui shader and its uniforms are already fully prepared, you may only call RenderMesh() and ignore the modelMat, position and size values - stack sizes however, are not covered by this.
	/// </summary>
	/// <param name="forObj"></param>
	/// <param name="rendererDelegate"></param>
	/// <param name="target"></param>
	void RegisterItemstackRenderer(CollectibleObject forObj, ItemRenderDelegate rendererDelegate, EnumItemRenderTarget target);

	/// <summary>
	/// Removes a previously registered itemstack renderer
	/// </summary>
	/// <param name="forObj"></param>
	/// <param name="target"></param>
	void UnregisterItemstackRenderer(CollectibleObject forObj, EnumItemRenderTarget target);

	/// <summary>
	/// Set up an asynchronous particle spawner. The async particle simulation does most of the work in a seperate thread and thus runs a lot faster, with the down side of not being exaclty in sync with player interactions. This method of spawning particles is best suited for ambient particles, such as rain fall.
	/// </summary>
	/// <param name="handler"></param>
	void RegisterAsyncParticleSpawner(ContinousParticleSpawnTaskDelegate handler);
}
