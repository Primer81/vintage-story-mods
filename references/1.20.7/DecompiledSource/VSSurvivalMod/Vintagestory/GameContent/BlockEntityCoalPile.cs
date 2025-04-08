using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityCoalPile : BlockEntityItemPile, ITexPositionSource, IHeatSource
{
	private static SimpleParticleProperties smokeParticles;

	private static SimpleParticleProperties smallMetalSparks;

	private bool burning;

	private double burnStartTotalHours;

	private ICoreClientAPI capi;

	private ILoadedSound ambientSound;

	private float cokeConversionRate;

	public float BurnHoursPerLayer = 4f;

	private long listenerId;

	private static BlockFacing[] facings;

	private bool isCokable;

	public override AssetLocation SoundLocation => new AssetLocation("sounds/block/charcoal");

	public override string BlockCode => "coalpile";

	public override int MaxStackSize => 16;

	public override int DefaultTakeQuantity => 2;

	public override int BulkTakeQuantity => 2;

	public int Layers
	{
		get
		{
			if (inventory[0].StackSize != 1)
			{
				return inventory[0].StackSize / 2;
			}
			return 1;
		}
	}

	public bool IsBurning => burning;

	public bool CanIgnite => !burning;

	public int BurnTemperature => inventory[0].Itemstack.Collectible.CombustibleProps.BurnTemperature;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (IsBurning)
			{
				return capi.BlockTextureAtlas.Positions[capi.World.GetBlock(new AssetLocation("ember")).FirstTextureInventory.Baked.TextureSubId];
			}
			string itemcode = inventory[0].Itemstack.Collectible.Code.Path;
			return capi.BlockTextureAtlas.Positions[base.Block.Textures[itemcode].Baked.TextureSubId];
		}
	}

	static BlockEntityCoalPile()
	{
		facings = (BlockFacing[])BlockFacing.ALLFACES.Clone();
		smokeParticles = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(150, 40, 40, 40), new Vec3d(), new Vec3d(1.0, 0.0, 1.0), new Vec3f(-1f / 32f, 0.1f, -1f / 32f), new Vec3f(1f / 32f, 0.1f, 1f / 32f), 2f, -1f / 160f, 0.2f, 1f, EnumParticleModel.Quad);
		smokeParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
		smokeParticles.SelfPropelled = true;
		smokeParticles.AddPos.Set(1.0, 0.0, 1.0);
		smallMetalSparks = new SimpleParticleProperties(0.2f, 1f, ColorUtil.ToRgba(255, 255, 150, 0), new Vec3d(), new Vec3d(), new Vec3f(-2f, 2f, -2f), new Vec3f(2f, 5f, 2f), 0.04f, 1f, 0.2f, 0.25f);
		smallMetalSparks.WithTerrainCollision = false;
		smallMetalSparks.VertexFlags = 150;
		smallMetalSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.2f);
		smallMetalSparks.SelfPropelled = true;
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		capi = api as ICoreClientAPI;
		updateBurningState();
	}

	public void TryIgnite()
	{
		if (!burning)
		{
			burning = true;
			burnStartTotalHours = Api.World.Calendar.TotalHours;
			MarkDirty();
			updateBurningState();
		}
	}

	public void Extinguish()
	{
		if (burning)
		{
			burning = false;
			UnregisterGameTickListener(listenerId);
			listenerId = 0L;
			MarkDirty(redrawOnClient: true);
			Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, 0.0, null, randomizePitch: false, 16f);
		}
	}

	private void updateBurningState()
	{
		if (!burning)
		{
			return;
		}
		if (Api.World.Side == EnumAppSide.Client)
		{
			if (ambientSound == null || !ambientSound.IsPlaying)
			{
				ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams
				{
					Location = new AssetLocation("sounds/effect/embers.ogg"),
					ShouldLoop = true,
					Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
					DisposeOnFinish = false,
					Volume = 1f
				});
				if (ambientSound != null)
				{
					ambientSound.PlaybackPosition = ambientSound.SoundLengthSeconds * (float)Api.World.Rand.NextDouble();
					ambientSound.Start();
				}
			}
			listenerId = RegisterGameTickListener(onBurningTickClient, 100);
		}
		else
		{
			listenerId = RegisterGameTickListener(onBurningTickServer, 10000);
		}
	}

	public static void SpawnBurningCoalParticles(ICoreAPI api, Vec3d pos, float addX = 1f, float addZ = 1f)
	{
		smokeParticles.MinQuantity = 0.25f;
		smokeParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -15f);
		smokeParticles.AddQuantity = 0f;
		smokeParticles.MinPos.Set(pos.X, pos.Y - 0.10000000149011612, pos.Z);
		smokeParticles.AddPos.Set(addX, 0.0, addZ);
		smallMetalSparks.MinPos.Set(pos.X, pos.Y, pos.Z);
		smallMetalSparks.AddPos.Set(addX, 0.10000000149011612, addZ);
		api.World.SpawnParticles(smallMetalSparks);
		int g = 30 + api.World.Rand.Next(30);
		smokeParticles.Color = ColorUtil.ToRgba(150, g, g, g);
		api.World.SpawnParticles(smokeParticles);
	}

	private void onBurningTickClient(float dt)
	{
		if (burning && Api.World.Rand.NextDouble() < 0.93)
		{
			if (isCokable)
			{
				smokeParticles.MinQuantity = 1f;
				smokeParticles.AddQuantity = 0f;
				smokeParticles.MinPos.Set(Pos.X, (float)(Pos.Y + 2) + 0.0625f, Pos.Z);
				int g = 30 + Api.World.Rand.Next(30);
				smokeParticles.Color = ColorUtil.ToRgba(150, g, g, g);
				Api.World.SpawnParticles(smokeParticles);
			}
			else
			{
				SpawnBurningCoalParticles(Api, Pos.ToVec3d().Add(0.0, (float)Layers / 8f, 0.0));
			}
		}
	}

	public float GetHoursLeft(double startTotalHours)
	{
		double totalHoursPassed = startTotalHours - burnStartTotalHours;
		return (float)((double)((float)inventory[0].StackSize / 2f * BurnHoursPerLayer) - totalHoursPassed);
	}

	private void onBurningTickServer(float dt)
	{
		facings.Shuffle(Api.World.Rand);
		BlockFacing[] array = facings;
		foreach (BlockFacing val in array)
		{
			BlockEntity blockEntity = Api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(val));
			if (blockEntity is BlockEntityCoalPile becp)
			{
				becp.TryIgnite();
				if (Api.World.Rand.NextDouble() < 0.75)
				{
					break;
				}
			}
			else if (blockEntity is BlockEntityGroundStorage besg)
			{
				besg.TryIgnite();
				if (Api.World.Rand.NextDouble() < 0.75)
				{
					break;
				}
			}
		}
		cokeConversionRate = inventory[0].Itemstack.ItemAttributes?["cokeConversionRate"].AsFloat() ?? 0f;
		if (cokeConversionRate > 0f && (isCokable = TestCokable()))
		{
			if (Api.World.Calendar.TotalHours - burnStartTotalHours > 12.0)
			{
				inventory[0].Itemstack = new ItemStack(Api.World.GetItem(new AssetLocation("coke")), (int)((float)inventory[0].StackSize * cokeConversionRate));
				burning = false;
				UnregisterGameTickListener(listenerId);
				listenerId = 0L;
				MarkDirty(redrawOnClient: true);
			}
			else
			{
				MarkDirty();
			}
			return;
		}
		bool changed = false;
		while (Api.World.Calendar.TotalHours - burnStartTotalHours > (double)(BurnHoursPerLayer / 2f))
		{
			burnStartTotalHours += BurnHoursPerLayer / 2f;
			inventory[0].TakeOut(1);
			if (inventory[0].Empty)
			{
				Api.World.BlockAccessor.SetBlock(0, Pos);
				break;
			}
			changed = true;
		}
		if (changed)
		{
			MarkDirty(redrawOnClient: true);
		}
	}

	private bool TestCokable()
	{
		IBlockAccessor bl = Api.World.BlockAccessor;
		bool haveDoor = false;
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing facing in hORIZONTALS)
		{
			Block block2 = bl.GetBlock(Pos.AddCopy(facing));
			haveDoor |= block2 is BlockCokeOvenDoor && block2.Variant["state"] == "closed";
		}
		int centerCount = 0;
		int cornerCount = 0;
		bl.WalkBlocks(Pos.AddCopy(-1, -1, -1), Pos.AddCopy(1, 1, 1), delegate(Block block, int x, int y, int z)
		{
			int num = Math.Abs(Pos.X - x);
			int num2 = Math.Abs(Pos.Z - z);
			bool flag = num == 1 && num2 == 1;
			JsonObject attributes = block.Attributes;
			if (attributes != null && attributes["cokeOvenViable"].AsBool())
			{
				centerCount += ((!flag) ? 1 : 0);
				cornerCount += (flag ? 1 : 0);
			}
		});
		if (haveDoor && centerCount >= 12 && cornerCount >= 8)
		{
			return bl.GetBlock(Pos.UpCopy()).Attributes?["cokeOvenViable"].AsBool() ?? false;
		}
		return false;
	}

	public override bool OnPlayerInteract(IPlayer byPlayer)
	{
		if (burning && !byPlayer.Entity.Controls.ShiftKey)
		{
			return false;
		}
		bool result = base.OnPlayerInteract(byPlayer);
		TriggerPileChanged();
		return result;
	}

	private void TriggerPileChanged()
	{
		if (Api.Side != EnumAppSide.Server)
		{
			return;
		}
		int maxSteepness = 4;
		BlockCoalPile belowcoalpile = Api.World.BlockAccessor.GetBlock(Pos.DownCopy()) as BlockCoalPile;
		int belowwlayers = belowcoalpile?.GetLayercount(Api.World, Pos.DownCopy()) ?? 0;
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing face in hORIZONTALS)
		{
			BlockPos npos = Pos.AddCopy(face);
			Block nblock = Api.World.BlockAccessor.GetBlock(npos);
			BlockCoalPile obj = Api.World.BlockAccessor.GetBlock(npos) as BlockCoalPile;
			int neighbourLayers = obj?.GetLayercount(Api.World, npos) ?? 0;
			int nearbyCollapsibleCount = ((nblock.Replaceable > 6000) ? (Layers - maxSteepness) : 0);
			int nearbyToPileCollapsibleCount = ((obj != null) ? (Layers - neighbourLayers - maxSteepness) : 0);
			BlockCoalPile nbelowblockcoalpile = Api.World.BlockAccessor.GetBlock(npos.DownCopy()) as BlockCoalPile;
			int nbelowwlayers = nbelowblockcoalpile?.GetLayercount(Api.World, npos.DownCopy()) ?? 0;
			int selfTallPileCollapsibleCount = ((belowcoalpile != null && nbelowblockcoalpile != null) ? (Layers + belowwlayers - nbelowwlayers - maxSteepness) : 0);
			int collapsibleLayerCount = GameMath.Max(nearbyCollapsibleCount, nearbyToPileCollapsibleCount, selfTallPileCollapsibleCount);
			if (Api.World.Rand.NextDouble() < (double)((float)collapsibleLayerCount / (float)maxSteepness) && TryPartialCollapse(npos.UpCopy(), 2))
			{
				break;
			}
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		bool wasBurning = burning;
		burning = tree.GetBool("burning");
		burnStartTotalHours = tree.GetDouble("lastTickTotalHours");
		isCokable = tree.GetBool("isCokable");
		if (!burning)
		{
			if (listenerId != 0L)
			{
				UnregisterGameTickListener(listenerId);
				listenerId = 0L;
			}
			ambientSound?.Stop();
			listenerId = 0L;
		}
		if (Api != null && Api.Side == EnumAppSide.Client && !wasBurning && burning)
		{
			updateBurningState();
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("burning", burning);
		tree.SetDouble("lastTickTotalHours", burnStartTotalHours);
		tree.SetBool("isCokable", isCokable);
	}

	public bool MergeWith(TreeAttribute blockEntityAttributes)
	{
		InventoryGeneric otherinv = new InventoryGeneric(1, BlockCode, null, null, null);
		otherinv.FromTreeAttributes(blockEntityAttributes.GetTreeAttribute("inventory"));
		otherinv.Api = Api;
		otherinv.ResolveBlocksOrItems();
		if (!inventory[0].Empty && otherinv[0].Itemstack.Equals(Api.World, inventory[0].Itemstack, GlobalConstants.IgnoredStackAttributes))
		{
			int quantityToMove = Math.Min(otherinv[0].StackSize, Math.Max(0, MaxStackSize - inventory[0].StackSize));
			inventory[0].Itemstack.StackSize += quantityToMove;
			otherinv[0].TakeOut(quantityToMove);
			if (otherinv[0].StackSize > 0)
			{
				BlockPos uppos = Pos.UpCopy();
				if (Api.World.BlockAccessor.GetBlock(uppos).Replaceable > 6000)
				{
					((IBlockItemPile)base.Block).Construct(otherinv[0], Api.World, uppos, null);
				}
			}
			MarkDirty(redrawOnClient: true);
			TriggerPileChanged();
		}
		return true;
	}

	private bool TryPartialCollapse(BlockPos pos, int quantity)
	{
		if (inventory[0].Empty)
		{
			return false;
		}
		IWorldAccessor world = Api.World;
		if (world.Side == EnumAppSide.Server && !((world as IServerWorldAccessor).Api as ICoreServerAPI).Server.Config.AllowFallingBlocks)
		{
			return false;
		}
		if ((IsReplacableBeneath(world, pos) || IsReplacableBeneathAndSideways(world, pos)) && world.GetNearestEntity(pos.ToVec3d().Add(0.5, 0.5, 0.5), 1f, 1.5f, (Entity e) => e is EntityBlockFalling && ((EntityBlockFalling)e).initialPos.Equals(pos)) == null)
		{
			ItemStack fallingStack = inventory[0].TakeOut(quantity);
			ItemStack remainingStack = inventory[0].Itemstack;
			inventory[0].Itemstack = fallingStack;
			EntityBlockFalling entityblock = new EntityBlockFalling(base.Block, this, pos, null, 0f, canFallSideways: true, 0.5f);
			entityblock.maxSpawnHeightForParticles = 0.3f;
			entityblock.DoRemoveBlock = false;
			world.SpawnEntity(entityblock);
			entityblock.ServerPos.Y -= 0.25;
			entityblock.Pos.Y -= 0.25;
			inventory[0].Itemstack = remainingStack;
			if (inventory.Empty)
			{
				Api.World.BlockAccessor.SetBlock(0, Pos);
			}
			return true;
		}
		return false;
	}

	private bool IsReplacableBeneathAndSideways(IWorldAccessor world, BlockPos pos)
	{
		for (int i = 0; i < 4; i++)
		{
			BlockFacing facing = BlockFacing.HORIZONTALS[i];
			Block nBlock = world.BlockAccessor.GetBlockOrNull(pos.X + facing.Normali.X, pos.Y + facing.Normali.Y, pos.Z + facing.Normali.Z);
			Block nBBlock = world.BlockAccessor.GetBlockOrNull(pos.X + facing.Normali.X, pos.Y + facing.Normali.Y - 1, pos.Z + facing.Normali.Z);
			if (nBlock != null && nBBlock != null && nBlock.Replaceable >= 6000 && nBBlock.Replaceable >= 6000)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsReplacableBeneath(IWorldAccessor world, BlockPos pos)
	{
		Block bottomBlock = world.BlockAccessor.GetBlockBelow(pos);
		if (bottomBlock != null)
		{
			return bottomBlock.Replaceable > 6000;
		}
		return false;
	}

	public void GetDecalMesh(ITexPositionSource decalTexSource, out MeshData meshdata)
	{
		int size = Layers * 2;
		Shape shape = capi.TesselatorManager.GetCachedShape(new AssetLocation("block/basic/layers/" + GameMath.Clamp(size, 2, 16) + "voxel"));
		capi.Tesselator.TesselateShape("coalpile", shape, out meshdata, decalTexSource, null, 0, 0, 0);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		lock (inventoryLock)
		{
			if (!inventory[0].Empty)
			{
				int size = Layers * 2;
				if (mesher is EntityBlockFallingRenderer)
				{
					size = 2;
				}
				Shape shape = capi.TesselatorManager.GetCachedShape(new AssetLocation("block/basic/layers/" + GameMath.Clamp(size, 2, 16) + "voxel"));
				capi.Tesselator.TesselateShape("coalpile", shape, out var meshdata, this, null, 0, 0, 0);
				if (burning)
				{
					for (int i = 0; i < meshdata.FlagsCount; i++)
					{
						meshdata.Flags[i] |= 196;
					}
				}
				mesher.AddMeshData(meshdata);
			}
		}
		return true;
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		ambientSound?.Dispose();
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		if (!burning)
		{
			base.OnBlockBroken(byPlayer);
		}
		ambientSound?.Dispose();
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		ambientSound?.Dispose();
	}

	public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
	{
		return (IsBurning && !isCokable) ? 10 : 0;
	}
}
