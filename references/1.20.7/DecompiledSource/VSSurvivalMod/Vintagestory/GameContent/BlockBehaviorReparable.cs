using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class BlockBehaviorReparable : BlockBehavior
{
	public BlockBehaviorReparable(Block block)
		: base(block)
	{
	}

	public virtual void Initialize(string type, BEBehaviorShapeFromAttributes bec)
	{
		BlockShapeFromAttributes clutterBlock = bec.clutterBlock;
		IShapeTypeProps cprops = clutterBlock.GetTypeProps(type, null, bec);
		if (cprops != null)
		{
			int reparability = cprops.Reparability;
			if (reparability == 0)
			{
				reparability = clutterBlock.Attributes["reparability"].AsInt();
			}
			bec.reparability = reparability;
			if (reparability == 1)
			{
				bec.repairState = 1f;
			}
		}
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		BEBehaviorShapeFromAttributes bec = block.GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
		if (bec == null || ShatterWhenBroken(world, bec, GetRule(world)))
		{
			if (byPlayer is IServerPlayer splr)
			{
				splr.SendLocalisedMessage(GlobalConstants.GeneralChatGroup, "clutter-didshatter", Lang.GetMatchingL(splr.LanguageCode, bec.GetFullCode()));
			}
			world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), pos, 0.0, null, randomizePitch: false, 12f);
			return new ItemStack[0];
		}
		ItemStack stack = block.OnPickBlock(world, pos);
		stack.Attributes.SetBool("collected", value: true);
		return new ItemStack[1] { stack };
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		BEBehaviorShapeFromAttributes bec = block.GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
		if (bec == null || bec.Collected)
		{
			return base.GetPlacedBlockInfo(world, pos, forPlayer);
		}
		if (world.Claims.TestAccess(forPlayer, pos, EnumBlockAccessFlags.BuildOrBreak) != 0)
		{
			return "";
		}
		EnumClutterDropRule rule = GetRule(world);
		if (rule == EnumClutterDropRule.Reparable)
		{
			if (bec.reparability > 0)
			{
				int repairLevel = GameMath.Clamp((int)(bec.repairState * 100.001f), 0, 100);
				if (repairLevel < 100)
				{
					return Lang.Get("clutter-reparable") + "\n" + Lang.Get("{0}% repaired", repairLevel) + "\n";
				}
				return Lang.Get("clutter-fullyrepaired", repairLevel) + "\n";
			}
			if (bec.reparability < 0)
			{
				return Lang.Get("clutter-willshatter") + "\n";
			}
		}
		if (rule == EnumClutterDropRule.NeverObtain)
		{
			return Lang.Get("clutter-willshatter") + "\n";
		}
		return "";
	}

	public virtual bool ShatterWhenBroken(IWorldAccessor world, BEBehaviorShapeFromAttributes bec, EnumClutterDropRule configRule)
	{
		if (bec.Collected)
		{
			return false;
		}
		return configRule switch
		{
			EnumClutterDropRule.NeverObtain => true, 
			EnumClutterDropRule.AlwaysObtain => false, 
			EnumClutterDropRule.Reparable => world.Rand.NextDouble() > (double)bec.repairState, 
			_ => true, 
		};
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		if (world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			_ = byPlayer.Entity;
			ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
			float repairQuantity = GetItemRepairAmount(world, slot);
			if (repairQuantity > 0f)
			{
				EnumClutterDropRule rule = GetRule(world);
				string message = null;
				string parameter = null;
				if (rule == EnumClutterDropRule.Reparable)
				{
					BEBehaviorShapeFromAttributes bec = block.GetBEBehavior<BEBehaviorShapeFromAttributes>(blockSel.Position);
					if (bec == null)
					{
						message = "clutter-error";
					}
					else if (bec.repairState < 1f && bec.reparability > 1)
					{
						if (repairQuantity < 0.001f)
						{
							message = "clutter-gluehardened";
						}
						else
						{
							bec.repairState += repairQuantity * 5f / (float)(bec.reparability - 1);
							if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
							{
								if (slot.Itemstack.Collectible is IBlockMealContainer mc)
								{
									float servingsLeft = mc.GetQuantityServings(world, slot.Itemstack) - 1f;
									mc.SetQuantityServings(world, slot.Itemstack, servingsLeft);
									if (servingsLeft <= 0f)
									{
										string emptyCode = slot.Itemstack.Collectible.Attributes["emptiedBlockCode"].AsString();
										if (emptyCode != null)
										{
											Block emptyPotBlock = world.GetBlock(new AssetLocation(emptyCode));
											if (emptyPotBlock != null)
											{
												slot.Itemstack = new ItemStack(emptyPotBlock);
											}
										}
									}
									slot.MarkDirty();
								}
								else
								{
									slot.TakeOut(1);
								}
							}
							message = "clutter-repaired";
							parameter = bec.GetFullCode();
							if (world.Side == EnumAppSide.Client)
							{
								AssetLocation sound = AssetLocation.Create("sounds/player/gluerepair");
								world.PlaySoundAt(sound, blockSel.Position, 0.0, byPlayer, randomizePitch: true, 8f);
							}
						}
					}
					else
					{
						message = "clutter-norepair";
					}
				}
				else
				{
					message = ((rule == EnumClutterDropRule.AlwaysObtain) ? "clutter-alwaysobtain" : "clutter-neverobtain");
				}
				if (byPlayer is IServerPlayer splr && message != null)
				{
					if (parameter == null)
					{
						splr.SendLocalisedMessage(GlobalConstants.GeneralChatGroup, message);
					}
					else
					{
						splr.SendLocalisedMessage(GlobalConstants.GeneralChatGroup, message, Lang.GetMatchingL(splr.LanguageCode, parameter));
					}
				}
				handling = EnumHandling.Handled;
				return true;
			}
		}
		handling = EnumHandling.PassThrough;
		return false;
	}

	private float GetItemRepairAmount(IWorldAccessor world, ItemSlot slot)
	{
		if (slot.Empty)
		{
			return 0f;
		}
		ItemStack stack = slot.Itemstack;
		JsonObject attributes = stack.Collectible.Attributes;
		if (attributes != null && attributes["repairGain"].Exists)
		{
			return stack.Collectible.Attributes["repairGain"].AsFloat(0.2f);
		}
		if (stack.Collectible is IBlockMealContainer mc)
		{
			ItemStack[] stacks = mc.GetNonEmptyContents(world, stack);
			if (stacks.Length != 0 && stacks[0] != null && stacks[0].Collectible.Code.PathStartsWith("glueportion"))
			{
				return stacks[0].Collectible.Attributes["repairGain"].AsFloat(0.2f);
			}
			string recipe = mc.GetRecipeCode(world, stack);
			if (recipe == null)
			{
				return 0f;
			}
			Item outputItem = world.GetItem(new AssetLocation(recipe));
			if (outputItem != null)
			{
				JsonObject attributes2 = outputItem.Attributes;
				if (attributes2 != null && attributes2["repairGain"].Exists)
				{
					return outputItem.Attributes["repairGain"].AsFloat(0.2f) * Math.Min(1f, mc.GetQuantityServings(world, stack));
				}
			}
		}
		return 0f;
	}

	protected EnumClutterDropRule GetRule(IWorldAccessor world)
	{
		string config = world.Config.GetString("clutterObtainable", "ifrepaired").ToLowerInvariant();
		if (config == "yes")
		{
			return EnumClutterDropRule.AlwaysObtain;
		}
		if (config == "no")
		{
			return EnumClutterDropRule.NeverObtain;
		}
		return EnumClutterDropRule.Reparable;
	}
}
