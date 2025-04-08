using System;
using System.IO;
using System.Runtime.Serialization;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract]
public class Auction : IComparable<Auction>
{
	public ItemStack ItemStack;

	[ProtoMember(1)]
	public long AuctionId;

	[ProtoMember(2)]
	public byte[] ItemStackSerialized;

	[ProtoMember(3)]
	public int Price;

	[ProtoMember(4)]
	public int TraderCut;

	[ProtoMember(5)]
	public double PostedTotalHours;

	[ProtoMember(6)]
	public double ExpireTotalHours;

	[ProtoMember(7)]
	public Vec3d SrcAuctioneerEntityPos;

	[ProtoMember(8)]
	public long SrcAuctioneerEntityId;

	[ProtoMember(9)]
	public string SellerUid;

	[ProtoMember(10)]
	public string SellerName;

	[ProtoMember(11)]
	public long SellerEntityId;

	[ProtoMember(12)]
	public string BuyerUid;

	[ProtoMember(13)]
	public string BuyerName;

	[ProtoMember(14)]
	public double RetrievableTotalHours;

	[ProtoMember(15)]
	public Vec3d DstAuctioneerEntityPos;

	[ProtoMember(16)]
	public long DstAuctioneerEntityId;

	[ProtoMember(17)]
	public EnumAuctionState State;

	[ProtoMember(18)]
	public bool MoneyCollected;

	[ProtoMember(19)]
	public bool WithDelivery;

	[OnDeserialized]
	protected void OnDeserializedMethod(StreamingContext context)
	{
		using MemoryStream ms = new MemoryStream(ItemStackSerialized);
		using BinaryReader reader = new BinaryReader(ms);
		ItemStack = new ItemStack(reader);
	}

	[ProtoBeforeSerialization]
	protected void BeforeSerialization()
	{
		using MemoryStream ms = new MemoryStream();
		using (BinaryWriter writer = new BinaryWriter(ms))
		{
			ItemStack.ToBytes(writer);
		}
		ItemStackSerialized = ms.ToArray();
	}

	public string GetExpireText(ICoreAPI api)
	{
		switch (State)
		{
		case EnumAuctionState.Active:
		{
			double activeHours = ExpireTotalHours - api.World.Calendar.TotalHours;
			return prettyHours(activeHours, api);
		}
		case EnumAuctionState.Expired:
		{
			double waithours = RetrievableTotalHours - api.World.Calendar.TotalHours;
			if (waithours > 0.0)
			{
				return Lang.Get("Expired, returning to owner. {0}", prettyHours(waithours, api));
			}
			return Lang.Get("Expired, returned to owner.");
		}
		case EnumAuctionState.Sold:
		{
			double waithours2 = RetrievableTotalHours - api.World.Calendar.TotalHours;
			if (api.World.Config.GetBool("allowMap", defaultValue: true))
			{
				if (WithDelivery)
				{
					string traderMapLink = string.Format("worldmap://={0}={1}={2}=" + Lang.Get("Delivery of {0}x{1}", ItemStack.StackSize, ItemStack.GetName()), DstAuctioneerEntityPos.XInt, DstAuctioneerEntityPos.YInt, DstAuctioneerEntityPos.ZInt);
					if (waithours2 > 0.0)
					{
						return Lang.Get("auctionhouse-sold-enroute-hoursleft", BuyerName, traderMapLink, prettyHours(waithours2, api));
					}
					return Lang.Get("auctionhouse-sold-delievered", BuyerName, traderMapLink);
				}
				string traderMapLink2 = string.Format("worldmap://={0}={1}={2}=" + Lang.Get("Pickup of {0}x{1}", ItemStack.StackSize, ItemStack.GetName()), DstAuctioneerEntityPos.XInt, DstAuctioneerEntityPos.YInt, DstAuctioneerEntityPos.ZInt);
				if (waithours2 > 0.0)
				{
					return Lang.Get("auctionhouse-sold-preparing-hoursleft", BuyerName, traderMapLink2, prettyHours(waithours2, api));
				}
				return Lang.Get("auctionhouse-sold-pickup", BuyerName, traderMapLink2);
			}
			BlockPos pos = DstAuctioneerEntityPos.AsBlockPos.Sub(api.World.DefaultSpawnPosition.AsBlockPos);
			if (waithours2 > 0.0)
			{
				return Lang.Get("auctionhouse-sold-enroute-nomap-hoursleft", BuyerName, pos.X, pos.Y, pos.Z, prettyHours(waithours2, api));
			}
			return Lang.Get("auctionhouse-sold-delievered-nomap", BuyerName, pos.X, pos.Y, pos.Z);
		}
		case EnumAuctionState.SoldRetrieved:
			return Lang.Get("Sold and retrieved.");
		default:
			return "unknown";
		}
	}

	public string prettyHours(double rlHours, ICoreAPI api)
	{
		string durationText = Lang.Get("{0:0} hrs left", rlHours);
		if (rlHours / (double)api.World.Calendar.HoursPerDay > 1.5)
		{
			durationText = Lang.Get("{0:0.#} days left", rlHours / (double)api.World.Calendar.HoursPerDay);
		}
		if (rlHours < 1.0)
		{
			durationText = Lang.Get("{0:0} min left", rlHours * 60.0);
		}
		return durationText;
	}

	public int CompareTo(Auction other)
	{
		if (State == EnumAuctionState.Active && other.State == EnumAuctionState.Active)
		{
			return (int)(1000.0 * (ExpireTotalHours - other.ExpireTotalHours));
		}
		if (State == other.State)
		{
			return (int)(1000.0 * (RetrievableTotalHours - other.RetrievableTotalHours));
		}
		return State - other.State;
	}
}
