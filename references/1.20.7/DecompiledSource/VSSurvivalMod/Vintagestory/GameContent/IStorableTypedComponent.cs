using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public interface IStorableTypedComponent
{
	string Type { get; }

	void OnLoaded(EntityActivitySystem vas);

	void StoreState(ITreeAttribute tree);

	void LoadState(ITreeAttribute tree);
}
