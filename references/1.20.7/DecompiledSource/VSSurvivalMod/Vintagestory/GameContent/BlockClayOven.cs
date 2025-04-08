using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockClayOven : Block, IIgnitable
{
	private WorldInteraction[] interactions;

	private AdvancedParticleProperties[] particles;

	private Vec3f[] basePos;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		ICoreClientAPI capi = api as ICoreClientAPI;
		if (capi != null)
		{
			interactions = ObjectCacheUtil.GetOrCreate(api, "ovenInteractions", delegate
			{
				List<ItemStack> list = new List<ItemStack>();
				List<ItemStack> fuelStacklist = new List<ItemStack>();
				List<ItemStack> list2 = BlockBehaviorCanIgnite.CanIgniteStacks(api, withFirestarter: true);
				foreach (CollectibleObject current in api.World.Collectibles)
				{
					JsonObject attributes = current.Attributes;
					if (attributes != null && attributes.IsTrue("isClayOvenFuel"))
					{
						List<ItemStack> handBookStacks = current.GetHandBookStacks(capi);
						if (handBookStacks != null)
						{
							fuelStacklist.AddRange(handBookStacks);
						}
					}
					else
					{
						if (current.Attributes?["bakingProperties"]?.AsObject<BakingProperties>() == null)
						{
							CombustibleProperties combustibleProps = current.CombustibleProps;
							if (combustibleProps == null || combustibleProps.SmeltingType != EnumSmeltType.Bake || current.CombustibleProps.SmeltedStack == null || current.CombustibleProps.MeltingPoint >= 260)
							{
								continue;
							}
						}
						List<ItemStack> handBookStacks2 = current.GetHandBookStacks(capi);
						if (handBookStacks2 != null)
						{
							list.AddRange(handBookStacks2);
						}
					}
				}
				return new WorldInteraction[3]
				{
					new WorldInteraction
					{
						ActionLangCode = "blockhelp-oven-bakeable",
						HotKeyCode = null,
						MouseButton = EnumMouseButton.Right,
						Itemstacks = list.ToArray(),
						GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
						{
							if (wi.Itemstacks.Length == 0)
							{
								return (ItemStack[])null;
							}
							return (!(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityOven blockEntityOven3)) ? null : blockEntityOven3.CanAdd(wi.Itemstacks);
						}
					},
					new WorldInteraction
					{
						ActionLangCode = "blockhelp-oven-fuel",
						HotKeyCode = null,
						MouseButton = EnumMouseButton.Right,
						Itemstacks = fuelStacklist.ToArray(),
						GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (!(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityOven blockEntityOven2)) ? null : blockEntityOven2.CanAddAsFuel(fuelStacklist.ToArray())
					},
					new WorldInteraction
					{
						ActionLangCode = "blockhelp-oven-ignite",
						MouseButton = EnumMouseButton.Right,
						HotKeyCode = "shift",
						Itemstacks = list2.ToArray(),
						GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
						{
							if (wi.Itemstacks.Length == 0)
							{
								return (ItemStack[])null;
							}
							return (!(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityOven blockEntityOven) || !blockEntityOven.CanIgnite()) ? null : wi.Itemstacks;
						}
					}
				};
			});
		}
		InitializeParticles();
	}

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return true;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection bs)
	{
		if (world.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityOven beo)
		{
			return beo.OnInteract(byPlayer, bs);
		}
		return base.OnBlockInteractStart(world, byPlayer, bs);
	}

	EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
	{
		if ((byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityOven).IsBurning)
		{
			if (!(secondsIgniting > 2f))
			{
				return EnumIgniteState.Ignitable;
			}
			return EnumIgniteState.IgniteNow;
		}
		return EnumIgniteState.NotIgnitable;
	}

	public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
	{
		if (!(byEntity.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityOven beo) || !beo.CanIgnite())
		{
			return EnumIgniteState.NotIgnitablePreventDefault;
		}
		if (!(secondsIgniting > 4f))
		{
			return EnumIgniteState.Ignitable;
		}
		return EnumIgniteState.IgniteNow;
	}

	public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		(byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityOven)?.TryIgnite();
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
	{
		if (manager.BlockAccess.GetBlockEntity(pos) is BlockEntityOven { IsBurning: not false } beo)
		{
			beo.RenderParticleTick(manager, pos, windAffectednessAtPos, secondsTicking, particles);
		}
		base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
	}

	private void InitializeParticles()
	{
		particles = new AdvancedParticleProperties[16];
		basePos = new Vec3f[particles.Length];
		Cuboidf[] spawnBoxes = new Cuboidf[4]
		{
			new Cuboidf(0.125f, 0f, 0.125f, 0.3125f, 0.5f, 0.875f),
			new Cuboidf(0.7125f, 0f, 0.125f, 0.875f, 0.5f, 0.875f),
			new Cuboidf(0.125f, 0f, 0.125f, 0.875f, 0.5f, 0.3125f),
			new Cuboidf(0.125f, 0f, 0.7125f, 0.875f, 0.5f, 0.875f)
		};
		for (int l = 0; l < 4; l++)
		{
			AdvancedParticleProperties props4 = ParticleProperties[0].Clone();
			Cuboidf box = spawnBoxes[l];
			basePos[l] = new Vec3f(0f, 0f, 0f);
			props4.PosOffset[0].avg = box.MidX;
			props4.PosOffset[0].var = box.Width / 2f;
			props4.PosOffset[1].avg = 0.3f;
			props4.PosOffset[1].var = 0.05f;
			props4.PosOffset[2].avg = box.MidZ;
			props4.PosOffset[2].var = box.Length / 2f;
			props4.Quantity.avg = 0.5f;
			props4.Quantity.var = 0.2f;
			props4.LifeLength.avg = 0.8f;
			particles[l] = props4;
		}
		for (int k = 4; k < 8; k++)
		{
			AdvancedParticleProperties props3 = ParticleProperties[1].Clone();
			props3.PosOffset[1].avg = 0.06f;
			props3.PosOffset[1].var = 0.02f;
			props3.Quantity.avg = 0.5f;
			props3.Quantity.var = 0.2f;
			props3.LifeLength.avg = 0.3f;
			props3.VertexFlags = 128;
			particles[k] = props3;
		}
		for (int j = 8; j < 12; j++)
		{
			AdvancedParticleProperties props2 = ParticleProperties[2].Clone();
			props2.PosOffset[1].avg = 0.09f;
			props2.PosOffset[1].var = 0.02f;
			props2.Quantity.avg = 0.5f;
			props2.Quantity.var = 0.2f;
			props2.LifeLength.avg = 0.18f;
			props2.VertexFlags = 192;
			particles[j] = props2;
		}
		for (int i = 12; i < 16; i++)
		{
			AdvancedParticleProperties props = ParticleProperties[3].Clone();
			props.PosOffset[1].avg = 0.12f;
			props.PosOffset[1].var = 0.03f;
			props.Quantity.avg = 0.2f;
			props.Quantity.var = 0.1f;
			props.LifeLength.avg = 0.12f;
			props.VertexFlags = 255;
			particles[i] = props;
		}
	}
}
