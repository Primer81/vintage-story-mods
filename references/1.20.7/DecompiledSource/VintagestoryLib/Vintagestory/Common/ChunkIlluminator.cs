using System;
using System.Collections.Generic;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Common;

public class ChunkIlluminator
{
	private ushort defaultSunLight;

	private const int MAXLIGHTSPREAD = 31;

	private const int VISITED_WIDTH = 63;

	private int mapsizex;

	private int mapsizey;

	private int mapsizez;

	private int XPlus = 1;

	private int YPlus;

	private int ZPlus;

	private IList<Block> blockTypes;

	private int chunkSize;

	internal IChunkProvider chunkProvider;

	private IBlockAccessor readBlockAccess;

	private Dictionary<Vec3i, LightSourcesAtBlock> VisitedNodes = new Dictionary<Vec3i, LightSourcesAtBlock>();

	private List<NearbyLightSource> nearbyLightSources = new List<NearbyLightSource>();

	private BlockPos tmpDiPos = new BlockPos();

	private BlockPos tmpPos = new BlockPos();

	private BlockPos tmpPosDimensionAware = new BlockPos();

	private int[] currentVisited;

	private int iteration;

	public bool IsValidPos(int x, int y, int z)
	{
		if (x >= 0 && y >= 0 && z >= 0 && x < mapsizex && y <= mapsizey)
		{
			return z <= mapsizez;
		}
		return false;
	}

	public ChunkIlluminator(IChunkProvider chunkProvider, IBlockAccessor readBlockAccess, int chunkSize)
	{
		this.readBlockAccess = readBlockAccess;
		this.chunkProvider = chunkProvider;
		this.chunkSize = chunkSize;
		YPlus = chunkSize * chunkSize;
		ZPlus = chunkSize;
		currentVisited = new int[250047];
	}

	public void InitForWorld(IList<Block> blockTypes, ushort defaultSunLight, int mapsizex, int mapsizey, int mapsizez)
	{
		this.blockTypes = blockTypes;
		this.defaultSunLight = defaultSunLight;
		this.mapsizex = mapsizex;
		this.mapsizey = mapsizey;
		this.mapsizez = mapsizez;
	}

	public void FullRelight(BlockPos minPos, BlockPos maxPos)
	{
		int chunkSize = this.chunkSize;
		Dictionary<Vec3i, IWorldChunk> chunks = new Dictionary<Vec3i, IWorldChunk>();
		int minx = GameMath.Clamp(Math.Min(minPos.X, maxPos.X) - chunkSize, 0, mapsizex - 1);
		int miny = GameMath.Clamp(Math.Min(minPos.Y, maxPos.Y) - chunkSize, 0, mapsizey - 1);
		int minz = GameMath.Clamp(Math.Min(minPos.Z, maxPos.Z) - chunkSize, 0, mapsizez - 1);
		int maxx = GameMath.Clamp(Math.Max(minPos.X, maxPos.X) + chunkSize, 0, mapsizex - 1);
		int maxy = GameMath.Clamp(Math.Max(minPos.Y, maxPos.Y) + chunkSize, 0, mapsizey - 1);
		int num = GameMath.Clamp(Math.Max(minPos.Z, maxPos.Z) + chunkSize, 0, mapsizez - 1);
		int mincx = minx / chunkSize;
		int mincy = miny / chunkSize;
		int mincz = minz / chunkSize;
		int maxcx = maxx / chunkSize;
		int maxcy = maxy / chunkSize;
		int maxcz = num / chunkSize;
		int dimensionOffsetY = minPos.dimension * 1024;
		for (int cx2 = mincx; cx2 <= maxcx; cx2++)
		{
			for (int cy2 = mincy; cy2 <= maxcy; cy2++)
			{
				for (int cz2 = mincz; cz2 <= maxcz; cz2++)
				{
					IWorldChunk chunk3 = chunkProvider.GetChunk(cx2, cy2 + dimensionOffsetY, cz2);
					if (chunk3 != null)
					{
						chunk3.Unpack();
						chunks[new Vec3i(cx2, cy2, cz2)] = chunk3;
					}
				}
			}
		}
		foreach (IWorldChunk value in chunks.Values)
		{
			value?.Lighting.ClearLight();
		}
		IWorldChunk[] chunkColumn = new IWorldChunk[mapsizey / chunkSize];
		for (int cx = mincx; cx <= maxcx; cx++)
		{
			for (int cz = mincz; cz <= maxcz; cz++)
			{
				bool anyNullChunks = false;
				for (int cy = 0; cy < chunkColumn.Length; cy++)
				{
					IWorldChunk chunk2 = chunkProvider.GetChunk(cx, cy + dimensionOffsetY, cz);
					if (chunk2 == null)
					{
						anyNullChunks = true;
					}
					chunkColumn[cy] = chunk2;
				}
				if (!anyNullChunks)
				{
					Sunlight(chunkColumn, cx, chunkColumn.Length - 1, cz, minPos.dimension);
					SunlightFlood(chunkColumn, cx, chunkColumn.Length - 1, cz);
					SunLightFloodNeighbourChunks(chunkColumn, cx, chunkColumn.Length - 1, cz, minPos.dimension);
				}
			}
		}
		Dictionary<BlockPos, Block> lightSources = new Dictionary<BlockPos, Block>();
		foreach (KeyValuePair<Vec3i, IWorldChunk> val2 in chunks)
		{
			Vec3i chunkPos = val2.Key;
			IWorldChunk chunk = val2.Value;
			if (chunk == null)
			{
				continue;
			}
			int posX = chunkPos.X * chunkSize;
			int posY = chunkPos.Y * chunkSize;
			int posZ = chunkPos.Z * chunkSize;
			foreach (int index3d in chunk.LightPositions)
			{
				int lposy = chunkPos.Y * chunkSize + index3d / (chunkSize * chunkSize);
				int lposz = chunkPos.Z * chunkSize + index3d / chunkSize % chunkSize;
				int lposx = chunkPos.X * chunkSize + index3d % chunkSize;
				lightSources[new BlockPos(posX + lposx, posY + lposy, posZ + lposz, minPos.dimension)] = blockTypes[chunk.Data[index3d]];
			}
		}
		foreach (KeyValuePair<BlockPos, Block> val in lightSources)
		{
			byte[] lightHsv = val.Value.GetLightHsv(readBlockAccess, val.Key);
			PlaceBlockLight(lightHsv, val.Key.X, val.Key.InternalY, val.Key.Z);
		}
	}

	public void Sunlight(IWorldChunk[] chunks, int chunkX, int chunkY, int chunkZ, int dim)
	{
		tmpPosDimensionAware.dimension = dim;
		int chunkSize = this.chunkSize;
		if (chunkY != chunks.Length - 1)
		{
			chunks[chunkY + 1].Unpack();
		}
		for (int cy2 = chunkY; cy2 >= 0; cy2--)
		{
			chunks[cy2].Unpack();
		}
		int baseX = chunkX * chunkSize;
		int baseZ = chunkZ * chunkSize;
		for (int lx = 0; lx < chunkSize; lx++)
		{
			for (int lz = 0; lz < chunkSize; lz++)
			{
				int sunLight = defaultSunLight;
				if (chunkY != chunks.Length - 1)
				{
					sunLight = chunks[chunkY + 1].Lighting.GetSunlight(lz * chunkSize + lx);
				}
				for (int cy = chunkY; cy >= 0; cy--)
				{
					int index3d = ((chunkSize - 1) * chunkSize + lz) * chunkSize + lx;
					IWorldChunk chunk = chunks[cy];
					IChunkLight chunklighting = chunks[cy].Lighting;
					tmpPosDimensionAware.Set(baseX + lx, cy * chunkSize + chunkSize - 1, baseZ + lz);
					for (int ly = chunkSize - 1; ly >= 0; ly--)
					{
						int absorption = chunk.GetLightAbsorptionAt(index3d, tmpPosDimensionAware, blockTypes);
						chunklighting.SetSunlight(index3d, sunLight);
						index3d -= YPlus;
						if (absorption > sunLight)
						{
							cy = -1;
							break;
						}
						sunLight -= (ushort)absorption;
						tmpPosDimensionAware.Y--;
					}
				}
			}
		}
	}

	public void SunlightFlood(IWorldChunk[] chunks, int chunkX, int chunkY, int chunkZ)
	{
		int chunkSize = this.chunkSize;
		Stack<BlockPos> stack = new Stack<BlockPos>();
		int baseX = chunkX * chunkSize;
		int baseZ = chunkZ * chunkSize;
		for (int cy = chunkY; cy >= 0; cy--)
		{
			IWorldChunk chunk = chunks[cy];
			chunk.Unpack();
			_ = chunk.Data;
			IChunkLight chunklighting = chunk.Lighting;
			for (int lx = 0; lx < chunkSize; lx++)
			{
				tmpPosDimensionAware.Set(baseX + lx, cy * chunkSize + chunkSize, baseZ);
				for (int lz = 0; lz < chunkSize; lz++)
				{
					int index3d = (chunkSize * chunkSize + lz) * chunkSize + lx;
					tmpPosDimensionAware.Z = baseZ + lz;
					for (int ly = chunkSize - 1; ly >= 0; ly--)
					{
						index3d -= YPlus;
						tmpPosDimensionAware.Y--;
						int spreadLight = chunklighting.GetSunlight(index3d) - 1;
						if (spreadLight <= 0)
						{
							break;
						}
						int absorption = chunk.GetLightAbsorptionAt(index3d, tmpPosDimensionAware, blockTypes);
						spreadLight -= absorption;
						if (spreadLight > 0 && ((lx < chunkSize - 1 && chunklighting.GetSunlight(index3d + XPlus) < spreadLight) || (lz < chunkSize - 1 && chunklighting.GetSunlight(index3d + ZPlus) < spreadLight) || (lx > 0 && chunklighting.GetSunlight(index3d - XPlus) < spreadLight) || (lz > 0 && chunklighting.GetSunlight(index3d - ZPlus) < spreadLight)))
						{
							stack.Push(new BlockPos(baseX + lx, cy * chunkSize + ly, baseZ + lz, tmpPosDimensionAware.dimension));
							if (stack.Count > 50)
							{
								SpreadSunLightInColumn(stack, chunks);
							}
						}
					}
				}
			}
		}
		SpreadSunLightInColumn(stack, chunks);
	}

	public byte SunLightFloodNeighbourChunks(IWorldChunk[] curChunks, int chunkX, int chunkY, int chunkZ, int dimension)
	{
		tmpPosDimensionAware.dimension = dimension;
		int chunkSize = this.chunkSize;
		byte spreadFaces = 0;
		Stack<BlockPos> curStack = new Stack<BlockPos>();
		Stack<BlockPos> neibStack = new Stack<BlockPos>();
		int[] mapping = new int[2];
		int[] lpos = new int[3];
		IWorldChunk[] neibChunks = new IWorldChunk[curChunks.Length];
		int baseX = chunkX * chunkSize;
		int baseZ = chunkZ * chunkSize;
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing facing in hORIZONTALS)
		{
			bool neibLoaded = true;
			int facingNormaliX = facing.Normali.X;
			int facingNormaliZ = facing.Normali.Z;
			for (int cy2 = 0; cy2 < curChunks.Length; cy2++)
			{
				neibChunks[cy2] = chunkProvider.GetChunk(chunkX + facingNormaliX, cy2 + dimension * 1024, chunkZ + facingNormaliZ);
				if (neibChunks[cy2] == null)
				{
					neibLoaded = false;
					if (cy2 != 0)
					{
						chunkProvider.Logger.Error("not full column loaded @{0} {1} {2}, lighting error will probably happen", chunkX, cy2, chunkZ);
					}
					break;
				}
				neibChunks[cy2].Unpack();
				curChunks[cy2].Unpack();
			}
			if (!neibLoaded)
			{
				continue;
			}
			int facingNormaliY = facing.Normali.Y;
			lpos[0] = (chunkSize - 1) * Math.Max(0, facingNormaliX);
			lpos[1] = (chunkSize - 1) * Math.Max(0, facingNormaliY);
			lpos[2] = (chunkSize - 1) * Math.Max(0, facingNormaliZ);
			int neibbaseX = (chunkX + facingNormaliX) * chunkSize;
			int i = 0;
			if (facingNormaliX == 0)
			{
				mapping[i++] = 0;
			}
			if (facingNormaliY == 0)
			{
				mapping[i++] = 1;
			}
			if (facingNormaliZ == 0)
			{
				mapping[i++] = 2;
			}
			for (int cy = chunkY; cy >= 0; cy--)
			{
				IWorldChunk neibChunk = neibChunks[cy];
				IWorldChunk curChunk = curChunks[cy];
				IChunkLight neibChunklighting = neibChunk.Lighting;
				IChunkLight curChunklighting = curChunk.Lighting;
				for (int a = chunkSize - 1; a >= 0; a--)
				{
					lpos[mapping[0]] = a;
					for (int b = chunkSize - 1; b >= 0; b--)
					{
						lpos[mapping[1]] = b;
						int ownIndex3d = (lpos[1] * chunkSize + lpos[2]) * chunkSize + lpos[0];
						int neibX = GameMath.Mod(lpos[0] + facingNormaliX, chunkSize);
						int neibZ = GameMath.Mod(lpos[2] + facingNormaliZ, chunkSize);
						int neibIndex3d = (lpos[1] * chunkSize + neibZ) * chunkSize + neibX;
						int neibLight = neibChunklighting.GetSunlight(neibIndex3d) - 1;
						int ownLight = curChunklighting.GetSunlight(ownIndex3d) - 1;
						tmpPosDimensionAware.Set(baseX + lpos[0], cy * chunkSize + lpos[1], baseZ + lpos[2]);
						int ownabsorption = curChunk.GetLightAbsorptionAt(ownIndex3d, tmpPosDimensionAware, blockTypes);
						tmpPosDimensionAware.Set(neibbaseX + neibX, cy * chunkSize + lpos[1], neibbaseX + neibZ);
						int neibabsorption = neibChunk.GetLightAbsorptionAt(neibIndex3d, tmpPosDimensionAware, blockTypes);
						int spreadNeibLight = neibLight - neibabsorption;
						int spreadOwnLight = ownLight - ownabsorption;
						if (spreadOwnLight > neibLight)
						{
							neibChunklighting.SetSunlight(neibIndex3d, spreadOwnLight);
							neibStack.Push(new BlockPos(baseX + neibX, cy * chunkSize + lpos[1], baseZ + neibZ, dimension));
							spreadFaces |= facing.Flag;
						}
						else if (spreadNeibLight > ownLight)
						{
							curChunklighting.SetSunlight(ownIndex3d, spreadNeibLight);
							curStack.Push(new BlockPos(baseX + lpos[0], cy * chunkSize + lpos[1], baseZ + lpos[2], dimension));
						}
					}
				}
			}
			if (neibStack.Count > 0)
			{
				SpreadSunLightInColumn(neibStack, neibChunks);
				for (int j = 0; j < neibChunks.Length; j++)
				{
					neibChunks[j].MarkModified();
				}
			}
			if (curStack.Count > 0)
			{
				SpreadSunLightInColumn(curStack, curChunks);
			}
		}
		return spreadFaces;
	}

	public void SpreadSunLightInColumn(Stack<BlockPos> stack, IWorldChunk[] chunks)
	{
		int chunkSize = this.chunkSize;
		while (stack.Count > 0)
		{
			BlockPos pos = stack.Pop();
			int cx = pos.X / chunkSize;
			int cy = pos.Y / chunkSize;
			int cz = pos.Z / chunkSize;
			int baseLx = pos.X % chunkSize;
			int num = pos.Y % chunkSize;
			int baseLz = pos.Z % chunkSize;
			int index3d = (num * chunkSize + baseLz) * chunkSize + baseLx;
			IWorldChunk chunk = chunks[cy];
			int absorption = chunk.GetLightAbsorptionAt(index3d, pos, blockTypes);
			int spreadLight = chunk.Lighting.GetSunlight(index3d) - absorption - 1;
			if (spreadLight <= 0)
			{
				continue;
			}
			int oldcy = cy;
			for (int i = 0; i < 6; i++)
			{
				Vec3i facingVector = BlockFacing.ALLNORMALI[i];
				int posY = pos.Y + facingVector.Y;
				int nlx = baseLx + facingVector.X;
				int nlz = baseLz + facingVector.Z;
				if (nlx >= 0 && posY >= 0 && nlz >= 0 && nlx < chunkSize && posY < mapsizey && nlz < chunkSize)
				{
					cy = posY / chunkSize;
					if (cy != oldcy)
					{
						chunk = chunks[cy];
						chunk.Unpack();
						oldcy = cy;
					}
					index3d = (posY % chunkSize * chunkSize + nlz) * chunkSize + nlx;
					if (chunk.Lighting.GetSunlight(index3d) < spreadLight)
					{
						chunk.Lighting.SetSunlight(index3d, spreadLight);
						stack.Push(new BlockPos(cx * chunkSize + nlx, posY, cz * chunkSize + nlz, pos.dimension));
					}
				}
			}
		}
	}

	private int SunLightLevelAt(int posX, int posY, int posZ, bool substractAbsorb = false)
	{
		int chunkSize = this.chunkSize;
		IWorldChunk chunk = chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize, notRecentlyAccessed: true);
		if (chunk == null)
		{
			return defaultSunLight;
		}
		int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
		return chunk.Lighting.GetSunlight(index3d) - (substractAbsorb ? chunk.GetLightAbsorptionAt(index3d, tmpPos.Set(posX, posY, posZ), blockTypes) : 0);
	}

	private void SetSunLightLevelAt(int posX, int posY, int posZ, int level)
	{
		int chunkSize = this.chunkSize;
		IWorldChunk chunk = chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize, notRecentlyAccessed: true);
		if (chunk != null)
		{
			int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
			chunk.Lighting.SetSunlight(index3d, level);
		}
	}

	private void ClearSunLightLevelAt(int posX, int posY, int posZ)
	{
		SetSunLightLevelAt(posX, posY, posZ, 0);
	}

	private int GetSunLightFromNeighbour(int posX, int posY, int posZ, bool directlyIlluminated)
	{
		int sunLightFromNeighbours = 0;
		for (int i = 0; i < 6; i++)
		{
			Vec3i face = BlockFacing.ALLNORMALI[i];
			int nposX = posX + face.X;
			int nposY = posY + face.Y;
			int nposZ = posZ + face.Z;
			if ((nposX | nposY | nposZ) >= 0 && nposX < mapsizex && nposY < mapsizey && nposZ < mapsizez)
			{
				IWorldChunk chunk = chunkProvider.GetUnpackedChunkFast(nposX / chunkSize, nposY / chunkSize, nposZ / chunkSize);
				if (chunk != null)
				{
					int index3d = (nposY % chunkSize * chunkSize + nposZ % chunkSize) * chunkSize + nposX % chunkSize;
					int absorb = chunk.GetLightAbsorptionAt(index3d, tmpPos.Set(nposX, nposY, nposZ), blockTypes);
					int neibSunlight = chunk.Lighting.GetSunlight(index3d) - absorb - ((!(i == 4 && directlyIlluminated)) ? 1 : 0);
					sunLightFromNeighbours = Math.Max(sunLightFromNeighbours, neibSunlight);
				}
			}
		}
		return sunLightFromNeighbours;
	}

	public FastSetOfLongs UpdateSunLight(int posX, int posY, int posZ, int oldAbsorb, int newAbsorb)
	{
		FastSetOfLongs touchedChunks = new FastSetOfLongs();
		if (newAbsorb == oldAbsorb)
		{
			return touchedChunks;
		}
		if (posX < 0 || posY < 0 || posZ < 0 || posX >= mapsizex || posY >= mapsizey || posZ >= mapsizez)
		{
			return touchedChunks;
		}
		QueueOfInt needToSpreadFromPositions = new QueueOfInt();
		bool directlyIlluminated = IsDirectlyIlluminated(posX, posY, posZ);
		BlockPos centerPos = new BlockPos(posX, posY, posZ);
		if (newAbsorb > oldAbsorb)
		{
			IWorldChunk chunk = chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize, notRecentlyAccessed: true);
			if (chunk == null)
			{
				return touchedChunks;
			}
			int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
			int oldLightLevel = chunk.Lighting.GetSunlight(index3d);
			chunk.Lighting.SetSunlight_Buffered(index3d, 0);
			QueueOfInt unhandledPositions = new QueueOfInt();
			for (int j = 0; j < 6; j++)
			{
				Vec3i face2 = BlockFacing.ALLNORMALI[j];
				int neibPosX = posX + face2.X;
				int neibPosY = posY + face2.Y;
				int neibPosZ = posZ + face2.Z;
				if (neibPosX >= 0 && neibPosY >= 0 && neibPosZ >= 0 && neibPosX < mapsizex && neibPosY < mapsizey && neibPosZ < mapsizez)
				{
					int neibPosW = oldLightLevel - oldAbsorb - 1 + ((directlyIlluminated && j == 5) ? 1 : 0);
					int neibLightNow = SunLightLevelAt(neibPosX, neibPosY, neibPosZ);
					if (neibPosW >= neibLightNow)
					{
						unhandledPositions.Enqueue(face2.X, face2.Y, face2.Z, neibPosW + (TileSideEnum.GetOpposite(j) + 1 << 5));
					}
				}
			}
			ClearSunlightAt(unhandledPositions, centerPos, directlyIlluminated, needToSpreadFromPositions, touchedChunks);
		}
		needToSpreadFromPositions.Enqueue(0, 0, 0, GetSunLightFromNeighbour(posX, posY, posZ, directlyIlluminated));
		SpreadSunlightAt(needToSpreadFromPositions, centerPos, directlyIlluminated, touchedChunks);
		if (posY > 0)
		{
			SetSunLightLevelAt(posX, posY - 1, posZ, GetSunLightFromNeighbour(posX, posY - 1, posZ, directlyIlluminated));
		}
		if (newAbsorb > oldAbsorb)
		{
			for (int i = 0; i < 6; i++)
			{
				Vec3i face = BlockFacing.ALLNORMALI[i];
				int x = posX + face.X;
				int y = posY + face.Y;
				int z = posZ + face.Z;
				if (IsValidPos(x, y, z))
				{
					int neiblight = GetSunLightFromNeighbour(x, y, z, IsDirectlyIlluminated(x, y, z));
					if (neiblight > SunLightLevelAt(x, y, z))
					{
						SetSunLightLevelAt(x, y, z, neiblight);
					}
				}
			}
		}
		return touchedChunks;
	}

	public bool IsDirectlyIlluminated(int posX, int posY, int posZ)
	{
		int chunkSize = this.chunkSize;
		int totalAbsorption = 0;
		int ownSunLightLevel = SunLightLevelAt(posX, posY, posZ);
		while (posY < mapsizey)
		{
			posY++;
			IWorldChunk chunk = chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize);
			if (chunk == null)
			{
				break;
			}
			int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
			int sunlightLevel = chunk.Lighting.GetSunlight(index3d);
			tmpDiPos.Set(posX, posY, posZ);
			totalAbsorption += chunk.GetLightAbsorptionAt(index3d, tmpDiPos, blockTypes);
			if (defaultSunLight - totalAbsorption < ownSunLightLevel)
			{
				return false;
			}
			if (sunlightLevel == defaultSunLight)
			{
				return true;
			}
			if (ownSunLightLevel > sunlightLevel)
			{
				return false;
			}
		}
		return defaultSunLight - totalAbsorption == ownSunLightLevel;
	}

	public void SpreadSunlightAt(QueueOfInt unhandledPositions, BlockPos centerPos, bool isDirectlyIlluminated, FastSetOfLongs touchedChunks)
	{
		int chunkSize = this.chunkSize;
		while (unhandledPositions.Count > 0)
		{
			int ipos = unhandledPositions.Dequeue();
			int posW = (ipos >> 24) & 0x1F;
			if (posW == 0)
			{
				continue;
			}
			int posX = (ipos & 0xFF) - 128 + centerPos.X;
			int posY = ((ipos >> 8) & 0xFF) - 128 + centerPos.Y;
			int posZ = ((ipos >> 16) & 0xFF) - 128 + centerPos.Z;
			IWorldChunk chunk = chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize);
			if (chunk == null)
			{
				continue;
			}
			int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
			chunk.Lighting.SetSunlight_Buffered(index3d, posW);
			int absorb = chunk.GetLightAbsorptionAt(index3d, tmpPos.Set(posX, posY, posZ), blockTypes);
			if (posW - absorb <= 0)
			{
				continue;
			}
			int directionIn = ((ipos >> 29) & 7) - 1;
			for (int i = 0; i < 6; i++)
			{
				if (i == directionIn)
				{
					continue;
				}
				Vec3i face = BlockFacing.ALLNORMALI[i];
				int nposX = posX + face.X;
				int nposY = posY + face.Y;
				int nposZ = posZ + face.Z;
				if ((nposX | nposY | nposZ) < 0 || nposX >= mapsizex || nposY >= mapsizey || nposZ >= mapsizez)
				{
					continue;
				}
				chunk = chunkProvider.GetUnpackedChunkFast(nposX / chunkSize, nposY / chunkSize, nposZ / chunkSize);
				if (chunk != null)
				{
					touchedChunks.Add(chunkProvider.ChunkIndex3D(nposX / chunkSize, nposY / chunkSize, nposZ / chunkSize));
					index3d = (nposY % chunkSize * chunkSize + nposZ % chunkSize) * chunkSize + nposX % chunkSize;
					int spreadLight = posW - absorb - ((!isDirectlyIlluminated || nposX != centerPos.X || nposZ != centerPos.Z || i != 5) ? 1 : 0);
					if (chunk.Lighting.GetSunlight(index3d) < spreadLight)
					{
						unhandledPositions.EnqueueIfLarger(nposX - centerPos.X, nposY - centerPos.Y, nposZ - centerPos.Z, spreadLight + (TileSideEnum.GetOpposite(i) + 1 << 5));
					}
				}
			}
		}
	}

	public void ClearSunlightAt(QueueOfInt positionsToClear, BlockPos centerPos, bool isDirectlyIlluminated, QueueOfInt needTospreadQueue, FastSetOfLongs touchedChunks)
	{
		int chunkSize = this.chunkSize;
		FastSetOfInts needToSpreadTmp = new FastSetOfInts();
		while (positionsToClear.Count > 0)
		{
			int ipos = positionsToClear.Dequeue();
			int posX = (ipos & 0xFF) - 128 + centerPos.X;
			int posY = ((ipos >> 8) & 0xFF) - 128 + centerPos.Y;
			int posZ = ((ipos >> 16) & 0xFF) - 128 + centerPos.Z;
			IWorldChunk chunk = chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize);
			if (chunk == null)
			{
				continue;
			}
			int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
			chunk.Lighting.SetSunlight_Buffered(index3d, 0);
			int absorb = chunk.GetLightAbsorptionAt(index3d, tmpPos.Set(posX, posY, posZ), blockTypes);
			int oldLight = ((ipos >> 24) & 0x1F) - absorb;
			if (oldLight <= 0)
			{
				continue;
			}
			int directionIn = ((ipos >> 29) & 7) - 1;
			for (int i = 0; i < 6; i++)
			{
				if (i == directionIn)
				{
					continue;
				}
				Vec3i face = BlockFacing.ALLNORMALI[i];
				int nposX = posX + face.X;
				int nposY = posY + face.Y;
				int nposZ = posZ + face.Z;
				if ((nposX | nposY | nposZ) < 0 || nposX >= mapsizex || nposY >= mapsizey || nposZ >= mapsizez)
				{
					continue;
				}
				chunk = chunkProvider.GetUnpackedChunkFast(nposX / chunkSize, nposY / chunkSize, nposZ / chunkSize);
				if (chunk == null)
				{
					continue;
				}
				touchedChunks.Add(chunkProvider.ChunkIndex3D(nposX / chunkSize, nposY / chunkSize, nposZ / chunkSize));
				int spreadLight = oldLight - 1 + ((isDirectlyIlluminated && nposX == centerPos.X && nposZ == centerPos.Z && i == 5) ? 1 : 0);
				if (spreadLight <= 0)
				{
					continue;
				}
				index3d = (nposY % chunkSize * chunkSize + nposZ % chunkSize) * chunkSize + nposX % chunkSize;
				int neibLight = chunk.Lighting.GetSunlight(index3d);
				if (neibLight != 0)
				{
					if (neibLight <= spreadLight)
					{
						needToSpreadTmp.RemoveIfMatches(nposX - centerPos.X, nposY - centerPos.Y, nposZ - centerPos.Z, neibLight);
						positionsToClear.EnqueueIfLarger(nposX - centerPos.X, nposY - centerPos.Y, nposZ - centerPos.Z, spreadLight + (TileSideEnum.GetOpposite(i) + 1 << 5));
					}
					else
					{
						needToSpreadTmp.Add(nposX - centerPos.X, nposY - centerPos.Y, nposZ - centerPos.Z, neibLight);
					}
				}
			}
		}
		foreach (int val in needToSpreadTmp)
		{
			needTospreadQueue.Enqueue(val);
		}
	}

	public FastSetOfLongs PlaceBlockLight(byte[] lightHsv, int posX, int posY, int posZ)
	{
		FastSetOfLongs touchedChunks = new FastSetOfLongs();
		IWorldChunk chunk = GetChunkAtPos(posX, posY, posZ);
		if (chunk == null)
		{
			return touchedChunks;
		}
		chunk.LightPositions.Add(InChunkIndex(posX, posY, posZ));
		UpdateLightAt(lightHsv[2], posX, posY, posZ, touchedChunks);
		return touchedChunks;
	}

	public void PlaceNonBlendingBlockLight(byte[] lightHsv, int posX, int posY, int posZ)
	{
		SetBlockLightLevel(lightHsv[0], lightHsv[1], lightHsv[2], posX, posY, posZ);
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing face in aLLFACES)
		{
			NonBlendingLightAxis(lightHsv[0], lightHsv[1], lightHsv[2], posX, posY, posZ, face);
		}
	}

	private void SetBlockLightLevel(byte hue, byte saturation, int value, int posX, int posY, int posZ)
	{
		IWorldChunk chunk = GetChunkAtPos(posX, posY, posZ);
		if (chunk != null && GetBlock(posX, posY, posZ) != null)
		{
			chunk.LightPositions.Add(InChunkIndex(posX, posY, posZ));
			int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
			chunk.Lighting.SetBlocklight_Buffered(index3d, (value << 5) | (hue << 10) | (saturation << 16));
		}
	}

	private int GetBlockLight(int x, int y, int z)
	{
		IWorldChunk chunk = GetChunkAtPos(x, y, z);
		if (chunk != null)
		{
			int index3d = (y % chunkSize * chunkSize + z % chunkSize) * chunkSize + x % chunkSize;
			chunk.Unpack_ReadOnly();
			return chunk.Lighting.GetBlocklight(index3d);
		}
		return 0;
	}

	private void NonBlendingLightAxis(byte hue, byte saturation, int lightLevel, int x, int y, int z, BlockFacing face)
	{
		int curLight = lightLevel - 1;
		while (curLight > 0)
		{
			x += face.Normali.X;
			y += face.Normali.Y;
			z += face.Normali.Z;
			if (y >= 0 && y <= mapsizey)
			{
				Block block = GetBlock(x, y, z);
				if (block != null && block.BlockId == 0 && GetBlockLight(x, y, z) < curLight)
				{
					SetBlockLightLevel(hue, saturation, curLight, x, y, z);
					if (face.Axis == EnumAxis.X)
					{
						NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.UP);
						NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.DOWN);
						NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.NORTH);
						NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.SOUTH);
					}
					else if (face.Axis == EnumAxis.Y)
					{
						NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.WEST);
						NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.EAST);
						NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.NORTH);
						NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.SOUTH);
					}
					else if (face.Axis == EnumAxis.Z)
					{
						NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.UP);
						NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.DOWN);
						NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.WEST);
						NonBlendingLightAxis(hue, saturation, curLight, x, y, z, BlockFacing.EAST);
					}
					curLight--;
					continue;
				}
				break;
			}
			break;
		}
	}

	public FastSetOfLongs RemoveBlockLight(byte[] oldLightHsv, int posX, int posY, int posZ)
	{
		FastSetOfLongs touchedChunks = new FastSetOfLongs();
		IWorldChunk chunk = GetChunkAtPos(posX, posY, posZ);
		if (chunk == null)
		{
			return touchedChunks;
		}
		chunk.LightPositions.Remove(InChunkIndex(posX, posY, posZ));
		int baseRange = oldLightHsv[2];
		if (baseRange == 18)
		{
			baseRange = 20;
		}
		int range = baseRange - chunk.GetLightAbsorptionAt(InChunkIndex(posX, posY, posZ), tmpPos.Set(posX, posY, posZ), blockTypes) - 1;
		SpreadDarkness(range, posX, posY, posZ, touchedChunks);
		UpdateLightAt(baseRange, posX, posY, posZ, touchedChunks);
		return touchedChunks;
	}

	public FastSetOfLongs UpdateBlockLight(int oldLightAbsorb, int newLightAbsorb, int posX, int posY, int posZ)
	{
		FastSetOfLongs touchedChunks = new FastSetOfLongs();
		int chunkSize = this.chunkSize;
		IWorldChunk chunk = chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize, notRecentlyAccessed: true);
		if (chunk == null)
		{
			return touchedChunks;
		}
		int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
		int v = chunk.Lighting.GetBlocklight(index3d);
		if (oldLightAbsorb == newLightAbsorb)
		{
			return touchedChunks;
		}
		if (v == 0)
		{
			return touchedChunks;
		}
		if (newLightAbsorb > oldLightAbsorb)
		{
			int range = v - oldLightAbsorb - 1;
			SpreadDarkness(range, posX, posY, posZ, touchedChunks);
		}
		UpdateLightAt(v, posX, posY, posZ, touchedChunks);
		return touchedChunks;
	}

	private void UpdateLightAt(int range, int posX, int posY, int posZ, FastSetOfLongs touchedChunks)
	{
		VisitedNodes.Clear();
		int chunkSize = this.chunkSize;
		LoadNearbyLightSources(posX, posY, posZ, range);
		foreach (NearbyLightSource nls in nearbyLightSources)
		{
			CollectLightValuesForLightSource(nls.posX, nls.posY, nls.posZ, posX, posY, posZ, range);
		}
		foreach (KeyValuePair<Vec3i, LightSourcesAtBlock> val in VisitedNodes)
		{
			RecalcBlockLightAtPos(val.Key, val.Value);
			touchedChunks.Add(chunkProvider.ChunkIndex3D(val.Key.X / chunkSize, val.Key.Y / chunkSize, val.Key.Z / chunkSize));
		}
	}

	private void SpreadDarkness(int rangeNext, int posX, int posY, int posZ, FastSetOfLongs touchedChunks)
	{
		if (rangeNext <= 0)
		{
			return;
		}
		int chunkSize = this.chunkSize;
		QueueOfInt bfsQueue = new QueueOfInt();
		bfsQueue.Enqueue(0x1F1F1F | (rangeNext << 24));
		bool nearMapEdge = posX < rangeNext - 1 || posZ < rangeNext - 1 || posX >= mapsizex - rangeNext + 1 || posZ >= mapsizez - rangeNext + 1;
		IWorldChunk chunk = chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize, notRecentlyAccessed: true);
		if (chunk == null)
		{
			return;
		}
		int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
		chunk.Lighting.SetBlocklight(index3d, 0);
		touchedChunks.Add(chunkProvider.ChunkIndex3D(posX / chunkSize, posY / chunkSize, posZ / chunkSize));
		int iteration = ++this.iteration;
		posX -= 31;
		posY -= 31;
		posZ -= 31;
		int visitedIndex = 125023;
		currentVisited[visitedIndex] = iteration;
		while (bfsQueue.Count > 0)
		{
			int pos = bfsQueue.Dequeue();
			for (int i = 0; i < 6; i++)
			{
				Vec3i facingVector = BlockFacing.ALLNORMALI[i];
				int ox = (pos & 0xFF) + facingVector.X;
				int oy = ((pos >> 8) & 0xFF) + facingVector.Y;
				int oz = ((pos >> 16) & 0xFF) + facingVector.Z;
				visitedIndex = ox + (oy * 63 + oz) * 63;
				if (currentVisited[visitedIndex] == iteration)
				{
					continue;
				}
				currentVisited[visitedIndex] = iteration;
				int nx = ox + posX;
				int ny = oy + posY;
				int nz = oz + posZ;
				if (ny < 0 || ny >= mapsizey || (nearMapEdge && (nx < 0 || nz < 0 || nx >= mapsizex || nz >= mapsizez)))
				{
					continue;
				}
				chunk = chunkProvider.GetUnpackedChunkFast(nx / chunkSize, ny / chunkSize, nz / chunkSize);
				if (chunk != null)
				{
					index3d = (ny % chunkSize * chunkSize + nz % chunkSize) * chunkSize + nx % chunkSize;
					if (chunk.Lighting.GetBlocklight(index3d) > 0)
					{
						touchedChunks.Add(chunkProvider.ChunkIndex3D(nx / chunkSize, ny / chunkSize, nz / chunkSize));
						chunk.Lighting.SetBlocklight_Buffered(index3d, 0);
					}
					int newRange = (pos >> 24) - chunk.GetLightAbsorptionAt(index3d, tmpPos.Set(nx, ny, nz), blockTypes) - 1;
					if (newRange > 0)
					{
						bfsQueue.Enqueue(ox | (oy << 8) | (oz << 16) | (newRange << 24));
					}
				}
			}
		}
	}

	private void CollectLightValuesForLightSource(int posX, int posY, int posZ, int forPosX, int forPosY, int forPosZ, int forRange)
	{
		int chunkSize = this.chunkSize;
		QueueOfInt bfsQueue = new QueueOfInt();
		Block block = GetBlock(posX, posY, posZ);
		if (block == null)
		{
			return;
		}
		byte[] lightHsv = block.GetLightHsv(readBlockAccess, tmpPos.Set(posX, posY, posZ));
		byte h = lightHsv[0];
		byte s = lightHsv[1];
		byte v = lightHsv[2];
		bfsQueue.Enqueue(0x1F1F1F | (v << 24));
		Vec3i npos = new Vec3i(posX, posY, posZ);
		VisitedNodes.TryGetValue(npos, out var lsab);
		if (lsab == null)
		{
			lsab = (VisitedNodes[npos] = new LightSourcesAtBlock());
		}
		lsab.AddHsv(h, s, v);
		bool nearMapEdge = posX < v - 1 || posZ < v - 1 || posX >= mapsizex - v + 1 || posZ >= mapsizez - v + 1;
		int iteration = ++this.iteration;
		posX -= 31;
		posY -= 31;
		posZ -= 31;
		int visitedIndex = 125023;
		currentVisited[visitedIndex] = iteration;
		while (bfsQueue.Count > 0)
		{
			int pos = bfsQueue.Dequeue();
			int ox = (pos & 0xFF) + posX;
			int oy = ((pos >> 8) & 0xFF) + posY;
			int oz = ((pos >> 16) & 0xFF) + posZ;
			IWorldChunk chunk = chunkProvider.GetUnpackedChunkFast(ox / chunkSize, oy / chunkSize, oz / chunkSize);
			if (chunk == null)
			{
				continue;
			}
			int index3d = (oy % chunkSize * chunkSize + oz % chunkSize) * chunkSize + ox % chunkSize;
			int spreadBright = (pos >> 24) - chunk.GetLightAbsorptionAt(index3d, tmpPos.Set(ox, oy, oz), blockTypes) - 1;
			if (spreadBright <= 0)
			{
				continue;
			}
			for (int i = 0; i < 6; i++)
			{
				Vec3i facingVector = BlockFacing.ALLNORMALI[i];
				int nx = ox + facingVector.X;
				int ny = oy + facingVector.Y;
				int nz = oz + facingVector.Z;
				visitedIndex = ((ny - posY) * 63 + nz - posZ) * 63 + nx - posX;
				if (currentVisited[visitedIndex] == iteration)
				{
					continue;
				}
				currentVisited[visitedIndex] = iteration;
				if (ny >= 0 && ny < mapsizey && (!nearMapEdge || (nx >= 0 && nz >= 0 && nx < mapsizex && nz < mapsizez)) && Math.Abs(nx - forPosX) + Math.Abs(ny - forPosY) + Math.Abs(nz - forPosZ) < forRange + spreadBright)
				{
					bfsQueue.Enqueue((nx - posX) | (ny - posY << 8) | (nz - posZ << 16) | (spreadBright << 24));
					npos = new Vec3i(nx, ny, nz);
					VisitedNodes.TryGetValue(npos, out lsab);
					if (lsab == null)
					{
						lsab = (VisitedNodes[npos] = new LightSourcesAtBlock());
					}
					lsab.AddHsv(h, s, (byte)spreadBright);
				}
			}
		}
	}

	private void RecalcBlockLightAtPos(Vec3i pos, LightSourcesAtBlock lsab)
	{
		int chunkSize = this.chunkSize;
		IWorldChunk chunk = chunkProvider.GetUnpackedChunkFast(pos.X / chunkSize, pos.Y / chunkSize, pos.Z / chunkSize, notRecentlyAccessed: true);
		if (chunk == null)
		{
			return;
		}
		int index3d = (pos.Y % chunkSize * chunkSize + pos.Z % chunkSize) * chunkSize + pos.X % chunkSize;
		float totalBright = 0f;
		int maxBright = 0;
		int lightCount = lsab.lightCount;
		for (int j = 0; j < lightCount; j++)
		{
			int v2 = lsab.lightHsvs[j * 3 + 2];
			maxBright = Math.Max(maxBright, v2);
			totalBright += (float)v2;
		}
		if (maxBright == 0)
		{
			chunk.Lighting.SetBlocklight(index3d, 0);
			return;
		}
		float finalRgbR = 0.5f;
		float finalRgbG = 0.5f;
		float finalRgbB = 0.5f;
		for (int i = 0; i < lightCount; i++)
		{
			int v = lsab.lightHsvs[i * 3 + 2];
			int rgb = ColorUtil.HsvToRgb(lsab.lightHsvs[i * 3] * 4, lsab.lightHsvs[i * 3 + 1] * 32, v * 8);
			float weight = (float)v / totalBright;
			finalRgbR += (float)(rgb >> 16) * weight;
			finalRgbG += (float)((rgb >> 8) & 0xFF) * weight;
			finalRgbB += (float)(rgb & 0xFF) * weight;
		}
		int num = ColorUtil.Rgb2Hsv(finalRgbR, finalRgbG, finalRgbB);
		int hBits = Math.Min((int)((float)(num & 0xFF) / 4f + 0.5f), ColorUtil.HueQuantities - 1);
		int sBits = Math.Min((int)((float)((num >> 8) & 0xFF) / 32f + 0.5f), ColorUtil.SatQuantities - 1);
		chunk.Lighting.SetBlocklight(index3d, (maxBright << 5) | (hBits << 10) | (sBits << 16));
	}

	private Block GetBlock(int posX, int posY, int posZ)
	{
		if ((posX | posY | posZ) < 0)
		{
			return null;
		}
		int chunkSize = this.chunkSize;
		IWorldChunk chunk = chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize, notRecentlyAccessed: true);
		if (chunk == null)
		{
			return null;
		}
		int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
		return blockTypes[chunk.Data[index3d]];
	}

	private int GetBlockLightAbsorb(int posX, int posY, int posZ)
	{
		if ((posX | posY | posZ) < 0)
		{
			return 0;
		}
		int chunkSize = this.chunkSize;
		IWorldChunk chunk = chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize, notRecentlyAccessed: true);
		if (chunk == null)
		{
			return 0;
		}
		int index3d = (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
		return chunk.GetLightAbsorptionAt(index3d, tmpPos.Set(posX, posY, posZ), blockTypes);
	}

	private IWorldChunk GetChunkAtPos(int posX, int posY, int posZ)
	{
		return chunkProvider.GetUnpackedChunkFast(posX / chunkSize, posY / chunkSize, posZ / chunkSize, notRecentlyAccessed: true);
	}

	private int InChunkIndex(int posX, int posY, int posZ)
	{
		return (posY % chunkSize * chunkSize + posZ % chunkSize) * chunkSize + posX % chunkSize;
	}

	internal long GetChunkIndexForPos(int posX, int posY, int posZ)
	{
		return chunkProvider.ChunkIndex3D(posX / chunkSize, posY / chunkSize, posZ / chunkSize);
	}

	private void LoadNearbyLightSources(int posX, int posY, int posZ, int range)
	{
		nearbyLightSources.Clear();
		int chunkX = posX / chunkSize;
		int chunkY = posY / chunkSize;
		int chunkZ = posZ / chunkSize;
		for (int cx = -1; cx <= 1; cx++)
		{
			for (int cy = -1; cy <= 1; cy++)
			{
				for (int cz = -1; cz <= 1; cz++)
				{
					IWorldChunk chunk = chunkProvider.GetChunk(chunkX + cx, chunkY + cy, chunkZ + cz);
					if (chunk == null)
					{
						continue;
					}
					chunk.Unpack_ReadOnly();
					foreach (int index3d in chunk.LightPositions)
					{
						int lposy = (chunkY + cy) * chunkSize + index3d / (chunkSize * chunkSize);
						int lposz = (chunkZ + cz) * chunkSize + index3d / chunkSize % chunkSize;
						int lposx = (chunkX + cx) * chunkSize + index3d % chunkSize;
						int manhattenDist = Math.Abs(posX - lposx) + Math.Abs(posY - lposy) + Math.Abs(posZ - lposz);
						Block blockEmitter = blockTypes[chunk.Data[index3d]];
						if (blockEmitter.GetLightHsv(readBlockAccess, tmpPos.Set(lposx, lposy, lposz))[2] + range > manhattenDist)
						{
							nearbyLightSources.Add(new NearbyLightSource
							{
								block = blockEmitter,
								posX = lposx,
								posY = lposy,
								posZ = lposz
							});
						}
					}
				}
			}
		}
	}
}
