using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

public interface IAiTask
{
	string Id { get; }

	int Slot { get; }

	float Priority { get; }

	float PriorityForCancel { get; }

	string ProfilerName { get; set; }

	bool ShouldExecute();

	void StartExecute();

	bool ContinueExecute(float dt);

	void FinishExecute(bool cancelled);

	void LoadConfig(JsonObject taskConfig, JsonObject aiConfig);

	void AfterInitialize();

	void OnStateChanged(EnumEntityState beforeState);

	bool Notify(string key, object data);

	void OnEntityLoaded();

	void OnEntitySpawn();

	void OnEntityDespawn(EntityDespawnData reason);

	void OnEntityHurt(DamageSource source, float damage);

	bool CanContinueExecute();
}
