using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityShelf : BlockEntityDisplay
{
	private InventoryGeneric inv;

	private Block block;

	private static int slotCount = 8;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "shelf";

	public override string AttributeTransformCode => "onshelfTransform";

	public BlockEntityShelf()
	{
		inv = new InventoryGeneric(slotCount, "shelf-0", null);
	}

	public override void Initialize(ICoreAPI api)
	{
		block = api.World.BlockAccessor.GetBlock(Pos);
		base.Initialize(api);
		inv.OnAcquireTransitionSpeed += Inv_OnAcquireTransitionSpeed;
	}

	private float Inv_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul)
	{
		if (transType == EnumTransitionType.Dry || transType == EnumTransitionType.Melt)
		{
			Room room = container.Room;
			if (room == null || room.ExitCount != 0)
			{
				return 0.5f;
			}
			return 2f;
		}
		if (Api == null)
		{
			return 0f;
		}
		if (transType == EnumTransitionType.Ripen)
		{
			float perishRate = container.GetPerishRate();
			return GameMath.Clamp((1f - perishRate - 0.5f) * 3f, 0f, 1f);
		}
		return 1f;
	}

	internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (slot.Empty)
		{
			if (TryTake(byPlayer, blockSel))
			{
				return true;
			}
			return false;
		}
		CollectibleObject colObj = slot.Itemstack.Collectible;
		if (colObj.Attributes != null && colObj.Attributes["shelvable"].AsBool())
		{
			AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;
			AssetLocation stackName = slot.Itemstack?.Collectible.Code;
			if (TryPut(slot, blockSel))
			{
				Api.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
				Api.World.Logger.Audit("{0} Put 1x{1} into Shelf at {2}.", byPlayer.PlayerName, stackName, Pos);
				MarkDirty();
				return true;
			}
			return false;
		}
		return false;
	}

	private bool TryPut(ItemSlot slot, BlockSelection blockSel)
	{
		bool num = blockSel.SelectionBoxIndex > 1;
		bool left = blockSel.SelectionBoxIndex % 2 == 0;
		int num2 = (num ? 4 : 0) + ((!left) ? 2 : 0);
		int end = num2 + 2;
		for (int i = num2; i < end; i++)
		{
			if (inv[i].Empty)
			{
				int num3 = slot.TryPutInto(Api.World, inv[i]);
				MarkDirty();
				(Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				return num3 > 0;
			}
		}
		return false;
	}

	private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
	{
		bool num = blockSel.SelectionBoxIndex > 1;
		bool left = blockSel.SelectionBoxIndex % 2 == 0;
		int start = (num ? 4 : 0) + ((!left) ? 2 : 0);
		for (int i = start + 2 - 1; i >= start; i--)
		{
			if (!inv[i].Empty)
			{
				ItemStack stack = inv[i].TakeOut(1);
				if (byPlayer.InventoryManager.TryGiveItemstack(stack))
				{
					AssetLocation sound = stack.Block?.Sounds?.Place;
					Api.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
				}
				if (stack.StackSize > 0)
				{
					Api.World.SpawnItemEntity(stack, Pos);
				}
				Api.World.Logger.Audit("{0} Took 1x{1} from Shelf at {2}.", byPlayer.PlayerName, stack.Collectible.Code, Pos);
				(Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				MarkDirty();
				return true;
			}
		}
		return false;
	}

	protected override float[][] genTransformationMatrices()
	{
		float[][] tfMatrices = new float[slotCount][];
		for (int index = 0; index < slotCount; index++)
		{
			float x = ((index % 4 >= 2) ? 0.75f : 0.25f);
			float y = ((index >= 4) ? 0.625f : 0.125f);
			float z = ((index % 2 == 0) ? 0.25f : 0.625f);
			tfMatrices[index] = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(block.Shape.rotateY).Translate(x - 0.5f, y, z - 0.5f)
				.Translate(-0.5f, 0f, -0.5f)
				.Values;
		}
		return tfMatrices;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		RedrawAfterReceivingTreeAttributes(worldForResolving);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		base.GetBlockInfo(forPlayer, sb);
		float ripenRate = GameMath.Clamp((1f - container.GetPerishRate() - 0.5f) * 3f, 0f, 1f);
		if (ripenRate > 0f)
		{
			sb.Append(Lang.Get("Suitable spot for food ripening."));
		}
		sb.AppendLine();
		bool up = forPlayer.CurrentBlockSelection != null && forPlayer.CurrentBlockSelection.SelectionBoxIndex > 1;
		for (int j = 3; j >= 0; j--)
		{
			int i = j + (up ? 4 : 0);
			i ^= 2;
			if (!inv[i].Empty)
			{
				ItemStack stack = inv[i].Itemstack;
				if (stack.Collectible is BlockCrock)
				{
					sb.Append(CrockInfoCompact(inv[i]));
				}
				else if (stack.Collectible.TransitionableProps != null && stack.Collectible.TransitionableProps.Length != 0)
				{
					sb.Append(PerishableInfoCompact(Api, inv[i], ripenRate));
				}
				else
				{
					sb.AppendLine(stack.GetName());
				}
			}
		}
	}

	public static string PerishableInfoCompact(ICoreAPI Api, ItemSlot contentSlot, float ripenRate, bool withStackName = true)
	{
		if (contentSlot.Empty)
		{
			return "";
		}
		StringBuilder dsc = new StringBuilder();
		if (withStackName)
		{
			dsc.Append(contentSlot.Itemstack.GetName());
		}
		TransitionState[] transitionStates = contentSlot.Itemstack?.Collectible.UpdateAndGetTransitionStates(Api.World, contentSlot);
		bool nowSpoiling = false;
		if (transitionStates != null)
		{
			bool appendLine = false;
			foreach (TransitionState state in transitionStates)
			{
				TransitionableProperties prop = state.Props;
				float perishRate = contentSlot.Itemstack.Collectible.GetTransitionRateMul(Api.World, contentSlot, prop.Type);
				if (perishRate <= 0f)
				{
					continue;
				}
				float transitionLevel = state.TransitionLevel;
				float freshHoursLeft = state.FreshHoursLeft / perishRate;
				switch (prop.Type)
				{
				case EnumTransitionType.Perish:
				{
					appendLine = true;
					if (transitionLevel > 0f)
					{
						nowSpoiling = true;
						dsc.Append(", " + Lang.Get("{0}% spoiled", (int)Math.Round(transitionLevel * 100f)));
						break;
					}
					double hoursPerday2 = Api.World.Calendar.HoursPerDay;
					if ((double)freshHoursLeft / hoursPerday2 >= (double)Api.World.Calendar.DaysPerYear)
					{
						dsc.Append(", " + Lang.Get("fresh for {0} years", Math.Round((double)freshHoursLeft / hoursPerday2 / (double)Api.World.Calendar.DaysPerYear, 1)));
					}
					else if ((double)freshHoursLeft > hoursPerday2)
					{
						dsc.Append(", " + Lang.Get("fresh for {0} days", Math.Round((double)freshHoursLeft / hoursPerday2, 1)));
					}
					else
					{
						dsc.Append(", " + Lang.Get("fresh for {0} hours", Math.Round(freshHoursLeft, 1)));
					}
					break;
				}
				case EnumTransitionType.Ripen:
				{
					if (nowSpoiling)
					{
						break;
					}
					appendLine = true;
					if (transitionLevel > 0f)
					{
						dsc.Append(", " + Lang.Get("{1:0.#} days left to ripen ({0}%)", (int)Math.Round(transitionLevel * 100f), (state.TransitionHours - state.TransitionedHours) / Api.World.Calendar.HoursPerDay / ripenRate));
						break;
					}
					double hoursPerday = Api.World.Calendar.HoursPerDay;
					if ((double)freshHoursLeft / hoursPerday >= (double)Api.World.Calendar.DaysPerYear)
					{
						dsc.Append(", " + Lang.Get("will ripen in {0} years", Math.Round((double)freshHoursLeft / hoursPerday / (double)Api.World.Calendar.DaysPerYear, 1)));
					}
					else if ((double)freshHoursLeft > hoursPerday)
					{
						dsc.Append(", " + Lang.Get("will ripen in {0} days", Math.Round((double)freshHoursLeft / hoursPerday, 1)));
					}
					else
					{
						dsc.Append(", " + Lang.Get("will ripen in {0} hours", Math.Round(freshHoursLeft, 1)));
					}
					break;
				}
				}
			}
			if (appendLine)
			{
				dsc.AppendLine();
			}
		}
		return dsc.ToString();
	}

	public string CrockInfoCompact(ItemSlot inSlot)
	{
		Api.World.GetBlock(new AssetLocation("bowl-meal"));
		BlockCrock crock = inSlot.Itemstack.Collectible as BlockCrock;
		IWorldAccessor world = Api.World;
		CookingRecipe recipe = crock.GetCookingRecipe(world, inSlot.Itemstack);
		ItemStack[] stacks = crock.GetNonEmptyContents(world, inSlot.Itemstack);
		if (stacks == null || stacks.Length == 0)
		{
			return Lang.Get("Empty Crock") + "\n";
		}
		StringBuilder dsc = new StringBuilder();
		if (recipe != null)
		{
			double servings = inSlot.Itemstack.Attributes.GetDecimal("quantityServings");
			if (recipe != null)
			{
				if (servings == 1.0)
				{
					dsc.Append(Lang.Get("{0:0.#}x {1}.", servings, recipe.GetOutputName(world, stacks)));
				}
				else
				{
					dsc.Append(Lang.Get("{0:0.#}x {1}.", servings, recipe.GetOutputName(world, stacks)));
				}
			}
		}
		else
		{
			int j = 0;
			ItemStack[] array = stacks;
			foreach (ItemStack stack2 in array)
			{
				if (stack2 != null)
				{
					if (j++ > 0)
					{
						dsc.Append(", ");
					}
					dsc.Append(stack2.StackSize + "x " + stack2.GetName());
				}
			}
			dsc.Append(".");
		}
		DummyInventory dummyInv = new DummyInventory(Api);
		ItemSlot contentSlot = BlockCrock.GetDummySlotForFirstPerishableStack(Api.World, stacks, null, dummyInv);
		dummyInv.OnAcquireTransitionSpeed += (EnumTransitionType transType, ItemStack stack, float mul) => mul * crock.GetContainingTransitionModifierContained(world, inSlot, transType) * inv.GetTransitionSpeedMul(transType, stack);
		TransitionState[] transitionStates = contentSlot.Itemstack?.Collectible.UpdateAndGetTransitionStates(Api.World, contentSlot);
		bool addNewLine = true;
		if (transitionStates != null)
		{
			foreach (TransitionState state in transitionStates)
			{
				TransitionableProperties prop = state.Props;
				float perishRate = contentSlot.Itemstack.Collectible.GetTransitionRateMul(world, contentSlot, prop.Type);
				if (perishRate <= 0f)
				{
					continue;
				}
				addNewLine = false;
				float transitionLevel = state.TransitionLevel;
				float freshHoursLeft = state.FreshHoursLeft / perishRate;
				if (prop.Type != 0)
				{
					continue;
				}
				if (transitionLevel > 0f)
				{
					dsc.AppendLine(" " + Lang.Get("{0}% spoiled", (int)Math.Round(transitionLevel * 100f)));
					continue;
				}
				double hoursPerday = Api.World.Calendar.HoursPerDay;
				if ((double)freshHoursLeft / hoursPerday >= (double)Api.World.Calendar.DaysPerYear)
				{
					dsc.AppendLine(" " + Lang.Get("Fresh for {0} years", Math.Round((double)freshHoursLeft / hoursPerday / (double)Api.World.Calendar.DaysPerYear, 1)));
				}
				else if ((double)freshHoursLeft > hoursPerday)
				{
					dsc.AppendLine(" " + Lang.Get("Fresh for {0} days", Math.Round((double)freshHoursLeft / hoursPerday, 1)));
				}
				else
				{
					dsc.AppendLine(" " + Lang.Get("Fresh for {0} hours", Math.Round(freshHoursLeft, 1)));
				}
			}
		}
		if (addNewLine)
		{
			dsc.AppendLine("");
		}
		return dsc.ToString();
	}
}
