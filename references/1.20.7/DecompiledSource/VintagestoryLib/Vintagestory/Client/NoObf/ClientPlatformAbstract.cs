using System;
using System.Collections.Generic;
using Cairo;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public abstract class ClientPlatformAbstract
{
	public delegate void OnFocusChanged(bool focus);

	public bool DoPostProcessingEffects { get; set; }

	public abstract Size2i ScreenSize { get; }

	public abstract Size2i WindowSize { get; }

	public abstract ShaderProgramMinimalGui MinimalGuiShader { get; }

	public abstract bool IsFocused { get; }

	public abstract bool DebugDrawCalls { get; set; }

	public abstract bool GlDebugMode { get; set; }

	public abstract bool GlErrorChecking { get; set; }

	public abstract AssetManager AssetManager { get; }

	public abstract ILogger Logger { get; }

	public IXPlatformInterface XPlatInterface { get; protected set; }

	public abstract int CpuCoreCount { get; }

	public abstract EnumWindowBorder WindowBorder { get; set; }

	public float MaxFps { get; set; } = 60f;


	public abstract long EllapsedMs { get; }

	public abstract bool MouseGrabbed { get; set; }

	public abstract string CurrentMouseCursor { get; protected set; }

	public abstract float MasterSoundLevel { get; set; }

	public abstract bool GlScissorFlagEnabled { get; }

	public abstract IList<string> AvailableAudioDevices { get; }

	public abstract string CurrentAudioDevice { get; set; }

	public abstract DefaultShaderUniforms ShaderUniforms { get; set; }

	public abstract List<FrameBufferRef> FrameBuffers { get; }

	public abstract bool IsServerRunning { get; set; }

	public abstract bool IsGamePaused { get; }

	public bool IsShuttingDown { get; set; }

	public event WindowResizedDelegate WindowResized;

	public abstract void ToggleOffscreenBuffer(bool enable);

	public abstract void RegisterOnFocusChange(OnFocusChanged handler);

	public abstract void CheckGlError(string errmsg = null);

	public abstract void CheckGlErrorAlways(string errmsg = null);

	public abstract string GlGetError();

	public abstract string GetGraphicCardInfos();

	public abstract string GetFrameworkInfos();

	public abstract bool IsExitAvailable();

	public abstract void SetWindowClosedHandler(Action handler);

	public abstract void SetFrameHandler(NewFrameHandler handler);

	public abstract void SetFileDropHandler(Action<string> handler);

	public abstract void RegisterKeyboardEvent(KeyEventHandler handler);

	public abstract void RegisterMouseEvent(MouseEventHandler handler);

	public abstract BitmapRef CreateBitmapFromPng(IAsset asset);

	public abstract BitmapRef CreateBitmapFromPixels(int[] pixels, int width, int height);

	public abstract BitmapRef CreateBitmapFromPng(byte[] data);

	public abstract BitmapRef CreateBitmapFromPng(byte[] data, int dataLength);

	public abstract BitmapRef CreateBitmap(int width, int height);

	public abstract void SetBitmapPixelsArgb(BitmapRef bmp, int[] pixels);

	public abstract string SaveScreenshot(string path = null, string filename = null, bool withAlpha = false, bool flip = false, string metaDataStr = null);

	public abstract BitmapRef GrabScreenshot(bool withAlpha = false, bool scale = false);

	public abstract BitmapRef GrabScreenshot(int width, int height, bool scaleScreenshot, bool flip, bool withAlpha = false);

	public abstract IAviWriter CreateAviWriter(float framerate, string codec);

	public abstract AvailableCodec[] GetAvailableCodecs();

	public abstract void SetVSync(bool enabled);

	public abstract void SetDirectMouseMode(bool enabled);

	public abstract string GetGameVersion();

	public abstract void WindowExit(string reason);

	public void TriggerWindowResized(int width, int height)
	{
		this.WindowResized?.Invoke(width, height);
	}

	public abstract void SetTitle(string applicationname);

	public abstract void AddOnCrash(OnCrashHandler handler);

	public abstract void ClearOnCrash();

	public abstract WindowState GetWindowState();

	public abstract void SetWindowState(WindowState value);

	public abstract void SetWindowAttribute(WindowAttribute attribute, bool value);

	public abstract void ThreadSpinWait(int iterations);

	public abstract void LoadAssets();

	public abstract int GenSampler(bool linear);

	public abstract bool LoadMouseCursor(string cursorCode, int hotx, int hoty, BitmapRef bmp);

	public abstract void UseMouseCursor(string cursorCode, bool forceUpdate = false);

	public abstract MouseEvent CreateMouseEvent(EnumMouseButton button);

	public abstract void RestoreWindowCursor();

	public abstract void SetWindowSize(int width, int height);

	public abstract AudioData CreateAudioData(IAsset asset);

	public abstract ILoadedSound CreateAudio(SoundParams sound, AudioData data);

	public abstract ILoadedSound CreateAudio(SoundParams sound, AudioData data, ClientMain game);

	public abstract void UpdateAudioListener(float posX, float posY, float posZ, float orientX, float orientY, float orientZ);

	public abstract void AddAudioSettingsWatchers();

	public abstract string GetGLShaderVersionString();

	public abstract void GLWireframes(bool toggle);

	public abstract void GlViewport(int x, int y, int width, int height);

	public abstract void GlScissor(int x, int y, int width, int height);

	public abstract void GlScissorFlag(bool enable);

	public abstract void GlDisableDepthTest();

	public abstract void GlClearColorRgbaf(float r, float g, float b, float a);

	public abstract void GlEnableDepthTest();

	public abstract void GlDisableCullFace();

	public abstract void GlEnableCullFace();

	public abstract void GlGenerateTex2DMipmaps();

	public abstract void GLLineWidth(float width);

	public abstract void SmoothLines(bool on);

	public abstract void GlDepthMask(bool flag);

	public abstract void GlDepthFunc(EnumDepthFunction depthFunc);

	public abstract void GlCullFaceBack();

	public abstract void GlToggleBlend(bool on, EnumBlendMode blendMode = EnumBlendMode.Standard);

	public abstract void UpdateMesh(MeshRef meshRef, MeshData updatedata);

	public abstract MeshRef UploadMesh(MeshData data);

	public abstract MeshRef AllocateEmptyMesh(int xyzSize, int normalsSize, int uvSize, int rgbaSize, int flagsSize, int indicesSize, CustomMeshDataPartFloat customFloats, CustomMeshDataPartShort customShorts, CustomMeshDataPartByte customBytes, CustomMeshDataPartInt customInts, EnumDrawMode drawMode = EnumDrawMode.Triangles, bool staticDraw = true);

	public abstract UBORef CreateUBO(int shaderProgramId, int bindingPoint, string blockName, int size);

	public abstract void RenderMesh(MeshRef vao);

	public abstract void RenderMesh(MeshRef meshRef, int[] indicesStarts, int[] indicesSizes, int groupCount);

	public abstract void DeleteMesh(MeshRef vao);

	public abstract void RenderMeshInstanced(MeshRef meshRef, int quantity = 1);

	public abstract void GLDeleteTexture(int id);

	public abstract int GlGetMaxTextureSize();

	public abstract void BindTexture2d(int texture);

	public abstract void BindTextureCubeMap(int texture);

	public abstract void UnBindTextureCubeMap();

	public abstract void LoadOrUpdateTextureFromBgra(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture);

	public abstract void LoadOrUpdateTextureFromRgba(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture);

	public abstract void LoadOrUpdateTextureFromBgra_DeferMipMap(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture);

	public abstract void LoadIntoTexture(IBitmap srcBmp, int targetTexture, int destX, int destY, bool generateMipmaps = false);

	public abstract void BuildMipMaps(int textureId);

	public abstract int LoadTexture(SKBitmap skBitmap, bool linearMag = false, int clampMode = 0, bool generateMipmaps = false);

	public abstract int LoadTexture(IBitmap bmp, bool linearMag = false, int clampMode = 0, bool generateMipmaps = false);

	public abstract int Load3DTextureCube(BitmapRef[] bmps);

	public abstract int LoadCairoTexture(ImageSurface surface, bool linearMag);

	public abstract void LoadOrUpdateCairoTexture(ImageSurface surface, bool linearMag, ref LoadedTexture intoTexture);

	public abstract int GetUniformLocation(ShaderProgram program, string name);

	public abstract bool CompileShader(Shader shader);

	public abstract bool CreateShaderProgram(ShaderProgram shaderprogram);

	public abstract void ClearFrameBuffer(EnumFrameBuffer framebuffer);

	public abstract void ClearFrameBuffer(FrameBufferRef framebuffer, float[] clearColor, bool clearDepthBuffer = true, bool clearColorBuffers = true);

	public abstract void LoadFrameBuffer(EnumFrameBuffer framebuffer);

	public abstract void UnloadFrameBuffer(EnumFrameBuffer framebuffer);

	public abstract void RebuildFrameBuffers();

	public abstract FrameBufferRef CreateFramebuffer(FramebufferAttrs framebuffer);

	public abstract void DisposeFrameBuffer(FrameBufferRef frameBuffer, bool disposeTextures = true);

	public abstract void ClearFrameBuffer(FrameBufferRef frameBuffer, bool clearDepth = true);

	public abstract void LoadFrameBuffer(FrameBufferRef frameBuffer);

	public abstract void LoadFrameBuffer(FrameBufferRef frameBuffer, int textureId);

	public abstract void UnloadFrameBuffer(FrameBufferRef frameBuffer);

	public abstract void MergeTransparentRenderPass();

	public abstract void RenderPostprocessingEffects(float[] projectMatrix);

	public abstract void RenderFinalComposition();

	public abstract void BlitPrimaryToDefault();

	public abstract void StartSinglePlayerServer(StartServerArgs serverargs);

	public abstract void ExitSinglePlayerServer();

	public abstract bool IsLoadedSinglePlayerServer();

	public abstract void SetGamePausedState(bool paused);

	public abstract void ResetGamePauseAndUptimeState();

	public abstract DummyNetwork[] GetSinglePlayerServerNetwork();
}
