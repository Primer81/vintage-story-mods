using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class EntityVicinityCondition : IActionCondition, IStorableTypedComponent
{
	[JsonProperty]
	private float range;

	[JsonProperty]
	private AssetLocation entityCode;

	private EntityActivitySystem vas;

	private EntityPartitioning ep;

	[JsonProperty]
	public bool Invert { get; set; }

	public virtual string Type => "entityvicinity";

	public EntityVicinityCondition()
	{
	}

	public EntityVicinityCondition(EntityActivitySystem vas, float range, AssetLocation entityCode, bool invert = false)
	{
		this.vas = vas;
		this.range = range;
		Invert = invert;
		ep = vas.Entity.Api.ModLoader.GetModSystem<EntityPartitioning>();
	}

	public bool ConditionSatisfied(Entity e)
	{
		return ep.GetNearestEntity(vas.Entity.Pos.XYZ, range, (Entity e) => e.WildCardMatch(entityCode)) != null;
	}

	public void LoadState(ITreeAttribute tree)
	{
	}

	public void StoreState(ITreeAttribute tree)
	{
	}

	public void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("Range", CairoFont.WhiteDetailText(), b).AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "range").AddStaticText("EntityCode", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 10.0))
			.AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "entityCode");
		singleComposer.GetTextInput("range").SetValue(range.ToString() ?? "");
		singleComposer.GetTextInput("entityCode").SetValue(((string)entityCode) ?? "");
	}

	public IActionCondition Clone()
	{
		return new EntityVicinityCondition(vas, range, entityCode, Invert);
	}

	public void StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		range = singleComposer.GetTextInput("range").GetText().ToFloat();
		entityCode = new AssetLocation(singleComposer.GetTextInput("entityCode").GetText());
	}

	public override string ToString()
	{
		return string.Format("When {2} in {0} blocks range of {1}", range, entityCode, Invert ? "NOT" : "");
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
		ep = vas?.Entity.Api.ModLoader.GetModSystem<EntityPartitioning>();
	}
}
