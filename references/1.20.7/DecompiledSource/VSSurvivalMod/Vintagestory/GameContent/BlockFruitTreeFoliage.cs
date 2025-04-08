using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockFruitTreeFoliage : BlockFruitTreePart
{
	private Block branchBlock;

	public Dictionary<string, DynFoliageProperties> foliageProps = new Dictionary<string, DynFoliageProperties>();

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(this);
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		branchBlock = api.World.GetBlock(AssetLocation.Create(Attributes["branchBlock"].AsString(), Code.Domain));
		Dictionary<string, DynFoliageProperties> tmpProps = Attributes["foliageProperties"].AsObject<Dictionary<string, DynFoliageProperties>>();
		if (tmpProps.TryGetValue("base", out var baseProps))
		{
			foreach (KeyValuePair<string, DynFoliageProperties> val in tmpProps)
			{
				if (!(val.Key == "base"))
				{
					val.Value.Rebase(baseProps);
					foliageProps[val.Key] = val.Value;
					AssetLocation texturesBasePath = new AssetLocation(val.Value.TexturesBasePath);
					if (api is ICoreClientAPI capi)
					{
						foreach (CompositeTexture tex in val.Value.Textures.Values)
						{
							tex.Base.WithLocationPrefixOnce(texturesBasePath);
							if (tex.BlendedOverlays != null)
							{
								BlendedOverlayTexture[] blendedOverlays = tex.BlendedOverlays;
								for (int i = 0; i < blendedOverlays.Length; i++)
								{
									blendedOverlays[i].Base.WithLocationPrefixOnce(texturesBasePath);
								}
							}
						}
						val.Value.LeafParticlesTexture?.Base.WithLocationPrefixOnce(texturesBasePath);
						val.Value.BlossomParticlesTexture?.Base.WithLocationPrefixOnce(texturesBasePath);
						val.Value.GetOrLoadTexture(capi, "largeleaves-plain");
					}
				}
			}
			return;
		}
		foliageProps = tmpProps;
	}

	public override bool ShouldMergeFace(int facingIndex, Block neighbourBlock, int intraChunkIndex3d)
	{
		if (facingIndex == 1 || facingIndex == 2 || facingIndex == 4)
		{
			if (neighbourBlock != this)
			{
				return neighbourBlock == branchBlock;
			}
			return true;
		}
		return false;
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityFruitTreeFoliage bebranch)
		{
			return Lang.Get("fruittree-foliage-" + bebranch.TreeType, Lang.Get("foliagestate-" + bebranch.FoliageState.ToString().ToLowerInvariant()));
		}
		return base.GetPlacedBlockName(world, pos);
	}

	public override int GetColor(ICoreClientAPI capi, BlockPos pos)
	{
		int color = 10000536;
		BlockEntityFruitTreeFoliage bef = capi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFruitTreeFoliage;
		string climateTint = null;
		string seasonTint = null;
		if (bef != null)
		{
			string treeType = bef.TreeType;
			if (treeType != null && treeType.Length > 0)
			{
				DynFoliageProperties dynFoliageProperties = foliageProps[bef.TreeType];
				climateTint = dynFoliageProperties.ClimateColorMap;
				seasonTint = dynFoliageProperties.SeasonColorMap;
				TextureAtlasPosition texPos = bef["largeleaves-plain"];
				if (texPos != null)
				{
					color = texPos.AvgColor;
				}
			}
		}
		if (climateTint == null)
		{
			climateTint = "climatePlantTint";
		}
		if (seasonTint == null)
		{
			seasonTint = "seasonalFoliage";
		}
		return capi.World.ApplyColorMapOnRgba(climateTint, seasonTint, color, pos.X, pos.Y, pos.Z);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		BlockEntityFruitTreeFoliage bef = capi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFruitTreeFoliage;
		string climateTint = null;
		string seasonTint = null;
		int texSubId = 0;
		if (bef != null)
		{
			string treeType = bef.TreeType;
			if (treeType != null && treeType.Length > 0)
			{
				DynFoliageProperties dynFoliageProperties = foliageProps[bef.TreeType];
				climateTint = dynFoliageProperties.ClimateColorMap;
				seasonTint = dynFoliageProperties.SeasonColorMap;
				if (dynFoliageProperties.Textures.TryGetValue("largeleaves-plain", out var ctex))
				{
					texSubId = ctex.Baked.TextureSubId;
				}
			}
		}
		if (climateTint == null)
		{
			climateTint = "climatePlantTint";
		}
		if (seasonTint == null)
		{
			seasonTint = "seasonalFoliage";
		}
		int color = capi.BlockTextureAtlas.GetRandomColor(texSubId, rndIndex);
		return capi.World.ApplyColorMapOnRgba(climateTint, seasonTint, color, pos.X, pos.Y, pos.Z);
	}
}
