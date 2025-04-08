using System;
using SkiaSharp;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

internal class SystemRenderSkyColor : ClientSystem
{
	private MeshRef skyIcosahedron;

	private BitmapRef skyTexture;

	public override string Name => "resc";

	public SystemRenderSkyColor(ClientMain game)
		: base(game)
	{
		MeshData modeldata = ModelIcosahedronUtil.genIcosahedron(3, 250f);
		modeldata.Uv = null;
		skyIcosahedron = game.Platform.UploadMesh(modeldata);
		game.eventManager.RegisterRenderer(OnRenderFrame3D, EnumRenderStage.Opaque, Name, 0.2);
	}

	internal override void OnLevelFinalize()
	{
		skyTexture = game.Platform.CreateBitmapFromPng(game.AssetManager.Get("textures/environment/sky.png"));
		game.skyTextureId = game.Platform.LoadTexture(skyTexture, linearMag: false, 1);
		IAsset asset = ScreenManager.Platform.AssetManager.Get("textures/environment/sunlight.png");
		BitmapRef bmp = game.Platform.CreateBitmapFromPng(asset);
		game.skyGlowTextureId = game.Platform.LoadTexture(bmp, linearMag: true, 1);
		bmp.Dispose();
	}

	public void OnRenderFrame3D(float deltaTime)
	{
		WireframeModes wfmodes = game.api.renderapi.WireframeDebugRender;
		if (game.Width != 0 && game.Height != 0 && !wfmodes.Vertex && skyTexture != null)
		{
			float daylightadjusted = 1.25f * GameMath.Max(game.GameWorldCalendar.DayLightStrength - game.GameWorldCalendar.MoonLightStrength / 2f, 0.05f);
			float space = (float)GameMath.Clamp((game.Player.Entity.Pos.Y - (double)game.SeaLevel - 1000.0) / 30000.0, 0.0, 1.0);
			daylightadjusted = Math.Max(0f, daylightadjusted * (1f - space));
			game.shUniforms.SkyDaylight = daylightadjusted;
			game.shUniforms.DitherSeed = (game.frameSeed + 1) % Math.Max(1, game.Width * game.Height);
			game.shUniforms.SkyTextureId = game.skyTextureId;
			game.shUniforms.GlowTextureId = game.skyGlowTextureId;
			Vec3f Sn = game.GameWorldCalendar.SunPositionNormalized;
			Vec3f viewVector = EntityPos.GetViewVector(game.mouseYaw, game.mousePitch);
			game.GlMatrixModeModelView();
			ShaderProgramSky sky = ShaderPrograms.Sky;
			sky.Use();
			sky.Sky2D = game.skyTextureId;
			sky.Glow2D = game.skyGlowTextureId;
			sky.SunPosition = Sn;
			sky.DayLight = game.shUniforms.SkyDaylight;
			sky.PlayerPos = game.EntityPlayer.Pos.XYZ.ToVec3f();
			sky.DitherSeed = (game.frameSeed = (game.frameSeed + 1) % (game.Width * game.Height));
			sky.HorizontalResolution = game.Width;
			sky.PlayerToSealevelOffset = (float)game.EntityPlayer.Pos.Y - (float)game.SeaLevel;
			sky.RgbaFogIn = game.AmbientManager.BlendedFogColor;
			sky.RgbaAmbientIn = game.AmbientManager.BlendedAmbientColor;
			sky.FogDensityIn = game.AmbientManager.BlendedFogDensity;
			sky.FogMinIn = game.AmbientManager.BlendedFogMin;
			sky.HorizonFog = game.AmbientManager.BlendedCloudDensity;
			sky.ProjectionMatrix = game.CurrentProjectionMatrix;
			sky.SunsetMod = (game.shUniforms.SunsetMod = game.Calendar.SunsetMod);
			calcSunColor(Sn, viewVector);
			game.Platform.GlDisableDepthTest();
			game.GlPushMatrix();
			MatrixToolsd.MatFollowPlayer(game.MvMatrix.Top);
			sky.ModelViewMatrix = game.CurrentModelViewMatrix;
			game.Platform.RenderMesh(skyIcosahedron);
			game.GlPopMatrix();
			game.Reset3DProjection();
			sky.Stop();
			game.Platform.GlEnableDepthTest();
		}
	}

	protected void calcSunColor(Vec3f Sn, Vec3f viewVector)
	{
		float sunIntensity = (GameMath.Clamp(Sn.Y * 1.5f, -1f, 1f) + 1f) / 2f;
		float moonIntensity = GameMath.Max(0f, (GameMath.Clamp((0f - Sn.Y) * 1.5f, -1f, 1f) + 0.9f) / 13f);
		SKColor Ks = skyTexture.GetPixelRel(sunIntensity, 0.99f);
		Vec3f sun = game.GameWorldCalendar.ReflectColor.Clone();
		Vec3f fog = game.FogColorSky.Set((float)(int)Ks.Red / 255f + moonIntensity / 2f, (float)(int)Ks.Green / 255f + moonIntensity / 2f, (float)(int)Ks.Blue / 255f + moonIntensity / 2f);
		float f = (sun.R + sun.G + sun.B) / 3f;
		float diff = game.GameWorldCalendar.DayLightStrength - f;
		float colorLoss = GameMath.Clamp(GameMath.Max(game.AmbientManager.BlendedFlatFogDensity * 40f, game.AmbientManager.BlendedCloudDensity * game.AmbientManager.BlendedCloudDensity), 0f, 1f);
		sun.R = colorLoss * f + (1f - colorLoss) * sun.R;
		sun.G = colorLoss * f + (1f - colorLoss) * sun.G;
		sun.B = colorLoss * f + (1f - colorLoss) * sun.B;
		game.AmbientManager.Sunglow.AmbientColor.Value[0] = sun.R + diff;
		game.AmbientManager.Sunglow.AmbientColor.Value[1] = sun.G + diff;
		game.AmbientManager.Sunglow.AmbientColor.Value[2] = sun.B + diff;
		game.AmbientManager.Sunglow.AmbientColor.Weight = 1f;
		float fac = (float)Math.Sqrt((Math.Abs(Sn.Y) + 0.2f) * (Math.Abs(Sn.Y) + 0.2f) + (viewVector.Z - Sn.Z) * (viewVector.Z - Sn.Z)) / 2f;
		game.AmbientManager.Sunglow.FogColor.Weight = 1f - sunIntensity;
		float r = fac * fog.R + (1f - fac) * sun.R;
		float g = fac * fog.G + (1f - fac) * sun.G;
		float b = fac * fog.B + (1f - fac) * sun.B;
		r = colorLoss * f + (1f - colorLoss) * r;
		g = colorLoss * f + (1f - colorLoss) * g;
		b = colorLoss * f + (1f - colorLoss) * b;
		game.AmbientManager.Sunglow.FogColor.Value[0] = r;
		game.AmbientManager.Sunglow.FogColor.Value[1] = g;
		game.AmbientManager.Sunglow.FogColor.Value[2] = b;
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}

	public override void Dispose(ClientMain game)
	{
		game.Platform.DeleteMesh(skyIcosahedron);
		game.Platform.GLDeleteTexture(game.skyGlowTextureId);
		game.Platform.GLDeleteTexture(game.skyTextureId);
		skyTexture?.Dispose();
	}
}
