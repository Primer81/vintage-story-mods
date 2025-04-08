using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockToolMold : Block
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		string createdByText = "metalmolding";
		JsonObject attributes = Attributes;
		if (attributes != null && attributes["createdByText"].Exists)
		{
			createdByText = Attributes["createdByText"].AsString();
		}
		JsonObject attributes2 = Attributes;
		if (attributes2 != null && attributes2["drop"].Exists)
		{
			JsonItemStack ojstack = Attributes["drop"].AsObject<JsonItemStack>();
			if (ojstack != null)
			{
				MetalProperty metals = api.Assets.TryGet("worldproperties/block/metal.json").ToObject<MetalProperty>();
				for (int i = 0; i < metals.Variants.Length; i++)
				{
					string metaltype = metals.Variants[i].Code.Path;
					string tooltype = LastCodePart();
					JsonItemStack jstack = ojstack.Clone();
					jstack.Code.Path = jstack.Code.Path.Replace("{tooltype}", tooltype).Replace("{metal}", metaltype);
					CollectibleObject collObj = ((jstack.Type != 0) ? ((CollectibleObject)api.World.GetItem(jstack.Code)) : ((CollectibleObject)api.World.GetBlock(jstack.Code)));
					if (collObj == null)
					{
						continue;
					}
					JsonObject attributes3 = collObj.Attributes;
					if (attributes3 == null || !attributes3["handbook"].Exists)
					{
						if (collObj.Attributes == null)
						{
							collObj.Attributes = new JsonObject(JToken.Parse("{ handbook: {} }"));
						}
						else
						{
							collObj.Attributes.Token["handbook"] = JToken.Parse("{ }");
						}
					}
					collObj.Attributes["handbook"].Token["createdBy"] = JToken.FromObject(createdByText);
				}
			}
		}
		interactions = ObjectCacheUtil.GetOrCreate(api, "toolmoldBlockInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject current in api.World.Collectibles)
			{
				if (current is BlockSmeltedContainer)
				{
					list.Add(new ItemStack(current));
				}
			}
			return new WorldInteraction[3]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-toolmold-pour",
					HotKeyCode = "shift",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (!(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityToolMold { IsFull: false })) ? null : wi.Itemstacks
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-toolmold-takeworkitem",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right,
					ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityToolMold { IsFull: not false } blockEntityToolMold2 && blockEntityToolMold2.IsHardened
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-toolmold-pickup",
					HotKeyCode = null,
					RequireFreeHand = true,
					MouseButton = EnumMouseButton.Right,
					ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityToolMold blockEntityToolMold && blockEntityToolMold.MetalContent == null
				}
			};
		});
	}

	public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
	{
		if (world.Rand.NextDouble() < 0.05)
		{
			BlockEntityToolMold blockEntity = GetBlockEntity<BlockEntityToolMold>(pos);
			if (blockEntity != null && blockEntity.Temperature > 300f)
			{
				entity.ReceiveDamage(new DamageSource
				{
					Source = EnumDamageSource.Block,
					SourceBlock = this,
					Type = EnumDamageType.Fire,
					SourcePos = pos.ToVec3d()
				}, 0.5f);
			}
		}
		base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
	}

	public override float GetTraversalCost(BlockPos pos, EnumAICreatureType creatureType)
	{
		if (creatureType == EnumAICreatureType.LandCreature || creatureType == EnumAICreatureType.Humanoid)
		{
			BlockEntityIngotMold be = GetBlockEntity<BlockEntityIngotMold>(pos);
			if (be == null)
			{
				return 0f;
			}
			if (be.TemperatureLeft > 300f || be.TemperatureRight > 300f)
			{
				return 10000f;
			}
		}
		return 0f;
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null)
		{
			base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}
		BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position.AddCopy(blockSel.Face.Opposite));
		IPlayer byPlayer = null;
		if (byEntity is EntityPlayer)
		{
			byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		if (byPlayer != null && be is BlockEntityToolMold && ((BlockEntityToolMold)be).OnPlayerInteract(byPlayer, blockSel.Face, blockSel.HitPosition))
		{
			handHandling = EnumHandHandling.PreventDefault;
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (blockSel == null)
		{
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityToolMold be)
		{
			return be.OnPlayerInteract(byPlayer, blockSel.Face, blockSel.HitPosition);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			failureCode = "onlywhensneaking";
			return false;
		}
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		if (world.BlockAccessor.GetBlock(blockSel.Position.DownCopy()).CanAttachBlockAt(world.BlockAccessor, this, blockSel.Position.DownCopy(), BlockFacing.UP))
		{
			return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
		}
		failureCode = "requiresolidground";
		return false;
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		List<ItemStack> stacks = new List<ItemStack>();
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityToolMold bet)
		{
			if (!bet.Shattered)
			{
				stacks.Add(new ItemStack(world.BlockAccessor.GetBlock(CodeWithVariant("side", "north"))));
			}
			ItemStack[] outstack = bet.GetStateAwareMoldedStacks();
			if (outstack != null)
			{
				stacks.AddRange(outstack);
			}
		}
		else
		{
			stacks.Add(new ItemStack(world.BlockAccessor.GetBlock(CodeWithVariant("side", "north"))));
		}
		return stacks.ToArray();
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
