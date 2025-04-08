using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public interface INpcCommand
{
	string Type { get; }

	void Start();

	void Stop();

	bool IsFinished();

	void ToAttribute(ITreeAttribute tree);

	void FromAttribute(ITreeAttribute tree);
}
