using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public abstract class EntityActionBase : IEntityAction, IStorableTypedComponent
{
	protected EntityActivitySystem vas;

	public abstract string Type { get; }

	public virtual bool ExecutionHasFailed { get; set; }

	public virtual void Start(EntityActivity entityActivity)
	{
	}

	public virtual void Cancel()
	{
	}

	public virtual void Finish()
	{
	}

	public virtual void Pause()
	{
	}

	public virtual void Resume()
	{
	}

	public virtual bool IsFinished()
	{
		return true;
	}

	public virtual void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
	}

	public virtual bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		return true;
	}

	public virtual void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
	}

	public abstract IEntityAction Clone();

	public virtual void OnTick(float dt)
	{
	}

	public virtual void StoreState(ITreeAttribute tree)
	{
	}

	public virtual void LoadState(ITreeAttribute tree)
	{
	}

	public virtual void OnHurt(DamageSource dmgSource, float damage)
	{
	}

	public virtual void OnVisualize(ActivityVisualizer visualizer)
	{
	}
}
