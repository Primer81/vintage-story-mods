namespace Vintagestory.API.Common.Entities;

public interface IPhysicsTickable
{
	bool Ticking { get; set; }

	/// <summary>
	/// Called at a fixed interval, potentially 30 times per second (if server is running smoothly)
	/// </summary>
	void OnPhysicsTick(float dt);

	/// <summary>
	/// Called once per server tick, after all physics ticking has occurred; on main thread.
	/// </summary>
	void AfterPhysicsTick(float dt);

	/// <summary>
	/// If physics is multithreaded, indicates whether this tickable can proceed to be worked on on this particular thread, or not
	/// </summary>
	/// <returns></returns>
	bool CanProceedOnThisThread();

	/// <summary>
	/// Should be called at the end of each individual physics tick, necessary for multithreading to share the work properly
	/// </summary>
	void OnPhysicsTickDone();
}
