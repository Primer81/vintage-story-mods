using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public interface ITutorial
{
	bool Complete { get; }

	float Progress { get; }

	string PageCode { get; }

	void Restart();

	void Skip(int count);

	void Load();

	void Save();

	bool OnStateUpdate(ActionBoolReturn<TutorialStepBase> stepCall);

	List<TutorialStepBase> GetTutorialSteps(bool skipOld);
}
