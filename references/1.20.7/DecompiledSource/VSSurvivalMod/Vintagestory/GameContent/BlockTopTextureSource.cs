using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockTopTextureSource : ITexPositionSource
{
	private ICoreClientAPI capi;

	private Block block;

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode] => capi.BlockTextureAtlas.GetPosition(block, "up");

	public BlockTopTextureSource(ICoreClientAPI capi, Block block)
	{
		this.capi = capi;
		this.block = block;
	}
}
