using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common.Database;
using Vintagestory.Server;

namespace Vintagestory.Common;

public abstract class WorldMap
{
	public const int chunksize = 32;

	public int index3dMulX;

	public int chunkMapSizeY;

	public int index3dMulZ;

	public float[] BlockLightLevels;

	public byte[] BlockLightLevelsByte;

	public byte[] hueLevels;

	public byte[] satLevels;

	public float[] SunLightLevels;

	public byte[] SunLightLevelsByte;

	public int SunBrightness;

	public Dictionary<long, List<LandClaim>> LandClaimByRegion = new Dictionary<long, List<LandClaim>>();

	public abstract IWorldAccessor World { get; }

	public abstract ILogger Logger { get; }

	public abstract IList<Block> Blocks { get; }

	public abstract Dictionary<AssetLocation, Block> BlocksByCode { get; }

	public abstract int MapSizeX { get; }

	public abstract int MapSizeY { get; }

	public abstract int MapSizeZ { get; }

	public abstract int RegionMapSizeX { get; }

	public abstract int RegionMapSizeY { get; }

	public abstract int RegionMapSizeZ { get; }

	public abstract int ChunkSize { get; }

	public abstract int ChunkSizeMask { get; }

	public abstract Vec3i MapSize { get; }

	public abstract int RegionSize { get; }

	public abstract List<LandClaim> All { get; }

	public abstract bool DebugClaimPrivileges { get; }

	public int ChunkMapSizeX => MapSizeX / 32;

	public int ChunkMapSizeY => chunkMapSizeY;

	public int ChunkMapSizeZ => MapSizeZ / 32;

	public int GetLightRGBsAsInt(int posX, int posY, int posZ)
	{
		int cx = posX / 32;
		int cy = posY / 32;
		int cz = posZ / 32;
		if (!IsValidPos(posX, posY, posZ))
		{
			return ColorUtil.HsvToRgba(0, 0, 0, (int)(SunLightLevels[SunBrightness] * 255f));
		}
		IWorldChunk chunk = GetChunk(cx, cy, cz);
		if (chunk == null)
		{
			return ColorUtil.HsvToRgba(0, 0, 0, (int)(SunLightLevels[SunBrightness] * 255f));
		}
		int index3d = MapUtil.Index3d(posX & ChunkSizeMask, posY & ChunkSizeMask, posZ & ChunkSizeMask, 32, 32);
		int blocksat;
		ushort num = chunk.Unpack_AndReadLight(index3d, out blocksat);
		int sunl = num & 0x1F;
		int blockl = (num >> 5) & 0x1F;
		int blockhue = num >> 10;
		int sunb = (int)(SunLightLevels[sunl] * 255f);
		byte h = hueLevels[blockhue];
		int blocks = satLevels[blocksat];
		int blockb = (int)(BlockLightLevels[blockl] * 255f);
		return ColorUtil.HsvToRgba(h, blocks, blockb, sunb);
	}

	public Vec4f GetLightRGBSVec4f(int posX, int posY, int posZ)
	{
		int levels = LoadLightHSVLevels(posX, posY, posZ);
		byte h = hueLevels[(levels >> 16) & 0xFF];
		int blocks = satLevels[(levels >> 24) & 0xFF];
		int blockb = (int)(BlockLightLevels[(levels >> 8) & 0xFF] * 255f);
		int rgb = ColorUtil.HsvToRgb(h, blocks, blockb);
		return new Vec4f((float)(rgb >> 16) / 255f, (float)((rgb >> 8) & 0xFF) / 255f, (float)(rgb & 0xFF) / 255f, SunLightLevels[levels & 0xFF]);
	}

	public int[] GetLightHSVLevels(int posX, int posY, int posZ)
	{
		int[] array = new int[4];
		int levels = LoadLightHSVLevels(posX, posY, posZ);
		array[0] = levels & 0xFF;
		array[1] = (levels >> 8) & 0xFF;
		array[2] = (levels >> 16) & 0xFF;
		array[3] = (levels >> 24) & 0xFF;
		return array;
	}

	public int LoadLightHSVLevels(int posX, int posY, int posZ)
	{
		int cx = posX / 32;
		int cy = posY / 32;
		int cz = posZ / 32;
		if (!IsValidPos(posX, posY, posZ))
		{
			return SunBrightness;
		}
		IWorldChunk chunk = GetChunk(cx, cy, cz);
		if (chunk == null)
		{
			return SunBrightness;
		}
		int index3d = MapUtil.Index3d(posX & ChunkSizeMask, posY & ChunkSizeMask, posZ & ChunkSizeMask, 32, 32);
		int blocksat;
		int light = chunk.Unpack_AndReadLight(index3d, out blocksat);
		return (light & 0x1F) | ((light & 0x3E0) << 3) | ((light & 0xFC00) << 6) | (blocksat << 24);
	}

	public LandClaim[] Get(BlockPos pos)
	{
		List<LandClaim> claims = new List<LandClaim>();
		long regionindex2d = MapRegionIndex2D(pos.X / RegionSize, pos.Z / RegionSize);
		if (!LandClaimByRegion.ContainsKey(regionindex2d))
		{
			return null;
		}
		foreach (LandClaim area in LandClaimByRegion[regionindex2d])
		{
			if (area.PositionInside(pos))
			{
				claims.Add(area);
			}
		}
		return claims.ToArray();
	}

	public bool TryAccess(IPlayer player, BlockPos pos, EnumBlockAccessFlags accessFlag)
	{
		string claimant;
		EnumWorldAccessResponse resp = TestBlockAccess(player, new BlockSelection
		{
			Position = pos
		}, accessFlag, out claimant);
		if (resp == EnumWorldAccessResponse.Granted)
		{
			return true;
		}
		if (player != null)
		{
			string code = "noprivilege-" + ((accessFlag == EnumBlockAccessFlags.Use) ? "use" : "buildbreak") + "-" + resp.ToString().ToLowerInvariant();
			string param = claimant;
			if (claimant.StartsWithOrdinal("custommessage-"))
			{
				code = "noprivilege-buildbreak-" + claimant.Substring("custommessage-".Length);
			}
			if (World.Side == EnumAppSide.Server)
			{
				(player as IServerPlayer).SendIngameError(code, null, param);
			}
			else
			{
				(World as ClientMain).api.TriggerIngameError(this, code, Lang.Get("ingameerror-" + code, claimant));
			}
			player?.InventoryManager.ActiveHotbarSlot?.MarkDirty();
			World.BlockAccessor.MarkBlockEntityDirty(pos);
			World.BlockAccessor.MarkBlockDirty(pos);
		}
		return false;
	}

	public EnumWorldAccessResponse TestAccess(IPlayer player, BlockPos pos, EnumBlockAccessFlags accessFlag)
	{
		string claimant;
		return TestBlockAccess(player, new BlockSelection
		{
			Position = pos
		}, accessFlag, out claimant);
	}

	public EnumWorldAccessResponse TestBlockAccess(IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType)
	{
		string claimant;
		return TestBlockAccess(player, blockSel, accessType, out claimant);
	}

	public EnumWorldAccessResponse TestBlockAccess(IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, out string claimant)
	{
		EnumWorldAccessResponse resp = testBlockAccessInternal(player, blockSel, accessType, out claimant);
		if (World.Side == EnumAppSide.Client)
		{
			return (World.Api.Event as ClientEventAPI).TriggerTestBlockAccess(player, blockSel, accessType, ref claimant, resp);
		}
		return (World.Api.Event as ServerEventAPI).TriggerTestBlockAccess(player, blockSel, accessType, ref claimant, resp);
	}

	private EnumWorldAccessResponse testBlockAccessInternal(IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, out string claimant)
	{
		EnumWorldAccessResponse resp = testBlockAccess(player, accessType, out claimant);
		if (resp != 0)
		{
			return resp;
		}
		bool canUseClaimed = player.HasPrivilege(Privilege.useblockseverywhere) && player.WorldData.CurrentGameMode == EnumGameMode.Creative;
		bool canBreakClaimed = player.HasPrivilege(Privilege.buildblockseverywhere) && player.WorldData.CurrentGameMode == EnumGameMode.Creative;
		if (DebugClaimPrivileges)
		{
			Logger.VerboseDebug("Privdebug: type: {3}, player: {0}, canUseClaimed: {1}, canBreakClaimed: {2}", player?.PlayerName, canUseClaimed, canBreakClaimed, accessType);
		}
		ServerMain server = World as ServerMain;
		if (accessType == EnumBlockAccessFlags.Use)
		{
			if (!canUseClaimed && (claimant = GetBlockingLandClaimant(player, blockSel.Position, EnumBlockAccessFlags.Use)) != null)
			{
				return EnumWorldAccessResponse.LandClaimed;
			}
			if (server != null && !server.EventManager.TriggerCanUse(player as IServerPlayer, blockSel))
			{
				return EnumWorldAccessResponse.DeniedByMod;
			}
			return EnumWorldAccessResponse.Granted;
		}
		if (!canBreakClaimed && (claimant = GetBlockingLandClaimant(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak)) != null)
		{
			return EnumWorldAccessResponse.LandClaimed;
		}
		if (server != null && !server.EventManager.TriggerCanPlaceOrBreak(player as IServerPlayer, blockSel, out claimant))
		{
			return EnumWorldAccessResponse.DeniedByMod;
		}
		return EnumWorldAccessResponse.Granted;
	}

	private EnumWorldAccessResponse testBlockAccess(IPlayer player, EnumBlockAccessFlags accessType, out string claimant)
	{
		if (player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
		{
			claimant = "custommessage-inspectatormode";
			return EnumWorldAccessResponse.InSpectatorMode;
		}
		if (!player.Entity.Alive)
		{
			claimant = "custommessage-dead";
			return EnumWorldAccessResponse.PlayerDead;
		}
		if (accessType == EnumBlockAccessFlags.BuildOrBreak)
		{
			if (player.WorldData.CurrentGameMode == EnumGameMode.Guest)
			{
				claimant = "custommessage-inguestmode";
				return EnumWorldAccessResponse.InGuestMode;
			}
			if (!player.HasPrivilege(Privilege.buildblocks))
			{
				claimant = "custommessage-nobuildprivilege";
				return EnumWorldAccessResponse.NoPrivilege;
			}
			claimant = null;
			return EnumWorldAccessResponse.Granted;
		}
		if (!player.HasPrivilege(Privilege.useblock))
		{
			claimant = "custommessage-nouseprivilege";
			return EnumWorldAccessResponse.NoPrivilege;
		}
		claimant = null;
		return EnumWorldAccessResponse.Granted;
	}

	public string GetBlockingLandClaimant(IPlayer forPlayer, BlockPos pos, EnumBlockAccessFlags accessFlag)
	{
		long regionindex2d = MapRegionIndex2D(pos.X / RegionSize, pos.Z / RegionSize);
		if (!LandClaimByRegion.ContainsKey(regionindex2d))
		{
			if (DebugClaimPrivileges)
			{
				Logger.VerboseDebug("Privdebug: No land claim in this region. Pos: {0}/{1}", pos.X, pos.Z);
			}
			return null;
		}
		if (DebugClaimPrivileges && LandClaimByRegion[regionindex2d].Count == 0)
		{
			Logger.VerboseDebug("Privdebug: Land claim list in this region is empty. Pos: {0}/{1}", pos.X, pos.Z);
		}
		foreach (LandClaim claim in LandClaimByRegion[regionindex2d])
		{
			if (DebugClaimPrivileges)
			{
				Logger.VerboseDebug("Privdebug: posinside: {0}, claim owned by: {3}, forplayer: {1}, canaccess: {2}", claim.PositionInside(pos), forPlayer?.PlayerName, (forPlayer != null) ? claim.TestPlayerAccess(forPlayer, accessFlag) : EnumPlayerAccessResult.Denied, claim.LastKnownOwnerName);
			}
			if (claim.PositionInside(pos) && (forPlayer == null || claim.TestPlayerAccess(forPlayer, accessFlag) == EnumPlayerAccessResult.Denied) && (!claim.AllowUseEveryone || accessFlag != EnumBlockAccessFlags.Use))
			{
				return claim.LastKnownOwnerName;
			}
		}
		if (forPlayer != null && forPlayer.Role.PrivilegeLevel >= 0)
		{
			return null;
		}
		return "Server";
	}

	public void RebuildLandClaimPartitions()
	{
		if (RegionSize == 0)
		{
			Logger.Warning("Call to RebuildLandClaimPartitions, but RegionSize is 0. Wrong startup sequence? Will ignore for now.");
			return;
		}
		HashSet<long> regions = new HashSet<long>();
		LandClaimByRegion.Clear();
		foreach (LandClaim claim in All)
		{
			regions.Clear();
			foreach (Cuboidi area in claim.Areas)
			{
				int minx = area.MinX / RegionSize;
				int maxx = area.MaxX / RegionSize;
				int minz = area.MinZ / RegionSize;
				int maxz = area.MaxZ / RegionSize;
				for (int x = minx; x <= maxx; x++)
				{
					for (int z = minz; z <= maxz; z++)
					{
						regions.Add(MapRegionIndex2D(x, z));
					}
				}
			}
			foreach (long index2d in regions)
			{
				if (!LandClaimByRegion.TryGetValue(index2d, out var claims))
				{
					claims = (LandClaimByRegion[index2d] = new List<LandClaim>());
				}
				claims.Add(claim);
			}
		}
	}

	public long MapRegionIndex2D(int regionX, int regionZ)
	{
		return (long)regionZ * (long)RegionMapSizeX + regionX;
	}

	[Obsolete("Use dimension aware versions instead, or else the BlockPos or EntityPos overloads")]
	public long ChunkIndex3D(int chunkX, int chunkY, int chunkZ)
	{
		return ((long)chunkY * (long)index3dMulZ + chunkZ) * index3dMulX + chunkX;
	}

	public long ChunkIndex3D(int chunkX, int chunkY, int chunkZ, int dim)
	{
		return ((long)(chunkY + dim * 1024) * (long)index3dMulZ + chunkZ) * index3dMulX + chunkX;
	}

	public long ChunkIndex3D(EntityPos pos)
	{
		ChunkPos cpos = ChunkPos.FromPosition((int)pos.X, (int)pos.Y, (int)pos.Z, pos.Dimension);
		return ChunkIndex3D(cpos);
	}

	public long ChunkIndex3D(ChunkPos cpos)
	{
		return ((long)(cpos.Y + cpos.Dimension * 1024) * (long)index3dMulZ + cpos.Z) * index3dMulX + cpos.X;
	}

	public long MapChunkIndex2D(int chunkX, int chunkZ)
	{
		return (long)chunkZ * (long)ChunkMapSizeX + chunkX;
	}

	public ChunkPos ChunkPosFromChunkIndex3D(long chunkIndex3d)
	{
		int internalCY = (int)(chunkIndex3d / ((long)index3dMulX * (long)index3dMulZ));
		return new ChunkPos((int)(chunkIndex3d % index3dMulX), internalCY % 1024, (int)(chunkIndex3d / index3dMulX % index3dMulZ), internalCY / 1024);
	}

	public ChunkPos ChunkPosFromChunkIndex2D(long index2d)
	{
		return new ChunkPos((int)(index2d % ChunkMapSizeX), 0, (int)(index2d / ChunkMapSizeX), 0);
	}

	public int ChunkSizedIndex3D(int lX, int lY, int lZ)
	{
		return (lY * 32 + lZ) * 32 + lX;
	}

	public bool IsValidPos(BlockPos pos)
	{
		if ((pos.X | pos.Y | pos.Z) >= 0)
		{
			if (pos.X >= MapSizeX || pos.Z >= MapSizeZ)
			{
				return pos.InternalY >= 32768;
			}
			return true;
		}
		return false;
	}

	public bool IsValidPos(int posX, int posY, int posZ)
	{
		if ((posX | posY | posZ) >= 0)
		{
			if (posX >= MapSizeX || posZ >= MapSizeZ)
			{
				return posY >= 32768;
			}
			return true;
		}
		return false;
	}

	public bool IsValidChunkPos(int chunkX, int chunkY, int chunkZ)
	{
		if ((chunkX | chunkY | chunkZ) >= 0)
		{
			if (chunkX >= ChunkMapSizeX || chunkZ >= ChunkMapSizeZ)
			{
				return chunkY >= 1024;
			}
			return true;
		}
		return false;
	}

	public abstract void MarkChunkDirty(int chunkX, int chunkY, int chunkZ, bool priority = false, bool sunRelight = false, Action OnRetesselated = null, bool fireDirtyEvent = true, bool edgeOnly = false);

	public abstract void TriggerNeighbourBlockUpdate(BlockPos pos);

	public abstract void MarkBlockModified(BlockPos pos, bool doRelight = true);

	public abstract void MarkBlockDirty(BlockPos pos, Action OnRetesselated);

	public abstract void MarkBlockDirty(BlockPos pos, IPlayer skipPlayer = null);

	public abstract void MarkBlockEntityDirty(BlockPos pos);

	public bool IsMovementRestrictedPos(double posX, double posY, double posZ, int dimension)
	{
		if (posX < 0.0 || posZ < 0.0 || posX >= (double)MapSizeX || posZ >= (double)MapSizeZ)
		{
			return World.Config.GetString("worldEdge") == "blocked";
		}
		if (posY >= 0.0 && posY < (double)MapSizeY)
		{
			return GetChunkAtPos((int)posX, (int)posY + dimension * 32768, (int)posZ) == null;
		}
		return false;
	}

	internal bool IsPosLoaded(BlockPos pos)
	{
		return GetChunkAtPos(pos.X, pos.Y, pos.Z) != null;
	}

	internal bool AnyLoadedChunkInMapRegion(int chunkx, int chunkz)
	{
		int cwdt = RegionSize / 32;
		for (int cdx = -1; cdx < cwdt + 1; cdx++)
		{
			for (int cdz = -1; cdz < cwdt + 1; cdz++)
			{
				if (IsValidChunkPos(chunkx + cdx, 0, chunkz + cdz) && GetMapChunk(chunkx + cdx, chunkz + cdz) != null)
				{
					return true;
				}
			}
		}
		return false;
	}

	public abstract IWorldChunk GetChunk(long chunkIndex3D);

	public abstract IWorldChunk GetChunkNonLocking(int chunkX, int chunkY, int chunkZ);

	public abstract IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ);

	public abstract IMapRegion GetMapRegion(int regionX, int regionZ);

	public abstract IMapChunk GetMapChunk(int chunkX, int chunkZ);

	public abstract IWorldChunk GetChunkAtPos(int posX, int posY, int posZ);

	public abstract WorldChunk GetChunk(BlockPos pos);

	public abstract void MarkDecorsDirty(BlockPos pos);

	public virtual void PrintChunkMap(Vec2i markChunkPos = null)
	{
	}

	public abstract void SendSetBlock(int blockId, int posX, int posY, int posZ);

	public abstract void SendExchangeBlock(int blockId, int posX, int posY, int posZ);

	public abstract void UpdateLighting(int oldblockid, int newblockid, BlockPos pos);

	public abstract void RemoveBlockLight(byte[] oldLightHsV, BlockPos pos);

	public abstract void UpdateLightingAfterAbsorptionChange(int oldAbsorption, int newAbsorption, BlockPos pos);

	public abstract void SendBlockUpdateBulk(IEnumerable<BlockPos> blockUpdates, bool doRelight);

	public abstract void SendBlockUpdateBulkMinimal(Dictionary<BlockPos, BlockUpdate> blockUpdates);

	public abstract void UpdateLightingBulk(Dictionary<BlockPos, BlockUpdate> blockUpdates);

	public abstract void SpawnBlockEntity(string classname, BlockPos position, ItemStack byItemStack = null);

	public abstract void SpawnBlockEntity(BlockEntity be);

	public abstract void RemoveBlockEntity(BlockPos position);

	public abstract BlockEntity GetBlockEntity(BlockPos position);

	public abstract ClimateCondition GetClimateAt(BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.WorldGenValues, double totalDays = 0.0);

	public abstract ClimateCondition GetClimateAt(BlockPos pos, ClimateCondition baseClimate, EnumGetClimateMode mode, double totalDays);

	public abstract ClimateCondition GetClimateAt(BlockPos pos, int climate);

	public abstract Vec3d GetWindSpeedAt(BlockPos pos);

	public abstract Vec3d GetWindSpeedAt(Vec3d pos);

	public abstract void DamageBlock(BlockPos pos, BlockFacing facing, float damage, IPlayer dualCallByPlayer = null);

	public abstract void SendDecorUpdateBulk(IEnumerable<BlockPos> updatedDecorPositions);
}
