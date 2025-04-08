using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

/// <summary>
/// Block texture Atlas
/// </summary>
public interface IBlockTextureAtlasAPI : ITextureAtlasAPI
{
	/// <summary>
	/// Returns the position in the block texture atlas of given block. 
	/// </summary>
	/// <param name="block"></param>
	/// <param name="textureName"></param>
	/// <param name="returnNullWhenMissing"></param>
	/// <returns></returns>
	TextureAtlasPosition GetPosition(Block block, string textureName, bool returnNullWhenMissing = false);
}
