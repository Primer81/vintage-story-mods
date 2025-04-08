using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class IngotMoldRenderer : IRenderer, IDisposable
{
	private BlockPos pos;

	private ICoreClientAPI api;

	private MeshRef quadModelRef;

	private Matrixf ModelMat = new Matrixf();

	public int LevelLeft;

	public int LevelRight;

	public float TemperatureLeft;

	public float TemperatureRight;

	public AssetLocation TextureNameLeft;

	public AssetLocation TextureNameRight;

	public int QuantityMolds = 1;

	private readonly BlockEntityIngotMold entity;

	public ItemStack stack;

	public double RenderOrder => 0.5;

	public int RenderRange => 24;

	public IngotMoldRenderer(BlockEntityIngotMold beim, ICoreClientAPI api)
	{
		pos = beim.Pos;
		this.api = api;
		entity = beim;
		MeshData modeldata = QuadMeshUtil.GetQuad();
		modeldata.Uv = new float[8] { 0.1875f, 0.4375f, 0f, 0.4375f, 0f, 0f, 0.1875f, 0f };
		modeldata.Rgba = new byte[16];
		modeldata.Rgba.Fill(byte.MaxValue);
		modeldata.Flags = new int[16];
		quadModelRef = api.Render.UploadMesh(modeldata);
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (LevelLeft > 0 || LevelRight > 0)
		{
			IRenderAPI rpi = api.Render;
			Vec3d camPos = api.World.Player.Entity.CameraPos;
			rpi.GlDisableCullFace();
			IStandardShaderProgram prog = rpi.StandardShader;
			prog.Use();
			prog.RgbaAmbientIn = rpi.AmbientColor;
			prog.RgbaFogIn = rpi.FogColor;
			prog.FogMinIn = rpi.FogMin;
			prog.FogDensityIn = rpi.FogDensity;
			prog.RgbaTint = ColorUtil.WhiteArgbVec;
			prog.DontWarpVertices = 0;
			prog.ExtraGodray = 0f;
			prog.AddRenderFlags = 0;
			if (stack != null)
			{
				prog.AverageColor = ColorUtil.ToRGBAVec4f(api.BlockTextureAtlas.GetAverageColor((stack.Item?.FirstTexture ?? stack.Block.FirstTextureInventory).Baked.TextureSubId));
				prog.TempGlowMode = 1;
			}
			if (LevelLeft > 0 && TextureNameLeft != null)
			{
				Vec4f lightrgbs2 = api.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
				float[] glowColor2 = ColorUtil.GetIncandescenceColorAsColor4f((int)TemperatureLeft);
				int extraGlow2 = (int)GameMath.Clamp((TemperatureLeft - 550f) / 1.5f, 0f, 255f);
				prog.RgbaLightIn = lightrgbs2;
				prog.RgbaGlowIn = new Vec4f(glowColor2[0], glowColor2[1], glowColor2[2], (float)extraGlow2 / 255f);
				prog.ExtraGlow = extraGlow2;
				prog.NormalShaded = 0;
				int texid2 = api.Render.GetOrLoadTexture(TextureNameLeft);
				rpi.BindTexture2d(texid2);
				float xzOffset = ((QuantityMolds > 1) ? 4.5f : 8.5f);
				ModelMat.Identity().Translate((double)pos.X - camPos.X, (double)pos.Y - camPos.Y, (double)pos.Z - camPos.Z).Translate(0.5f, 0f, 0.5f)
					.RotateY(entity.MeshAngle)
					.Translate(-0.5f, 0f, -0.5f)
					.Translate(xzOffset / 16f, 0.0625f + (float)LevelLeft / 850f, 17f / 32f)
					.RotateX((float)Math.PI / 2f)
					.Scale(3f / 32f, 7f / 32f, 0.5f);
				prog.ModelMatrix = ModelMat.Values;
				prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
				prog.ViewMatrix = rpi.CameraMatrixOriginf;
				rpi.RenderMesh(quadModelRef);
			}
			if (LevelRight > 0 && QuantityMolds > 1 && TextureNameRight != null)
			{
				Vec4f lightrgbs = api.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
				float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f((int)TemperatureRight);
				int extraGlow = (int)GameMath.Clamp((TemperatureRight - 550f) / 1.5f, 0f, 255f);
				prog.RgbaLightIn = lightrgbs;
				prog.RgbaGlowIn = new Vec4f(glowColor[0], glowColor[1], glowColor[2], (float)extraGlow / 255f);
				prog.ExtraGlow = extraGlow;
				prog.NormalShaded = 0;
				int texid = api.Render.GetOrLoadTexture(TextureNameRight);
				rpi.BindTexture2d(texid);
				ModelMat.Identity().Translate((double)pos.X - camPos.X, (double)pos.Y - camPos.Y, (double)pos.Z - camPos.Z).Translate(0.5f, 0f, 0.5f)
					.RotateY(entity.MeshAngle)
					.Translate(-0.5f, 0f, -0.5f)
					.Translate(23f / 32f, 0.0625f + (float)LevelRight / 850f, 17f / 32f)
					.RotateX((float)Math.PI / 2f)
					.Scale(3f / 32f, 7f / 32f, 0.5f);
				prog.ModelMatrix = ModelMat.Values;
				prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
				prog.ViewMatrix = rpi.CameraMatrixOriginf;
				rpi.RenderMesh(quadModelRef);
			}
			prog.Stop();
			rpi.GlEnableCullFace();
		}
	}

	public void Dispose()
	{
		api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		quadModelRef?.Dispose();
	}
}
