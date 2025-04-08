using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class BEBehaviorBurning : BlockEntityBehavior
{
	public float startDuration;

	public float remainingBurnDuration;

	private Block fireBlock;

	private Block fuelBlock;

	private string startedByPlayerUid;

	private static Cuboidf fireCuboid = new Cuboidf(-0.125f, 0f, -0.125f, 1.125f, 1f, 1.125f);

	private WeatherSystemBase wsys;

	private Vec3d tmpPos = new Vec3d();

	private ICoreClientAPI capi;

	public Action<float> OnFireTick;

	public Action<bool> OnFireDeath;

	public ActionBoolReturn ShouldBurn;

	public ActionBoolReturn<BlockPos> OnCanBurn;

	public bool IsBurning;

	public BlockPos FirePos;

	public BlockPos FuelPos;

	private long l1;

	private long l2;

	private BlockFacing particleFacing;

	private bool unloaded;

	private static Random rand = new Random();

	public float TimePassed => startDuration - remainingBurnDuration;

	public BEBehaviorBurning(BlockEntity be)
		: base(be)
	{
		OnCanBurn = (BlockPos pos) => getBurnDuration(pos) > 0f;
		ShouldBurn = () => true;
		OnFireTick = delegate
		{
			if (remainingBurnDuration <= 0f)
			{
				KillFire(consumeFuel: true);
			}
		};
		OnFireDeath = delegate(bool consumefuel)
		{
			if (consumefuel)
			{
				(Api.World.BlockAccessor.GetBlockEntity(FuelPos) as BlockEntityContainer)?.OnBlockBroken();
				Api.World.BlockAccessor.SetBlock(0, FuelPos);
				Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(FuelPos);
				if (((ICoreServerAPI)Api).Server.Config.AllowFireSpread)
				{
					TrySpreadTo(FuelPos);
				}
			}
			Api.World.BlockAccessor.SetBlock(0, FirePos);
			Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(FirePos);
		};
	}

	private float getBurnDuration(BlockPos pos)
	{
		Block block = Api.World.BlockAccessor.GetBlock(pos);
		if (block.CombustibleProps != null)
		{
			return block.CombustibleProps.BurnDuration;
		}
		if (block is ICombustible bic)
		{
			return bic.GetBurnDuration(Api.World, pos);
		}
		return 0f;
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		fireBlock = Api.World.GetBlock(new AssetLocation("fire"));
		if (fireBlock == null)
		{
			fireBlock = new Block();
		}
		if (IsBurning)
		{
			initSoundsAndTicking();
		}
		capi = api as ICoreClientAPI;
		if (capi != null)
		{
			capi.Event.RegisterAsyncParticleSpawner(onAsyncParticles);
		}
	}

	public void OnFirePlaced(BlockFacing fromFacing, string startedByPlayerUid)
	{
		OnFirePlaced(Blockentity.Pos, Blockentity.Pos.AddCopy(fromFacing.Opposite), startedByPlayerUid);
	}

	public void OnFirePlaced(BlockPos firePos, BlockPos fuelPos, string startedByPlayerUid, bool didSpread = false)
	{
		if (IsBurning || !ShouldBurn())
		{
			return;
		}
		this.startedByPlayerUid = startedByPlayerUid;
		if (!string.IsNullOrEmpty(startedByPlayerUid))
		{
			IPlayer playerByUid = Api.World.PlayerByUid(startedByPlayerUid);
			if (playerByUid != null)
			{
				if (didSpread)
				{
					Api.Logger.Audit($"{playerByUid.PlayerName} started a fire that spread to {firePos}");
				}
				else
				{
					Api.Logger.Audit($"{playerByUid.PlayerName} started a fire at {firePos}");
				}
			}
		}
		FirePos = firePos.Copy();
		FuelPos = fuelPos.Copy();
		if (FuelPos == null || !canBurn(FuelPos))
		{
			BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
			foreach (BlockFacing facing in aLLFACES)
			{
				BlockPos npos = FirePos.AddCopy(facing);
				if (canBurn(npos))
				{
					FuelPos = npos;
					startDuration = (remainingBurnDuration = getBurnDuration(npos));
					startBurning();
					return;
				}
			}
			startDuration = 1f;
			remainingBurnDuration = 1f;
			FuelPos = FirePos.Copy();
		}
		else
		{
			float bdur = getBurnDuration(fuelPos);
			if (bdur > 0f)
			{
				startDuration = (remainingBurnDuration = bdur);
			}
		}
		startBurning();
	}

	private void startBurning()
	{
		if (!IsBurning)
		{
			IsBurning = true;
			if (Api != null)
			{
				initSoundsAndTicking();
			}
		}
	}

	private void initSoundsAndTicking()
	{
		fuelBlock = Api.World.BlockAccessor.GetBlock(FuelPos);
		l1 = Blockentity.RegisterGameTickListener(OnTick, 25);
		if (Api.Side == EnumAppSide.Server)
		{
			l2 = Blockentity.RegisterGameTickListener(OnSlowServerTick, 1000);
		}
		wsys = Api.ModLoader.GetModSystem<WeatherSystemBase>();
		Api.World.BlockAccessor.MarkBlockDirty(base.Pos);
		particleFacing = BlockFacing.FromNormal(new Vec3i(FirePos.X - FuelPos.X, FirePos.Y - FuelPos.Y, FirePos.Z - FuelPos.Z));
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		unloaded = true;
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		unloaded = true;
	}

	private void OnSlowServerTick(float dt)
	{
		if (!canBurn(FuelPos))
		{
			KillFire(consumeFuel: false);
			return;
		}
		Entity[] entities = Api.World.GetEntitiesAround(FirePos.ToVec3d().Add(0.5, 0.5, 0.5), 3f, 3f, (Entity e) => true);
		Vec3d ownPos = FirePos.ToVec3d();
		foreach (Entity entity in entities)
		{
			if (CollisionTester.AabbIntersect(entity.SelectionBox, entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z, fireCuboid, ownPos))
			{
				if (entity.Alive)
				{
					entity.ReceiveDamage(new DamageSource
					{
						Source = EnumDamageSource.Block,
						SourceBlock = fireBlock,
						SourcePos = ownPos,
						Type = EnumDamageType.Fire
					}, 2f);
				}
				if (Api.World.Rand.NextDouble() < 0.125)
				{
					entity.Ignite();
				}
			}
		}
		IBlockAccessor bl = Api.World.BlockAccessor;
		if (FuelPos != FirePos)
		{
			JsonObject attributes = bl.GetBlock(FirePos, 2).Attributes;
			if (attributes == null || !attributes.IsTrue("smothersFire"))
			{
				JsonObject attributes2 = bl.GetBlock(FuelPos, 2).Attributes;
				if (attributes2 == null || !attributes2.IsTrue("smothersFire"))
				{
					goto IL_01b1;
				}
			}
			KillFire(consumeFuel: false);
			return;
		}
		goto IL_01b1;
		IL_01b1:
		if (bl.GetRainMapHeightAt(FirePos.X, FirePos.Z) > FirePos.Y)
		{
			return;
		}
		tmpPos.Set((double)FirePos.X + 0.5, (double)FirePos.Y + 0.5, (double)FirePos.Z + 0.5);
		double rain = wsys.GetPrecipitation(tmpPos);
		if (rain > 0.05)
		{
			if (rand.NextDouble() < rain / 2.0)
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), FirePos, -0.25, null, randomizePitch: false, 16f);
			}
			if (rand.NextDouble() < rain / 2.0)
			{
				KillFire(consumeFuel: false);
			}
		}
	}

	private void OnTick(float dt)
	{
		if (Api.Side == EnumAppSide.Server)
		{
			remainingBurnDuration -= dt;
			OnFireTick?.Invoke(dt);
			float spreadChance = (TimePassed - 2.5f) / 450f;
			if (((ICoreServerAPI)Api).Server.Config.AllowFireSpread && (double)spreadChance > Api.World.Rand.NextDouble())
			{
				TrySpreadFireAllDirs();
			}
		}
	}

	private bool onAsyncParticles(float dt, IAsyncParticleManager manager)
	{
		if (fuelBlock == null)
		{
			return true;
		}
		int index = Math.Min(fireBlock.ParticleProperties.Length - 1, Api.World.Rand.Next(fireBlock.ParticleProperties.Length + 1));
		AdvancedParticleProperties particles = fireBlock.ParticleProperties[index];
		particles.basePos = RandomBlockPos(Api.World.BlockAccessor, FuelPos, fuelBlock, particleFacing);
		particles.Quantity.avg = ((index == 1) ? 4f : 0.75f);
		particles.TerrainCollision = false;
		manager.Spawn(particles);
		particles.Quantity.avg = 0f;
		if (!unloaded)
		{
			return IsBurning;
		}
		return false;
	}

	public void KillFire(bool consumeFuel)
	{
		IsBurning = false;
		Blockentity.UnregisterGameTickListener(l1);
		Blockentity.UnregisterGameTickListener(l2);
		l1 = 0L;
		l2 = 0L;
		OnFireDeath(consumeFuel);
		unloaded = true;
	}

	protected void TrySpreadFireAllDirs()
	{
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing facing in aLLFACES)
		{
			BlockPos npos = FirePos.AddCopy(facing);
			TrySpreadTo(npos);
		}
		if (FuelPos != FirePos)
		{
			TrySpreadTo(FirePos);
		}
	}

	public bool TrySpreadTo(BlockPos pos)
	{
		BlockEntity be = Api.World.BlockAccessor.GetBlockEntity(pos);
		if (Api.World.BlockAccessor.GetBlock(pos).Replaceable < 6000 && !(be is BlockEntityGroundStorage))
		{
			return false;
		}
		BEBehaviorBurning obj = be?.GetBehavior<BEBehaviorBurning>();
		if (obj != null && obj.IsBurning)
		{
			return false;
		}
		bool hasFuel = false;
		BlockPos npos = null;
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing firefacing in aLLFACES)
		{
			npos = pos.AddCopy(firefacing);
			if (canBurn(npos) && Api.World.BlockAccessor.GetBlockEntity(npos)?.GetBehavior<BEBehaviorBurning>() == null)
			{
				hasFuel = true;
				break;
			}
		}
		BlockEntityGroundStorage begs = be as BlockEntityGroundStorage;
		if (!hasFuel && begs != null && !begs.IsBurning && begs.CanIgnite)
		{
			hasFuel = true;
		}
		if (!hasFuel)
		{
			return false;
		}
		IPlayer player = Api.World.PlayerByUid(startedByPlayerUid);
		if (player != null && (Api.World.Claims.TestAccess(player, pos, EnumBlockAccessFlags.BuildOrBreak) != 0 || Api.World.Claims.TestAccess(player, npos, EnumBlockAccessFlags.BuildOrBreak) != 0))
		{
			return false;
		}
		SpreadTo(pos, npos, begs);
		return true;
	}

	private void SpreadTo(BlockPos pos, BlockPos npos, BlockEntityGroundStorage begs)
	{
		if (begs == null)
		{
			Api.World.BlockAccessor.SetBlock(fireBlock.BlockId, pos);
		}
		BEBehaviorBurning buhu = Api.World.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorBurning>();
		if (begs != null && !begs.IsBurning && buhu != null && !buhu.IsBurning)
		{
			begs.TryIgnite();
		}
		else
		{
			buhu?.OnFirePlaced(pos, npos, startedByPlayerUid, didSpread: true);
		}
	}

	protected bool canBurn(BlockPos pos)
	{
		if (OnCanBurn(pos))
		{
			ModSystemBlockReinforcement modSystem = Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
			if (modSystem == null)
			{
				return true;
			}
			return !modSystem.IsReinforced(pos);
		}
		return false;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		remainingBurnDuration = tree.GetFloat("remainingBurnDuration");
		startDuration = tree.GetFloat("startDuration");
		if (!tree.HasAttribute("fireposX"))
		{
			BlockFacing fromFacing = BlockFacing.ALLFACES[tree.GetInt("fromFacing")];
			FirePos = Blockentity.Pos.Copy();
			FuelPos = FirePos.AddCopy(fromFacing);
		}
		else
		{
			FirePos = new BlockPos(tree.GetInt("fireposX"), tree.GetInt("fireposY"), tree.GetInt("fireposZ"));
			FuelPos = new BlockPos(tree.GetInt("fuelposX"), tree.GetInt("fuelposY"), tree.GetInt("fuelposZ"));
		}
		bool wasBurning = IsBurning;
		bool nowBurning = tree.GetBool("isBurning");
		if (nowBurning && !wasBurning)
		{
			startBurning();
		}
		if (!nowBurning && wasBurning)
		{
			KillFire(remainingBurnDuration <= 0f);
			IsBurning = nowBurning;
		}
		startedByPlayerUid = tree.GetString("startedByPlayerUid");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetFloat("remainingBurnDuration", remainingBurnDuration);
		tree.SetFloat("startDuration", startDuration);
		tree.SetBool("isBurning", IsBurning);
		tree.SetInt("fireposX", FirePos.X);
		tree.SetInt("fireposY", FirePos.Y);
		tree.SetInt("fireposZ", FirePos.Z);
		tree.SetInt("fuelposX", FuelPos.X);
		tree.SetInt("fuelposY", FuelPos.Y);
		tree.SetInt("fuelposZ", FuelPos.Z);
		if (startedByPlayerUid != null)
		{
			tree.SetString("startedByPlayerUid", startedByPlayerUid);
		}
	}

	public static Vec3d RandomBlockPos(IBlockAccessor blockAccess, BlockPos pos, Block block, BlockFacing facing = null)
	{
		if (facing == null)
		{
			Cuboidf[] selectionBoxes = block.GetSelectionBoxes(blockAccess, pos);
			Cuboidf box = ((selectionBoxes != null && selectionBoxes.Length != 0) ? selectionBoxes[0] : Block.DefaultCollisionBox);
			return new Vec3d((double)((float)pos.X + box.X1) + rand.NextDouble() * (double)box.XSize, (double)((float)pos.Y + box.Y1) + rand.NextDouble() * (double)box.YSize, (double)((float)pos.Z + box.Z1) + rand.NextDouble() * (double)box.ZSize);
		}
		Vec3i face = facing.Normali;
		Cuboidf[] collisionBoxes = block.GetCollisionBoxes(blockAccess, pos);
		bool haveCollisionBox = collisionBoxes != null && collisionBoxes.Length != 0;
		Vec3d basepos = new Vec3d((float)pos.X + 0.5f + (float)face.X / 1.95f + ((!haveCollisionBox || facing.Axis != 0) ? 0f : ((face.X > 0) ? (collisionBoxes[0].X2 - 1f) : collisionBoxes[0].X1)), (float)pos.Y + 0.5f + (float)face.Y / 1.95f + ((!haveCollisionBox || facing.Axis != EnumAxis.Y) ? 0f : ((face.Y > 0) ? (collisionBoxes[0].Y2 - 1f) : collisionBoxes[0].Y1)), (float)pos.Z + 0.5f + (float)face.Z / 1.95f + ((!haveCollisionBox || facing.Axis != EnumAxis.Z) ? 0f : ((face.Z > 0) ? (collisionBoxes[0].Z2 - 1f) : collisionBoxes[0].Z1)));
		Vec3d posVariance = new Vec3d(1f * (float)(1 - face.X), 1f * (float)(1 - face.Y), 1f * (float)(1 - face.Z));
		return new Vec3d(basepos.X + (rand.NextDouble() - 0.5) * posVariance.X, basepos.Y + (rand.NextDouble() - 0.5) * posVariance.Y, basepos.Z + (rand.NextDouble() - 0.5) * posVariance.Z);
	}
}
