using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public interface IActionCondition : IStorableTypedComponent
{
	bool Invert { get; set; }

	void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer);

	IActionCondition Clone();

	bool ConditionSatisfied(Entity e);

	void StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer);
}
