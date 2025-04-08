using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityCookedContainer : BlockEntityContainer, IBlockEntityMealContainer
{
	internal InventoryGeneric inventory;

	internal BlockCookedContainer ownBlock;

	private MeshData currentMesh;

	private bool wasRotten;

	private int tickCnt;

	public override InventoryBase Inventory => inventory;

	public override string InventoryClassName => "cookedcontainer";

	public float QuantityServings { get; set; }

	public string RecipeCode { get; set; }

	InventoryBase IBlockEntityMealContainer.inventory => inventory;

	public bool Rotten
	{
		get
		{
			bool rotten = false;
			for (int i = 0; i < inventory.Count; i++)
			{
				rotten |= inventory[i].Itemstack?.Collectible.Code.Path == "rot";
			}
			return rotten;
		}
	}

	public CookingRecipe FromRecipe => Api.GetCookingRecipe(RecipeCode);

	public BlockEntityCookedContainer()
	{
		inventory = new InventoryGeneric(4, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		ownBlock = base.Block as BlockCookedContainer;
		if (Api.Side == EnumAppSide.Client)
		{
			RegisterGameTickListener(Every100ms, 200);
		}
	}

	private void Every100ms(float dt)
	{
		float temp = GetTemperature();
		if (Api.World.Rand.NextDouble() < (double)((temp - 50f) / 160f))
		{
			BlockCookedContainer.smokeHeld.MinPos = Pos.ToVec3d().AddCopy(0.45, 0.3125, 0.45);
			Api.World.SpawnParticles(BlockCookedContainer.smokeHeld);
		}
		if (tickCnt++ % 20 == 0 && !wasRotten && Rotten)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
			wasRotten = true;
		}
	}

	private int GetTemperature()
	{
		ItemStack[] stacks = GetNonEmptyContentStacks(cloned: false);
		if (stacks.Length == 0 || stacks[0] == null)
		{
			return 0;
		}
		return (int)stacks[0].Collectible.GetTemperature(Api.World, stacks[0]);
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		if (byItemStack?.Block is BlockCookedContainer blockpot)
		{
			TreeAttribute tempTree = byItemStack.Attributes?["temperature"] as TreeAttribute;
			ItemStack[] stacks = blockpot.GetNonEmptyContents(Api.World, byItemStack);
			for (int i = 0; i < stacks.Length; i++)
			{
				ItemStack stack = stacks[i].Clone();
				Inventory[i].Itemstack = stack;
				if (tempTree != null)
				{
					stack.Attributes["temperature"] = tempTree.Clone();
				}
			}
			RecipeCode = blockpot.GetRecipeCode(Api.World, byItemStack);
			QuantityServings = blockpot.GetServings(Api.World, byItemStack);
		}
		if (Api.Side == EnumAppSide.Client)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
		}
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		QuantityServings = (float)tree.GetDecimal("quantityServings", 1.0);
		RecipeCode = tree.GetString("recipeCode");
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client && currentMesh == null)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetFloat("quantityServings", QuantityServings);
		tree.SetString("recipeCode", (RecipeCode == null) ? "" : RecipeCode);
	}

	public bool ServeInto(IPlayer player, ItemSlot slot)
	{
		try
		{
			int capacity = slot.Itemstack.Collectible.Attributes["servingCapacity"].AsInt();
			float servings = Math.Min(QuantityServings, capacity);
			ItemStack mealStack;
			if (slot.Itemstack.Collectible is IBlockMealContainer ibm && ibm.GetQuantityServings(Api.World, slot.Itemstack) > 0f)
			{
				float existingServings = ibm.GetQuantityServings(Api.World, slot.Itemstack);
				ItemStack[] existingContent = ibm.GetNonEmptyContents(Api.World, slot.Itemstack);
				servings = Math.Min(servings, (float)capacity - existingServings);
				ItemStack[] potStacks = GetNonEmptyContentStacks();
				if (servings == 0f)
				{
					return false;
				}
				if (existingContent.Length != potStacks.Length)
				{
					return false;
				}
				for (int i = 0; i < existingContent.Length; i++)
				{
					if (!existingContent[i].Equals(Api.World, potStacks[i], GlobalConstants.IgnoredStackAttributes))
					{
						return false;
					}
				}
				if (slot.StackSize == 1)
				{
					mealStack = slot.Itemstack;
					ibm.SetContents(RecipeCode, slot.Itemstack, GetNonEmptyContentStacks(), existingServings + servings);
				}
				else
				{
					mealStack = slot.Itemstack.Clone();
					ibm.SetContents(RecipeCode, mealStack, GetNonEmptyContentStacks(), existingServings + servings);
				}
			}
			else
			{
				mealStack = new ItemStack(Api.World.GetBlock(AssetLocation.Create(slot.Itemstack.Collectible.Attributes["mealBlockCode"].AsString(), slot.Itemstack.Collectible.Code.Domain)));
				mealStack.StackSize = 1;
				(mealStack.Collectible as IBlockMealContainer).SetContents(RecipeCode, mealStack, GetNonEmptyContentStacks(), servings);
			}
			if (slot.StackSize == 1)
			{
				slot.Itemstack = mealStack;
				slot.MarkDirty();
			}
			else
			{
				slot.TakeOut(1);
				if (!player.InventoryManager.TryGiveItemstack(mealStack, slotNotifyEffect: true))
				{
					Api.World.SpawnItemEntity(mealStack, Pos);
				}
				Api.World.Logger.Audit("{0} Took 1x{1} Meal from {2} at {3}.", player.PlayerName, mealStack.Collectible.Code, base.Block.Code, Pos);
				slot.MarkDirty();
			}
			QuantityServings -= servings;
			if (QuantityServings <= 0f)
			{
				Block block = Api.World.GetBlock(ownBlock.CodeWithPath(ownBlock.FirstCodePart() + "-burned"));
				Api.World.BlockAccessor.SetBlock(block.BlockId, Pos);
				return true;
			}
			if (Api.Side == EnumAppSide.Client)
			{
				currentMesh = GenMesh();
				(player as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
			}
			MarkDirty(redrawOnClient: true);
		}
		catch (NullReferenceException)
		{
			Api.World.Logger.Error("NRE in BECookedContainer.");
			Api.World.Logger.Error("slot: " + slot?.Itemstack?.GetName());
			Api.World.Logger.Error("slot cap: " + slot?.Itemstack?.Collectible?.Attributes?["servingCapacity"]);
			throw;
		}
		return true;
	}

	public MeshData GenMesh()
	{
		if (ownBlock == null)
		{
			return null;
		}
		ItemStack[] stacks = GetNonEmptyContentStacks();
		if (stacks == null || stacks.Length == 0)
		{
			return null;
		}
		return (Api as ICoreClientAPI).ModLoader.GetModSystem<MealMeshCache>().GenMealInContainerMesh(ownBlock, FromRecipe, stacks, new Vec3f(0f, 5f / 32f, 0f));
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		if (currentMesh == null)
		{
			currentMesh = GenMesh();
		}
		if (currentMesh != null)
		{
			mesher.AddMeshData(currentMesh);
			return true;
		}
		return false;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		ItemStack[] contentStacks = GetNonEmptyContentStacks();
		CookingRecipe recipe = Api.GetCookingRecipe(RecipeCode);
		if (recipe == null)
		{
			return;
		}
		float servings = QuantityServings;
		int temp = GetTemperature();
		string temppretty = Lang.Get("{0}°C", temp);
		if (temp < 20)
		{
			temppretty = Lang.Get("Cold");
		}
		string nutriFacts = (Api.World.GetBlock(new AssetLocation("bowl-meal")) as BlockMeal).GetContentNutritionFacts(Api.World, inventory[0], contentStacks, forPlayer.Entity);
		if (servings == 1f)
		{
			dsc.Append(Lang.Get("cookedcontainer-servingstemp-singular", Math.Round(servings, 1), recipe.GetOutputName(forPlayer.Entity.World, contentStacks), temppretty, (nutriFacts != null) ? "\n" : "", nutriFacts));
		}
		else
		{
			dsc.Append(Lang.Get("cookedcontainer-servingstemp-plural", Math.Round(servings, 1), recipe.GetOutputName(forPlayer.Entity.World, contentStacks), temppretty, (nutriFacts != null) ? "\n" : "", nutriFacts));
		}
		foreach (ItemSlot slot in inventory)
		{
			if (!slot.Empty)
			{
				TransitionableProperties[] propsm = slot.Itemstack.Collectible.GetTransitionableProperties(Api.World, slot.Itemstack, null);
				if (propsm != null && propsm.Length != 0)
				{
					slot.Itemstack.Collectible.AppendPerishableInfoText(slot, dsc, Api.World);
					break;
				}
			}
		}
	}
}
