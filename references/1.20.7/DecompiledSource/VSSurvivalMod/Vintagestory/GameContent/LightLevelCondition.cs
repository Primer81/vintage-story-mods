using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class LightLevelCondition : IActionCondition, IStorableTypedComponent
{
	[JsonProperty]
	private float minLight;

	[JsonProperty]
	private float maxLight;

	[JsonProperty]
	private EnumLightLevelType lightLevelType;

	protected EntityActivitySystem vas;

	[JsonProperty]
	public bool Invert { get; set; }

	public string Type => "lightlevel";

	public LightLevelCondition()
	{
	}

	public LightLevelCondition(EntityActivitySystem vas, float minLight, float maxLight, EnumLightLevelType lightLevelType, bool invert = false)
	{
		this.vas = vas;
		this.minLight = minLight;
		this.maxLight = maxLight;
		this.lightLevelType = lightLevelType;
		Invert = invert;
	}

	public virtual bool ConditionSatisfied(Entity e)
	{
		int lightLevel = e.Api.World.BlockAccessor.GetLightLevel(e.Pos.AsBlockPos, lightLevelType);
		if ((float)lightLevel >= minLight)
		{
			return (float)lightLevel <= maxLight;
		}
		return false;
	}

	public void LoadState(ITreeAttribute tree)
	{
	}

	public void StoreState(ITreeAttribute tree)
	{
	}

	public void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		string[] values = new string[6] { "0", "1", "2", "3", "4", "5" };
		string[] names = Enum.GetNames(typeof(EnumLightLevelType));
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("Light level type", CairoFont.WhiteDetailText(), b).AddDropDown(values, names, (int)lightLevelType, null, b = b.BelowCopy(0.0, -5.0), CairoFont.WhiteDetailText(), "lightleveltype").AddStaticText("Min light level (0..32)", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 10.0))
			.AddNumberInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "minLight")
			.AddStaticText("Max light level (0..32)", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 10.0))
			.AddNumberInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "maxLight");
		singleComposer.GetNumberInput("minLight").SetValue(minLight.ToString() ?? "");
		singleComposer.GetNumberInput("maxLight").SetValue(maxLight.ToString() ?? "");
	}

	public void StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		minLight = singleComposer.GetNumberInput("minLight").GetValue();
		maxLight = singleComposer.GetNumberInput("maxLight").GetValue();
		lightLevelType = (EnumLightLevelType)singleComposer.GetDropDown("lightleveltype").SelectedIndices[0];
	}

	public IActionCondition Clone()
	{
		return new LightLevelCondition(vas, minLight, maxLight, lightLevelType, Invert);
	}

	public override string ToString()
	{
		if (!Invert)
		{
			return $"{lightLevelType} within {minLight} to {maxLight}";
		}
		return $"{lightLevelType} outside of {minLight} to {maxLight}";
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
	}
}
