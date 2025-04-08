using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.Common;

public interface IWearableShapeSupplier
{
	/// <summary>
	///
	/// </summary>
	/// <param name="stack"></param>
	/// <param name="forEntity"></param>
	/// <param name="texturePrefixCode"></param>
	/// <returns>null for returning back to default behavior (read shape from attributes)</returns>
	Shape GetShape(ItemStack stack, Entity forEntity, string texturePrefixCode);
}
