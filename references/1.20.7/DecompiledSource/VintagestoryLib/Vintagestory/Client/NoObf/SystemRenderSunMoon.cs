using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class SystemRenderSunMoon : ClientSystem
{
	private MeshRef quadModel;

	private int suntextureId;

	private int[] moontextureIds;

	internal int ImageSize;

	public Matrixf ModelMat = new Matrixf();

	private int occlQueryId;

	private bool nowQuerying;

	private Matrix4 sunmat;

	private Matrix4 moonmat;

	private float targetSunSpec;

	private bool firstTickDone;

	public float sunScale = 0.04f;

	public float moonScale = 0.023100002f;

	public override string Name => "resm";

	public SystemRenderSunMoon(ClientMain game)
		: base(game)
	{
		suntextureId = -1;
		moontextureIds = new int[8];
		moontextureIds.Fill(-1);
		ImageSize = 256;
		MeshData meshdata = QuadMeshUtilExt.GetCustomQuadModelData(0f, 0f, 0f, ImageSize, ImageSize, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		meshdata.Flags = new int[4];
		quadModel = game.Platform.UploadMesh(meshdata);
		game.eventManager.RegisterRenderer(OnRenderFrame3D, EnumRenderStage.Opaque, Name, 0.3);
		game.eventManager.RegisterRenderer(OnRenderFrame3DPost, EnumRenderStage.Opaque, Name, 999.0);
		GL.GenQueries(1, out occlQueryId);
	}

	private void OnRenderFrame3DPost(float obj)
	{
		ClientPlatformAbstract platform = game.Platform;
		platform.GlEnableDepthTest();
		platform.GlToggleBlend(on: true);
		platform.GlDisableCullFace();
		platform.GlDepthMask(flag: false);
		GL.ColorMask(red: false, green: false, blue: false, alpha: false);
		Vec3f sunPos = game.Calendar.SunPosition;
		Quaternion quat = CreateLookRotation(new Vector3(sunPos.X, sunPos.Y, sunPos.Z));
		sunmat = Matrix4.CreateTranslation(-ImageSize / 2, -ImageSize, -ImageSize / 2) * Matrix4.CreateScale(sunScale, sunScale * 7f, sunScale) * Matrix4.CreateFromQuaternion(quat) * Matrix4.CreateTranslation(new Vector3(sunPos.X, sunPos.Y, sunPos.Z));
		ShaderProgramStandard prog = ShaderPrograms.Standard;
		prog.Use();
		prog.RgbaTint = ColorUtil.WhiteArgbVec;
		prog.RgbaAmbientIn = ColorUtil.WhiteRgbVec;
		prog.RgbaLightIn = new Vec4f(0f, 0f, 0f, (float)Math.Sin((double)game.ElapsedMilliseconds / 1000.0) / 2f + 0.5f);
		prog.RgbaFogIn = game.AmbientManager.BlendedFogColor;
		prog.ExtraGlow = 0;
		prog.FogMinIn = game.AmbientManager.BlendedFogMin;
		prog.FogDensityIn = game.AmbientManager.BlendedFogDensity;
		prog.DontWarpVertices = 0;
		prog.AddRenderFlags = 0;
		prog.ExtraZOffset = 0f;
		prog.NormalShaded = 0;
		prog.OverlayOpacity = 0f;
		prog.ExtraGodray = 1f / 3f;
		prog.UniformMatrix("modelMatrix", ref sunmat);
		prog.ViewMatrix = game.api.renderapi.CameraMatrixOriginf;
		prog.ProjectionMatrix = game.api.renderapi.CurrentProjectionMatrix;
		prog.Tex2D = suntextureId;
		if (firstTickDone)
		{
			GL.GetQueryObject(occlQueryId, GetQueryObjectParam.QueryResultAvailable, out int resultReady);
			if (resultReady > 0)
			{
				GL.GetQueryObject(occlQueryId, GetQueryObjectParam.QueryResult, out int samplesRendered);
				targetSunSpec = GameMath.Clamp((float)samplesRendered / 1500f, 0f, 1f);
				nowQuerying = false;
			}
		}
		firstTickDone = true;
		bool didBeginQuery = false;
		if (!nowQuerying)
		{
			GL.BeginQuery(QueryTarget.SamplesPassed, occlQueryId);
			nowQuerying = true;
			didBeginQuery = true;
		}
		platform.RenderMesh(quadModel);
		prog.Stop();
		if (didBeginQuery)
		{
			GL.EndQuery(QueryTarget.SamplesPassed);
		}
		platform.GlDepthMask(flag: true);
		GL.ColorMask(red: true, green: true, blue: true, alpha: true);
	}

	public void OnRenderFrame3D(float dt)
	{
		ClientPlatformAbstract platform = game.Platform;
		if (suntextureId == -1)
		{
			suntextureId = game.GetOrLoadCachedTexture(new AssetLocation("environment/sun.png"));
			for (int j = 0; j < 8; j++)
			{
				moontextureIds[j] = game.GetOrLoadCachedTexture(new AssetLocation("environment/moon/" + j + ".png"));
			}
		}
		Vec3f moonPosition = game.Calendar.MoonPosition;
		Vec3f sunPosRel = game.GameWorldCalendar.SunPositionNormalized;
		game.shUniforms.SunPosition3D = sunPosRel;
		Vec3f moonPosRel = moonPosition.Clone().Normalize();
		float moonb = game.GameWorldCalendar.MoonLightStrength;
		float sunb = game.GameWorldCalendar.SunLightStrength;
		float t = GameMath.Clamp(50f * (moonb - sunb), 0f, 1f);
		game.shUniforms.LightPosition3D.Set(GameMath.Lerp(sunPosRel.X, moonPosRel.X, t), GameMath.Lerp(sunPosRel.Y, moonPosRel.Y, t), GameMath.Lerp(sunPosRel.Z, moonPosRel.Z, t));
		if (sunPosRel.Y < -0.05f)
		{
			double[] projMat2 = game.PMatrix.Top;
			double[] viewMat2 = game.api.renderapi.CameraMatrixOrigin;
			double[] modelViewMat2 = new double[16];
			Mat4d.Mul(modelViewMat2, viewMat2, ModelMat.ValuesAsDouble);
			Vec3d screenPos2 = MatrixToolsd.Project(new Vec3d((float)ImageSize / 4f, (float)(-ImageSize) / 4f, 0.0), projMat2, modelViewMat2, game.Width, game.Height);
			Vec3f centeredRelPos2 = new Vec3f((float)screenPos2.X / (float)game.Width * 2f - 1f, (float)screenPos2.Y / (float)game.Height * 2f - 1f, (float)screenPos2.Z);
			game.shUniforms.SunPositionScreen = centeredRelPos2;
		}
		platform.GlToggleBlend(on: true);
		platform.GlDisableCullFace();
		platform.GlDisableDepthTest();
		prepareSunMat();
		Vec3f suncol = game.Calendar.SunColor.Clone();
		float f = (suncol.R + suncol.G + suncol.B) / 3f;
		float colorLoss = GameMath.Clamp(GameMath.Max(game.AmbientManager.BlendedFlatFogDensity * 40f, game.AmbientManager.BlendedCloudDensity * game.AmbientManager.BlendedCloudDensity), 0f, 1f);
		suncol.R = colorLoss * f + (1f - colorLoss) * suncol.R;
		suncol.G = colorLoss * f + (1f - colorLoss) * suncol.G;
		suncol.B = colorLoss * f + (1f - colorLoss) * suncol.B;
		ShaderProgramStandard prog = ShaderPrograms.Standard;
		prog.Use();
		prog.Uniform("skyShaded", 1);
		Vec4f rgbatint = new Vec4f(1f, 1f, 1f, 1f);
		DefaultShaderUniforms shu = game.api.renderapi.ShaderUniforms;
		if (shu.FogSphereQuantity > 0)
		{
			for (int i = 0; i < shu.FogSphereQuantity; i++)
			{
				float num = shu.FogSpheres[i * 8];
				float y = shu.FogSpheres[i * 8 + 1];
				float z = shu.FogSpheres[i * 8 + 2];
				float rad = shu.FogSpheres[i * 8 + 3];
				float dense = shu.FogSpheres[i * 8 + 4];
				double d = Math.Sqrt(num * num + y * y + z * z);
				double fogAmount = (1.0 - d / (double)rad) * (double)rad * (double)dense;
				rgbatint.A = (float)GameMath.Clamp((double)rgbatint.A - fogAmount, 0.0, 1.0);
			}
		}
		prog.FadeFromSpheresFog = 1;
		prog.RgbaTint = rgbatint;
		prog.RgbaAmbientIn = ColorUtil.WhiteRgbVec;
		prog.RgbaLightIn = new Vec4f(suncol.R, suncol.G, suncol.B, 1f);
		prog.RgbaFogIn = game.AmbientManager.BlendedFogColor;
		prog.ExtraGlow = 0;
		prog.FogMinIn = game.AmbientManager.BlendedFogMin;
		prog.FogDensityIn = game.AmbientManager.BlendedFogDensity;
		prog.DontWarpVertices = 0;
		prog.AddRenderFlags = 0;
		prog.ExtraZOffset = 0f;
		prog.NormalShaded = 0;
		prog.OverlayOpacity = 0f;
		prog.ExtraGodray = 1f / 3f;
		prog.ShadowIntensity = 0f;
		prog.ApplySsao = 0;
		prog.AlphaTest = 0.01f;
		prog.UniformMatrix("modelMatrix", ref sunmat);
		prog.ViewMatrix = game.api.renderapi.CameraMatrixOriginf;
		prog.ProjectionMatrix = game.api.renderapi.CurrentProjectionMatrix;
		prog.Tex2D = suntextureId;
		platform.RenderMesh(quadModel);
		prog.Uniform("skyShaded", 0);
		prog.ExtraGodray = 0f;
		prog.ApplySsao = 1;
		prog.FadeFromSpheresFog = 0;
		prog.Stop();
		if (sunPosRel.Y >= -0.05f)
		{
			double[] projMat = game.PMatrix.Top;
			double[] viewMat = game.api.renderapi.CameraMatrixOrigin;
			double[] modelViewMat = new double[16];
			Mat4d.Mul(modelViewMat, viewMat, new double[16]
			{
				sunmat.M11, sunmat.M12, sunmat.M13, sunmat.M14, sunmat.M21, sunmat.M22, sunmat.M23, sunmat.M24, sunmat.M31, sunmat.M32,
				sunmat.M33, sunmat.M34, sunmat.M41, sunmat.M42, sunmat.M43, sunmat.M44
			});
			Vec3d screenPos = MatrixToolsd.Project(new Vec3d((float)ImageSize / 2f, (float)ImageSize / 2f, 0.0), projMat, modelViewMat, game.Width, game.Height);
			Vec3f centeredRelPos = new Vec3f((float)screenPos.X / (float)game.Width * 2f - 1f, (float)screenPos.Y / (float)game.Height * 2f - 1f, (float)screenPos.Z);
			game.shUniforms.SunPositionScreen = centeredRelPos;
		}
		game.shUniforms.SunSpecularIntensity = GameMath.Clamp(game.shUniforms.SunSpecularIntensity + (targetSunSpec - game.shUniforms.SunSpecularIntensity) * dt * 20f, 0f, 1f);
		prepareMoonMat();
		ShaderProgramCelestialobject cprog = ShaderPrograms.Celestialobject;
		cprog.Use();
		cprog.Sky2D = game.skyTextureId;
		cprog.Glow2D = game.skyGlowTextureId;
		cprog.SunPosition = sunPosRel;
		cprog.DayLight = game.shUniforms.SkyDaylight;
		cprog.WeirdMathToMakeMoonLookNicer = 1;
		cprog.DitherSeed = (game.frameSeed + 1) % Math.Max(1, game.Width * game.Height);
		cprog.HorizontalResolution = game.Width;
		cprog.PlayerToSealevelOffset = (float)game.EntityPlayer.Pos.Y - (float)game.SeaLevel;
		cprog.RgbaFogIn = game.AmbientManager.BlendedFogColor;
		cprog.FogMinIn = game.AmbientManager.BlendedFogMin;
		cprog.FogDensityIn = game.AmbientManager.BlendedFogDensity;
		cprog.HorizonFog = game.AmbientManager.BlendedCloudDensity;
		cprog.ExtraGlow = 0;
		cprog.ExtraGodray = 0.5f;
		cprog.UniformMatrix("modelMatrix", ref moonmat);
		cprog.ViewMatrix = game.api.renderapi.CameraMatrixOriginf;
		cprog.ProjectionMatrix = game.api.renderapi.CurrentProjectionMatrix;
		cprog.Tex2D = moontextureIds[(int)game.Calendar.MoonPhase];
		platform.RenderMesh(quadModel);
		cprog.WeirdMathToMakeMoonLookNicer = 0;
		cprog.Stop();
		platform.GlToggleBlend(on: false);
		platform.GlEnableDepthTest();
	}

	public void prepareSunMat()
	{
		Vec3f sunPos = game.Calendar.SunPosition;
		float sunPosY = sunPos.Y + (float)game.EntityPlayer.LocalEyePos.Y - ((float)game.EntityPlayer.Pos.Y - (float)game.SeaLevel) / 10000f;
		Quaternion quat = CreateLookRotation(new Vector3(sunPos.X, sunPosY, sunPos.Z));
		sunmat = Matrix4.CreateTranslation(-ImageSize / 2, -ImageSize / 2, -ImageSize / 2) * Matrix4.CreateScale(sunScale) * Matrix4.CreateFromQuaternion(quat) * Matrix4.CreateTranslation(new Vector3(sunPos.X, sunPosY, sunPos.Z));
	}

	public void prepareMoonMat()
	{
		Vec3f moonPos = game.Calendar.MoonPosition;
		float moonPosY = moonPos.Y + (float)game.EntityPlayer.LocalEyePos.Y - ((float)game.EntityPlayer.Pos.Y - (float)game.SeaLevel) / 10000f;
		Quaternion quat = CreateLookRotation(new Vector3(moonPos.X, moonPosY, moonPos.Z));
		moonmat = Matrix4.CreateTranslation(-ImageSize / 2, -ImageSize / 2, -ImageSize / 2) * Matrix4.CreateScale(moonScale) * Matrix4.CreateFromQuaternion(quat) * Matrix4.CreateTranslation(new Vector3(moonPos.X, moonPosY, moonPos.Z));
	}

	public static Quaternion CreateLookRotation(Vector3 direction)
	{
		Vector3 forwardVecXz = new Vector3(direction.X, 0f, direction.Z).Normalized();
		double rotY = Math.Atan2(forwardVecXz.X, forwardVecXz.Z);
		Quaternion quaternion = Quaternion.FromAxisAngle(Vector3.UnitY, (float)rotY);
		Vector3 forwardVecZy = new Vector3(z: new Vector2(direction.X, direction.Z).Length, x: 0f, y: direction.Y).Normalized();
		Quaternion xQuat = Quaternion.FromAxisAngle(angle: (float)Math.Atan2(forwardVecZy.Y, forwardVecZy.Z), axis: -Vector3.UnitX);
		return quaternion * xQuat;
	}

	public override void Dispose(ClientMain game)
	{
		game.Platform.DeleteMesh(quadModel);
		game.Platform.GLDeleteTexture(suntextureId);
		for (int i = 0; i < moontextureIds.Length; i++)
		{
			game.Platform.GLDeleteTexture(moontextureIds[i]);
		}
		GL.DeleteQuery(occlQueryId);
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}
}
