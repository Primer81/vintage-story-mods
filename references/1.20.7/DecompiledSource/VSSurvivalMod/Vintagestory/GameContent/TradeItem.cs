using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class TradeItem : JsonItemStack
{
	public NatFloat Price;

	public NatFloat Stock;

	public RestockOpts Restock = new RestockOpts
	{
		HourDelay = 24f,
		Quantity = 1f
	};

	public SupplyDemandOpts SupplyDemand = new SupplyDemandOpts
	{
		PriceChangePerDay = 0.1f,
		PriceChangePerPurchase = 0.1f
	};

	public string AttributesToIgnore;

	public ResolvedTradeItem Resolve(IWorldAccessor world)
	{
		Resolve(world, "TradeItem");
		foreach (KeyValuePair<string, IAttribute> attr in ResolvedItemstack.Attributes)
		{
			if (attr.Value.Equals("*"))
			{
				ResolvedItemstack.Attributes.RemoveAttribute(attr.Key);
				AttributesToIgnore = ((AttributesToIgnore == null) ? "" : (AttributesToIgnore + ",")) + attr.Key;
			}
		}
		return new ResolvedTradeItem
		{
			Stack = ResolvedItemstack,
			AttributesToIgnore = AttributesToIgnore,
			Price = (int)Math.Max(1.0, Math.Round(Price.nextFloat(1f, world.Rand))),
			Stock = ((Stock != null) ? ((int)Math.Round(Stock.nextFloat(1f, world.Rand))) : 0),
			Restock = Restock,
			SupplyDemand = SupplyDemand
		};
	}
}
