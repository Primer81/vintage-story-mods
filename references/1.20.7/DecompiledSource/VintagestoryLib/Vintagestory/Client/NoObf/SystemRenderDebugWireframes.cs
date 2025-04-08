using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemRenderDebugWireframes : ClientSystem
{
	private WireframeCube chunkWf;

	private WireframeCube entityWf;

	private WireframeCube beWf;

	private WireframeModes wfmodes => game.api.renderapi.WireframeDebugRender;

	public override string Name => "debwf";

	public SystemRenderDebugWireframes(ClientMain game)
		: base(game)
	{
		chunkWf = WireframeCube.CreateCenterOriginCube(game.api);
		entityWf = WireframeCube.CreateCenterOriginCube(game.api, -1);
		beWf = WireframeCube.CreateCenterOriginCube(game.api, -939523896);
		game.eventManager.RegisterRenderer(OnRenderFrame3D, EnumRenderStage.Opaque, Name, 0.5);
	}

	public override void Dispose(ClientMain game)
	{
		chunkWf.Dispose();
		entityWf.Dispose();
		beWf.Dispose();
	}

	public void OnRenderFrame3D(float deltaTime)
	{
		int plrDim = game.EntityPlayer.Pos.Dimension;
		if (wfmodes.Entity)
		{
			foreach (Entity entity in game.LoadedEntities.Values)
			{
				if (entity.Pos.Dimension == plrDim && entity.SelectionBox != null)
				{
					float scaleX = entity.SelectionBox.XSize / 2f;
					float scaleY = entity.SelectionBox.YSize / 2f;
					float scaleZ = entity.SelectionBox.ZSize / 2f;
					double x = entity.Pos.X + (double)entity.SelectionBox.X1 + (double)scaleX;
					double y = entity.Pos.InternalY + (double)entity.SelectionBox.Y1 + (double)scaleY;
					double z = entity.Pos.Z + (double)entity.SelectionBox.Z1 + (double)scaleZ;
					float lineWidth = ((game.EntitySelection != null && entity.EntityId == game.EntitySelection.Entity.EntityId) ? 3f : 1f);
					entityWf.Render(game.api, x, y, z, scaleX, scaleY, scaleZ, lineWidth, new Vec4f(0f, 0f, 1f, 1f));
					float selScaleX = entity.SelectionBox.XSize / 2f;
					float selScaleY = entity.SelectionBox.YSize / 2f;
					float selScaleZ = entity.SelectionBox.ZSize / 2f;
					if (selScaleX != scaleX || selScaleY != scaleY || selScaleZ != scaleZ)
					{
						x = entity.Pos.X + (double)entity.SelectionBox.X1 + (double)selScaleX;
						y = entity.Pos.InternalY + (double)entity.SelectionBox.Y1 + (double)selScaleY;
						z = entity.Pos.Z + (double)entity.SelectionBox.Z1 + (double)selScaleZ;
						entityWf.Render(game.api, x, y, z, selScaleX, selScaleY, selScaleZ, lineWidth, new Vec4f(0f, 0f, 1f, 1f));
					}
					float colScaleX = entity.CollisionBox.XSize / 2f;
					float colScaleY = entity.CollisionBox.YSize / 2f;
					float colScaleZ = entity.CollisionBox.ZSize / 2f;
					x = entity.Pos.X + (double)entity.CollisionBox.X1 + (double)colScaleX;
					y = entity.Pos.InternalY + (double)entity.CollisionBox.Y1 + (double)colScaleY;
					z = entity.Pos.Z + (double)entity.CollisionBox.Z1 + (double)colScaleZ;
					entityWf.Render(game.api, x, y, z, colScaleX, colScaleY, colScaleZ, lineWidth, new Vec4f(1f, 0f, 0f, 1f));
				}
			}
		}
		if (wfmodes.Chunk)
		{
			int chunksize2 = game.WorldMap.ClientChunkSize;
			BlockPos pos4 = game.EntityPlayer.Pos.AsBlockPos / chunksize2 * chunksize2 + chunksize2 / 2;
			chunkWf.Render(game.api, (float)pos4.X + 0.01f, (float)pos4.InternalY + 0.01f, (float)pos4.Z + 0.01f, chunksize2 / 2, chunksize2 / 2, chunksize2 / 2, 8f);
		}
		if (wfmodes.ServerChunk)
		{
			int chunksize = game.WorldMap.ServerChunkSize;
			BlockPos pos3 = game.EntityPlayer.Pos.AsBlockPos / chunksize * chunksize + chunksize / 2;
			chunkWf.Render(game.api, (float)pos3.X + 0.01f, (float)pos3.InternalY + 0.01f, (float)pos3.Z + 0.01f, chunksize / 2, chunksize / 2, chunksize / 2, 8f);
		}
		if (wfmodes.Region)
		{
			int regionSize3 = game.WorldMap.RegionSize;
			BlockPos pos2 = game.EntityPlayer.Pos.AsBlockPos / regionSize3 * regionSize3 + regionSize3 / 2;
			chunkWf.Render(game.api, (float)pos2.X + 0.01f, (float)pos2.InternalY + 0.01f, (float)pos2.Z + 0.01f, regionSize3 / 2, regionSize3 / 2, regionSize3 / 2, 16f);
		}
		if (wfmodes.LandClaim && game.WorldMap.LandClaims != null)
		{
			int regionSize2 = game.WorldMap.RegionSize;
			foreach (LandClaim claims in game.WorldMap.LandClaims)
			{
				if (claims.Areas.Count == 0 || claims.Areas[0].Center.X / regionSize2 != (int)game.EntityPlayer.Pos.X / regionSize2 || claims.Areas[0].Center.Z / regionSize2 != (int)game.EntityPlayer.Pos.Z / regionSize2)
				{
					continue;
				}
				Vec4f colorLandClaim = new Vec4f(1f, 1f, 0.5f, 1f);
				foreach (Cuboidi claim in claims.Areas)
				{
					entityWf.Render(game.api, (float)claim.X1 + (float)claim.SizeX / 2f, (float)claim.Y1 + (float)claim.SizeY / 2f, (float)claim.Z1 + (float)claim.SizeZ / 2f, (float)claim.SizeX / 2f, (float)claim.SizeY / 2f, (float)claim.SizeZ / 2f, 4f, colorLandClaim);
				}
			}
		}
		if (wfmodes.Structures)
		{
			int regionSize = game.WorldMap.RegionSize;
			int regionX = (int)(game.EntityPlayer.Pos.X / (double)regionSize);
			int regionZ = (int)(game.EntityPlayer.Pos.Z / (double)regionSize);
			IMapRegion region = game.WorldMap.GetMapRegion(regionX, regionZ);
			if (region != null)
			{
				Vec4f colorStruc = new Vec4f(1f, 0f, 1f, 1f);
				foreach (GeneratedStructure structure in region.GeneratedStructures)
				{
					entityWf.Render(game.api, (float)structure.Location.X1 + (float)structure.Location.SizeX / 2f, (float)structure.Location.Y1 + (float)structure.Location.SizeY / 2f, (float)structure.Location.Z1 + (float)structure.Location.SizeZ / 2f, (float)structure.Location.SizeX / 2f, (float)structure.Location.SizeY / 2f, (float)structure.Location.SizeZ / 2f, 2f, colorStruc);
				}
			}
		}
		if (wfmodes.BlockEntity)
		{
			BlockPos chunkpos = game.EntityPlayer.Pos.AsBlockPos / 32;
			int dimensionAdjust = chunkpos.dimension * 1024;
			for (int cx = -1; cx <= 1; cx++)
			{
				for (int cy = -1; cy <= 1; cy++)
				{
					for (int cz = -1; cz <= 1; cz++)
					{
						ClientChunk chunk = game.WorldMap.GetClientChunk(chunkpos.X + cx, chunkpos.Y + dimensionAdjust + cy, chunkpos.Z + cz);
						if (chunk == null)
						{
							continue;
						}
						foreach (KeyValuePair<BlockPos, BlockEntity> blockEntity in chunk.BlockEntities)
						{
							BlockPos bePos = blockEntity.Key;
							beWf.Render(game.api, (float)bePos.X + 0.5f, (float)bePos.InternalY + 0.5f, (float)bePos.Z + 0.5f, 0.5f, 0.5f, 0.5f, 1f);
						}
					}
				}
			}
		}
		if (wfmodes.Inside)
		{
			BlockPos tmpPos = new BlockPos();
			Block block = game.player.Entity.GetInsideTorsoBlockSoundSource(tmpPos);
			renderBoxes(block, tmpPos, new Vec4f(1f, 0.7f, 0.2f, 1f));
			block = game.player.Entity.GetInsideLegsBlockSoundSource(tmpPos);
			renderBoxes(block, tmpPos, new Vec4f(1f, 1f, 0f, 1f));
			EntityPos pos = game.player.Entity.SidedPos;
			block = game.player.Entity.GetNearestBlockSoundSource(tmpPos, -0.03, 4, usecollisionboxes: true);
			renderBoxes(block, tmpPos, new Vec4f(1f, 0f, 1f, 1f));
			tmpPos.Set((int)pos.X, (int)(pos.Y + 0.10000000149011612), (int)pos.Z);
			block = game.blockAccessor.GetBlock(tmpPos, 2);
			if (block.Id != 0)
			{
				renderBoxes(block, tmpPos, new Vec4f(0f, 1f, 1f, 1f));
			}
		}
	}

	private void renderBoxes(Block block, BlockPos tmpPos, Vec4f color)
	{
		if (block != null)
		{
			Cuboidf[] selectionBoxes = block.GetSelectionBoxes(game.blockAccessor, tmpPos);
			foreach (Cuboidf box in selectionBoxes)
			{
				entityWf.Render(game.api, (float)tmpPos.X + box.MidX, (float)tmpPos.Y + box.MidY, (float)tmpPos.Z + box.MidZ, box.XSize / 2f, box.YSize / 2f, box.ZSize / 2f, 1f, color);
			}
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
