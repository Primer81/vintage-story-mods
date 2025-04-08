using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class TradeHandbookInfo : ModSystem
{
	private ICoreClientAPI capi;

	public override double ExecuteOrder()
	{
		return 0.15;
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Event.LevelFinalize += Event_LevelFinalize;
	}

	private void Event_LevelFinalize()
	{
		foreach (EntityProperties entitytype in capi.World.EntityTypes)
		{
			TradeProperties tradeProps = null;
			string stringpath = entitytype.Attributes?["tradePropsFile"].AsString();
			AssetLocation filepath = null;
			JsonObject attributes = entitytype.Attributes;
			if ((attributes != null && attributes["tradeProps"].Exists) || stringpath != null)
			{
				try
				{
					filepath = ((stringpath == null) ? null : AssetLocation.Create(stringpath, entitytype.Code.Domain));
					tradeProps = ((!(filepath != null)) ? entitytype.Attributes["tradeProps"].AsObject<TradeProperties>(null, entitytype.Code.Domain) : capi.Assets.Get(filepath.WithPathAppendixOnce(".json")).ToObject<TradeProperties>());
				}
				catch (Exception e)
				{
					capi.World.Logger.Error("Failed deserializing tradeProps attribute for entitiy {0}, exception logged to verbose debug", entitytype.Code);
					capi.World.Logger.Error(e);
					capi.World.Logger.VerboseDebug("Failed deserializing TradeProperties:");
					capi.World.Logger.VerboseDebug("=================");
					capi.World.Logger.VerboseDebug("Tradeprops json:");
					if (filepath != null)
					{
						capi.World.Logger.VerboseDebug("File path {0}:", filepath);
					}
					capi.World.Logger.VerboseDebug("{0}", entitytype.Server?.Attributes["tradeProps"].ToJsonToken());
				}
			}
			if (tradeProps != null)
			{
				string traderName = Lang.Get(entitytype.Code.Domain + ":item-creature-" + entitytype.Code.Path);
				string handbookTitle = Lang.Get("Sold by");
				TradeItem[] list = tradeProps.Selling.List;
				foreach (TradeItem val2 in list)
				{
					AddTraderHandbookInfo(val2, traderName, handbookTitle);
				}
				handbookTitle = Lang.Get("Purchased by");
				list = tradeProps.Buying.List;
				foreach (TradeItem val in list)
				{
					AddTraderHandbookInfo(val, traderName, handbookTitle);
				}
			}
		}
		capi.Logger.VerboseDebug("Done traders handbook stuff");
	}

	private void AddTraderHandbookInfo(TradeItem val, string traderName, string title)
	{
		if (!val.Resolve(capi.World, "tradehandbookinfo " + traderName))
		{
			return;
		}
		CollectibleObject collobj = val.ResolvedItemstack.Collectible;
		if (collobj.Attributes == null)
		{
			collobj.Attributes = new JsonObject(JToken.Parse("{}"));
		}
		CollectibleBehaviorHandbookTextAndExtraInfo bh = collobj.GetBehavior<CollectibleBehaviorHandbookTextAndExtraInfo>();
		ExtraHandbookSection section = bh.ExtraHandBookSections?.FirstOrDefault((ExtraHandbookSection ele) => ele.Title == title);
		if (section == null)
		{
			section = new ExtraHandbookSection
			{
				Title = title,
				TextParts = new string[0]
			};
			if (bh.ExtraHandBookSections != null)
			{
				bh.ExtraHandBookSections.Append(section);
			}
			else
			{
				bh.ExtraHandBookSections = new ExtraHandbookSection[1] { section };
			}
		}
		section.TextParts = section.TextParts.Append(traderName);
	}
}
