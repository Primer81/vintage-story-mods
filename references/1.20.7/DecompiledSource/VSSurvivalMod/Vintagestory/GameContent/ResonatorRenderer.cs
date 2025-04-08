using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ResonatorRenderer : IRenderer, IDisposable
{
	private ICoreClientAPI api;

	private BlockPos pos;

	public MeshRef cylinderMeshRef;

	public Vec3f discPos = new Vec3f(0f, 0.7f, 0f);

	public Vec3f discRotRad = new Vec3f(0f, 0f, 0f);

	private Matrixf ModelMat = new Matrixf();

	private float blockRotation;

	private long updatedTotalMs;

	public double RenderOrder => 0.5;

	public int RenderRange => 24;

	public ResonatorRenderer(BlockPos pos, ICoreClientAPI capi, float blockRot)
	{
		this.pos = pos;
		api = capi;
		blockRotation = blockRot;
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (cylinderMeshRef != null)
		{
			long ellapsedMs = api.InWorldEllapsedMilliseconds;
			IRenderAPI rpi = api.Render;
			Vec3d camPos = api.World.Player.Entity.CameraPos;
			Vec4f lightrgbs = api.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
			rpi.GlDisableCullFace();
			IStandardShaderProgram standardShader = rpi.StandardShader;
			standardShader.Use();
			standardShader.ExtraGlow = 0;
			standardShader.RgbaAmbientIn = rpi.AmbientColor;
			standardShader.RgbaFogIn = rpi.FogColor;
			standardShader.FogMinIn = rpi.FogMin;
			standardShader.FogDensityIn = rpi.FogDensity;
			standardShader.RgbaTint = ColorUtil.WhiteArgbVec;
			standardShader.RgbaLightIn = lightrgbs;
			standardShader.DontWarpVertices = 0;
			standardShader.AddRenderFlags = 0;
			standardShader.ExtraGodray = 0f;
			standardShader.NormalShaded = 1;
			rpi.BindTexture2d(api.ItemTextureAtlas.AtlasTextures[0].TextureId);
			float origx = -0.5f;
			float origy = -0.5f;
			float origz = -0.5f;
			discPos.X = -0.25f;
			discPos.Y = 103f / 160f;
			discPos.Z = 0.1375f;
			discRotRad.X = 0f;
			discRotRad.Y = (float)(ellapsedMs - updatedTotalMs) / 500f * (float)Math.PI;
			discRotRad.Z = 0f;
			standardShader.NormalShaded = 0;
			standardShader.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - camPos.X, (double)pos.Y - camPos.Y, (double)pos.Z - camPos.Z).Translate(0f - origx, 0f, 0f - origz)
				.RotateYDeg(blockRotation)
				.Translate(discPos.X, discPos.Y, discPos.Z)
				.Rotate(discRotRad)
				.Scale(0.9f, 0.9f, 0.9f)
				.Translate(origx, origy, origz)
				.Values;
			rpi.RenderMesh(cylinderMeshRef);
			standardShader.Stop();
		}
	}

	internal void UpdateMeshes(MeshData cylinderMesh)
	{
		cylinderMeshRef?.Dispose();
		cylinderMeshRef = null;
		if (cylinderMesh != null)
		{
			cylinderMeshRef = api.Render.UploadMesh(cylinderMesh);
		}
		updatedTotalMs = api.InWorldEllapsedMilliseconds;
	}

	public void Dispose()
	{
		api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		api.Event.UnregisterRenderer(this, EnumRenderStage.AfterFinalComposition);
		cylinderMeshRef?.Dispose();
	}
}
