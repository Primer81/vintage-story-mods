using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public interface IEntityAction : IStorableTypedComponent
{
	bool ExecutionHasFailed { get; }

	void Start(EntityActivity entityActivity);

	void Cancel();

	void Finish();

	void Pause();

	void Resume();

	void OnTick(float dt);

	bool IsFinished();

	void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer);

	IEntityAction Clone();

	bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer);

	void OnVisualize(ActivityVisualizer visualizer);

	void OnHurt(DamageSource dmgSource, float damage);
}
