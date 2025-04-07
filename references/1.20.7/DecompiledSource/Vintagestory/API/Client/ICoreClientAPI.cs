#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

//
// Summary:
//     The core api implemented by the client. The main interface for accessing the
//     client. Contains all sub components and some miscellaneous methods.
public interface ICoreClientAPI : ICoreAPI, ICoreAPICommon
{
    //
    // Summary:
    //     Add your own link protocol here if you want to implement a custom protocol. E.g.
    //     image://url-to-picture
    Dictionary<string, Action<LinkTextComponent>> LinkProtocols { get; }

    //
    // Summary:
    //     Add your own rich text elements here. Your will need to convert a VTML tag into
    //     a RichTextComponentBase element.
    Dictionary<string, Tag2RichTextDelegate> TagConverters { get; }

    //
    // Summary:
    //     The clients game settings as stored in the clientsettings.json
    ISettings Settings { get; }

    //
    // Summary:
    //     Platform independent ui methods and features.
    IXPlatformInterface Forms { get; }

    //
    // Summary:
    //     Api to the client side macros system
    IMacroManager MacroManager { get; }

    //
    // Summary:
    //     Amount of milliseconds ellapsed since client startup
    long ElapsedMilliseconds { get; }

    //
    // Summary:
    //     Amount of milliseconds ellapsed while in a running game that is not paused
    long InWorldEllapsedMilliseconds { get; }

    //
    // Summary:
    //     True if the client is currently in the process of exiting
    bool IsShuttingDown { get; }

    //
    // Summary:
    //     True if the game is currently paused (only available in singleplayer)
    bool IsGamePaused { get; }

    //
    // Summary:
    //     True if this is a singleplayer session
    bool IsSinglePlayer { get; }

    bool OpenedToLan { get; }

    //
    // Summary:
    //     If true, the player is in gui-less mode (through the F4 key)
    bool HideGuis { get; }

    //
    // Summary:
    //     True if all SendPlayerNowReady() was sent, signalling the player is now ready
    //     (called by the character selector upon submit)
    bool PlayerReadyFired { get; }

    //
    // Summary:
    //     API Component to control the clients ambient values
    IAmbientManager Ambient { get; }

    //
    // Summary:
    //     API Component for registering to various Events
    new IClientEventAPI Event { get; }

    //
    // Summary:
    //     API for Rendering stuff onto the screen using OpenGL
    IRenderAPI Render { get; }

    //
    // Summary:
    //     API for GUI Related methods
    IGuiAPI Gui { get; }

    //
    // Summary:
    //     API for Mouse / Keyboard input related things
    IInputAPI Input { get; }

    //
    // Summary:
    //     Holds the default meshes of all blocks
    ITesselatorManager TesselatorManager { get; }

    //
    // Summary:
    //     API for Meshing in the Mainthread. Thread safe.
    ITesselatorAPI Tesselator { get; }

    //
    // Summary:
    //     API for the Block Texture Atlas
    IBlockTextureAtlasAPI BlockTextureAtlas { get; }

    //
    // Summary:
    //     API for the Item Texture Atlas
    IItemTextureAtlasAPI ItemTextureAtlas { get; }

    //
    // Summary:
    //     API for the Entity Texture Atlas
    ITextureAtlasAPI EntityTextureAtlas { get; }

    //
    // Summary:
    //     Fetch color configs, used for accessibility e.g. for knapping wireframe gridlines
    IColorPresets ColorPreset { get; }

    //
    // Summary:
    //     API for Rendering stuff onto the screen using OpenGL
    IShaderAPI Shader { get; }

    //
    // Summary:
    //     API for doing sending/receiving network packets
    new IClientNetworkAPI Network { get; }

    //
    // Summary:
    //     API for accessing anything in the game world
    new IClientWorldAccessor World { get; }

    //
    // Summary:
    //     Active GUI objects.
    IEnumerable<object> OpenedGuis { get; }

    //
    // Summary:
    //     Returns the currently playing music track, if any is playing
    IMusicTrack CurrentMusicTrack { get; }

    //
    // Summary:
    //     Registers a chat command
    //
    // Parameters:
    //   chatcommand:
    [Obsolete("Use ChatCommand subapi instead")]
    bool RegisterCommand(ClientChatCommand chatcommand);

    //
    // Summary:
    //     Registers a chat command
    //
    // Parameters:
    //   command:
    //
    //   descriptionMsg:
    //
    //   syntaxMsg:
    //
    //   handler:
    [Obsolete("Use ChatCommand subapi instead")]
    bool RegisterCommand(string command, string descriptionMsg, string syntaxMsg, ClientChatCommandDelegate handler);

    //
    // Summary:
    //     Registers an entity renderer for given entity
    //
    // Parameters:
    //   className:
    //
    //   rendererType:
    void RegisterEntityRendererClass(string className, Type rendererType);

    //
    // Summary:
    //     Register a link protocol handler
    //
    // Parameters:
    //   protocolname:
    //
    //   onLinkClicked:
    void RegisterLinkProtocol(string protocolname, Action<LinkTextComponent> onLinkClicked);

    //
    // Summary:
    //     Shows a client side only chat message in the current chat channel. Uses the same
    //     code paths a server => client message takes. Does not execute client commands.
    //
    //
    // Parameters:
    //   message:
    void ShowChatMessage(string message);

    //
    // Summary:
    //     Triggers a discovery event. HudDiscoveryMessage registers to this event and fades
    //     in/out a "discovery message" on the players screen
    //
    // Parameters:
    //   sender:
    //
    //   errorCode:
    //
    //   text:
    void TriggerIngameDiscovery(object sender, string errorCode, string text);

    //
    // Summary:
    //     Triggers an in-game-error event. HudIngameError registers to this event and shows
    //     a vibrating red text on the players screen
    //
    // Parameters:
    //   sender:
    //
    //   errorCode:
    //
    //   text:
    void TriggerIngameError(object sender, string errorCode, string text);

    //
    // Summary:
    //     Same as Vintagestory.API.Client.ICoreClientAPI.ShowChatMessage(System.String)
    //     but will also execute client commands if they are prefixed with a dot.
    //
    // Parameters:
    //   message:
    void TriggerChatMessage(string message);

    //
    // Summary:
    //     Sends a chat message to the server
    //
    // Parameters:
    //   message:
    //
    //   groupId:
    //
    //   data:
    void SendChatMessage(string message, int groupId, string data = null);

    //
    // Summary:
    //     Sends a chat message to the server in the players currently active channel
    //
    // Parameters:
    //   message:
    //
    //   data:
    void SendChatMessage(string message, string data = null);

    //
    // Summary:
    //     Tells the music engine to load and immediately start given track once loaded,
    //     if the priority is higher than the currently playing track. May also be stopped
    //     while playing if another track with a higher priority is started. If you supply
    //     an onLoaded method the track is not started immediately and you can manually
    //     start it at any given time by calling sound.Start()
    //
    // Parameters:
    //   soundLocation:
    //
    //   priority:
    //
    //   soundType:
    //
    //   onLoaded:
    MusicTrack StartTrack(AssetLocation soundLocation, float priority, EnumSoundType soundType, Action<ILoadedSound> onLoaded = null);

    void StartTrack(MusicTrack track, float priority, EnumSoundType soundType, bool playnow = true);

    void PauseGame(bool paused);
}
#if false // Decompilation log
'168' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Could not find by name: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Could not find by name: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Specialized.dll'
------------------
Resolve: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NetworkInformation.dll'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ObjectModel.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Could not find by name: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NameResolution.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.NonGeneric.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
