using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class CoordinateCondition : IActionCondition, IStorableTypedComponent
{
	[JsonProperty]
	public int Axis;

	[JsonProperty]
	public double Value;

	protected EntityActivitySystem vas;

	[JsonProperty]
	public bool Invert { get; set; }

	public string Type => "coordinate";

	public CoordinateCondition()
	{
	}

	public CoordinateCondition(EntityActivitySystem vas, int axis, double value, bool invert = false)
	{
		this.vas = vas;
		Axis = axis;
		Value = value;
		Invert = invert;
	}

	public virtual bool ConditionSatisfied(Entity e)
	{
		EntityPos pos = e.ServerPos;
		int offset = 0;
		if (vas != null)
		{
			offset = (new int[3]
			{
				vas.ActivityOffset.X,
				vas.ActivityOffset.Y,
				vas.ActivityOffset.Z
			})[Axis];
		}
		return Axis switch
		{
			0 => pos.X < Value + (double)offset, 
			1 => pos.Y < Value + (double)offset, 
			2 => pos.Z < Value + (double)offset, 
			_ => false, 
		};
	}

	public void LoadState(ITreeAttribute tree)
	{
	}

	public void StoreState(ITreeAttribute tree)
	{
	}

	public void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds bc = ElementBounds.Fixed(0.0, 0.0, 200.0, 20.0);
		singleComposer.AddStaticText("When", CairoFont.WhiteDetailText(), bc).AddDropDown(new string[3] { "x", "y", "z" }, new string[3] { "X", "Y", "Z" }, Axis, null, bc = bc.BelowCopy().WithFixedWidth(100.0), "axis").AddSmallButton("Tp to", () => btnTp(singleComposer, capi), bc.CopyOffsetedSibling(110.0).WithFixedWidth(50.0), EnumButtonStyle.Small)
			.AddStaticText("Is smaller than", CairoFont.WhiteDetailText(), bc = bc.BelowCopy(0.0, 5.0).WithFixedWidth(200.0))
			.AddNumberInput(bc = bc.BelowCopy().WithFixedWidth(200.0), null, CairoFont.WhiteDetailText(), "value")
			.AddSmallButton("Insert Player Pos", () => onClickPlayerPos(capi, singleComposer), bc = bc.BelowCopy(), EnumButtonStyle.Small);
		singleComposer.GetNumberInput("value").SetValue(Value);
	}

	private bool btnTp(GuiComposer s, ICoreClientAPI capi)
	{
		int index = s.GetDropDown("axis").SelectedIndices[0];
		double targetX = capi.World.Player.Entity.Pos.X;
		double targetY = capi.World.Player.Entity.Pos.Y;
		double targetZ = capi.World.Player.Entity.Pos.Z;
		double val = (targetX = s.GetNumberInput("value").GetValue());
		int offset = 0;
		if (vas != null)
		{
			offset = (new int[3]
			{
				vas.ActivityOffset.X,
				vas.ActivityOffset.Y,
				vas.ActivityOffset.Z
			})[index];
		}
		switch (index)
		{
		case 0:
			targetX = val + (double)offset;
			break;
		case 1:
			targetY = val + (double)offset;
			break;
		case 2:
			targetZ = val + (double)offset;
			break;
		}
		capi.SendChatMessage($"/tp ={targetX} ={targetY} ={targetZ}");
		return false;
	}

	private bool onClickPlayerPos(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		int index = singleComposer.GetDropDown("axis").SelectedIndices[0];
		double val = 0.0;
		switch (index)
		{
		case 0:
			val = capi.World.Player.Entity.Pos.X;
			break;
		case 1:
			val = capi.World.Player.Entity.Pos.Y;
			break;
		case 2:
			val = capi.World.Player.Entity.Pos.Z;
			break;
		}
		singleComposer.GetTextInput("value").SetValue(Math.Round(val, 1).ToString() ?? "");
		return true;
	}

	public void StoreGuiEditFields(ICoreClientAPI capi, GuiComposer s)
	{
		Value = s.GetNumberInput("value").GetValue();
		Axis = s.GetDropDown("axis").SelectedIndices[0];
	}

	public IActionCondition Clone()
	{
		return new CoordinateCondition(vas, Axis, Value, Invert);
	}

	public override string ToString()
	{
		string axis = (new string[3] { "X", "Y", "Z" })[Axis];
		int offset = 0;
		if (vas != null)
		{
			offset = (new int[3]
			{
				vas.ActivityOffset.X,
				vas.ActivityOffset.Y,
				vas.ActivityOffset.Z
			})[Axis];
		}
		return string.Format("When {0} {1} {2}", axis, Invert ? "&gt;=" : "&lt;", Value + (double)offset);
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
	}
}
