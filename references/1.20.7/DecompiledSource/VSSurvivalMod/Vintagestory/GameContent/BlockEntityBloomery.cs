using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityBloomery : BlockEntity, IHeatSource
{
	private ILoadedSound ambientSound;

	private BloomeryContentsRenderer renderer;

	private static SimpleParticleProperties breakSparks;

	private static SimpleParticleProperties smallMetalSparks;

	private static SimpleParticleProperties smoke;

	private BlockFacing ownFacing;

	internal InventoryGeneric bloomeryInv;

	private bool burning;

	private double burningUntilTotalDays;

	private double burningStartTotalDays;

	private const int FuelCapacity = 6;

	public AssetLocation FuelSoundLocation => new AssetLocation("sounds/block/charcoal");

	public AssetLocation OreSoundLocation => new AssetLocation("sounds/block/loosestone");

	public bool IsBurning => burning;

	private ItemSlot FuelSlot => bloomeryInv[0];

	private ItemSlot OreSlot => bloomeryInv[1];

	private ItemSlot OutSlot => bloomeryInv[2];

	private ItemStack FuelStack => FuelSlot.Itemstack;

	private ItemStack OreStack => OreSlot.Itemstack;

	private ItemStack OutStack => OutSlot.Itemstack;

	private int OreCapacity => Ore2FuelRatio * 6;

	private int Ore2FuelRatio
	{
		get
		{
			int ratio = (OreStack?.Collectible.CombustibleProps?.SmeltedRatio).GetValueOrDefault(1);
			return (OreStack?.ItemAttributes?["bloomeryFuelRatio"].AsInt(ratio)).GetValueOrDefault(ratio);
		}
	}

	static BlockEntityBloomery()
	{
		smallMetalSparks = new SimpleParticleProperties(2f, 5f, ColorUtil.ToRgba(255, 255, 233, 83), new Vec3d(), new Vec3d(), new Vec3f(-3f, 8f, -3f), new Vec3f(3f, 12f, 3f), 0.1f, 1f, 0.25f, 0.25f, EnumParticleModel.Quad);
		smallMetalSparks.WithTerrainCollision = false;
		smallMetalSparks.VertexFlags = 128;
		smallMetalSparks.AddPos.Set(0.0625, 0.0, 0.0625);
		smallMetalSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.QUADRATIC, -0.5f);
		smallMetalSparks.AddPos.Set(0.25, 0.1875, 0.25);
		smallMetalSparks.ParticleModel = EnumParticleModel.Cube;
		smallMetalSparks.LifeLength = 0.04f;
		smallMetalSparks.MinQuantity = 1f;
		smallMetalSparks.AddQuantity = 1f;
		smallMetalSparks.MinSize = 0.2f;
		smallMetalSparks.MaxSize = 0.2f;
		smallMetalSparks.GravityEffect = 0f;
		breakSparks = new SimpleParticleProperties(40f, 80f, ColorUtil.ToRgba(255, 255, 233, 83), new Vec3d(), new Vec3d(), new Vec3f(-1f, 0.5f, -1f), new Vec3f(2f, 1.5f, 2f), 0.5f, 1f, 0.25f, 0.25f);
		breakSparks.VertexFlags = 128;
		breakSparks.AddPos.Set(0.25, 0.25, 0.25);
		breakSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
		smoke = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(128, 110, 110, 110), new Vec3d(), new Vec3d(), new Vec3f(-0.2f, 0.3f, -0.2f), new Vec3f(0.2f, 0.3f, 0.2f), 2f, 0f, 0.5f, 1f, EnumParticleModel.Quad);
		smoke.SelfPropelled = true;
		smoke.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255f);
		smoke.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 2f);
	}

	public BlockEntityBloomery()
	{
		bloomeryInv = new InventoryGeneric(3, "bloomery-1", null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		bloomeryInv.LateInitialize("bloomery-1", api);
		RegisterGameTickListener(OnGameTick, 100);
		updateSoundState();
		if (api.Side == EnumAppSide.Client)
		{
			ICoreClientAPI capi = (ICoreClientAPI)api;
			capi.Event.RegisterRenderer(renderer = new BloomeryContentsRenderer(Pos, capi), EnumRenderStage.Opaque, "bloomery");
			UpdateRenderer();
		}
		ownFacing = BlockFacing.FromCode(api.World.BlockAccessor.GetBlock(Pos).LastCodePart());
	}

	private void UpdateRenderer()
	{
		float fillLevel = Math.Min(14f, (float)FuelSlot.StackSize + 8f * (float)OreSlot.StackSize / (float)OreCapacity + (float)OutSlot.StackSize);
		renderer.SetFillLevel(fillLevel);
		double easinLevel = Math.Min(1.0, 24.0 * (Api.World.Calendar.TotalDays - burningStartTotalDays));
		double easeoutLevel = Math.Min(1.0, 24.0 * (burningUntilTotalDays - Api.World.Calendar.TotalDays));
		double glowLevel = Math.Max(0.0, Math.Min(easinLevel, easeoutLevel) * 250.0);
		renderer.glowLevel = (burning ? ((int)glowLevel) : 0);
	}

	public void updateSoundState()
	{
		if (burning)
		{
			startSound();
		}
		else
		{
			stopSound();
		}
	}

	public void startSound()
	{
		if (ambientSound == null)
		{
			ICoreAPI api = Api;
			if (api != null && api.Side == EnumAppSide.Client)
			{
				ambientSound = (Api as ICoreClientAPI).World.LoadSound(new SoundParams
				{
					Location = new AssetLocation("sounds/environment/fire.ogg"),
					ShouldLoop = true,
					Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
					DisposeOnFinish = false,
					Volume = 0.3f,
					Range = 8f
				});
				ambientSound.Start();
			}
		}
	}

	public void stopSound()
	{
		if (ambientSound != null)
		{
			ambientSound.Stop();
			ambientSound.Dispose();
			ambientSound = null;
		}
	}

	private void OnGameTick(float dt)
	{
		if (Api.Side == EnumAppSide.Client)
		{
			UpdateRenderer();
			if (burning)
			{
				EmitParticles();
			}
		}
		if (burning && Api.Side == EnumAppSide.Server && burningUntilTotalDays < Api.World.Calendar.TotalDays)
		{
			DoSmelt();
		}
	}

	private void DoSmelt()
	{
		CombustibleProperties combustProps = OreStack.Collectible.CombustibleProps;
		if (combustProps != null)
		{
			int q = OreStack.StackSize / combustProps.SmeltedRatio;
			JsonObject itemAttributes = OreStack.ItemAttributes;
			if (itemAttributes != null && itemAttributes.IsTrue("mergeUnitsInBloomery"))
			{
				OutSlot.Itemstack = combustProps.SmeltedStack.ResolvedItemstack.Clone();
				OutStack.StackSize = 1;
				float qf = (float)OreStack.StackSize / (float)combustProps.SmeltedRatio;
				OutStack.Attributes.SetFloat("units", qf * 100f);
			}
			else
			{
				OutSlot.Itemstack = combustProps.SmeltedStack.ResolvedItemstack.Clone();
				OutStack.StackSize *= q;
			}
			OutStack.Collectible.SetTemperature(Api.World, OutSlot.Itemstack, 900f);
			FuelSlot.Itemstack = null;
			OreStack.StackSize -= q * combustProps.SmeltedRatio;
			if (OreSlot.StackSize == 0)
			{
				OreSlot.Itemstack = null;
			}
			burning = false;
			burningUntilTotalDays = 0.0;
			MarkDirty();
		}
	}

	private void EmitParticles()
	{
		if (Api.World.Rand.Next(5) > 0)
		{
			smoke.MinPos.Set((double)Pos.X + 0.5 - 0.125, (float)(Pos.Y + 1) + 0.625f, (double)Pos.Z + 0.5 - 0.125);
			smoke.AddPos.Set(0.25, 0.0, 0.25);
			Api.World.SpawnParticles(smoke);
		}
		if (renderer.glowLevel > 80 && Api.World.Rand.Next(3) == 0)
		{
			Vec3f dir = ownFacing.Normalf;
			Vec3d particlePos = smallMetalSparks.MinPos;
			particlePos.Set((double)Pos.X + 0.5, Pos.Y, (double)Pos.Z + 0.5);
			particlePos.Sub((double)dir.X * 0.375 + 0.125, 0.0, (double)dir.Z * 0.375 + 0.125);
			smallMetalSparks.MinPos = particlePos;
			smallMetalSparks.VertexFlags = (byte)renderer.glowLevel;
			smallMetalSparks.MinVelocity = new Vec3f(-0.5f - dir.X, -0.3f, -0.5f - dir.Z);
			smallMetalSparks.AddVelocity = new Vec3f(1f - dir.X, 0.6f, 1f - dir.Z);
			Api.World.SpawnParticles(smallMetalSparks);
		}
	}

	public bool CanAdd(ItemStack stack, int quantity = 1)
	{
		if (IsBurning)
		{
			return false;
		}
		if (OutSlot.StackSize > 0)
		{
			return false;
		}
		CombustibleProperties combustProps = stack?.Collectible.CombustibleProps;
		if (combustProps == null)
		{
			return false;
		}
		if (combustProps.SmeltedStack != null && combustProps.MeltingPoint < 1500 && combustProps.MeltingPoint >= 1000)
		{
			if (OreSlot.StackSize + quantity > OreCapacity)
			{
				return false;
			}
			if (!OreSlot.Empty && !OreStack.Equals(Api.World, stack, GlobalConstants.IgnoredStackAttributes))
			{
				return false;
			}
			return true;
		}
		if (combustProps.BurnTemperature >= 1200 && combustProps.BurnDuration > 30f)
		{
			if (FuelSlot.StackSize + quantity > 6)
			{
				return false;
			}
			if (!FuelSlot.Empty && !FuelStack.Equals(Api.World, stack, GlobalConstants.IgnoredStackAttributes))
			{
				return false;
			}
			return true;
		}
		return false;
	}

	public bool TryAdd(IPlayer byPlayer, int quantity = 1)
	{
		ItemSlot sourceSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (IsBurning)
		{
			return false;
		}
		if (OutSlot.StackSize > 0)
		{
			return false;
		}
		if (sourceSlot.Itemstack == null)
		{
			return false;
		}
		CombustibleProperties combustProps = sourceSlot.Itemstack.Collectible.CombustibleProps;
		if (combustProps == null)
		{
			return true;
		}
		if (combustProps.SmeltedStack != null && combustProps.MeltingPoint < 1500 && combustProps.MeltingPoint >= 1000)
		{
			if (sourceSlot.TryPutInto(Api.World, OreSlot, Math.Min(OreCapacity - OreSlot.StackSize, quantity)) > 0)
			{
				MarkDirty();
				Api.World.PlaySoundAt(OreSoundLocation, Pos, 0.0, byPlayer);
				return true;
			}
			return false;
		}
		if (combustProps.BurnTemperature >= 1200 && combustProps.BurnDuration > 30f)
		{
			int maxRequired = (int)Math.Ceiling((float)OreSlot.StackSize / (float)Ore2FuelRatio);
			if (sourceSlot.TryPutInto(Api.World, FuelSlot, Math.Min(maxRequired - FuelSlot.StackSize, quantity)) > 0)
			{
				MarkDirty();
				Api.World.PlaySoundAt(FuelSoundLocation, Pos, 0.0, byPlayer);
				return true;
			}
			return false;
		}
		return false;
	}

	public bool TryIgnite()
	{
		if (!CanIgnite() || burning)
		{
			return false;
		}
		if (!Api.World.BlockAccessor.GetBlock(Pos.UpCopy()).Code.Path.Contains("bloomerychimney"))
		{
			return false;
		}
		burning = true;
		burningUntilTotalDays = Api.World.Calendar.TotalDays + 5.0 / 12.0;
		burningStartTotalDays = Api.World.Calendar.TotalDays;
		MarkDirty();
		updateSoundState();
		return true;
	}

	public bool CanIgnite()
	{
		if (!burning && FuelSlot.StackSize > 0 && OreSlot.StackSize > 0)
		{
			return (float)OreSlot.StackSize / (float)FuelSlot.StackSize <= (float)Ore2FuelRatio;
		}
		return false;
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		if (burning)
		{
			Vec3d dpos = Pos.ToVec3d().Add(0.5, 0.5, 0.5);
			bloomeryInv.DropSlots(dpos, 0, 2);
			breakSparks.MinPos = Pos.ToVec3d().AddCopy(dpos.X - 0.25, dpos.Y - 0.25, dpos.Z - 0.25);
			Api.World.SpawnParticles(breakSparks);
		}
		else
		{
			bloomeryInv.DropAll(Pos.ToVec3d().Add(0.5, 0.5, 0.5));
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		renderer?.Dispose();
		ambientSound?.Dispose();
	}

	public override void OnBlockRemoved()
	{
		renderer?.Dispose();
		ambientSound?.Dispose();
		base.OnBlockRemoved();
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		bloomeryInv.FromTreeAttributes(tree);
		burning = tree.GetInt("burning") > 0;
		burningUntilTotalDays = tree.GetDouble("burningUntilTotalDays");
		burningStartTotalDays = tree.GetDouble("burningStartTotalDays");
		updateSoundState();
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		bloomeryInv.ToTreeAttributes(tree);
		tree.SetInt("burning", burning ? 1 : 0);
		tree.SetDouble("burningUntilTotalDays", burningUntilTotalDays);
		tree.SetDouble("burningStartTotalDays", burningStartTotalDays);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (Api.World.EntityDebugMode && forPlayer != null && (forPlayer.WorldData?.CurrentGameMode).GetValueOrDefault() == EnumGameMode.Creative)
		{
			dsc.AppendLine(string.Format("Burning: {3}, Current total days: {0}, BurningStart total days: {1}, BurningUntil total days: {2}", Api.World.Calendar.TotalDays, burningStartTotalDays, burningUntilTotalDays, burning));
		}
		for (int i = 0; i < 3; i++)
		{
			ItemStack stack = bloomeryInv[i].Itemstack;
			if (stack != null)
			{
				if (dsc.Length == 0)
				{
					dsc.AppendLine(Lang.Get("Contents:"));
				}
				dsc.AppendLine("  " + stack.StackSize + "x " + stack.GetName());
			}
		}
		base.GetBlockInfo(forPlayer, dsc);
	}

	public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
	{
		return IsBurning ? 7 : 0;
	}
}
