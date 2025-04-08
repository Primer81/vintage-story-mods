using Vintagestory.API.Client;

namespace Vintagestory.GameContent;

public interface IFlatListItem
{
	bool Visible { get; }

	void RenderListEntryTo(ICoreClientAPI capi, float dt, double x, double y, double cellWidth, double cellHeight);

	void Dispose();
}
