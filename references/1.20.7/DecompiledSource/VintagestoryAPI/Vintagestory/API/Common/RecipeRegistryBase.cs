namespace Vintagestory.API.Common;

public abstract class RecipeRegistryBase
{
	public abstract void ToBytes(IWorldAccessor resolver, out byte[] data, out int quantity);

	public abstract void FromBytes(IWorldAccessor resolver, int quantity, byte[] data);
}
