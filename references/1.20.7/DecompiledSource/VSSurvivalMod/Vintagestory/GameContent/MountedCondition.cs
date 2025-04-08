using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class MountedCondition : IActionCondition, IStorableTypedComponent
{
	[JsonProperty]
	private string mountCode = "*";

	protected EntityActivitySystem vas;

	[JsonProperty]
	public bool Invert { get; set; }

	public string Type => "mounted";

	public MountedCondition()
	{
	}

	public MountedCondition(EntityActivitySystem vas, string mountCode, bool invert = false)
	{
		this.vas = vas;
		this.mountCode = mountCode;
		Invert = invert;
	}

	public virtual bool ConditionSatisfied(Entity e)
	{
		if (vas.Entity.MountedOn == null)
		{
			return false;
		}
		Entity me = vas.Entity.MountedOn.Entity;
		if (me != null)
		{
			return me.WildCardMatch(mountCode);
		}
		if (vas.Entity.MountedOn is BlockEntity be)
		{
			return be.Block.WildCardMatch(mountCode);
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
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("Mountable Block/Entity Code", CairoFont.WhiteDetailText(), b).AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "mountCode");
		singleComposer.GetTextInput("mountCode").SetValue(mountCode);
	}

	public void StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		mountCode = singleComposer.GetTextInput("mountCode").GetText();
	}

	public IActionCondition Clone()
	{
		return new MountedCondition(vas, mountCode, Invert);
	}

	public override string ToString()
	{
		return (Invert ? "When not mounted on " : "When mounted on ") + mountCode;
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
	}
}
