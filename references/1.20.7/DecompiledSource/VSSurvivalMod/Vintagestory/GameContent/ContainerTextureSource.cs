using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ContainerTextureSource : ITexPositionSource
{
	public ItemStack forContents;

	private ICoreClientAPI capi;

	private CompositeTexture contentTexture;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			capi.BlockTextureAtlas.GetOrInsertTexture(contentTexture.Baked.BakedName, out var _, out var contentTextPos);
			return contentTextPos;
		}
	}

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public ContainerTextureSource(ICoreClientAPI capi, ItemStack forContents, CompositeTexture contentTexture)
	{
		this.capi = capi;
		this.forContents = forContents;
		this.contentTexture = contentTexture;
		contentTexture.Bake(capi.Assets);
	}
}
