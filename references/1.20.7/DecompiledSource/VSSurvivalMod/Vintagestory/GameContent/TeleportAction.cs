using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class TeleportAction : EntityActionBase
{
	[JsonProperty]
	public double TargetX { get; set; }

	[JsonProperty]
	public double TargetY { get; set; }

	[JsonProperty]
	public double TargetZ { get; set; }

	[JsonProperty]
	public double Yaw { get; set; }

	public override string Type => "teleport";

	public TeleportAction()
	{
	}

	public TeleportAction(EntityActivitySystem vas, double targetX, double targetY, double targetZ, double yaw)
	{
		base.vas = vas;
		TargetX = targetX;
		TargetY = targetY;
		TargetZ = targetZ;
		Yaw = yaw;
	}

	public TeleportAction(EntityActivitySystem vas)
	{
		base.vas = vas;
	}

	public override void Start(EntityActivity act)
	{
		vas.Entity.TeleportToDouble(TargetX + (double)vas.ActivityOffset.X, TargetY + (double)vas.ActivityOffset.Y, TargetZ + (double)vas.ActivityOffset.Z);
		vas.Entity.Controls.StopAllMovement();
		vas.wppathTraverser.Stop();
		vas.Entity.ServerPos.Yaw = (float)Yaw;
		vas.Entity.Pos.Yaw = (float)Yaw;
		vas.Entity.BodyYaw = (float)Yaw;
		vas.Entity.BodyYawServer = (float)Yaw;
		vas.ClearNextActionDelay();
	}

	public override string ToString()
	{
		double x = TargetX;
		double y = TargetY;
		double z = TargetZ;
		if (vas != null)
		{
			x += (double)vas.ActivityOffset.X;
			y += (double)vas.ActivityOffset.Y;
			z += (double)vas.ActivityOffset.Z;
		}
		return $"Teleport to {x}/{y}/{z}";
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds bc = ElementBounds.Fixed(0.0, 0.0, 65.0, 20.0);
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 200.0, 20.0);
		singleComposer.AddStaticText("x/y/z Pos", CairoFont.WhiteDetailText(), bc).AddTextInput(bc = bc.BelowCopy(), null, CairoFont.WhiteDetailText(), "x").AddTextInput(bc = bc.CopyOffsetedSibling(70.0), null, CairoFont.WhiteDetailText(), "y")
			.AddTextInput(bc = bc.CopyOffsetedSibling(70.0), null, CairoFont.WhiteDetailText(), "z")
			.AddSmallButton("Tp to", () => onClickTpTo(capi), bc = bc.CopyOffsetedSibling(70.0), EnumButtonStyle.Small)
			.AddSmallButton("Insert Player Pos", () => onClickPlayerPos(capi, singleComposer), b = b.FlatCopy().FixedUnder(bc), EnumButtonStyle.Small)
			.AddStaticText("Yaw (in radians)", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 15.0).WithFixedWidth(120.0))
			.AddTextInput(b = b.BelowCopy(0.0, 5.0), null, CairoFont.WhiteDetailText(), "yaw");
		GuiComposer composer = singleComposer;
		composer.GetTextInput("x").SetValue(TargetX.ToString() ?? "");
		composer.GetTextInput("y").SetValue(TargetY.ToString() ?? "");
		composer.GetTextInput("z").SetValue(TargetZ.ToString() ?? "");
		composer.GetTextInput("yaw").SetValue(Yaw.ToString() ?? "");
	}

	private bool onClickTpTo(ICoreClientAPI capi)
	{
		double x = TargetX;
		double y = TargetY;
		double z = TargetZ;
		if (vas != null)
		{
			x += (double)vas.ActivityOffset.X;
			y += (double)vas.ActivityOffset.Y;
			z += (double)vas.ActivityOffset.Z;
		}
		capi.SendChatMessage($"/tp ={x} ={y} ={z}");
		return false;
	}

	private bool onClickPlayerPos(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		Vec3d plrPos = capi.World.Player.Entity.Pos.XYZ;
		singleComposer.GetTextInput("x").SetValue(Math.Round(plrPos.X, 1).ToString() ?? "");
		singleComposer.GetTextInput("y").SetValue(Math.Round(plrPos.Y, 1).ToString() ?? "");
		singleComposer.GetTextInput("z").SetValue(Math.Round(plrPos.Z, 1).ToString() ?? "");
		singleComposer.GetTextInput("yaw").SetValue(Math.Round(capi.World.Player.Entity.ServerPos.Yaw - (float)Math.PI / 2f, 1).ToString() ?? "");
		return true;
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer s)
	{
		TargetX = s.GetTextInput("x").GetText().ToDouble();
		TargetY = s.GetTextInput("y").GetText().ToDouble();
		TargetZ = s.GetTextInput("z").GetText().ToDouble();
		Yaw = s.GetTextInput("yaw").GetText().ToDouble();
		return true;
	}

	public override IEntityAction Clone()
	{
		return new TeleportAction(vas, TargetX, TargetY, TargetZ, Yaw);
	}

	public override void OnVisualize(ActivityVisualizer visualizer)
	{
		Vec3d target = new Vec3d(TargetX, TargetY, TargetZ);
		if (vas != null)
		{
			target.Add(vas.ActivityOffset);
		}
		visualizer.GoTo(target, ColorUtil.ColorFromRgba(255, 255, 0, 255));
	}
}
