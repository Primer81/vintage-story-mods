using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class TalkAction : EntityActionBase
{
	[JsonProperty]
	private string talkType;

	public override string Type => "talk";

	public TalkAction()
	{
	}

	public TalkAction(EntityActivitySystem vas, string talkType)
	{
		base.vas = vas;
		this.talkType = talkType;
	}

	public override void Start(EntityActivity act)
	{
		int index = Enum.GetNames(typeof(EnumTalkType)).IndexOf<string>(talkType);
		(vas.Entity.Api as ICoreServerAPI).Network.BroadcastEntityPacket(vas.Entity.EntityId, 203, SerializerUtil.Serialize(index));
	}

	public override string ToString()
	{
		return "Talk utterance: " + talkType;
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 300.0, 25.0);
		string[] vals = Enum.GetNames(typeof(EnumTalkType));
		singleComposer.AddStaticText("Utterance", CairoFont.WhiteDetailText(), b).AddDropDown(vals, vals, vals.IndexOf(talkType), null, b.BelowCopy(0.0, -5.0), "talkType");
	}

	public override IEntityAction Clone()
	{
		return new TalkAction(vas, talkType);
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		talkType = singleComposer.GetDropDown("talkType").SelectedValue;
		return true;
	}
}
