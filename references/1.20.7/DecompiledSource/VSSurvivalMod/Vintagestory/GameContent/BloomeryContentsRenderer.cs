using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BloomeryContentsRenderer : IRenderer, IDisposable
{
	private BlockPos pos;

	private ICoreClientAPI api;

	private MeshRef cubeModelRef;

	private int textureId;

	private float voxelHeight;

	public int glowLevel;

	protected Matrixf ModelMat = new Matrixf();

	public double RenderOrder => 0.5;

	public int RenderRange => 24;

	public BloomeryContentsRenderer(BlockPos pos, ICoreClientAPI api)
	{
		this.pos = pos;
		this.api = api;
		textureId = api.Render.GetOrLoadTexture(new AssetLocation("block/coal/orecoalmix.png"));
	}

	public void SetFillLevel(float voxelHeight)
	{
		if (this.voxelHeight != voxelHeight || cubeModelRef == null)
		{
			this.voxelHeight = voxelHeight;
			cubeModelRef?.Dispose();
			if (voxelHeight != 0f)
			{
				MeshData modeldata = CubeMeshUtil.GetCube(0.25f, voxelHeight / 32f, new Vec3f(0f, 0f, 0f));
				modeldata.Flags = new int[24];
				cubeModelRef = api.Render.UploadMesh(modeldata);
			}
		}
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (voxelHeight != 0f)
		{
			IStandardShaderProgram standardShaderProgram = api.Render.PreparedStandardShader(pos.X, pos.Y, pos.Z, new Vec4f(1f + (float)glowLevel / 128f, 1f + (float)glowLevel / 128f, 1f + (float)glowLevel / 512f, 1f));
			standardShaderProgram.ExtraGlow = glowLevel;
			IRenderAPI rpi = api.Render;
			Vec3d camPos = api.World.Player.Entity.CameraPos;
			rpi.BindTexture2d(textureId);
			standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((double)(0.5f + (float)pos.X) - camPos.X, (double)pos.Y - camPos.Y + (double)(voxelHeight / 32f), (double)(0.5f + (float)pos.Z) - camPos.Z).Values;
			standardShaderProgram.ViewMatrix = rpi.CameraMatrixOriginf;
			standardShaderProgram.ProjectionMatrix = rpi.CurrentProjectionMatrix;
			rpi.RenderMesh(cubeModelRef);
			standardShaderProgram.Stop();
		}
	}

	public void Dispose()
	{
		api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		cubeModelRef?.Dispose();
	}
}
