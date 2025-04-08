using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class MealTextureSource : ITexPositionSource
{
	public Block textureSourceBlock;

	public ItemStack ForStack;

	public string[] customTextureMapping;

	private ICoreClientAPI capi;

	private ITexPositionSource blockTextureSource;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (textureCode == "rot-solids" || textureCode == "rot-spill")
			{
				return blockTextureSource[textureCode];
			}
			if (ForStack != null)
			{
				string itemcode = ForStack.Collectible.Code.Path;
				string[] mapping = (textureSourceBlock.Attributes?["textureMapping"])?[itemcode]?.AsArray<string>();
				if (customTextureMapping != null)
				{
					mapping = customTextureMapping;
				}
				if (mapping != null && mapping[0] == textureCode)
				{
					return blockTextureSource[mapping[1]];
				}
			}
			if (textureCode == "ceramic" || textureCode == "mat")
			{
				return blockTextureSource["ceramic"];
			}
			return blockTextureSource["transparent"];
		}
	}

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public MealTextureSource(ICoreClientAPI capi, Block textureSourceBlock)
	{
		this.capi = capi;
		this.textureSourceBlock = textureSourceBlock;
		blockTextureSource = capi.Tesselator.GetTextureSource(textureSourceBlock);
	}
}
