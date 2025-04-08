using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockWithGrassOverlay : Block
{
	private CompositeTexture grassTex;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side == EnumAppSide.Client && (Textures == null || !Textures.TryGetValue("specialSecondTexture", out grassTex)))
		{
			grassTex = Textures?.First().Value;
		}
	}

	public override int GetColorWithoutTint(ICoreClientAPI capi, BlockPos pos)
	{
		string grasscover = LastCodePart();
		if (grasscover == "none")
		{
			return base.GetColorWithoutTint(capi, pos);
		}
		int? textureSubId = grassTex?.Baked.TextureSubId;
		if (!textureSubId.HasValue)
		{
			return -1;
		}
		int grassColor = capi.BlockTextureAtlas.GetRandomColor(textureSubId.Value);
		if (grasscover == "normal")
		{
			return grassColor;
		}
		return ColorUtil.ColorOverlay(capi.BlockTextureAtlas.GetRandomColor(Textures["up"].Baked.TextureSubId), grassColor, (grasscover == "verysparse") ? 0.5f : 0.75f);
	}

	public override int GetColor(ICoreClientAPI capi, BlockPos pos)
	{
		string grasscover = LastCodePart();
		if (grasscover == "none")
		{
			return base.GetColorWithoutTint(capi, pos);
		}
		int? textureSubId = grassTex?.Baked.TextureSubId;
		if (!textureSubId.HasValue)
		{
			return -1;
		}
		int grassColor = capi.BlockTextureAtlas.GetAverageColor(textureSubId.Value);
		if (ClimateColorMapResolved != null)
		{
			grassColor = capi.World.ApplyColorMapOnRgba(ClimateColorMapResolved, SeasonColorMapResolved, grassColor, pos.X, pos.Y, pos.Z, flipRb: false);
		}
		if (grasscover == "normal")
		{
			return grassColor;
		}
		return ColorUtil.ColorOverlay(capi.BlockTextureAtlas.GetAverageColor(Textures["up"].Baked.TextureSubId), grassColor, (grasscover == "verysparse") ? 0.5f : 0.75f);
	}
}
