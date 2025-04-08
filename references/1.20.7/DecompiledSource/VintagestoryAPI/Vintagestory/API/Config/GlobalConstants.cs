using System;
using System.Collections.Generic;
using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Config;

/// <summary>
/// Contains some global constants and static values
/// </summary>
public class GlobalConstants
{
	public static CultureInfo DefaultCultureInfo = CultureInfo.InvariantCulture;

	/// <summary>
	/// Prefix for all default asset locations
	/// </summary>
	public const string DefaultDomain = "game";

	/// <summary>
	/// Hard-enforced world size limit, above this the code may break
	/// </summary>
	public const int MaxWorldSizeXZ = 67108864;

	/// <summary>
	/// Hard-enforced world height limit, above this the code may break.
	/// </summary>
	public const int MaxWorldSizeY = 16384;

	/// <summary>
	/// Now a hard-coded constant
	/// </summary>
	public const int ChunkSize = 32;

	/// <summary>
	/// Used in various places if the dimension of a chunk is combined into the chunk's y value.
	/// </summary>
	public const int DimensionSizeInChunks = 1024;

	/// <summary>
	/// Max. amount of "bones" for animated model. Limited by max amount of shader uniforms of around 60, but depends on the gfx card
	/// This value is overriden by ClientSettings.cs
	/// </summary>
	public static int MaxAnimatedElements = 230;

	/// <summary>
	/// Max. amount of "bones" for color maps. Limited by max amount of shader uniforms, but depends on the gfx card
	/// </summary>
	public const int MaxColorMaps = 40;

	public static int CaveArtColsPerRow = 6;

	/// <summary>
	/// Frame time for physics simulation
	/// </summary>
	public static float PhysicsFrameTime = 1f / 30f;

	/// <summary>
	/// Limits the amount of world time that can be simulated by the physics engine if the server is ticking slowly: if ticks are slower than this, entities will seem to slow down (viewed on client might even jump backwards)
	/// <br /> Recommended range 0.1f to 0.4f
	/// </summary>
	public static float MaxPhysicsIntervalInSlowTicks = 0.135f;

	/// <summary>
	/// A multiplier applied to the y motion of all particles affected by gravity.
	/// </summary>
	public static float GravityStrengthParticle = 0.3f;

	/// <summary>
	/// Attack range when using hands
	/// </summary>
	public static float DefaultAttackRange = 1.5f;

	/// <summary>
	/// Multiplied to all motions and animation speeds
	/// </summary>
	public static float OverallSpeedMultiplier = 1f;

	/// <summary>
	/// Multiplier applied to the players movement motion
	/// </summary>
	public static float BaseMoveSpeed = 1.5f;

	/// <summary>
	/// Multiplier applied to the players jump motion
	/// </summary>
	public static float BaseJumpForce = 8.2f;

	/// <summary>
	/// Multiplier applied to the players sneaking motion
	/// </summary>
	public static float SneakSpeedMultiplier = 0.35f;

	/// <summary>
	/// Multiplier applied to the players sprinting motion
	/// </summary>
	public static double SprintSpeedMultiplier = 2.0;

	/// <summary>
	/// Multiplier applied to entity motion while on the ground or in air
	/// </summary>
	public static float AirDragAlways = 0.983f;

	/// <summary>
	/// Multiplier applied to entity motion while flying (creative mode)
	/// </summary>
	public static float AirDragFlying = 0.8f;

	/// <summary>
	/// Multiplier applied to entity motion while walking in water
	/// </summary>
	public static float WaterDrag = 0.92f;

	/// <summary>
	/// Amount of gravity per tick applied to all entities affected by gravity
	/// </summary>
	public static float GravityPerSecond = 0.37f;

	/// <summary>
	/// Range in blocks at where this entity is simulated on the server (MagicNum.cs sets this value)
	/// </summary>
	public static int DefaultSimulationRange = 128;

	/// <summary>
	/// Range in blocks a player can interact with blocks (break, use, place)
	/// </summary>
	public static float DefaultPickingRange = 4.5f;

	/// <summary>
	/// Time in seconds for dropped items to remain when dropped after player death; overrides the despawn time set in item.json
	/// </summary>
	public static int TimeToDespawnPlayerInventoryDrops = 600;

	/// <summary>
	/// Set by the WeatherSimulation System in the survival mod at the players position
	/// </summary>
	public static Vec3f CurrentWindSpeedClient = new Vec3f();

	public static Vec3f CurrentSurfaceWindSpeedClient = new Vec3f();

	/// <summary>
	/// Set by the SystemPlayerEnvAwarenessTracker System in the engine at the players position, once every second. 12 horizontal, 4 vertical search distance
	/// </summary>
	public static float CurrentDistanceToRainfallClient;

	/// <summary>
	/// Set by the game client at the players position
	/// </summary>
	public static float CurrentNearbyRelLeavesCountClient;

	/// <summary>
	/// Set by the weather simulation system to determine if snowed variants of blocks should melt. Used a static var to improve performance and reduce memory usage
	/// </summary>
	public static bool MeltingFreezingEnabled;

	public static float GuiGearRotJitter = 0f;

	public const int MaxViewDistanceForLodBiases = 640;

	/// <summary>
	/// These reserved characters or sequences should not be used in texture filenames or asset locations, they can mess up the BakedTexture system
	/// </summary>
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

	/// <summary>
	/// Channel name for the general chat
	/// </summary>
	public static int GeneralChatGroup = 0;

	/// <summary>
	/// Channel name for the general chat
	/// </summary>
	public static int ServerInfoChatGroup = -1;

	/// <summary>
	/// Channel name for the damage chat log
	/// </summary>
	public static int DamageLogChatGroup = -5;

	/// <summary>
	/// Channel name for the info chat log
	/// </summary>
	public static int InfoLogChatGroup = -6;

	/// <summary>
	/// Special channel key typically to reply a Command inside the same the channel the player sent it
	/// </summary>
	public static int CurrentChatGroup = -2;

	/// <summary>
	/// Special channel key typically to reply a Command inside the same the channel the player sent it
	/// </summary>
	public static int AllChatGroups = -3;

	/// <summary>
	/// Special channel key for message sent via server console
	/// </summary>
	public static int ConsoleGroup = -4;

	/// <summary>
	/// Allowed characters for a player group name
	/// </summary>
	public static string AllowedChatGroupChars = "a-z0-9A-Z_";

	/// <summary>
	/// Bit of a helper thing for single player servers to display the correct entitlements
	/// </summary>
	public static string SinglePlayerEntitlements;

	/// <summary>
	/// The entity class used when spawning items in the world
	/// </summary>
	public static AssetLocation EntityItemTypeCode = new AssetLocation("item");

	/// <summary>
	/// The entity class used when spawning players
	/// </summary>
	public static AssetLocation EntityPlayerTypeCode = new AssetLocation("player");

	/// <summary>
	/// The entity class used when spawning falling blocks
	/// </summary>
	public static AssetLocation EntityBlockFallingTypeCode = new AssetLocation("blockfalling");

	/// <summary>
	/// Default Itemstack attributes that should always be ignored during a stack.Collectible.Equals() comparison
	/// </summary>
	public static string[] IgnoredStackAttributes = new string[4] { "temperature", "toolMode", "renderVariant", "transitionstate" };

	/// <summary>
	/// Global modifier to change the spoil rate of foods. Can be changed during run-time. The value is multiplied to the normal spoilage rate (default: 1)
	/// </summary>
	public static float PerishSpeedModifier = 1f;

	/// <summary>
	/// Global modifier to change the rate of player hunger. Can be changed during run-time. The value is multiplied to the normal spoilage rate (default: 1)
	/// </summary>
	public static float HungerSpeedModifier = 1f;

	/// <summary>
	/// Global modifier to change the damage melee attacks from creatures inflict. Can be changed during run-time. The value is multiplied to the normal damage value (default: 1)
	/// </summary>
	public static float CreatureDamageModifier = 1f;

	/// <summary>
	/// Global modifier to change the block breaking speed of all tools. Can be changed during run-time. The value is multiplied to the breaking speed (default: 1)
	/// </summary>
	public static float ToolMiningSpeedModifier = 1f;

	public static FoodSpoilageCalcDelegate FoodSpoilHealthLossMulHandler => (float spoilState, ItemStack stack, EntityAgent byEntity) => Math.Max(0f, 1f - spoilState);

	public static FoodSpoilageCalcDelegate FoodSpoilSatLossMulHandler => (float spoilState, ItemStack stack, EntityAgent byEntity) => Math.Max(0f, 1f - spoilState);

	/// <summary>
	/// Returns true if the player fell out of the world (which is map boundaries + 30 blocks in every direction)
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="blockAccessor"></param>
	/// <returns></returns>
	public static bool OutsideWorld(int x, int y, int z, IBlockAccessor blockAccessor)
	{
		if (x >= -30 && z >= -30 && y >= -30 && x <= blockAccessor.MapSizeX + 30)
		{
			return z > blockAccessor.MapSizeZ + 30;
		}
		return true;
	}

	/// <summary>
	/// Returns true if the player fell out of the world (which is map boundaries + 30 blocks in every direction)
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="blockAccessor"></param>
	/// <returns></returns>
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
