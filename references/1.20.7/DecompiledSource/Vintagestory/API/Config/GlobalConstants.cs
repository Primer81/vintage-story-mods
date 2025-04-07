#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Config;

//
// Summary:
//     Contains some global constants and static values
public class GlobalConstants
{
    public static CultureInfo DefaultCultureInfo = CultureInfo.InvariantCulture;

    //
    // Summary:
    //     Prefix for all default asset locations
    public const string DefaultDomain = "game";

    //
    // Summary:
    //     Hard-enforced world size limit, above this the code may break
    public const int MaxWorldSizeXZ = 67108864;

    //
    // Summary:
    //     Hard-enforced world height limit, above this the code may break.
    public const int MaxWorldSizeY = 16384;

    //
    // Summary:
    //     Now a hard-coded constant
    public const int ChunkSize = 32;

    //
    // Summary:
    //     Used in various places if the dimension of a chunk is combined into the chunk's
    //     y value.
    public const int DimensionSizeInChunks = 1024;

    //
    // Summary:
    //     Max. amount of "bones" for animated model. Limited by max amount of shader uniforms
    //     of around 60, but depends on the gfx card This value is overriden by ClientSettings.cs
    public static int MaxAnimatedElements = 230;

    //
    // Summary:
    //     Max. amount of "bones" for color maps. Limited by max amount of shader uniforms,
    //     but depends on the gfx card
    public const int MaxColorMaps = 40;

    public static int CaveArtColsPerRow = 6;

    //
    // Summary:
    //     Frame time for physics simulation
    public static float PhysicsFrameTime = 1f / 30f;

    //
    // Summary:
    //     Limits the amount of world time that can be simulated by the physics engine if
    //     the server is ticking slowly: if ticks are slower than this, entities will seem
    //     to slow down (viewed on client might even jump backwards)
    //     Recommended range 0.1f to 0.4f
    public static float MaxPhysicsIntervalInSlowTicks = 0.135f;

    //
    // Summary:
    //     A multiplier applied to the y motion of all particles affected by gravity.
    public static float GravityStrengthParticle = 0.3f;

    //
    // Summary:
    //     Attack range when using hands
    public static float DefaultAttackRange = 1.5f;

    //
    // Summary:
    //     Multiplied to all motions and animation speeds
    public static float OverallSpeedMultiplier = 1f;

    //
    // Summary:
    //     Multiplier applied to the players movement motion
    public static float BaseMoveSpeed = 1.5f;

    //
    // Summary:
    //     Multiplier applied to the players jump motion
    public static float BaseJumpForce = 8.2f;

    //
    // Summary:
    //     Multiplier applied to the players sneaking motion
    public static float SneakSpeedMultiplier = 0.35f;

    //
    // Summary:
    //     Multiplier applied to the players sprinting motion
    public static double SprintSpeedMultiplier = 2.0;

    //
    // Summary:
    //     Multiplier applied to entity motion while on the ground or in air
    public static float AirDragAlways = 0.983f;

    //
    // Summary:
    //     Multiplier applied to entity motion while flying (creative mode)
    public static float AirDragFlying = 0.8f;

    //
    // Summary:
    //     Multiplier applied to entity motion while walking in water
    public static float WaterDrag = 0.92f;

    //
    // Summary:
    //     Amount of gravity per tick applied to all entities affected by gravity
    public static float GravityPerSecond = 0.37f;

    //
    // Summary:
    //     Range in blocks at where this entity is simulated on the server (MagicNum.cs
    //     sets this value)
    public static int DefaultSimulationRange = 128;

    //
    // Summary:
    //     Range in blocks a player can interact with blocks (break, use, place)
    public static float DefaultPickingRange = 4.5f;

    //
    // Summary:
    //     Time in seconds for dropped items to remain when dropped after player death;
    //     overrides the despawn time set in item.json
    public static int TimeToDespawnPlayerInventoryDrops = 600;

    //
    // Summary:
    //     Set by the WeatherSimulation System in the survival mod at the players position
    public static Vec3f CurrentWindSpeedClient = new Vec3f();

    public static Vec3f CurrentSurfaceWindSpeedClient = new Vec3f();

    //
    // Summary:
    //     Set by the SystemPlayerEnvAwarenessTracker System in the engine at the players
    //     position, once every second. 12 horizontal, 4 vertical search distance
    public static float CurrentDistanceToRainfallClient;

    //
    // Summary:
    //     Set by the game client at the players position
    public static float CurrentNearbyRelLeavesCountClient;

    //
    // Summary:
    //     Set by the weather simulation system to determine if snowed variants of blocks
    //     should melt. Used a static var to improve performance and reduce memory usage
    public static bool MeltingFreezingEnabled;

    public static float GuiGearRotJitter = 0f;

    public const int MaxViewDistanceForLodBiases = 640;

    //
    // Summary:
    //     These reserved characters or sequences should not be used in texture filenames
    //     or asset locations, they can mess up the BakedTexture system
    public static string[] ReservedCharacterSequences = new string[6] { "å", "~", "++", "@90", "@180", "@270" };

    public const string WorldSaveExtension = ".vcdbs";

    public const string hotBarInvClassName = "hotbar";

    public const string creativeInvClassName = "creative";

    public const string backpackInvClassName = "backpack";

    public const string groundInvClassName = "ground";

    public const string mousecursorInvClassName = "mouse";

    public const string characterInvClassName = "character";

    public const string craftingInvClassName = "craftinggrid";

    public static Dictionary<string, double[]> playerColorByEntitlement = new Dictionary<string, double[]>
    {
        {
            "vsteam",
            new double[4]
            {
                13.0 / 255.0,
                128.0 / 255.0,
                62.0 / 255.0,
                1.0
            }
        },
        {
            "vscontributor",
            new double[4]
            {
                0.5294117647058824,
                179.0 / 255.0,
                148.0 / 255.0,
                1.0
            }
        },
        {
            "vssupporter",
            new double[4]
            {
                254.0 / 255.0,
                197.0 / 255.0,
                0.0,
                1.0
            }
        },
        {
            "securityresearcher",
            new double[4]
            {
                49.0 / 255.0,
                53.0 / 85.0,
                58.0 / 85.0,
                1.0
            }
        },
        {
            "bughunter",
            new double[4]
            {
                58.0 / 85.0,
                32.0 / 85.0,
                49.0 / 255.0,
                1.0
            }
        },
        {
            "chiselmaster",
            new double[4]
            {
                242.0 / 255.0,
                244.0 / 255.0,
                11.0 / 15.0,
                1.0
            }
        }
    };

    public static Dictionary<string, TextBackground> playerTagBackgroundByEntitlement = new Dictionary<string, TextBackground>
    {
        {
            "vsteam",
            new TextBackground
            {
                FillColor = GuiStyle.DialogLightBgColor,
                Padding = 3,
                Radius = GuiStyle.ElementBGRadius,
                Shade = true,
                BorderColor = GuiStyle.DialogBorderColor,
                BorderWidth = 3.0
            }
        },
        {
            "vscontributor",
            new TextBackground
            {
                FillColor = GuiStyle.DialogLightBgColor,
                Padding = 3,
                Radius = GuiStyle.ElementBGRadius,
                Shade = true,
                BorderColor = GuiStyle.DialogBorderColor,
                BorderWidth = 3.0
            }
        },
        {
            "vssupporter",
            new TextBackground
            {
                FillColor = GuiStyle.DialogLightBgColor,
                Padding = 3,
                Radius = GuiStyle.ElementBGRadius,
                Shade = true,
                BorderColor = GuiStyle.DialogBorderColor,
                BorderWidth = 3.0
            }
        },
        {
            "securityresearcher",
            new TextBackground
            {
                FillColor = GuiStyle.DialogLightBgColor,
                Padding = 3,
                Radius = GuiStyle.ElementBGRadius,
                Shade = true,
                BorderColor = GuiStyle.DialogBorderColor,
                BorderWidth = 3.0
            }
        },
        {
            "bughunter",
            new TextBackground
            {
                FillColor = GuiStyle.DialogLightBgColor,
                Padding = 3,
                Radius = GuiStyle.ElementBGRadius,
                Shade = true,
                BorderColor = GuiStyle.DialogBorderColor,
                BorderWidth = 3.0
            }
        },
        {
            "chiselmaster",
            new TextBackground
            {
                FillColor = GuiStyle.DialogLightBgColor,
                Padding = 3,
                Radius = GuiStyle.ElementBGRadius,
                Shade = true,
                BorderColor = GuiStyle.DialogBorderColor,
                BorderWidth = 3.0
            }
        }
    };

    public static int[] DefaultChatGroups = new int[5] { GeneralChatGroup, ServerInfoChatGroup, DamageLogChatGroup, InfoLogChatGroup, ConsoleGroup };

    //
    // Summary:
    //     Channel name for the general chat
    public static int GeneralChatGroup = 0;

    //
    // Summary:
    //     Channel name for the general chat
    public static int ServerInfoChatGroup = -1;

    //
    // Summary:
    //     Channel name for the damage chat log
    public static int DamageLogChatGroup = -5;

    //
    // Summary:
    //     Channel name for the info chat log
    public static int InfoLogChatGroup = -6;

    //
    // Summary:
    //     Special channel key typically to reply a Command inside the same the channel
    //     the player sent it
    public static int CurrentChatGroup = -2;

    //
    // Summary:
    //     Special channel key typically to reply a Command inside the same the channel
    //     the player sent it
    public static int AllChatGroups = -3;

    //
    // Summary:
    //     Special channel key for message sent via server console
    public static int ConsoleGroup = -4;

    //
    // Summary:
    //     Allowed characters for a player group name
    public static string AllowedChatGroupChars = "a-z0-9A-Z_";

    //
    // Summary:
    //     Bit of a helper thing for single player servers to display the correct entitlements
    public static string SinglePlayerEntitlements;

    //
    // Summary:
    //     The entity class used when spawning items in the world
    public static AssetLocation EntityItemTypeCode = new AssetLocation("item");

    //
    // Summary:
    //     The entity class used when spawning players
    public static AssetLocation EntityPlayerTypeCode = new AssetLocation("player");

    //
    // Summary:
    //     The entity class used when spawning falling blocks
    public static AssetLocation EntityBlockFallingTypeCode = new AssetLocation("blockfalling");

    //
    // Summary:
    //     Default Itemstack attributes that should always be ignored during a stack.Collectible.Equals()
    //     comparison
    public static string[] IgnoredStackAttributes = new string[4] { "temperature", "toolMode", "renderVariant", "transitionstate" };

    //
    // Summary:
    //     Global modifier to change the spoil rate of foods. Can be changed during run-time.
    //     The value is multiplied to the normal spoilage rate (default: 1)
    public static float PerishSpeedModifier = 1f;

    //
    // Summary:
    //     Global modifier to change the rate of player hunger. Can be changed during run-time.
    //     The value is multiplied to the normal spoilage rate (default: 1)
    public static float HungerSpeedModifier = 1f;

    //
    // Summary:
    //     Global modifier to change the damage melee attacks from creatures inflict. Can
    //     be changed during run-time. The value is multiplied to the normal damage value
    //     (default: 1)
    public static float CreatureDamageModifier = 1f;

    //
    // Summary:
    //     Global modifier to change the block breaking speed of all tools. Can be changed
    //     during run-time. The value is multiplied to the breaking speed (default: 1)
    public static float ToolMiningSpeedModifier = 1f;

    public static FoodSpoilageCalcDelegate FoodSpoilHealthLossMulHandler => (float spoilState, ItemStack stack, EntityAgent byEntity) => Math.Max(0f, 1f - spoilState);

    public static FoodSpoilageCalcDelegate FoodSpoilSatLossMulHandler => (float spoilState, ItemStack stack, EntityAgent byEntity) => Math.Max(0f, 1f - spoilState);

    //
    // Summary:
    //     Returns true if the player fell out of the world (which is map boundaries + 30
    //     blocks in every direction)
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    //
    //   blockAccessor:
    public static bool OutsideWorld(int x, int y, int z, IBlockAccessor blockAccessor)
    {
        if (x >= -30 && z >= -30 && y >= -30 && x <= blockAccessor.MapSizeX + 30)
        {
            return z > blockAccessor.MapSizeZ + 30;
        }

        return true;
    }

    //
    // Summary:
    //     Returns true if the player fell out of the world (which is map boundaries + 30
    //     blocks in every direction)
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    //
    //   blockAccessor:
    public static bool OutsideWorld(double x, double y, double z, IBlockAccessor blockAccessor)
    {
        if (!(x < -30.0) && !(z < -30.0) && !(y < -30.0) && !(x > (double)(blockAccessor.MapSizeX + 30)))
        {
            return z > (double)(blockAccessor.MapSizeZ + 30);
        }

        return true;
    }

    public static float FoodSpoilageHealthLossMul(float spoilState, ItemStack stack, EntityAgent byEntity)
    {
        return FoodSpoilHealthLossMulHandler(spoilState, stack, byEntity);
    }

    public static float FoodSpoilageSatLossMul(float spoilState, ItemStack stack, EntityAgent byEntity)
    {
        return FoodSpoilSatLossMulHandler(spoilState, stack, byEntity);
    }
}
#if false // Decompilation log
'181' items in cache
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
Found single assembly: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\SkiaSharp.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Found single assembly: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\protobuf-net.dll'
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
Found single assembly: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\cairo-sharp.dll'
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
Found single assembly: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.GraphicsLibraryFramework.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Found single assembly: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Microsoft.Data.Sqlite.dll'
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
Found single assembly: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Desktop.dll'
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
