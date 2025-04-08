using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class ResolvedTradeItem
{
	public ItemStack Stack;

	public string AttributesToIgnore;

	public int Price;

	public int Stock;

	public RestockOpts Restock = new RestockOpts
	{
		HourDelay = 24f,
		Quantity = 1f
	};

	public SupplyDemandOpts SupplyDemand;

	public ResolvedTradeItem()
	{
	}

	public ResolvedTradeItem(ITreeAttribute treeAttribute)
	{
		if (treeAttribute != null)
		{
			FromTreeAttributes(treeAttribute);
		}
	}

	public void FromTreeAttributes(ITreeAttribute tree)
	{
		Stack = tree.GetItemstack("stack");
		AttributesToIgnore = tree.GetString("attributesToIgnore");
		Price = tree.GetInt("price");
		Stock = tree.GetInt("stock");
		Restock = new RestockOpts
		{
			HourDelay = tree.GetFloat("restockHourDelay"),
			Quantity = tree.GetFloat("restockQuantity")
		};
		SupplyDemand = new SupplyDemandOpts
		{
			PriceChangePerDay = tree.GetFloat("supplyDemandPriceChangePerDay"),
			PriceChangePerPurchase = tree.GetFloat("supplyDemandPriceChangePerPurchase")
		};
	}

	public void ToTreeAttributes(ITreeAttribute tree)
	{
		tree.SetItemstack("stack", Stack);
		if (AttributesToIgnore != null)
		{
			tree.SetString("attributesToIgnore", AttributesToIgnore);
		}
		tree.SetInt("price", Price);
		tree.SetInt("stock", Stock);
		tree.SetFloat("restockHourDelay", Restock.HourDelay);
		tree.SetFloat("restockQuantity", Restock.Quantity);
		tree.SetFloat("supplyDemandPriceChangePerDay", SupplyDemand.PriceChangePerDay);
		tree.SetFloat("supplyDemandPriceChangePerPurchase", SupplyDemand.PriceChangePerPurchase);
	}
}
