using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class KnappingRenderer : IRenderer, IDisposable
{
	protected ICoreClientAPI api;

	protected BlockPos pos;

	protected MeshRef workItemMeshRef;

	protected MeshRef recipeOutlineMeshRef;

	protected ItemStack workItem;

	protected int texId;

	public string Material;

	protected Matrixf ModelMat = new Matrixf();

	protected Vec4f outLineColorMul = new Vec4f(1f, 1f, 1f, 1f);

	protected Vec3f origin = new Vec3f(0f, 0f, 0f);

	public double RenderOrder => 0.5;

	public int RenderRange => 24;

	public KnappingRenderer(BlockPos pos, ICoreClientAPI capi)
	{
		this.pos = pos;
		api = capi;
		capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "knappingsurface");
		capi.Event.RegisterRenderer(this, EnumRenderStage.AfterFinalComposition, "knappingsurface");
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (workItemMeshRef != null)
		{
			if (stage == EnumRenderStage.AfterFinalComposition)
			{
				RenderRecipeOutLine();
				return;
			}
			IRenderAPI rpi = api.Render;
			Vec3d camPos = api.World.Player.Entity.CameraPos;
			rpi.GlDisableCullFace();
			IStandardShaderProgram standardShaderProgram = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
			rpi.BindTexture2d(texId);
			standardShaderProgram.ProjectionMatrix = rpi.CurrentProjectionMatrix;
			standardShaderProgram.ViewMatrix = rpi.CameraMatrixOriginf;
			standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - camPos.X, (double)pos.Y - camPos.Y, (double)pos.Z - camPos.Z).Values;
			rpi.RenderMesh(workItemMeshRef);
			standardShaderProgram.ModelMatrix = rpi.CurrentModelviewMatrix;
			standardShaderProgram.Stop();
		}
	}

	private void RenderRecipeOutLine()
	{
		if (recipeOutlineMeshRef != null && !api.HideGuis)
		{
			IRenderAPI rpi = api.Render;
			IClientWorldAccessor world = api.World;
			EntityPos plrPos = world.Player.Entity.Pos;
			Vec3d camPos = world.Player.Entity.CameraPos;
			outLineColorMul.A = 1f - GameMath.Clamp((float)Math.Sqrt(plrPos.SquareDistanceTo(pos.X, pos.Y, pos.Z)) / 5f - 1f, 0f, 1f);
			ModelMat.Set(rpi.CameraMatrixOriginf).Translate((double)pos.X - camPos.X, (double)pos.Y - camPos.Y, (double)pos.Z - camPos.Z);
			float linewidth = (rpi.LineWidth = api.Settings.Float["wireframethickness"]);
			rpi.GLEnableDepthTest();
			rpi.GlToggleBlend(blend: true);
			IShaderProgram engineShader = rpi.GetEngineShader(EnumShaderProgram.Wireframe);
			engineShader.Use();
			engineShader.Uniform("origin", origin);
			engineShader.Uniform("colorIn", outLineColorMul);
			engineShader.UniformMatrix("projectionMatrix", rpi.CurrentProjectionMatrix);
			engineShader.UniformMatrix("modelViewMatrix", ModelMat.Values);
			rpi.RenderMesh(recipeOutlineMeshRef);
			engineShader.Stop();
			if (linewidth != 1.6f)
			{
				rpi.LineWidth = 1.6f;
			}
			rpi.GLDepthMask(on: false);
		}
	}

	public void RegenMesh(bool[,] Voxels, KnappingRecipe recipeToOutline)
	{
		workItemMeshRef?.Dispose();
		workItemMeshRef = null;
		workItem = new ItemStack(api.World.GetBlock(new AssetLocation("knappingsurface")));
		if (workItem?.Block == null)
		{
			return;
		}
		if (recipeToOutline != null)
		{
			RegenOutlineMesh(recipeToOutline, Voxels);
		}
		MeshData workItemMesh = new MeshData(24, 36);
		float subPixelPaddingx = api.BlockTextureAtlas.SubPixelPaddingX;
		float subPixelPaddingy = api.BlockTextureAtlas.SubPixelPaddingY;
		TextureAtlasPosition tpos = api.BlockTextureAtlas.GetPosition(workItem.Block, Material);
		MeshData singleVoxelMesh = CubeMeshUtil.GetCubeOnlyScaleXyz(1f / 32f, 1f / 32f, new Vec3f(1f / 32f, 1f / 32f, 1f / 32f));
		singleVoxelMesh.Rgba = new byte[96].Fill(byte.MaxValue);
		CubeMeshUtil.SetXyzFacesAndPacketNormals(singleVoxelMesh);
		texId = tpos.atlasTextureId;
		for (int k = 0; k < singleVoxelMesh.Uv.Length; k += 2)
		{
			singleVoxelMesh.Uv[k] = tpos.x1 + singleVoxelMesh.Uv[k] * 2f / (float)api.BlockTextureAtlas.Size.Width - subPixelPaddingx;
			singleVoxelMesh.Uv[k + 1] = tpos.y1 + singleVoxelMesh.Uv[k + 1] * 2f / (float)api.BlockTextureAtlas.Size.Height - subPixelPaddingy;
		}
		singleVoxelMesh.XyzFaces = (byte[])CubeMeshUtil.CubeFaceIndices.Clone();
		singleVoxelMesh.XyzFacesCount = 6;
		singleVoxelMesh.ClimateColorMapIds = new byte[6];
		singleVoxelMesh.SeasonColorMapIds = new byte[6];
		singleVoxelMesh.ColorMapIdsCount = 6;
		MeshData voxelMeshOffset = singleVoxelMesh.Clone();
		for (int x = 0; x < 16; x++)
		{
			for (int z = 0; z < 16; z++)
			{
				if (Voxels[x, z])
				{
					float px = (float)x / 16f;
					float pz = (float)z / 16f;
					for (int j = 0; j < singleVoxelMesh.xyz.Length; j += 3)
					{
						voxelMeshOffset.xyz[j] = px + singleVoxelMesh.xyz[j];
						voxelMeshOffset.xyz[j + 1] = singleVoxelMesh.xyz[j + 1];
						voxelMeshOffset.xyz[j + 2] = pz + singleVoxelMesh.xyz[j + 2];
					}
					float offsetX = px * 32f / (float)api.BlockTextureAtlas.Size.Width;
					float offsetZ = pz * 32f / (float)api.BlockTextureAtlas.Size.Height;
					for (int i = 0; i < singleVoxelMesh.Uv.Length; i += 2)
					{
						voxelMeshOffset.Uv[i] = singleVoxelMesh.Uv[i] + offsetX;
						voxelMeshOffset.Uv[i + 1] = singleVoxelMesh.Uv[i + 1] + offsetZ;
					}
					workItemMesh.AddMeshData(voxelMeshOffset);
				}
			}
		}
		workItemMeshRef = api.Render.UploadMesh(workItemMesh);
	}

	private void RegenOutlineMesh(KnappingRecipe recipeToOutline, bool[,] Voxels)
	{
		MeshData recipeOutlineMesh = new MeshData(24, 36, withNormals: false, withUv: false, withRgba: true, withFlags: false);
		recipeOutlineMesh.SetMode(EnumDrawMode.Lines);
		int greenCol = api.ColorPreset.GetColor("voxelColorGreen");
		int color = api.ColorPreset.GetColor("voxelColorOrange");
		MeshData greenVoxelMesh = LineMeshUtil.GetCube(greenCol);
		MeshData orangeVoxelMesh = LineMeshUtil.GetCube(color);
		for (int j = 0; j < greenVoxelMesh.xyz.Length; j++)
		{
			greenVoxelMesh.xyz[j] = greenVoxelMesh.xyz[j] / 32f + 1f / 32f;
			orangeVoxelMesh.xyz[j] = orangeVoxelMesh.xyz[j] / 32f + 1f / 32f;
		}
		MeshData voxelMeshOffset = greenVoxelMesh.Clone();
		for (int x = 0; x < 16; x++)
		{
			for (int z = 0; z < 16; z++)
			{
				bool shouldFill = recipeToOutline.Voxels[x, 0, z];
				bool didFill = Voxels[x, z];
				if (shouldFill != didFill)
				{
					float px = (float)x / 16f;
					float py = 0.001f;
					float pz = (float)z / 16f;
					for (int i = 0; i < greenVoxelMesh.xyz.Length; i += 3)
					{
						voxelMeshOffset.xyz[i] = px + greenVoxelMesh.xyz[i];
						voxelMeshOffset.xyz[i + 1] = py + greenVoxelMesh.xyz[i + 1];
						voxelMeshOffset.xyz[i + 2] = pz + greenVoxelMesh.xyz[i + 2];
					}
					voxelMeshOffset.Rgba = ((shouldFill && !didFill) ? greenVoxelMesh.Rgba : orangeVoxelMesh.Rgba);
					recipeOutlineMesh.AddMeshData(voxelMeshOffset);
				}
			}
		}
		recipeOutlineMeshRef?.Dispose();
		recipeOutlineMeshRef = null;
		if (recipeOutlineMesh.VerticesCount > 0)
		{
			recipeOutlineMeshRef = api.Render.UploadMesh(recipeOutlineMesh);
		}
	}

	public void Dispose()
	{
		api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		api.Event.UnregisterRenderer(this, EnumRenderStage.AfterFinalComposition);
		recipeOutlineMeshRef?.Dispose();
		workItemMeshRef?.Dispose();
	}
}
