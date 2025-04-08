using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.Common;

internal interface ITabbedInventory : IInventory, IReadOnlyCollection<ItemSlot>, IEnumerable<ItemSlot>, IEnumerable
{
	CreativeTabs CreativeTabs { get; }

	CreativeTab CurrentTab { get; }

	int CurrentTabIndex { get; }

	void SetTab(int tabIndex);
}
