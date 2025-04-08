using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class AnvilWorkItemRenderer : IRenderer, IDisposable
{
	private ICoreClientAPI api;

	private BlockPos pos;

	private MeshRef workItemMeshRef;

	private MeshRef recipeOutlineMeshRef;

	private ItemStack ingot;

	private int texId;

	private Vec4f outLineColorMul = new Vec4f(1f, 1f, 1f, 1f);

	protected Matrixf ModelMat = new Matrixf();

	private SurvivalCoreSystem coreMod;

	private BlockEntityAnvil beAnvil;

	private Vec4f glowRgb = new Vec4f();

	protected Vec3f origin = new Vec3f(0f, 0f, 0f);

	public double RenderOrder => 0.5;

	public int RenderRange => 24;

	public AnvilWorkItemRenderer(BlockEntityAnvil beAnvil, BlockPos pos, ICoreClientAPI capi)
	{
		this.pos = pos;
		api = capi;
		this.beAnvil = beAnvil;
		coreMod = capi.ModLoader.GetModSystem<SurvivalCoreSystem>();
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (workItemMeshRef == null)
		{
			return;
		}
		if (stage == EnumRenderStage.AfterFinalComposition)
		{
			if (api.World.Player?.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Collectible is ItemHammer)
			{
				RenderRecipeOutLine();
			}
			return;
		}
		IRenderAPI rpi = api.Render;
		IClientWorldAccessor worldAccess = api.World;
		Vec3d camPos = worldAccess.Player.Entity.CameraPos;
		int num = (int)ingot.Collectible.GetTemperature(api.World, ingot);
		Vec4f lightrgbs = worldAccess.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
		int extraGlow = GameMath.Clamp((num - 550) / 2, 0, 255);
		float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(num);
		glowRgb.R = glowColor[0];
		glowRgb.G = glowColor[1];
		glowRgb.B = glowColor[2];
		glowRgb.A = (float)extraGlow / 255f;
		rpi.GlDisableCullFace();
		IShaderProgram anvilShaderProg = coreMod.anvilShaderProg;
		anvilShaderProg.Use();
		rpi.BindTexture2d(texId);
		anvilShaderProg.Uniform("rgbaAmbientIn", rpi.AmbientColor);
		anvilShaderProg.Uniform("rgbaFogIn", rpi.FogColor);
		anvilShaderProg.Uniform("fogMinIn", rpi.FogMin);
		anvilShaderProg.Uniform("dontWarpVertices", 0);
		anvilShaderProg.Uniform("addRenderFlags", 0);
		anvilShaderProg.Uniform("fogDensityIn", rpi.FogDensity);
		anvilShaderProg.Uniform("rgbaTint", ColorUtil.WhiteArgbVec);
		anvilShaderProg.Uniform("rgbaLightIn", lightrgbs);
		anvilShaderProg.Uniform("rgbaGlowIn", glowRgb);
		anvilShaderProg.Uniform("extraGlow", extraGlow);
		anvilShaderProg.UniformMatrix("modelMatrix", ModelMat.Identity().Translate((double)pos.X - camPos.X, (double)pos.Y - camPos.Y, (double)pos.Z - camPos.Z).Values);
		anvilShaderProg.UniformMatrix("viewMatrix", rpi.CameraMatrixOriginf);
		anvilShaderProg.UniformMatrix("projectionMatrix", rpi.CurrentProjectionMatrix);
		rpi.RenderMesh(workItemMeshRef);
		anvilShaderProg.UniformMatrix("modelMatrix", rpi.CurrentModelviewMatrix);
		anvilShaderProg.Stop();
	}

	private void RenderRecipeOutLine()
	{
		if (recipeOutlineMeshRef != null && !api.HideGuis)
		{
			IRenderAPI rpi = api.Render;
			IClientWorldAccessor world = api.World;
			EntityPos plrPos = world.Player.Entity.Pos;
			Vec3d camPos = world.Player.Entity.CameraPos;
			ModelMat.Set(rpi.CameraMatrixOriginf).Translate((double)pos.X - camPos.X, (double)pos.Y - camPos.Y, (double)pos.Z - camPos.Z);
			outLineColorMul.A = 1f - GameMath.Clamp((float)Math.Sqrt(plrPos.SquareDistanceTo(pos.X, pos.Y, pos.Z)) / 5f - 1f, 0f, 1f);
			float linewidth = (rpi.LineWidth = 2f * api.Settings.Float["wireframethickness"]);
			rpi.GLEnableDepthTest();
			rpi.GlToggleBlend(blend: true);
			IShaderProgram engineShader = rpi.GetEngineShader(EnumShaderProgram.Wireframe);
			engineShader.Use();
			engineShader.Uniform("origin", origin);
			engineShader.UniformMatrix("projectionMatrix", rpi.CurrentProjectionMatrix);
			engineShader.UniformMatrix("modelViewMatrix", ModelMat.Values);
			engineShader.Uniform("colorIn", outLineColorMul);
			rpi.RenderMesh(recipeOutlineMeshRef);
			engineShader.Stop();
			if (linewidth != 1.6f)
			{
				rpi.LineWidth = 1.6f;
			}
			rpi.GLDepthMask(on: false);
		}
	}

	public void RegenMesh(ItemStack workitemStack, byte[,,] voxels, bool[,,] recipeToOutlineVoxels)
	{
		workItemMeshRef?.Dispose();
		workItemMeshRef = null;
		ingot = workitemStack;
		if (workitemStack != null)
		{
			ObjectCacheUtil.Delete(api, workitemStack.Attributes.GetInt("meshRefId").ToString() ?? "");
			workitemStack.Attributes.RemoveAttribute("meshRefId");
			if (recipeToOutlineVoxels != null)
			{
				RegenOutlineMesh(recipeToOutlineVoxels, voxels);
			}
			MeshData workItemMesh = ItemWorkItem.GenMesh(api, workitemStack, voxels, out texId);
			workItemMeshRef = api.Render.UploadMesh(workItemMesh);
		}
	}

	private void RegenOutlineMesh(bool[,,] recipeToOutlineVoxels, byte[,,] voxels)
	{
		MeshData recipeOutlineMesh = new MeshData(24, 36, withNormals: false, withUv: false, withRgba: true, withFlags: false);
		recipeOutlineMesh.SetMode(EnumDrawMode.Lines);
		int greenCol = api.ColorPreset.GetColor("anvilColorGreen");
		int color = api.ColorPreset.GetColor("anvilColorRed");
		MeshData greenVoxelMesh = LineMeshUtil.GetCube(greenCol);
		MeshData orangeVoxelMesh = LineMeshUtil.GetCube(color);
		for (int j = 0; j < greenVoxelMesh.xyz.Length; j++)
		{
			greenVoxelMesh.xyz[j] = greenVoxelMesh.xyz[j] / 32f + 1f / 32f;
			orangeVoxelMesh.xyz[j] = orangeVoxelMesh.xyz[j] / 32f + 1f / 32f;
		}
		MeshData voxelMeshOffset = greenVoxelMesh.Clone();
		int yMax = recipeToOutlineVoxels.GetLength(1);
		for (int x = 0; x < 16; x++)
		{
			for (int y = 0; y < 6; y++)
			{
				for (int z = 0; z < 16; z++)
				{
					bool requireMetalHere = y < yMax && recipeToOutlineVoxels[x, y, z];
					EnumVoxelMaterial mat = (EnumVoxelMaterial)voxels[x, y, z];
					if ((!requireMetalHere || mat != EnumVoxelMaterial.Metal) && (requireMetalHere || mat != 0))
					{
						float px = (float)x / 16f;
						float py = 0.625f + (float)y / 16f;
						float pz = (float)z / 16f;
						for (int i = 0; i < greenVoxelMesh.xyz.Length; i += 3)
						{
							voxelMeshOffset.xyz[i] = px + greenVoxelMesh.xyz[i];
							voxelMeshOffset.xyz[i + 1] = py + greenVoxelMesh.xyz[i + 1];
							voxelMeshOffset.xyz[i + 2] = pz + greenVoxelMesh.xyz[i + 2];
						}
						voxelMeshOffset.Rgba = ((requireMetalHere && mat == EnumVoxelMaterial.Empty) ? greenVoxelMesh.Rgba : orangeVoxelMesh.Rgba);
						recipeOutlineMesh.AddMeshData(voxelMeshOffset);
					}
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
