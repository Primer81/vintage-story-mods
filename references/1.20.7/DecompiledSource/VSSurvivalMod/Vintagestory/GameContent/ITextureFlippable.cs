using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public interface ITextureFlippable
{
	void FlipTexture(BlockPos pos, string newTextureCode);

	OrderedDictionary<string, CompositeTexture> GetAvailableTextures(BlockPos pos);
}
