using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

[JsonObject(MemberSerialization.OptIn)]
public class ClientSettings : SettingsBase
{
	private List<Action<string, KeyCombination>> OnKeyCombinationsUpdated = new List<Action<string, KeyCombination>>();

	public static ClientSettings Inst;

	public override string FileName => Path.Combine(GamePaths.Config, "clientsettings.json");

	public override string BkpFileName => Path.Combine(GamePaths.Config, "clientsettings.bkp");

	public override string TempFileName => Path.Combine(GamePaths.Config, "clientsettings.tmp");

	public static Dictionary<string, Vec2i> DialogPositions
	{
		get
		{
			return Inst.dialogPositions;
		}
		set
		{
			Inst.dialogPositions = value;
			Inst.OtherDirty = true;
		}
	}

	public static Dictionary<string, KeyCombination> KeyMapping
	{
		get
		{
			return Inst.keyMapping;
		}
		set
		{
			Inst.keyMapping = value;
			Inst.OtherDirty = true;
		}
	}

	public static bool SelectedBlockOutline
	{
		get
		{
			return Inst.GetBoolSetting("selectedBlockOutline");
		}
		set
		{
			Inst.Bool["selectedBlockOutline"] = value;
		}
	}

	public static bool ShowPasswordProtectedServers
	{
		get
		{
			return Inst.GetBoolSetting("showPasswordProtectedServers");
		}
		set
		{
			Inst.Bool["showPasswordProtectedServers"] = value;
		}
	}

	public static bool TestGlExtensions
	{
		get
		{
			return Inst.GetBoolSetting("testGlExtensions");
		}
		set
		{
			Inst.Bool["testGlExtensions"] = value;
		}
	}

	public static int ScreenshotExifDataMode
	{
		get
		{
			return Inst.GetIntSetting("screenshotExifDataMode");
		}
		set
		{
			Inst.Int["screenshotExifDataMode"] = value;
		}
	}

	public static bool ShowOpenForAllServers
	{
		get
		{
			return Inst.GetBoolSetting("showOpenForAllServers");
		}
		set
		{
			Inst.Bool["showOpenForAllServers"] = value;
		}
	}

	public static bool ShowWhitelistedServers
	{
		get
		{
			return Inst.GetBoolSetting("showWhitelistedServers");
		}
		set
		{
			Inst.Bool["showWhitelistedServers"] = value;
		}
	}

	public static bool ShowModdedServers
	{
		get
		{
			return Inst.GetBoolSetting("showModdedServers");
		}
		set
		{
			Inst.Bool["showModdedServers"] = value;
		}
	}

	public static bool ShowMoreGfxOptions
	{
		get
		{
			return Inst.GetBoolSetting("showMoreGfxOptions");
		}
		set
		{
			Inst.Bool["showMoreGfxOptions"] = value;
		}
	}

	public static bool DynamicColorGrading
	{
		get
		{
			return Inst.GetBoolSetting("dynamicColorGrading");
		}
		set
		{
			Inst.Bool["dynamicColorGrading"] = value;
		}
	}

	public static bool Occlusionculling
	{
		get
		{
			return Inst.GetBoolSetting("occlusionculling");
		}
		set
		{
			Inst.Bool["occlusionculling"] = value;
		}
	}

	public static bool GlDebugMode
	{
		get
		{
			return Inst.GetBoolSetting("glDebugMode");
		}
		set
		{
			Inst.Bool["glDebugMode"] = value;
		}
	}

	public static bool GlErrorChecking
	{
		get
		{
			return Inst.GetBoolSetting("glErrorChecking");
		}
		set
		{
			Inst.Bool["glErrorChecking"] = value;
		}
	}

	public static bool MultipleInstances
	{
		get
		{
			return Inst.GetBoolSetting("multipleInstances");
		}
		set
		{
			Inst.Bool["multipleInstances"] = value;
		}
	}

	public static bool StartupErrorDialog
	{
		get
		{
			return Inst.GetBoolSetting("startupErrorDialog");
		}
		set
		{
			Inst.Bool["startupErrorDialog"] = value;
		}
	}

	public static bool HighQualityAnimations
	{
		get
		{
			return Inst.GetBoolSetting("highQualityAnimations");
		}
		set
		{
			Inst.Bool["highQualityAnimations"] = value;
		}
	}

	public static bool ToggleSprint
	{
		get
		{
			return Inst.GetBoolSetting("toggleSprint");
		}
		set
		{
			Inst.Bool["toggleSprint"] = value;
		}
	}

	public static bool SeparateCtrl
	{
		get
		{
			return Inst.GetBoolSetting("separateCtrlKeyForMouse");
		}
		set
		{
			Inst.Bool["separateCtrlKeyForMouse"] = value;
		}
	}

	public static int WebRequestTimeout
	{
		get
		{
			return Inst.GetIntSetting("webRequestTimeout");
		}
		set
		{
			Inst.Int["webRequestTimeout"] = value;
		}
	}

	public static int ArchiveLogFileCount
	{
		get
		{
			return Inst.GetIntSetting("archiveLogFileCount");
		}
		set
		{
			Inst.Int["archiveLogFileCount"] = value;
		}
	}

	public static int ArchiveLogFileMaxSizeMb
	{
		get
		{
			return Inst.GetIntSetting("archiveLogFileMaxSizeMb");
		}
		set
		{
			Inst.Int["archiveLogFileMaxSizeMb"] = value;
		}
	}

	public static int OptimizeRamMode
	{
		get
		{
			return Inst.GetIntSetting("optimizeRamMode");
		}
		set
		{
			Inst.Int["optimizeRamMode"] = value;
		}
	}

	public static int LeftDialogMargin
	{
		get
		{
			return Inst.GetIntSetting("leftDialogMargin");
		}
		set
		{
			Inst.Int["leftDialogMargin"] = value;
		}
	}

	public static int RightDialogMargin
	{
		get
		{
			return Inst.GetIntSetting("rightDialogMargin");
		}
		set
		{
			Inst.Int["rightDialogMargin"] = value;
		}
	}

	public static bool ShowSurvivalHelpDialog
	{
		get
		{
			return Inst.GetBoolSetting("showSurvivalHelpDialog");
		}
		set
		{
			Inst.Bool["showSurvivalHelpDialog"] = value;
		}
	}

	public static bool ShowCreativeHelpDialog
	{
		get
		{
			return Inst.GetBoolSetting("showCreativeHelpDialog");
		}
		set
		{
			Inst.Bool["showCreativeHelpDialog"] = value;
		}
	}

	public static bool ViewBobbing
	{
		get
		{
			return Inst.GetBoolSetting("viewBobbing");
		}
		set
		{
			Inst.Bool["viewBobbing"] = value;
		}
	}

	public static bool ChatDialogVisible
	{
		get
		{
			return Inst.GetBoolSetting("chatdialogvisible");
		}
		set
		{
			Inst.Bool["chatdialogvisible"] = value;
		}
	}

	public static string UserEmail
	{
		get
		{
			return Inst.GetStringSetting("useremail");
		}
		set
		{
			Inst.String["useremail"] = value;
		}
	}

	public static string MpToken
	{
		get
		{
			return Inst.GetStringSetting("mptoken");
		}
		set
		{
			Inst.String["mptoken"] = value;
		}
	}

	public static bool HasGameServer
	{
		get
		{
			return Inst.GetBoolSetting("hasGameServer");
		}
		set
		{
			Inst.Bool["hasGameServer"] = value;
		}
	}

	public static string Sessionkey
	{
		get
		{
			return Inst.GetStringSetting("sessionkey");
		}
		set
		{
			Inst.String["sessionkey"] = value;
		}
	}

	public static string SessionSignature
	{
		get
		{
			return Inst.GetStringSetting("sessionsignature");
		}
		set
		{
			Inst.String["sessionsignature"] = value;
		}
	}

	public static string MasterserverUrl
	{
		get
		{
			return Inst.GetStringSetting("masterserverUrl");
		}
		set
		{
			Inst.String["masterserverUrl"] = value;
		}
	}

	public static string ModDbUrl
	{
		get
		{
			return Inst.GetStringSetting("modDbUrl");
		}
		set
		{
			Inst.String["modDbUrl"] = value;
		}
	}

	public static string PlayerUID
	{
		get
		{
			return Inst.GetStringSetting("playeruid");
		}
		set
		{
			Inst.String["playeruid"] = value;
		}
	}

	public static string PlayerName
	{
		get
		{
			return Inst.GetStringSetting("playername");
		}
		set
		{
			Inst.String["playername"] = value;
		}
	}

	public static string Entitlements
	{
		get
		{
			return Inst.GetStringSetting("entitlements");
		}
		set
		{
			Inst.String["entitlements"] = value;
		}
	}

	public static string SettingsVersion
	{
		get
		{
			return Inst.GetStringSetting("settingsVersion");
		}
		set
		{
			Inst.String["settingsVersion"] = value;
		}
	}

	public static float GUIScale
	{
		get
		{
			return Inst.GetFloatSetting("guiScale");
		}
		set
		{
			Inst.Float["guiScale"] = value;
		}
	}

	public static float SwimmingMouseSmoothing
	{
		get
		{
			return Inst.GetFloatSetting("swimmingMouseSmoothing");
		}
		set
		{
			Inst.Float["swimmingMouseSmoothing"] = value;
		}
	}

	public static float FontSize
	{
		get
		{
			return Inst.GetFloatSetting("fontSize");
		}
		set
		{
			Inst.Float["fontSize"] = value;
		}
	}

	public static float LodBias
	{
		get
		{
			return Inst.GetFloatSetting("lodBias");
		}
		set
		{
			Inst.Float["lodBias"] = value;
		}
	}

	public static float LodBiasFar
	{
		get
		{
			return Inst.GetFloatSetting("lodBiasFar");
		}
		set
		{
			Inst.Float["lodBiasFar"] = value;
		}
	}

	public static bool SmoothShadows
	{
		get
		{
			return Inst.GetBoolSetting("smoothShadows");
		}
		set
		{
			Inst.Bool["smoothShadows"] = value;
		}
	}

	public static int SSAOQuality
	{
		get
		{
			return Inst.GetIntSetting("ssaoQuality");
		}
		set
		{
			Inst.Int["ssaoquality"] = value;
		}
	}

	public static bool FlipScreenshot
	{
		get
		{
			return Inst.GetBoolSetting("flipScreenshot");
		}
		set
		{
			Inst.Bool["flipScreenshot"] = value;
		}
	}

	public static bool DeveloperMode
	{
		get
		{
			return Inst.GetBoolSetting("developerMode");
		}
		set
		{
			Inst.Bool["developerMode"] = value;
		}
	}

	public static int ViewDistance
	{
		get
		{
			return Inst.GetIntSetting("viewDistance");
		}
		set
		{
			Inst.Int["viewDistance"] = value;
		}
	}

	public static bool FXAA
	{
		get
		{
			return Inst.GetBoolSetting("fxaa");
		}
		set
		{
			Inst.Bool["fxaa"] = value;
		}
	}

	public static bool RenderMetaBlocks
	{
		get
		{
			return Inst.GetBoolSetting("renderMetaBlocks");
		}
		set
		{
			Inst.Bool["renderMetaBlocks"] = value;
		}
	}

	public static int ChunkVerticesUploadRateLimiter
	{
		get
		{
			return Inst.GetIntSetting("chunkVerticesUploadRateLimiter");
		}
		set
		{
			Inst.Int["chunkVerticesUploadRateLimiter"] = value;
		}
	}

	public static float SSAA
	{
		get
		{
			return Inst.GetFloatSetting("ssaa");
		}
		set
		{
			Inst.Float["ssaa"] = value;
			Inst.isnewfile = false;
		}
	}

	public static float MegaScreenshotSizeMul
	{
		get
		{
			return Inst.GetFloatSetting("megaScreenshotSizeMul");
		}
		set
		{
			Inst.Float["megaScreenshotSizeMul"] = value;
		}
	}

	public static bool WavingFoliage
	{
		get
		{
			return Inst.GetBoolSetting("wavingStuff");
		}
		set
		{
			Inst.Bool["wavingStuff"] = value;
		}
	}

	public static bool LiquidFoamAndShinyEffect
	{
		get
		{
			return Inst.GetBoolSetting("liquidFoamAndShinyEffect");
		}
		set
		{
			Inst.Bool["liquidFoamAndShinyEffect"] = value;
		}
	}

	public static bool PauseGameOnLostFocus
	{
		get
		{
			return Inst.GetBoolSetting("pauseGameOnLostFocus");
		}
		set
		{
			Inst.Bool["pauseGameOnLostFocus"] = value;
		}
	}

	public static bool Bloom
	{
		get
		{
			return Inst.GetBoolSetting("bloom");
		}
		set
		{
			Inst.Bool["bloom"] = value;
		}
	}

	public static int GraphicsPresetId
	{
		get
		{
			return Inst.GetIntSetting("graphicsPresetId");
		}
		set
		{
			Inst.Int["graphicsPresetId"] = value;
		}
	}

	public static int GameWindowMode
	{
		get
		{
			return Inst.GetIntSetting("gameWindowMode");
		}
		set
		{
			Inst.Int["gameWindowMode"] = value;
		}
	}

	public static int MasterSoundLevel
	{
		get
		{
			return Inst.GetIntSetting("masterSoundLevel");
		}
		set
		{
			Inst.Int["masterSoundLevel"] = value;
		}
	}

	public static int SoundLevel
	{
		get
		{
			return Inst.GetIntSetting("soundLevel");
		}
		set
		{
			Inst.Int["soundLevel"] = value;
		}
	}

	public static int EntitySoundLevel
	{
		get
		{
			return Inst.GetIntSetting("entitySoundLevel");
		}
		set
		{
			Inst.Int["entitySoundLevel"] = value;
		}
	}

	public static int AmbientSoundLevel
	{
		get
		{
			return Inst.GetIntSetting("ambientSoundLevel");
		}
		set
		{
			Inst.Int["ambientSoundLevel"] = value;
		}
	}

	public static int WeatherSoundLevel
	{
		get
		{
			return Inst.GetIntSetting("weatherSoundLevel");
		}
		set
		{
			Inst.Int["weatherSoundLevel"] = value;
		}
	}

	public static int MusicLevel
	{
		get
		{
			return Inst.GetIntSetting("musicLevel");
		}
		set
		{
			Inst.Int["musicLevel"] = value;
		}
	}

	public static int MusicFrequency
	{
		get
		{
			return Inst.GetIntSetting("musicFrequency");
		}
		set
		{
			Inst.Int["musicFrequency"] = value;
		}
	}

	public static string Language
	{
		get
		{
			return Inst.GetStringSetting("language");
		}
		set
		{
			Inst.String["language"] = value;
		}
	}

	public static string DefaultFontName
	{
		get
		{
			return Inst.GetStringSetting("defaultFontName");
		}
		set
		{
			Inst.String["defaultFontName"] = value;
		}
	}

	public static string DecorativeFontName
	{
		get
		{
			return Inst.GetStringSetting("decorativeFontName");
		}
		set
		{
			Inst.String["decorativeFontName"] = value;
		}
	}

	public static bool UseServerTextures
	{
		get
		{
			return Inst.GetBoolSetting("useServerTextures");
		}
		set
		{
			Inst.Bool["useServerTextures"] = value;
		}
	}

	public static int ScreenWidth
	{
		get
		{
			return Inst.GetIntSetting("screenWidth");
		}
		set
		{
			Inst.Int["screenWidth"] = value;
		}
	}

	public static int ScreenHeight
	{
		get
		{
			return Inst.GetIntSetting("screenHeight");
		}
		set
		{
			Inst.Int["screenHeight"] = value;
		}
	}

	public static int WeirdMacOSMouseYOffset
	{
		get
		{
			return Inst.GetIntSetting("weirdMacOSMouseYOffset");
		}
		set
		{
			Inst.Int["weirdMacOSMouseYOffset"] = value;
		}
	}

	public static float BlockAtlasSubPixelPadding
	{
		get
		{
			return Inst.GetFloatSetting("blockAtlasSubPixelPadding");
		}
		set
		{
			Inst.Float["blockAtlasSubPixelPadding"] = value;
		}
	}

	public static float ItemAtlasSubPixelPadding
	{
		get
		{
			return Inst.GetFloatSetting("itemAtlasSubPixelPadding");
		}
		set
		{
			Inst.Float["itemAtlasSubPixelPadding"] = value;
		}
	}

	public static int MipMapLevel
	{
		get
		{
			return Inst.GetIntSetting("mipmapLevel");
		}
		set
		{
			Inst.Int["mipmapLevel"] = value;
		}
	}

	public static int MaxQuadParticles
	{
		get
		{
			return Inst.GetIntSetting("maxQuadParticles");
		}
		set
		{
			Inst.Int["maxQuadParticles"] = value;
		}
	}

	public static int MaxCubeParticles
	{
		get
		{
			return Inst.GetIntSetting("maxCubeParticles");
		}
		set
		{
			Inst.Int["maxCubeParticles"] = value;
		}
	}

	public static int MaxAsyncQuadParticles
	{
		get
		{
			return Inst.GetIntSetting("maxAsyncQuadParticles");
		}
		set
		{
			Inst.Int["maxAsyncQuadParticles"] = value;
		}
	}

	public static int MaxAsyncCubeParticles
	{
		get
		{
			return Inst.GetIntSetting("maxAsyncCubeParticles");
		}
		set
		{
			Inst.Int["maxAsyncCubeParticles"] = value;
		}
	}

	public static int MouseSensivity
	{
		get
		{
			return Inst.GetIntSetting("mouseSensivity");
		}
		set
		{
			Inst.Int["mouseSensivity"] = value;
		}
	}

	public static int MouseSmoothing
	{
		get
		{
			return Inst.GetIntSetting("mouseSmoothing");
		}
		set
		{
			Inst.Int["mouseSmoothing"] = value;
		}
	}

	public static int VsyncMode
	{
		get
		{
			return Inst.GetIntSetting("vsyncMode");
		}
		set
		{
			Inst.Int["vsyncMode"] = value;
		}
	}

	public static int ModelDataPoolMaxVertexSize
	{
		get
		{
			return Inst.GetIntSetting("modelDataPoolMaxVertexSize");
		}
		set
		{
			Inst.Int["modelDataPoolMaxVertexSize"] = value;
		}
	}

	public static int ModelDataPoolMaxIndexSize
	{
		get
		{
			return Inst.GetIntSetting("modelDataPoolMaxIndexSize");
		}
		set
		{
			Inst.Int["modelDataPoolMaxIndexSize"] = value;
		}
	}

	public static int ModelDataPoolMaxParts
	{
		get
		{
			return Inst.GetIntSetting("modelDataPoolMaxParts");
		}
		set
		{
			Inst.Int["modelDataPoolMaxParts"] = value;
		}
	}

	public static int FieldOfView
	{
		get
		{
			return Inst.GetIntSetting("fieldOfView");
		}
		set
		{
			Inst.Int["fieldOfView"] = value;
		}
	}

	public static int MaxTextureAtlasWidth
	{
		get
		{
			return Inst.GetIntSetting("maxTextureAtlasWidth");
		}
		set
		{
			Inst.Int["maxTextureAtlasWidth"] = value;
		}
	}

	public static int MaxTextureAtlasHeight
	{
		get
		{
			return Inst.GetIntSetting("maxTextureAtlasHeight");
		}
		set
		{
			Inst.Int["maxTextureAtlasHeight"] = value;
		}
	}

	public static bool SkipNvidiaProfileCheck
	{
		get
		{
			return Inst.GetBoolSetting("skipNvidiaProfileCheck");
		}
		set
		{
			Inst.Bool["skipNvidiaProfileCheck"] = value;
		}
	}

	public static float AmbientBloomLevel
	{
		get
		{
			return Inst.GetFloatSetting("ambientBloomLevel");
		}
		set
		{
			Inst.Float["ambientBloomLevel"] = value;
		}
	}

	public static float ExtraContrastLevel
	{
		get
		{
			return Inst.GetFloatSetting("extraContrastLevel");
		}
		set
		{
			Inst.Float["extraContrastLevel"] = value;
		}
	}

	public static int GodRayQuality
	{
		get
		{
			return Inst.GetIntSetting("godRays");
		}
		set
		{
			Inst.Int["godRays"] = value;
		}
	}

	public static float Minbrightness
	{
		get
		{
			return Inst.GetFloatSetting("minbrightness");
		}
		set
		{
			Inst.Float["minbrightness"] = value;
		}
	}

	public static int RecordingBufferSize
	{
		get
		{
			return Inst.GetIntSetting("recordingBufferSize");
		}
		set
		{
			Inst.Int["recordingBufferSize"] = value;
		}
	}

	public static bool UseHRTFAudio
	{
		get
		{
			return Inst.GetBoolSetting("useHRTFaudio");
		}
		set
		{
			Inst.Bool["useHRTFaudio"] = value;
		}
	}

	public static bool AllowSettingHRTFAudio
	{
		get
		{
			return Inst.GetBoolSetting("allowSettingHRTFaudio");
		}
		set
		{
			Inst.Bool["allowSettingHRTFaudio"] = value;
		}
	}

	public static bool Force48kHzHRTFAudio
	{
		get
		{
			return Inst.GetBoolSetting("force48khzHRTFaudio");
		}
		set
		{
			Inst.Bool["force48khzHRTFaudio"] = value;
		}
	}

	public static bool RenderParticles
	{
		get
		{
			return Inst.GetBoolSetting("renderParticles");
		}
		set
		{
			Inst.Bool["renderParticles"] = value;
		}
	}

	public static bool AmbientParticles
	{
		get
		{
			return Inst.GetBoolSetting("ambientParticles");
		}
		set
		{
			Inst.Bool["ambientParticles"] = value;
		}
	}

	public static bool RenderClouds
	{
		get
		{
			return Inst.GetBoolSetting("renderClouds");
		}
		set
		{
			Inst.Bool["renderClouds"] = value;
		}
	}

	public static bool TransparentRenderPass
	{
		get
		{
			return Inst.GetBoolSetting("transparentRenderPass");
		}
		set
		{
			Inst.Bool["transparentRenderPass"] = value;
		}
	}

	public static float GammaLevel
	{
		get
		{
			return Inst.GetFloatSetting("gammaLevel");
		}
		set
		{
			Inst.Float["gammaLevel"] = value;
		}
	}

	public static float ExtraGammaLevel
	{
		get
		{
			return Inst.GetFloatSetting("extraGammaLevel");
		}
		set
		{
			Inst.Float["extraGammaLevel"] = value;
		}
	}

	public static float BrightnessLevel
	{
		get
		{
			return Inst.GetFloatSetting("brightnessLevel");
		}
		set
		{
			Inst.Float["brightnessLevel"] = value;
		}
	}

	public static float SepiaLevel
	{
		get
		{
			return Inst.GetFloatSetting("sepiaLevel");
		}
		set
		{
			Inst.Float["sepiaLevel"] = value;
		}
	}

	public static float CameraShakeStrength
	{
		get
		{
			return Inst.GetFloatSetting("cameraShakeStrength");
		}
		set
		{
			Inst.Float["cameraShakeStrength"] = value;
		}
	}

	public static float Wireframethickness
	{
		get
		{
			return Inst.GetFloatSetting("wireframethickness");
		}
		set
		{
			Inst.Float["wireframethickness"] = value;
		}
	}

	public static int guiColorsPreset
	{
		get
		{
			return Inst.GetIntSetting("guiColorsPreset");
		}
		set
		{
			Inst.Int["guiColorsPreset"] = value;
		}
	}

	public static float InstabilityWavingStrength
	{
		get
		{
			return Inst.GetFloatSetting("instabilityWavingStrength");
		}
		set
		{
			Inst.Float["instabilityWavingStrength"] = value;
		}
	}

	public static List<string> ModPaths
	{
		get
		{
			return Inst.GetStringListSetting("modPaths");
		}
		set
		{
			Inst.Strings["modPaths"] = value;
		}
	}

	public static List<string> DisabledMods
	{
		get
		{
			return Inst.GetStringListSetting("disabledMods");
		}
		set
		{
			Inst.Strings["disabledMods"] = value;
		}
	}

	public static bool ExtendedDebugInfo
	{
		get
		{
			return Inst.GetBoolSetting("extendedDebugInfo");
		}
		set
		{
			Inst.Bool["extendedDebugInfo"] = value;
		}
	}

	public static bool ScaleScreenshot
	{
		get
		{
			return Inst.GetBoolSetting("scaleScreenshot");
		}
		set
		{
			Inst.Bool["scaleScreenshot"] = value;
		}
	}

	public static float RecordingFrameRate
	{
		get
		{
			return Inst.GetFloatSetting("recordingFrameRate");
		}
		set
		{
			Inst.Float["recordingFrameRate"] = value;
		}
	}

	public static float GameTickFrameRate
	{
		get
		{
			return Inst.GetFloatSetting("gameTickFrameRate");
		}
		set
		{
			Inst.Float["gameTickFrameRate"] = value;
		}
	}

	public static string RecordingCodec
	{
		get
		{
			return Inst.GetStringSetting("recordingCodec");
		}
		set
		{
			Inst.String["recordingCodec"] = value;
		}
	}

	public static int ChatWindowWidth
	{
		get
		{
			return Inst.GetIntSetting("chatWindowWidth");
		}
		set
		{
			Inst.Int["chatWindowWidth"] = value;
		}
	}

	public static int ChatWindowHeight
	{
		get
		{
			return Inst.GetIntSetting("chatWindowHeight");
		}
		set
		{
			Inst.Int["chatWindowHeight"] = value;
		}
	}

	public static int MaxFPS
	{
		get
		{
			return Inst.GetIntSetting("maxFps");
		}
		set
		{
			Inst.Int["maxFps"] = value;
		}
	}

	public static bool ShowEntityDebugInfo
	{
		get
		{
			return Inst.GetBoolSetting("showEntityDebugInfo");
		}
		set
		{
			Inst.Bool["showEntityDebugInfo"] = value;
		}
	}

	public static bool ShowBlockInfoHud
	{
		get
		{
			return Inst.GetBoolSetting("showBlockInfoHud");
		}
		set
		{
			Inst.Bool["showBlockInfoHud"] = value;
		}
	}

	public static bool ShowBlockInteractionHelp
	{
		get
		{
			return Inst.GetBoolSetting("showBlockInteractionHelp");
		}
		set
		{
			Inst.Bool["showBlockInteractionHelp"] = value;
		}
	}

	public static bool ShowCoordinateHud
	{
		get
		{
			return Inst.GetBoolSetting("showCoordinateHud");
		}
		set
		{
			Inst.Bool["showCoordinateHud"] = value;
		}
	}

	public static int ShadowMapQuality
	{
		get
		{
			return Inst.GetIntSetting("shadowMapQuality");
		}
		set
		{
			Inst.Int["shadowMapQuality"] = value;
		}
	}

	public static int ParticleLevel
	{
		get
		{
			return Inst.GetIntSetting("particleLevel");
		}
		set
		{
			Inst.Int["particleLevel"] = value;
		}
	}

	public static int MaxDynamicLights
	{
		get
		{
			return Inst.GetIntSetting("maxDynamicLights");
		}
		set
		{
			Inst.Int["maxDynamicLights"] = value;
		}
	}

	public static float MouseWheelSensivity
	{
		get
		{
			return Inst.GetFloatSetting("mouseWheelSensivity");
		}
		set
		{
			Inst.Float["mouseWheelSensivity"] = value;
		}
	}

	public static string VideoFileTarget
	{
		get
		{
			return Inst.GetStringSetting("videofiletarget");
		}
		set
		{
			Inst.String["videofiletarget"] = value;
		}
	}

	public static string GlContextVersion
	{
		get
		{
			return Inst.GetStringSetting("glContextVersion");
		}
		set
		{
			Inst.String["glContextVersion"] = value;
		}
	}

	public static string AudioDevice
	{
		get
		{
			return Inst.GetStringSetting("audioDevice");
		}
		set
		{
			Inst.String["audioDevice"] = value;
		}
	}

	public static bool DirectMouseMode
	{
		get
		{
			return Inst.GetBoolSetting("directMouseMode");
		}
		set
		{
			Inst.Bool["directMouseMode"] = value;
		}
	}

	public static bool InvertMouseYAxis
	{
		get
		{
			return Inst.GetBoolSetting("invertMouseYAxis");
		}
		set
		{
			Inst.Bool["invertMouseYAxis"] = value;
		}
	}

	public static bool ImmersiveMouseMode
	{
		get
		{
			return Inst.GetBoolSetting("immersiveMouseMode");
		}
		set
		{
			Inst.Bool["immersiveMouseMode"] = value;
		}
	}

	public static bool ImmersiveFpMode
	{
		get
		{
			return Inst.GetBoolSetting("immersiveFpMode");
		}
		set
		{
			Inst.Bool["immersiveFpMode"] = value;
		}
	}

	public static bool AutoChat
	{
		get
		{
			return Inst.GetBoolSetting("autoChat");
		}
		set
		{
			Inst.Bool["autoChat"] = value;
		}
	}

	public static bool AutoChatOpenSelected
	{
		get
		{
			return Inst.GetBoolSetting("autoChatOpenSelected");
		}
		set
		{
			Inst.Bool["autoChatOpenSelected"] = value;
		}
	}

	public static int ItemCollectMode
	{
		get
		{
			return Inst.GetIntSetting("itemCollectMode");
		}
		set
		{
			Inst.Int["itemCollectMode"] = value;
		}
	}

	public static int WindowBorder
	{
		get
		{
			return Inst.GetIntSetting("windowBorder");
		}
		set
		{
			Inst.Int["windowBorder"] = value;
		}
	}

	public static bool OffThreadMipMapCreation
	{
		get
		{
			return Inst.GetBoolSetting("offThreadMipMaps");
		}
		set
		{
			Inst.Bool["offThreadMipMaps"] = value;
		}
	}

	public static float FpHandsYOffset
	{
		get
		{
			return Inst.GetFloatSetting("fpHandsYOffset");
		}
		set
		{
			Inst.Float["fpHandsYOffset"] = value;
		}
	}

	public static int FpHandsFoV
	{
		get
		{
			return Inst.GetIntSetting("fpHandsFoV");
		}
		set
		{
			Inst.Int["fpHandsFoV"] = value;
		}
	}

	public static bool IsNewSettingsFile => Inst.isnewfile;

	static ClientSettings()
	{
		Inst = new ClientSettings();
		try
		{
			Inst.Load();
		}
		catch (Exception e)
		{
			ScreenManager.Platform.Logger.Error("Couldn't load client settings, probably problems with parsing json. Will use default values. The error was:");
			ScreenManager.Platform.Logger.Error(e);
			Inst.LoadDefaultValues();
		}
		if (Inst.isnewfile && File.Exists("default.lang"))
		{
			Language = File.ReadAllText("default.lang").Trim();
		}
	}

	public void SetDialogPosition(string key, Vec2i pos)
	{
		Inst.dialogPositions[key] = pos;
		Inst.OtherDirty = true;
	}

	public Vec2i GetDialogPosition(string key)
	{
		Vec2i value = null;
		Inst.dialogPositions.TryGetValue(key, out value);
		return value;
	}

	public void SetKeyMapping(string key, KeyCombination value)
	{
		keyMapping[key] = value;
		Inst.OtherDirty = true;
		foreach (Action<string, KeyCombination> item in OnKeyCombinationsUpdated)
		{
			item(key, value);
		}
	}

	public KeyCombination GetKeyMapping(string key)
	{
		KeyCombination value = null;
		keyMapping.TryGetValue(key, out value);
		return value;
	}

	public void AddKeyCombinationUpdatedWatcher(Action<string, KeyCombination> handler)
	{
		OnKeyCombinationsUpdated.Add(handler);
	}

	public override void ClearWatchers()
	{
		base.ClearWatchers();
		OnKeyCombinationsUpdated.Clear();
	}

	private ClientSettings()
	{
		keyMapping = new Dictionary<string, KeyCombination>();
		dialogPositions = new Dictionary<string, Vec2i>();
	}

	public override void LoadDefaultValues()
	{
		base.stringSettings["settingsVersion"] = "1.9";
		base.floatSettings["guiScale"] = 1f;
		base.floatSettings["fontSize"] = 1f;
		base.floatSettings["lodBias"] = 0.33f;
		base.floatSettings["lodBiasFar"] = 0.67f;
		base.floatSettings["blockAtlasSubPixelPadding"] = 0.01f;
		base.floatSettings["itemAtlasSubPixelPadding"] = 0f;
		base.floatSettings["gammaLevel"] = 2.2f;
		base.floatSettings["extraGammaLevel"] = 1f;
		base.floatSettings["brightnessLevel"] = 1f;
		base.floatSettings["sepiaLevel"] = 0.2f;
		base.floatSettings["cameraShakeStrength"] = 1f;
		base.floatSettings["wireframethickness"] = 1f;
		base.floatSettings["previewTransparency"] = 0.3f;
		base.floatSettings["fpHandsYOffset"] = 0f;
		base.floatSettings["swimmingMouseSmoothing"] = 0.9f;
		base.intSettings["fpHandsFoV"] = 75;
		base.intSettings["mipmapLevel"] = 3;
		base.intSettings["musicFrequency"] = 2;
		base.intSettings["graphicsPresetId"] = 6;
		base.intSettings["webRequestTimeout"] = 10;
		base.intSettings["maxAnimatedElements"] = 230;
		base.intSettings["archiveLogFileCount"] = 5;
		base.intSettings["archiveLogFileMaxSizeMb"] = 1024;
		base.boolSettings["selectedBlockOutline"] = true;
		base.boolSettings["showMoreGfxOptions"] = false;
		base.boolSettings["dynamicColorGrading"] = true;
		base.boolSettings["multipleInstances"] = false;
		base.boolSettings["showSurvivalHelpDialog"] = true;
		base.boolSettings["showCreativeHelpDialog"] = true;
		base.boolSettings["smoothShadows"] = true;
		base.boolSettings["flipScreenshot"] = true;
		base.boolSettings["renderClouds"] = true;
		base.boolSettings["renderParticles"] = true;
		base.boolSettings["transparentRenderPass"] = true;
		base.boolSettings["extendedDebugInfo"] = false;
		base.boolSettings["showentitydebuginfo"] = false;
		base.boolSettings["showBlockInfoHud"] = true;
		base.boolSettings["showBlockInteractionHelp"] = true;
		base.boolSettings["showCoordinateHud"] = false;
		base.intSettings["shadowMapQuality"] = 0;
		base.boolSettings["wavingStuff"] = true;
		base.boolSettings["renderMetaBlocks"] = false;
		base.boolSettings["ambientParticles"] = true;
		base.boolSettings["pauseGameOnLostFocus"] = false;
		base.boolSettings["viewBobbing"] = true;
		base.boolSettings["invertMouseYAxis"] = false;
		base.boolSettings["highQualityAnimations"] = true;
		base.intSettings["optimizeRamMode"] = 1;
		base.boolSettings["occlusionculling"] = true;
		base.boolSettings["developerMode"] = false;
		base.boolSettings["glDebugMode"] = false;
		base.boolSettings["showWhitelistedServers"] = true;
		base.boolSettings["showModdedServers"] = true;
		base.boolSettings["showPasswordProtectedServers"] = true;
		base.boolSettings["showOpenForAllServers"] = true;
		base.boolSettings["liquidFoamAndShinyEffect"] = true;
		base.boolSettings["testGlExtensions"] = true;
		base.boolSettings["offThreadMipMaps"] = false;
		base.intSettings["ssaoQuality"] = 0;
		base.intSettings["viewDistance"] = 256;
		base.intSettings["fieldOfView"] = 70;
		base.intSettings["maxFps"] = 75;
		base.intSettings["particleLevel"] = 100;
		base.intSettings["maxDynamicLights"] = 10;
		IntSettings["weirdMacOSMouseYOffset"] = 5;
		IntSettings["screenshotExifDataMode"] = 0;
		IntSettings["chunkVerticesUploadRateLimiter"] = 3;
		base.intSettings["masterSoundLevel"] = 100;
		base.intSettings["soundLevel"] = 100;
		base.intSettings["entitySoundLevel"] = 100;
		base.intSettings["ambientSoundLevel"] = 100;
		base.intSettings["weatherSoundLevel"] = 100;
		base.intSettings["musicLevel"] = 20;
		base.intSettings["screenWidth"] = 1024;
		base.intSettings["screenHeight"] = 768;
		base.intSettings["gameWindowMode"] = 1;
		base.intSettings["rightDialogMargin"] = 0;
		base.intSettings["leftDialogMargin"] = 0;
		base.stringSettings["language"] = "en";
		base.stringSettings["defaultFontName"] = "sans-serif";
		base.stringSettings["decorativeFontName"] = "Lora";
		base.intSettings["maxQuadParticles"] = 8000;
		base.intSettings["maxCubeParticles"] = 4000;
		base.intSettings["maxAsyncQuadParticles"] = 80000;
		base.intSettings["maxAsyncCubeParticles"] = 80000;
		base.intSettings["itemCollectMode"] = 0;
		base.boolSettings["toggleSprint"] = false;
		base.boolSettings["separateCtrlKeyForMouse"] = false;
		base.boolSettings["allowSettingHRTFaudio"] = true;
		base.boolSettings["useHRTFaudio"] = false;
		base.boolSettings["force48khzHRTFaudio"] = true;
		base.intSettings["mouseSmoothing"] = 30;
		base.intSettings["mouseSensivity"] = 50;
		base.floatSettings["mouseWheelSensivity"] = ((RuntimeEnv.OS != OS.Mac) ? 1 : 10);
		base.intSettings["modelDataPoolMaxVertexSize"] = 500000;
		base.intSettings["modelDataPoolMaxIndexSize"] = 750000;
		base.intSettings["modelDataPoolMaxParts"] = 1500;
		base.intSettings["maxTextureAtlasWidth"] = 4096;
		base.intSettings["maxTextureAtlasHeight"] = 2048;
		base.floatSettings["ambientBloomLevel"] = 0.2f;
		base.floatSettings["extraContrastLevel"] = 0f;
		base.intSettings["godRays"] = 0;
		base.intSettings["chatWindowWidth"] = 700;
		base.intSettings["chatWindowHeight"] = 200;
		base.intSettings["recordingBufferSize"] = 60;
		base.floatSettings["recordingFrameRate"] = 30f;
		base.floatSettings["gameTickFrameRate"] = -1f;
		base.floatSettings["instabilityWavingStrength"] = 1f;
		base.stringSettings["recordingCodec"] = "rawv";
		base.intSettings["vsyncMode"] = 0;
		base.intSettings["windowBorder"] = 0;
		base.boolSettings["fxaa"] = true;
		base.boolSettings["autoChat"] = true;
		base.boolSettings["autoChatOpenSelected"] = true;
		base.floatSettings["ssaa"] = 1f;
		base.floatSettings["megaScreenshotSizeMul"] = 2f;
		base.floatSettings["minbrightness"] = 0f;
		base.boolSettings["bloom"] = true;
		base.boolSettings["skipNvidiaProfileCheck"] = false;
		base.boolSettings["scaleScreenshot"] = false;
		base.boolSettings["immersiveMouseMode"] = false;
		base.boolSettings["startupErrorDialog"] = false;
		base.stringSettings["glContextVersion"] = "3.3";
		base.stringSettings["mptoken"] = "";
		base.stringSettings["sessionkey"] = "";
		base.stringSettings["sessionsignature"] = "";
		base.stringSettings["useremail"] = "";
		base.stringSettings["entitlements"] = "";
		base.stringSettings["masterserverUrl"] = "https://masterserver.vintagestory.at/api/v1/servers/";
		base.stringSettings["modDbUrl"] = "https://mods.vintagestory.at/";
		base.stringListSettings["multiplayerservers"] = new List<string>();
		base.stringListSettings["disabledMods"] = new List<string>();
		base.stringListSettings["dialogPositions"] = new List<string>();
		base.stringListSettings["modPaths"] = new List<string>(new string[2]
		{
			"Mods",
			GamePaths.DataPathMods
		});
		base.stringListSettings["customPlayStyles"] = new List<string>();
		base.intSettings["guiColorsPreset"] = 1;
		GraphicsPreset high = GraphicsPreset.High;
		GraphicsPresetId = high.PresetId;
		ViewDistance = high.ViewDistance;
		SmoothShadows = high.SmoothLight;
		FXAA = high.FXAA;
		SSAOQuality = high.SSAO;
		WavingFoliage = high.WavingFoliage;
		LiquidFoamAndShinyEffect = high.LiquidFoamEffect;
		Bloom = high.Bloom;
		GodRayQuality = (high.GodRays ? 1 : 0);
		ShadowMapQuality = high.ShadowMapQuality;
		ParticleLevel = high.ParticleLevel;
		MaxDynamicLights = high.DynamicLights;
		SSAA = high.Resolution;
		MaxFPS = high.MaxFps;
	}

	internal override void DidDeserialize()
	{
		if (keyMapping == null)
		{
			keyMapping = new Dictionary<string, KeyCombination>();
		}
		if (dialogPositions == null)
		{
			dialogPositions = new Dictionary<string, Vec2i>();
		}
		base.intSettings.Remove("ambientBloomLevel");
		if (SettingsVersion == null)
		{
			GammaLevel = 2.2f;
			SettingsVersion = "1.0";
			Save();
		}
		if (SettingsVersion == "1.0")
		{
			base.intSettings["maxQuadParticles"] = 8000;
			base.intSettings["maxCubeParticles"] = 4000;
			SettingsVersion = "1.1";
			Save();
		}
		if (SettingsVersion == "1.1")
		{
			BrightnessLevel = 1f;
			ExtraGammaLevel = 1f;
			SettingsVersion = "1.2";
			Save();
		}
		if (SettingsVersion == "1.2")
		{
			MipMapLevel = 3;
			SettingsVersion = "1.3";
			Save();
		}
		if (SettingsVersion == "1.3")
		{
			ScaleScreenshot = false;
			LodBias = 0.33f;
			MegaScreenshotSizeMul = 2f;
			SettingsVersion = "1.4";
			Save();
		}
		if (SettingsVersion == "1.4")
		{
			base.stringSettings["modDbUrl"] = "https://mods.vintagestory.at/";
			SettingsVersion = "1.5";
			Save();
		}
		if (SettingsVersion == "1.5")
		{
			base.intSettings["maxTextureAtlasHeight"] = 4096;
			SettingsVersion = "1.6";
			Save();
		}
		if (SettingsVersion == "1.6" || SettingsVersion == "1.7")
		{
			SettingsVersion = "1.8";
			Save();
		}
		if (SettingsVersion == "1.8" || SettingsVersion == "1.9" || SettingsVersion == "1.10")
		{
			base.intSettings["maxAnimatedElements"] = 46;
			SettingsVersion = "1.11";
			Save();
		}
		if (SettingsVersion == "1.11")
		{
			if (base.intSettings["fpHandsFoV"] == 90)
			{
				base.intSettings["fpHandsFoV"] = 75;
			}
			SettingsVersion = "1.12";
			Save();
		}
		if (SettingsVersion == "1.12")
		{
			base.intSettings["maxAnimatedElements"] = 230;
			SettingsVersion = "1.13";
			Save();
		}
		if (!base.boolSettings.TryGetValue("separateCtrlKeyForMouse", out var unlockedModifiers) || !unlockedModifiers)
		{
			if (keyMapping.TryGetValue("sprint", out var sprintKey) && !keyMapping.ContainsKey("ctrl"))
			{
				keyMapping["ctrl"] = sprintKey.Clone();
			}
			if (keyMapping.TryGetValue("sneak", out var sneakKey) && !keyMapping.ContainsKey("shift"))
			{
				keyMapping["shift"] = sneakKey.Clone();
			}
		}
		GlobalConstants.MaxAnimatedElements = base.intSettings["maxAnimatedElements"];
	}

	public override bool Save(bool force = false)
	{
		bool num = base.Save(force);
		if (!num)
		{
			ClientPlatformAbstract platform = ScreenManager.Platform;
			if (platform == null)
			{
				return num;
			}
			platform.Logger.Notification("Failed saving clientsettings.json, will try again in a few seconds");
		}
		return num;
	}
}
