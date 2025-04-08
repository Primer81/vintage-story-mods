using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class TurnAction : EntityActionBase
{
	[JsonProperty]
	private float yaw;

	public override string Type => "turn";

	public TurnAction()
	{
	}

	public TurnAction(EntityActivitySystem vas, float yaw)
	{
		base.vas = vas;
		this.yaw = yaw;
	}

	public override void Start(EntityActivity act)
	{
		vas.Entity.ServerPos.Yaw = yaw * ((float)Math.PI / 180f);
	}

	public override string ToString()
	{
		return "Turn to look direction " + yaw + " degrees";
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 300.0, 25.0);
		singleComposer.AddStaticText("Yaw (in degrees)", CairoFont.WhiteDetailText(), b).AddNumberInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "yaw").AddSmallButton("Insert Player Yaw", () => onClickPlayerYaw(capi, singleComposer), b = b.FlatCopy().WithFixedPosition(0.0, 0.0).FixedUnder(b, 2.0), EnumButtonStyle.Small);
		singleComposer.GetNumberInput("yaw").SetValue(yaw);
	}

	private bool onClickPlayerYaw(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		EntityPos plrPos = capi.World.Player.Entity.Pos;
		singleComposer.GetTextInput("yaw").SetValue(Math.Round(GameMath.Mod(plrPos.Yaw * (180f / (float)Math.PI), 360f), 1).ToString() ?? "");
		return true;
	}

	public override IEntityAction Clone()
	{
		return new TurnAction(vas, yaw);
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		yaw = singleComposer.GetTextInput("yaw").GetText().ToFloat();
		return true;
	}
}
