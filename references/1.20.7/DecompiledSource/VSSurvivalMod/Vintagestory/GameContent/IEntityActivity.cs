using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public interface IEntityActivity
{
	double Priority { get; set; }

	int Slot { get; set; }

	string Name { get; set; }

	string Code { get; set; }

	IActionCondition[] Conditions { get; }

	IEntityAction[] Actions { get; }

	IEntityAction CurrentAction { get; }

	EnumConditionLogicOp ConditionsOp { get; set; }

	bool Finished { get; }

	void StoreState(ITreeAttribute tree);

	void Cancel();

	void Finish();

	void Start();

	void OnTick(float dt);

	void Pause();

	void Resume();
}
