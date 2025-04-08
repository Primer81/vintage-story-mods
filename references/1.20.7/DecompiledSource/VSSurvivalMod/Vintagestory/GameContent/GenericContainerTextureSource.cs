using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GenericContainerTextureSource : ITexPositionSource
{
	public ITexPositionSource blockTextureSource;

	public string curType;

	public Size2i AtlasSize => blockTextureSource.AtlasSize;

	public TextureAtlasPosition this[string textureCode] => blockTextureSource[curType + "-" + textureCode];
}
