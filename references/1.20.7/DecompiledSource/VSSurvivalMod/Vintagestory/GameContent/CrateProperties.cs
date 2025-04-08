using System.Collections.Generic;

namespace Vintagestory.GameContent;

public class CrateProperties
{
	public Dictionary<string, CrateTypeProperties> Properties;

	public string[] Types;

	public Dictionary<string, LabelProps> Labels;

	public string DefaultType = "wood-aged";

	public string VariantByGroup;

	public string VariantByGroupInventory;

	public string InventoryClassName = "crate";

	public CrateTypeProperties this[string type]
	{
		get
		{
			if (!Properties.TryGetValue(type, out var props))
			{
				return Properties["*"];
			}
			return props;
		}
	}
}
