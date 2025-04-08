using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

/// <summary>
/// Item texture Atlas.
/// </summary>
public interface IItemTextureAtlasAPI : ITextureAtlasAPI
{
	/// <summary>
	/// Returns the position in the item texture atlas of given item. For items that don't use custom shapes you don't have to supply the textureName
	/// </summary>
	/// <param name="item"></param>
	/// <param name="textureName"></param>
	/// <param name="returnNullWhenMissing"></param>
	/// <returns></returns>
	TextureAtlasPosition GetPosition(Item item, string textureName = null, bool returnNullWhenMissing = false);
}
