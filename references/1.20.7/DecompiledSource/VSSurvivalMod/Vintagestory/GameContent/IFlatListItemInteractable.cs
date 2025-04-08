using Vintagestory.API.Client;

namespace Vintagestory.GameContent;

public interface IFlatListItemInteractable : IFlatListItem
{
	void OnMouseMove(ICoreClientAPI api, MouseEvent args);

	void OnMouseDown(ICoreClientAPI api, MouseEvent mouse);

	void OnMouseUp(ICoreClientAPI api, MouseEvent args);
}
