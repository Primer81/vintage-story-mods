using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBlockFallingRenderer : EntityRenderer, ITerrainMeshPool
{
	protected EntityBlockFalling blockFallingEntity;

	protected MultiTextureMeshRef meshRef;

	protected Block block;

	protected Matrixf ModelMat = new Matrixf();

	private Vec3d prevPos = new Vec3d();

	private Vec3d curPos = new Vec3d();

	internal bool DoRender;

	private MeshData mesh = new MeshData(4, 3);

	private double rotaccum;

	public EntityBlockFallingRenderer(Entity entity, ICoreClientAPI api)
		: base(entity, api)
	{
		blockFallingEntity = (EntityBlockFalling)entity;
		block = blockFallingEntity.Block;
		entity.PhysicsUpdateWatcher = OnPhysicsTick;
		if (!blockFallingEntity.InitialBlockRemoved)
		{
			int posx = blockFallingEntity.blockEntityAttributes?.GetInt("posx", blockFallingEntity.initialPos.X) ?? blockFallingEntity.initialPos.X;
			int posy = blockFallingEntity.blockEntityAttributes?.GetInt("posy", blockFallingEntity.initialPos.Y) ?? blockFallingEntity.initialPos.Y;
			int posz = blockFallingEntity.blockEntityAttributes?.GetInt("posz", blockFallingEntity.initialPos.Z) ?? blockFallingEntity.initialPos.Z;
			api.World.BlockAccessor.GetBlockEntity(new BlockPos(posx, posy, posz))?.OnTesselation(this, capi.Tesselator);
			if (this.mesh.VerticesCount > 0)
			{
				this.mesh.CustomBytes = null;
				this.mesh.CustomFloats = null;
				this.mesh.CustomInts = null;
				meshRef = capi.Render.UploadMultiTextureMesh(this.mesh);
			}
		}
		if (meshRef == null)
		{
			MeshData mesh = api.TesselatorManager.GetDefaultBlockMesh(block);
			meshRef = api.Render.UploadMultiTextureMesh(mesh);
		}
		_ = block.FirstTextureInventory.Baked.TextureSubId;
		prevPos.Set(entity.Pos.X + (double)entity.SelectionBox.X1, entity.Pos.Y + (double)entity.SelectionBox.Y1, entity.Pos.Z + (double)entity.SelectionBox.Z1);
	}

	public void OnPhysicsTick(float nextAccum, Vec3d prevPos)
	{
		this.prevPos.Set(prevPos.X + (double)entity.SelectionBox.X1, prevPos.Y + (double)entity.SelectionBox.Y1, prevPos.Z + (double)entity.SelectionBox.Z1);
	}

	public void AddMeshData(MeshData data, int lodlevel = 1)
	{
		if (data != null)
		{
			mesh.AddMeshData(data);
		}
	}

	public void AddMeshData(MeshData data, ColorMapData colormapdata, int lodlevel = 1)
	{
		if (data != null)
		{
			mesh.AddMeshData(data);
		}
	}

	public void AddMeshData(MeshData data, float[] tfMatrix, int lodLevel = 1)
	{
		if (data != null)
		{
			mesh.AddMeshData(data);
		}
	}

	public override void DoRender3DOpaque(float dt, bool isShadowPass)
	{
		if (!isShadowPass && DoRender && (blockFallingEntity.InitialBlockRemoved || entity.World.BlockAccessor.GetBlock(blockFallingEntity.initialPos).Id == 0))
		{
			rotaccum += dt;
			curPos.Set(entity.Pos.X + (double)entity.SelectionBox.X1, entity.Pos.Y + (double)entity.SelectionBox.Y1, entity.Pos.Z + (double)entity.SelectionBox.Z1);
			RenderFallingBlockEntity();
		}
	}

	private void RenderFallingBlockEntity()
	{
		IRenderAPI rapi = capi.Render;
		rapi.GlDisableCullFace();
		rapi.GlToggleBlend(blend: true);
		float div = (entity.Collided ? 4f : 1.5f);
		IStandardShaderProgram standardShaderProgram = rapi.PreparedStandardShader((int)entity.Pos.X, (int)(entity.Pos.Y + 0.2), (int)entity.Pos.Z);
		Vec3d camPos = capi.World.Player.Entity.CameraPos;
		standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate(curPos.X - camPos.X + (double)(GameMath.Sin((float)capi.InWorldEllapsedMilliseconds / 120f + 30f) / 20f / div), curPos.Y - camPos.Y, curPos.Z - camPos.Z + (double)(GameMath.Cos((float)capi.InWorldEllapsedMilliseconds / 110f + 20f) / 20f / div)).RotateX((float)(Math.Sin(rotaccum * 10.0) / 10.0 / (double)div))
			.RotateZ((float)(Math.Cos(10.0 + rotaccum * 9.0) / 10.0 / (double)div))
			.Values;
		standardShaderProgram.ViewMatrix = rapi.CameraMatrixOriginf;
		standardShaderProgram.ProjectionMatrix = rapi.CurrentProjectionMatrix;
		rapi.RenderMultiTextureMesh(meshRef, "tex");
		standardShaderProgram.Stop();
	}

	public override void Dispose()
	{
		meshRef?.Dispose();
	}
}
