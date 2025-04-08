using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class RandomStack
{
	public EnumItemClass Type;

	public string Code;

	public NatFloat Quantity = NatFloat.createUniform(1f, 0f);

	public float Chance;

	public ItemStack ResolvedStack;

	[JsonProperty]
	[JsonConverter(typeof(JsonAttributesConverter))]
	public JsonObject Attributes;

	internal void Resolve(IWorldAccessor world)
	{
		if (Type == EnumItemClass.Block)
		{
			Block block = world.GetBlock(new AssetLocation(Code));
			if (block == null)
			{
				world.Logger.Error("Cannot resolve stack randomizer block with code {0}, wrong code?", Code);
				return;
			}
			ResolvedStack = new ItemStack(block);
			if (Attributes != null)
			{
				ResolvedStack.Attributes = Attributes.ToAttribute() as ITreeAttribute;
			}
			return;
		}
		Item item = world.GetItem(new AssetLocation(Code));
		if (item == null)
		{
			world.Logger.Error("Cannot resolve stack randomizer item with code {0}, wrong code?", Code);
			return;
		}
		ResolvedStack = new ItemStack(item);
		if (Attributes != null)
		{
			ResolvedStack.Attributes = Attributes.ToAttribute() as ITreeAttribute;
		}
	}
}
