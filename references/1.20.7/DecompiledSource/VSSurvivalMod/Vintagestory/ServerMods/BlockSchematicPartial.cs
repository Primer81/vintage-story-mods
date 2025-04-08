using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class BlockSchematicPartial : BlockSchematicStructure
{
	public List<Entity> EntitiesDecoded;

	private static BlockPos Zero = new BlockPos(0, 0, 0);

	public virtual int PlacePartial(IServerChunk[] chunks, IWorldGenBlockAccessor blockAccessor, IWorldAccessor worldForResolve, int chunkX, int chunkZ, BlockPos startPos, EnumReplaceMode mode, EnumStructurePlacement? structurePlacement, bool replaceMeta, bool resolveImports, Dictionary<int, Dictionary<int, int>> resolvedRockTypeRemaps = null, int[] replaceWithBlockLayersBlockids = null, Block rockBlock = null)
	{
		Unpack(worldForResolve.Api);
		Rectanglei rect = new Rectanglei(chunkX * 32, chunkZ * 32, 32, 32);
		if (!rect.IntersectsOrTouches(startPos.X, startPos.Z, startPos.X + SizeX, startPos.Z + SizeZ))
		{
			return 0;
		}
		int placed = 0;
		BlockPos curPos = new BlockPos();
		int climateUpLeft = 0;
		int climateUpRight = 0;
		int climateBotLeft = 0;
		int climateBotRight = 0;
		if (replaceWithBlockLayersBlockids != null)
		{
			int regionChunkSize = blockAccessor.RegionSize / 32;
			IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
			int rlX = chunkX % regionChunkSize;
			int rlZ = chunkZ % regionChunkSize;
			float facC = (float)climateMap.InnerSize / (float)regionChunkSize;
			climateUpLeft = climateMap.GetUnpaddedInt((int)((float)rlX * facC), (int)((float)rlZ * facC));
			climateUpRight = climateMap.GetUnpaddedInt((int)((float)rlX * facC + facC), (int)((float)rlZ * facC));
			climateBotLeft = climateMap.GetUnpaddedInt((int)((float)rlX * facC), (int)((float)rlZ * facC + facC));
			climateBotRight = climateMap.GetUnpaddedInt((int)((float)rlX * facC + facC), (int)((float)rlZ * facC + facC));
		}
		if (genBlockLayers == null)
		{
			genBlockLayers = worldForResolve.Api.ModLoader.GetModSystem<GenBlockLayers>();
		}
		int rockblockid = rockBlock?.BlockId ?? chunks[0].MapChunk.TopRockIdMap[495];
		int i = -1;
		foreach (uint index2 in Indices)
		{
			i++;
			int dx = (int)(index2 & 0x3FF);
			int posX2 = startPos.X + dx;
			int dz = (int)((index2 >> 10) & 0x3FF);
			int posZ2 = startPos.Z + dz;
			if (!rect.Contains(posX2, posZ2))
			{
				continue;
			}
			int dy = (int)((index2 >> 20) & 0x3FF);
			int posY2 = startPos.Y + dy;
			int storedBlockid = BlockIds[i];
			AssetLocation blockCode = BlockCodes[storedBlockid];
			Block newBlock = blockAccessor.GetBlock(blockCode);
			if (newBlock == null || (replaceMeta && (newBlock.Id == BlockSchematic.UndergroundBlockId || newBlock.Id == BlockSchematic.AbovegroundBlockId)))
			{
				continue;
			}
			int blockId = ((replaceMeta && IsFillerOrPath(newBlock)) ? empty : newBlock.BlockId);
			IChunkBlocks chunkData = chunks[posY2 / 32].Data;
			int posIndex = (posY2 % 32 * 32 + posZ2 % 32) * 32 + posX2 % 32;
			if (structurePlacement.HasValue && structurePlacement.GetValueOrDefault() == EnumStructurePlacement.SurfaceRuin && newBlock.Id == BlockSchematic.FillerBlockId)
			{
				uint belowIndex = (uint)((dy - 1 << 20) | (dz << 10) | dx);
				if (!Indices.Contains(belowIndex))
				{
					int belowPos = ((posY2 - 1) % 32 * 32 + posZ2 % 32) * 32 + posX2 % 32;
					Block belowBlock = blockAccessor.GetBlock(chunkData[belowPos]);
					if (belowBlock.BlockMaterial == EnumBlockMaterial.Soil)
					{
						int belowBlockId = GetReplaceLayerBlockId(blockAccessor, worldForResolve, replaceWithBlockLayersBlockids, belowBlock.Id, curPos, posX2, posY2, posZ2, 32, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight, dy, dx, dz, rockblockid, ref belowBlock, topBlockOnly: true);
						chunkData[belowPos] = belowBlockId;
					}
				}
			}
			blockId = GetReplaceLayerBlockId(blockAccessor, worldForResolve, replaceWithBlockLayersBlockids, blockId, curPos, posX2, posY2, posZ2, 32, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight, dy, dx, dz, rockblockid, ref newBlock);
			blockId = GetRocktypeBlockId(blockAccessor, resolvedRockTypeRemaps, blockId, rockblockid, ref newBlock);
			if (structurePlacement.HasValue && structurePlacement.GetValueOrDefault() == EnumStructurePlacement.Surface)
			{
				Block oldBlock = blockAccessor.GetBlock(chunkData[posIndex]);
				if ((newBlock.Replaceable >= 5500 || newBlock.BlockMaterial == EnumBlockMaterial.Plant) && oldBlock.Replaceable < newBlock.Replaceable && !newBlock.IsLiquid())
				{
					continue;
				}
			}
			if (newBlock.ForFluidsLayer && index2 != Indices[i - 1])
			{
				chunkData[posIndex] = 0;
			}
			if (newBlock.ForFluidsLayer)
			{
				chunkData.SetFluid(posIndex, blockId);
			}
			else
			{
				chunkData.SetFluid(posIndex, 0);
				chunkData[posIndex] = blockId;
			}
			if (newBlock.LightHsv[2] > 0)
			{
				curPos.Set(posX2, posY2, posZ2);
				blockAccessor.ScheduleBlockLightUpdate(curPos, 0, newBlock.BlockId);
			}
			placed++;
		}
		PlaceDecors(blockAccessor, startPos, rect);
		int schematicSeed = worldForResolve.Rand.Next();
		foreach (KeyValuePair<uint, string> val in BlockEntities)
		{
			uint index = val.Key;
			int posX = startPos.X + (int)(index & 0x3FF);
			int posZ = startPos.Z + (int)((index >> 10) & 0x3FF);
			if (!rect.Contains(posX, posZ))
			{
				continue;
			}
			int posY = startPos.Y + (int)((index >> 20) & 0x3FF);
			curPos.Set(posX, posY, posZ);
			BlockEntity be = blockAccessor.GetBlockEntity(curPos);
			if (be == null && blockAccessor != null)
			{
				Block block2 = blockAccessor.GetBlock(curPos, 1);
				if (block2.EntityClass != null)
				{
					blockAccessor.SpawnBlockEntity(block2.EntityClass, curPos);
					be = blockAccessor.GetBlockEntity(curPos);
				}
			}
			if (be != null)
			{
				Block block = blockAccessor.GetBlock(curPos, 1);
				if (block.EntityClass != worldForResolve.ClassRegistry.GetBlockEntityClass(be.GetType()))
				{
					worldForResolve.Logger.Warning("Could not import block entity data for schematic at {0}. There is already {1}, expected {2}. Probably overlapping ruins.", curPos, be.GetType(), block.EntityClass);
					continue;
				}
				ITreeAttribute tree = DecodeBlockEntityData(val.Value);
				tree.SetInt("posx", curPos.X);
				tree.SetInt("posy", curPos.Y);
				tree.SetInt("posz", curPos.Z);
				int climate = GameMath.BiLerpRgbColor(GameMath.Clamp((float)(posX % 32) / 32f, 0f, 1f), GameMath.Clamp((float)(posZ % 32) / 32f, 0f, 1f), climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
				Block layerBlock = GetBlockLayerBlock((climate >> 8) & 0xFF, (climate >> 16) & 0xFF, curPos.Y, rockblockid, 0, null, worldForResolve.Blocks, curPos, -1);
				be.FromTreeAttributes(tree, worldForResolve);
				be.OnLoadCollectibleMappings(worldForResolve, BlockCodes, ItemCodes, schematicSeed, resolveImports);
				be.OnPlacementBySchematic(worldForResolve.Api as ICoreServerAPI, blockAccessor, curPos, resolvedRockTypeRemaps, rockblockid, layerBlock, resolveImports);
			}
		}
		if (EntitiesDecoded == null)
		{
			DecodeEntities(worldForResolve, startPos, worldForResolve as IServerWorldAccessor);
		}
		foreach (Entity entity in EntitiesDecoded)
		{
			if (!rect.Contains((int)entity.Pos.X, (int)entity.Pos.Z))
			{
				continue;
			}
			if (OriginalPos != null)
			{
				BlockPos prevOffset = entity.WatchedAttributes.GetBlockPos("importOffset", Zero);
				entity.WatchedAttributes.SetBlockPos("importOffset", startPos - OriginalPos + prevOffset);
			}
			if (blockAccessor != null)
			{
				blockAccessor.AddEntity(entity);
				entity.OnInitialized += delegate
				{
					entity.OnLoadCollectibleMappings(worldForResolve, BlockCodes, ItemCodes, schematicSeed, resolveImports);
				};
			}
			else
			{
				worldForResolve.SpawnEntity(entity);
				entity.OnLoadCollectibleMappings(worldForResolve, BlockCodes, ItemCodes, schematicSeed, resolveImports);
			}
		}
		return placed;
	}

	private int GetReplaceLayerBlockId(IWorldGenBlockAccessor blockAccessor, IWorldAccessor worldForResolve, int[] replaceWithBlockLayersBlockids, int blockId, BlockPos curPos, int posX, int posY, int posZ, int chunksize, int climateUpLeft, int climateUpRight, int climateBotLeft, int climateBotRight, int dy, int dx, int dz, int rockblockid, ref Block newBlock, bool topBlockOnly = false)
	{
		if (replaceWithBlockLayersBlockids != null && replaceWithBlockLayersBlockids.Contains(blockId))
		{
			curPos.Set(posX, posY, posZ);
			int climate = GameMath.BiLerpRgbColor(GameMath.Clamp((float)(posX % chunksize) / (float)chunksize, 0f, 1f), GameMath.Clamp((float)(posZ % chunksize) / (float)chunksize, 0f, 1f), climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
			int depth = 0;
			if (dy + 1 < SizeY && !topBlockOnly)
			{
				Block aboveBlock = blocksByPos[dx, dy + 1, dz];
				if (aboveBlock != null && aboveBlock.SideSolid[BlockFacing.DOWN.Index] && aboveBlock.BlockMaterial != EnumBlockMaterial.Wood && aboveBlock.BlockMaterial != EnumBlockMaterial.Snow && aboveBlock.BlockMaterial != EnumBlockMaterial.Ice)
				{
					depth = 1;
				}
			}
			blockId = GetBlockLayerBlock((climate >> 8) & 0xFF, (climate >> 16) & 0xFF, curPos.Y - 1, rockblockid, depth, null, worldForResolve.Blocks, curPos, -1)?.Id ?? rockblockid;
			newBlock = blockAccessor.GetBlock(blockId);
		}
		return blockId;
	}

	private static int GetRocktypeBlockId(IWorldGenBlockAccessor blockAccessor, Dictionary<int, Dictionary<int, int>> resolvedRockTypeRemaps, int blockId, int rockblockid, ref Block newBlock)
	{
		if (resolvedRockTypeRemaps != null && resolvedRockTypeRemaps.TryGetValue(blockId, out var replaceByBlock) && replaceByBlock.TryGetValue(rockblockid, out var newBlockId))
		{
			blockId = newBlockId;
			newBlock = blockAccessor.GetBlock(blockId);
		}
		return blockId;
	}

	private void DecodeEntities(IWorldAccessor worldForResolve, BlockPos startPos, IServerWorldAccessor serverWorldAccessor)
	{
		EntitiesDecoded = new List<Entity>(Entities.Count);
		foreach (string entity2 in Entities)
		{
			using MemoryStream ms = new MemoryStream(Ascii85.Decode(entity2));
			BinaryReader reader = new BinaryReader(ms);
			string className = reader.ReadString();
			Entity entity = worldForResolve.ClassRegistry.CreateEntity(className);
			entity.Api = worldForResolve.Api;
			entity.FromBytes(reader, isSync: false, serverWorldAccessor.RemappedEntities);
			entity.DidImportOrExport(startPos);
			EntitiesDecoded.Add(entity);
		}
	}

	public override BlockSchematic ClonePacked()
	{
		return new BlockSchematicPartial
		{
			SizeX = SizeX,
			SizeY = SizeY,
			SizeZ = SizeZ,
			OffsetY = base.OffsetY,
			GameVersion = GameVersion,
			FromFileName = FromFileName,
			BlockCodes = new Dictionary<int, AssetLocation>(BlockCodes),
			ItemCodes = new Dictionary<int, AssetLocation>(ItemCodes),
			Indices = new List<uint>(Indices),
			BlockIds = new List<int>(BlockIds),
			BlockEntities = new Dictionary<uint, string>(BlockEntities),
			Entities = new List<string>(Entities),
			DecorIndices = new List<uint>(DecorIndices),
			DecorIds = new List<long>(DecorIds),
			ReplaceMode = ReplaceMode,
			EntranceRotation = EntranceRotation,
			OriginalPos = OriginalPos
		};
	}
}
