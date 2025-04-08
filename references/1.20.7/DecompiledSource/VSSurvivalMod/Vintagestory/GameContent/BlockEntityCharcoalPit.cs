using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityCharcoalPit : BlockEntity
{
	private static float BurnHours = 18f;

	private Dictionary<BlockPos, int> smokeLocations = new Dictionary<BlockPos, int>();

	private double finishedAfterTotalHours;

	private double startingAfterTotalHours;

	private int state;

	private string startedByPlayerUid;

	private bool lit;

	private int maxSize = 11;

	public bool Lit => lit;

	public int MaxPileSize
	{
		get
		{
			return maxSize;
		}
		set
		{
			maxSize = value;
		}
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (api.Side == EnumAppSide.Client)
		{
			RegisterGameTickListener(OnClientTick, 150);
		}
		else
		{
			RegisterGameTickListener(OnServerTick, 3000);
		}
		if (Lit)
		{
			FindHolesInPit();
		}
	}

	private void OnClientTick(float dt)
	{
		if (!lit || base.Block?.ParticleProperties == null)
		{
			return;
		}
		BlockPos pos = new BlockPos();
		foreach (KeyValuePair<BlockPos, int> val in smokeLocations)
		{
			if (Api.World.Rand.NextDouble() < 0.20000000298023224 && base.Block.ParticleProperties.Length != 0)
			{
				pos.Set(val.Key.X, val.Value + 1, val.Key.Z);
				Block upblock = Api.World.BlockAccessor.GetBlock(pos);
				AdvancedParticleProperties particles = base.Block.ParticleProperties[0];
				particles.basePos = BEBehaviorBurning.RandomBlockPos(Api.World.BlockAccessor, pos, upblock, BlockFacing.UP);
				particles.Quantity.avg = 1f;
				Api.World.SpawnParticles(particles);
				particles.Quantity.avg = 0f;
			}
		}
	}

	private void OnServerTick(float dt)
	{
		if (!lit)
		{
			return;
		}
		if (startingAfterTotalHours <= Api.World.Calendar.TotalHours && state == 0)
		{
			finishedAfterTotalHours = Api.World.Calendar.TotalHours + (double)BurnHours;
			state = 1;
			MarkDirty();
		}
		if (state == 0)
		{
			return;
		}
		List<BlockPos> holes = FindHolesInPit();
		if (holes != null && holes.Count > 0)
		{
			Block fireblock = Api.World.GetBlock(new AssetLocation("fire"));
			finishedAfterTotalHours = Api.World.Calendar.TotalHours + (double)BurnHours;
			{
				foreach (BlockPos holePos in holes)
				{
					BlockPos firePos = holePos.Copy();
					Block block = Api.World.BlockAccessor.GetBlock(holePos);
					if (block.BlockId == 0 || block.BlockId == base.Block.BlockId)
					{
						continue;
					}
					BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
					foreach (BlockFacing facing in aLLFACES)
					{
						facing.IterateThruFacingOffsets(firePos);
						if (Api.World.BlockAccessor.GetBlock(firePos).BlockId == 0 && Api.World.Rand.NextDouble() > 0.8999999761581421)
						{
							Api.World.BlockAccessor.SetBlock(fireblock.BlockId, firePos);
							Api.World.BlockAccessor.GetBlockEntity(firePos)?.GetBehavior<BEBehaviorBurning>()?.OnFirePlaced(facing, startedByPlayerUid);
						}
					}
				}
				return;
			}
		}
		if (finishedAfterTotalHours <= Api.World.Calendar.TotalHours)
		{
			ConvertPit();
		}
	}

	public void IgniteNow()
	{
		if (!lit)
		{
			lit = true;
			startingAfterTotalHours = Api.World.Calendar.TotalHours + 0.5;
			MarkDirty(redrawOnClient: true);
			FindHolesInPit();
		}
	}

	private void ConvertPit()
	{
		Dictionary<BlockPos, Vec3i> quantityPerColumn = new Dictionary<BlockPos, Vec3i>();
		HashSet<BlockPos> visitedPositions = new HashSet<BlockPos>();
		Queue<BlockPos> bfsQueue = new Queue<BlockPos>();
		bfsQueue.Enqueue(Pos);
		BlockPos minPos = Pos.Copy();
		BlockPos maxPos = Pos.Copy();
		while (bfsQueue.Count > 0)
		{
			BlockPos bpos = bfsQueue.Dequeue();
			BlockPos npos = bpos.Copy();
			BlockPos bposGround = bpos.Copy();
			bposGround.Y = 0;
			if (quantityPerColumn.TryGetValue(bposGround, out var curQuantityAndYMinMax))
			{
				curQuantityAndYMinMax.Y = Math.Min(curQuantityAndYMinMax.Y, bpos.Y);
				curQuantityAndYMinMax.Z = Math.Max(curQuantityAndYMinMax.Z, bpos.Y);
			}
			else
			{
				Vec3i vec3i2 = (quantityPerColumn[bposGround] = new Vec3i(0, bpos.Y, bpos.Y));
				curQuantityAndYMinMax = vec3i2;
			}
			curQuantityAndYMinMax.X += BlockFirepit.GetFireWoodQuanity(Api.World, bpos);
			BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
			for (int i = 0; i < aLLFACES.Length; i++)
			{
				aLLFACES[i].IterateThruFacingOffsets(npos);
				if (!BlockFirepit.IsFirewoodPile(Api.World, npos))
				{
					if (Api.World.BlockAccessor.GetChunkAtBlockPos(npos) == null)
					{
						return;
					}
				}
				else if (InCube(npos, ref minPos, ref maxPos) && !visitedPositions.Contains(npos))
				{
					bfsQueue.Enqueue(npos.Copy());
					visitedPositions.Add(npos.Copy());
				}
			}
		}
		BlockPos lpos = new BlockPos();
		foreach (KeyValuePair<BlockPos, Vec3i> val in quantityPerColumn)
		{
			lpos.Set(val.Key.X, val.Value.Y, val.Key.Z);
			int charCoalQuantity = (int)((float)val.Value.X * (0.125f + (float)Api.World.Rand.NextDouble() / 8f));
			int maxY = val.Value.Z;
			while (lpos.Y <= maxY)
			{
				if (BlockFirepit.IsFirewoodPile(Api.World, lpos) || lpos == Pos)
				{
					if (charCoalQuantity > 0)
					{
						Block charcoalBlock = Api.World.GetBlock(new AssetLocation("charcoalpile-" + GameMath.Clamp(charCoalQuantity, 1, 8)));
						Api.World.BlockAccessor.SetBlock(charcoalBlock.BlockId, lpos);
						charCoalQuantity -= 8;
					}
					else
					{
						Api.World.BlockAccessor.SetBlock(0, lpos);
					}
				}
				lpos.Up();
			}
		}
	}

	internal void Init(IPlayer player)
	{
		startedByPlayerUid = player?.PlayerUID;
	}

	private List<BlockPos> FindHolesInPit()
	{
		smokeLocations.Clear();
		List<BlockPos> holes = new List<BlockPos>();
		HashSet<BlockPos> visitedPositions = new HashSet<BlockPos>();
		Queue<BlockPos> bfsQueue = new Queue<BlockPos>();
		bfsQueue.Enqueue(Pos);
		int charcoalPitBlockId = Api.World.GetBlock(new AssetLocation("charcoalpit")).BlockId;
		BlockPos minPos = Pos.Copy();
		BlockPos maxPos = Pos.Copy();
		while (bfsQueue.Count > 0)
		{
			BlockPos bpos = bfsQueue.Dequeue();
			BlockPos npos = bpos.Copy();
			BlockPos bposGround = bpos.Copy();
			bposGround.Y = 0;
			smokeLocations.TryGetValue(bposGround, out var yMax);
			smokeLocations[bposGround] = Math.Max(yMax, bpos.Y);
			BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
			foreach (BlockFacing facing in aLLFACES)
			{
				facing.IterateThruFacingOffsets(npos);
				IWorldChunk chunk = Api.World.BlockAccessor.GetChunkAtBlockPos(npos);
				if (chunk == null)
				{
					return null;
				}
				Block nBlock = chunk.GetLocalBlockAtBlockPos(Api.World, npos);
				bool solid = nBlock.GetLiquidBarrierHeightOnSide(facing.Opposite, npos) == 1f || nBlock.GetLiquidBarrierHeightOnSide(facing, bpos) == 1f;
				bool num = BlockFirepit.IsFirewoodPile(Api.World, npos);
				if (!num && nBlock.BlockId != charcoalPitBlockId)
				{
					if (IsCombustible(npos))
					{
						holes.Add(npos.Copy());
					}
					else if (!solid)
					{
						holes.Add(bpos.Copy());
					}
				}
				if (num && InCube(npos, ref minPos, ref maxPos) && !visitedPositions.Contains(npos))
				{
					bfsQueue.Enqueue(npos.Copy());
					visitedPositions.Add(npos.Copy());
				}
			}
		}
		return holes;
	}

	private bool InCube(BlockPos npos, ref BlockPos minPos, ref BlockPos maxPos)
	{
		BlockPos nmin = minPos.Copy();
		BlockPos nmax = maxPos.Copy();
		if (npos.X < minPos.X)
		{
			nmin.X = npos.X;
		}
		else if (npos.X > maxPos.X)
		{
			nmax.X = npos.X;
		}
		if (npos.Y < minPos.Y)
		{
			nmin.Y = npos.Y;
		}
		else if (npos.Y > maxPos.Y)
		{
			nmax.Y = npos.Y;
		}
		if (npos.Z < minPos.Z)
		{
			nmin.Z = npos.Z;
		}
		else if (npos.Z > maxPos.Z)
		{
			nmax.Z = npos.Z;
		}
		if (nmax.X - nmin.X + 1 <= maxSize && nmax.Y - nmin.Y + 1 <= maxSize && nmax.Z - nmin.Z + 1 <= maxSize)
		{
			minPos = nmin.Copy();
			maxPos = nmax.Copy();
			return true;
		}
		return false;
	}

	private bool IsCombustible(BlockPos pos)
	{
		Block block = Api.World.BlockAccessor.GetBlock(pos);
		if (block.CombustibleProps != null)
		{
			return block.CombustibleProps.BurnDuration > 0f;
		}
		if (block is ICombustible bic)
		{
			return bic.GetBurnDuration(Api.World, pos) > 0f;
		}
		return false;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		int num = state;
		bool beforeLit = lit;
		base.FromTreeAttributes(tree, worldForResolving);
		finishedAfterTotalHours = tree.GetDouble("finishedAfterTotalHours");
		startingAfterTotalHours = tree.GetDouble("startingAfterTotalHours");
		state = tree.GetInt("state");
		startedByPlayerUid = tree.GetString("startedByPlayerUid");
		lit = tree.GetBool("lit", defaultValue: true);
		if (num != state || beforeLit != lit)
		{
			ICoreAPI api = Api;
			if (api != null && api.Side == EnumAppSide.Client)
			{
				FindHolesInPit();
			}
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetDouble("finishedAfterTotalHours", finishedAfterTotalHours);
		tree.SetDouble("startingAfterTotalHours", startingAfterTotalHours);
		tree.SetInt("state", state);
		tree.SetBool("lit", lit);
		if (startedByPlayerUid != null)
		{
			tree.SetString("startedByPlayerUid", startedByPlayerUid);
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		double minutesLeft = 60.0 * (startingAfterTotalHours - Api.World.Calendar.TotalHours);
		if (lit)
		{
			if (minutesLeft <= 0.0)
			{
				dsc.AppendLine(Lang.Get("Lit."));
				return;
			}
			dsc.AppendLine(Lang.Get("lit-starting", (int)minutesLeft));
		}
		else
		{
			dsc.AppendLine(Lang.Get("Unlit."));
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (!lit)
		{
			MeshData litCharcoalMesh = ObjectCacheUtil.GetOrCreate(Api, "litCharcoalMesh", delegate
			{
				((ICoreClientAPI)Api).Tesselator.TesselateShape(base.Block, Shape.TryGet(Api, "shapes/block/wood/firepit/cold-normal.json"), out var modeldata);
				return modeldata;
			});
			mesher.AddMeshData(litCharcoalMesh);
			return true;
		}
		return false;
	}
}
