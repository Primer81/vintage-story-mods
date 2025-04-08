using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BECheese : BlockEntityContainer
{
	private InventoryGeneric inv;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "cheese";

	public int SlicesLeft
	{
		get
		{
			if (inv[0].Empty)
			{
				return 0;
			}
			return (inv[0].Itemstack.Collectible as ItemCheese)?.Part switch
			{
				"1slice" => 1, 
				"2slice" => 2, 
				"3slice" => 3, 
				"4slice" => 4, 
				_ => 0, 
			};
		}
	}

	public BECheese()
	{
		inv = new InventoryGeneric(1, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		inv.LateInitialize("cheese-" + Pos, api);
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		if (byItemStack != null)
		{
			inv[0].Itemstack = byItemStack.Clone();
			inv[0].Itemstack.StackSize = 1;
		}
	}

	public ItemStack TakeSlice()
	{
		if (inv[0].Empty)
		{
			return null;
		}
		ItemCheese cheese = inv[0].Itemstack.Collectible as ItemCheese;
		MarkDirty(redrawOnClient: true);
		switch (cheese.Part)
		{
		case "1slice":
		{
			ItemStack result = inv[0].Itemstack.Clone();
			inv[0].Itemstack = null;
			Api.World.BlockAccessor.SetBlock(0, Pos);
			return result;
		}
		case "2slice":
		{
			ItemStack stack = new ItemStack(Api.World.GetItem(cheese.CodeWithVariant("part", "1slice")));
			inv[0].Itemstack = stack;
			return stack.Clone();
		}
		case "3slice":
		{
			ItemStack itemStack2 = new ItemStack(Api.World.GetItem(cheese.CodeWithVariant("part", "1slice")));
			inv[0].Itemstack = new ItemStack(Api.World.GetItem(cheese.CodeWithVariant("part", "2slice")));
			return itemStack2.Clone();
		}
		case "4slice":
		{
			ItemStack itemStack = new ItemStack(Api.World.GetItem(cheese.CodeWithVariant("part", "1slice")));
			inv[0].Itemstack = new ItemStack(Api.World.GetItem(cheese.CodeWithVariant("part", "3slice")));
			return itemStack.Clone();
		}
		default:
			return null;
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (inv[0].Empty)
		{
			return true;
		}
		tessThreadTesselator.TesselateShape(base.Block, (Api as ICoreClientAPI).TesselatorManager.GetCachedShape(inv[0].Itemstack.Item.Shape.Base), out var modeldata);
		modeldata.Scale(new Vec3f(0.5f, 0f, 0.5f), 0.75f, 0.75f, 0.75f);
		mesher.AddMeshData(modeldata);
		return true;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		dsc.Append(BlockEntityShelf.PerishableInfoCompact(Api, inv[0], 0f));
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		if (worldForResolving.Side == EnumAppSide.Client)
		{
			MarkDirty(redrawOnClient: true);
		}
	}
}
