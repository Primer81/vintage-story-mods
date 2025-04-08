using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ToolMoldRenderer : IRenderer, IDisposable
{
	private BlockPos pos;

	private ICoreClientAPI api;

	private MeshRef[] quadModelRefs;

	public Matrixf ModelMat = new Matrixf();

	public float Level;

	public float Temperature;

	public AssetLocation TextureName;

	internal Cuboidf[] fillQuadsByLevel;

	public ItemStack stack;

	public double RenderOrder => 0.5;

	public int RenderRange => 24;

	public ToolMoldRenderer(BlockPos pos, ICoreClientAPI api, Cuboidf[] fillQuadsByLevel = null)
	{
		this.pos = pos;
		this.api = api;
		this.fillQuadsByLevel = fillQuadsByLevel;
		quadModelRefs = new MeshRef[fillQuadsByLevel.Length];
		MeshData modeldata = QuadMeshUtil.GetQuad();
		modeldata.Rgba = new byte[16];
		modeldata.Rgba.Fill(byte.MaxValue);
		modeldata.Flags = new int[16];
		for (int i = 0; i < quadModelRefs.Length; i++)
		{
			Cuboidf size = fillQuadsByLevel[i];
			modeldata.Uv = new float[8]
			{
				size.X2 / 16f,
				size.Z2 / 16f,
				size.X1 / 16f,
				size.Z2 / 16f,
				size.X1 / 16f,
				size.Z1 / 16f,
				size.X2 / 16f,
				size.Z1 / 16f
			};
			quadModelRefs[i] = api.Render.UploadMesh(modeldata);
		}
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (!(Level <= 0f) && !(TextureName == null))
		{
			int voxelY = (int)GameMath.Clamp(Level, 0f, fillQuadsByLevel.Length - 1);
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
			prog.AddRenderFlags = 0;
			prog.ExtraGodray = 0f;
			prog.NormalShaded = 0;
			if (stack != null)
			{
				prog.AverageColor = ColorUtil.ToRGBAVec4f(api.BlockTextureAtlas.GetAverageColor((stack.Item?.FirstTexture ?? stack.Block.FirstTextureInventory).Baked.TextureSubId));
				prog.TempGlowMode = 1;
			}
			Vec4f lightrgbs = api.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
			float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f((int)Temperature);
			int extraGlow = (int)GameMath.Clamp((Temperature - 550f) / 2f, 0f, 255f);
			prog.RgbaLightIn = lightrgbs;
			prog.RgbaGlowIn = new Vec4f(glowColor[0], glowColor[1], glowColor[2], (float)extraGlow / 255f);
			prog.ExtraGlow = extraGlow;
			int texid = api.Render.GetOrLoadTexture(TextureName);
			Cuboidf rect = fillQuadsByLevel[voxelY];
			rpi.BindTexture2d(texid);
			prog.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - camPos.X, (double)pos.Y - camPos.Y, (double)pos.Z - camPos.Z).Translate(1f - rect.X1 / 16f, 0.063125f + Math.Max(0f, Level / 16f - 1f / 48f), 1f - rect.Z1 / 16f)
				.RotateX((float)Math.PI / 2f)
				.Scale(0.5f * rect.Width / 16f, 0.5f * rect.Length / 16f, 0.5f)
				.Translate(-1f, -1f, 0f)
				.Values;
			prog.ViewMatrix = rpi.CameraMatrixOriginf;
			prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
			rpi.RenderMesh(quadModelRefs[voxelY]);
			prog.Stop();
			rpi.GlEnableCullFace();
		}
	}

	public void Dispose()
	{
		api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		for (int i = 0; i < quadModelRefs.Length; i++)
		{
			quadModelRefs[i]?.Dispose();
		}
	}
}
