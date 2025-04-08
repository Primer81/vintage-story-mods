using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityMeal : BlockEntityContainer, IBlockEntityMealContainer
{
	internal InventoryGeneric inventory;

	internal BlockMeal ownBlock;

	private MeshData currentMesh;

	private bool wasRotten;

	private int tickCnt;

	InventoryBase IBlockEntityMealContainer.inventory => inventory;

	public override InventoryBase Inventory => inventory;

	public override string InventoryClassName => "meal";

	public string RecipeCode { get; set; }

	public float QuantityServings { get; set; }

	public CookingRecipe FromRecipe => Api.GetCookingRecipe(RecipeCode);

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

	public BlockEntityMeal()
	{
		inventory = new InventoryGeneric(4, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		ownBlock = Api.World.BlockAccessor.GetBlock(Pos) as BlockMeal;
		if (Api.Side == EnumAppSide.Client)
		{
			RegisterGameTickListener(Every100ms, 200);
		}
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		if (byItemStack?.Block is BlockMeal blockmeal)
		{
			ItemStack[] stacks = blockmeal.GetContents(Api.World, byItemStack);
			for (int i = 0; i < stacks.Length; i++)
			{
				Inventory[i].Itemstack = stacks[i];
			}
			RecipeCode = blockmeal.GetRecipeCode(Api.World, byItemStack);
			QuantityServings = blockmeal.GetQuantityServings(Api.World, byItemStack);
		}
		if (Api.Side == EnumAppSide.Client)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
		}
	}

	private void Every100ms(float dt)
	{
		float temp = GetTemperature();
		if (Api.World.Rand.NextDouble() < (double)((temp - 50f) / 320f))
		{
			BlockCookedContainer.smokeHeld.MinPos = Pos.ToVec3d().AddCopy(0.45, 0.125, 0.45);
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

	internal MeshData GenMesh()
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
		return (Api as ICoreClientAPI).ModLoader.GetModSystem<MealMeshCache>().GenMealInContainerMesh(ownBlock, FromRecipe, stacks);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		if (currentMesh == null)
		{
			currentMesh = GenMesh();
		}
		mesher.AddMeshData(currentMesh);
		return true;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		RecipeCode = tree.GetString("recipeCode");
		QuantityServings = (float)tree.GetDecimal("quantityServings");
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
		tree.SetString("recipeCode", (RecipeCode == null) ? "" : RecipeCode);
		tree.SetFloat("quantityServings", QuantityServings);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		CookingRecipe recipe = FromRecipe;
		if (recipe == null)
		{
			if (inventory.Count > 0 && !inventory[0].Empty)
			{
				dsc.AppendLine(inventory[0].StackSize + "x " + inventory[0].Itemstack.GetName());
			}
			return;
		}
		dsc.AppendLine(Lang.Get("{0} serving of {1}", Math.Round(QuantityServings, 1), recipe.GetOutputName(forPlayer.Entity.World, GetNonEmptyContentStacks()).UcFirst()));
		if (ownBlock == null)
		{
			return;
		}
		int temp = GetTemperature();
		string temppretty = Lang.Get("{0}°C", temp);
		if (temp < 20)
		{
			temppretty = Lang.Get("Cold");
		}
		dsc.AppendLine(Lang.Get("Temperature: {0}", temppretty));
		string nutriFacts = ownBlock.GetContentNutritionFacts(Api.World, inventory[0], GetNonEmptyContentStacks(cloned: false), forPlayer.Entity);
		if (nutriFacts != null)
		{
			dsc.Append(nutriFacts);
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
