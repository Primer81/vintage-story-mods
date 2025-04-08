using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.API.Client;

/// <summary>
/// Holds data about a texture. Also allows textures to be overlayed on top of one another.
/// </summary>
/// <example>
/// <code language="json">
///             "textures": {
///             	"charcoal": { "base": "block/coal/charcoal" },
///             	"coke": { "base": "block/coal/coke" },
///             	"ore-anthracite": { "base": "block/coal/anthracite" },
///             	"ore-lignite": { "base": "block/coal/lignite" },
///             	"ore-bituminouscoal": { "base": "block/coal/bituminous" },
///             	"ember": { "base": "block/coal/ember" }
///             },
/// </code>
/// <code language="json">
///             "textures": {
///             	"ore": {
///             		"base": "block/stone/rock/{rock}1",
///             		"overlays": [ "block/stone/ore/{ore}1" ]
///             	}
///             },
/// </code>
/// Connected textures example (See https://discord.com/channels/302152934249070593/479736466453561345/1134187385501007962)
/// <code language="json">
///             "textures": {
///             	"all": {
///             		"base": "block/stone/cobblestone/tiling/1",
///             		"tiles": [
///             			{ "base": "block/stone/cobblestone/tiling/*" }
///             		],
///             		"tilesWidth": 4
///             	}
///             }
/// </code>
/// </example>
[DocumentAsJson]
public class CompositeTexture
{
	public const char AlphaSeparator = 'å';

	public const string AlphaSeparatorRegexSearch = "å\\d+";

	public const string OverlaysSeparator = "++";

	public const char BlendmodeSeparator = '~';

	/// <summary>
	/// <!--<jsonoptional>Required</jsonoptional>-->
	/// The basic texture for this composite texture
	/// </summary>
	[DocumentAsJson]
	public AssetLocation Base;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// A set of textures to overlay above this texture. The base texture may be overlayed with any quantity of textures. These are baked together during texture atlas creation.
	/// </summary>
	[DocumentAsJson]
	public BlendedOverlayTexture[] BlendedOverlays;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// The texture may consists of any amount of alternatives, one of which will be randomly chosen when the block is placed in the world.
	/// </summary>
	[DocumentAsJson]
	public CompositeTexture[] Alternates;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// A way of basic support for connected textures. Textures should be named numerically from 1 to <see cref="F:Vintagestory.API.Client.CompositeTexture.TilesWidth" /> squared.
	/// <br />E.g., if <see cref="F:Vintagestory.API.Client.CompositeTexture.TilesWidth" /> is 3, the order follows the pattern of:<br />
	/// 1 2 3 <br />
	/// 4 5 6 <br />
	/// 7 8 9
	/// </summary>
	[DocumentAsJson]
	public CompositeTexture[] Tiles;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>0</jsondefault>-->
	/// The number of tiles in one direction that make up the full connected textures defined in <see cref="F:Vintagestory.API.Client.CompositeTexture.Tiles" />.
	/// </summary>
	[DocumentAsJson]
	public int TilesWidth;

	/// <summary>
	/// BakedCompositeTexture is an expanded, atlas friendly version of CompositeTexture. Required during texture atlas generation.
	/// </summary>
	public BakedCompositeTexture Baked;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>0</jsondefault>-->
	/// Rotation of the texture may only be a multiple of 90
	/// </summary>
	[DocumentAsJson]
	public int Rotation;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>255</jsondefault>-->
	/// Can be used to modify the opacity of the texture. 255 is fully opaque, 0 is fully transparent.
	/// </summary>
	[DocumentAsJson]
	public int Alpha = 255;

	[ThreadStatic]
	public static Dictionary<AssetLocation, CompositeTexture> basicTexturesCache;

	[ThreadStatic]
	public static Dictionary<AssetLocation, List<IAsset>> wildcardsCache;

	public AssetLocation WildCardNoFiles;

	/// <summary>
	/// <!--<jsonoptional>Obsolete</jsonoptional>-->
	/// Obsolete. Use <see cref="F:Vintagestory.API.Client.CompositeTexture.BlendedOverlays" /> instead.
	/// </summary>
	[DocumentAsJson]
	public AssetLocation[] Overlays
	{
		set
		{
			BlendedOverlays = value.Select((AssetLocation o) => new BlendedOverlayTexture
			{
				Base = o
			}).ToArray();
		}
	}

	public AssetLocation AnyWildCardNoFiles
	{
		get
		{
			if (WildCardNoFiles != null)
			{
				return WildCardNoFiles;
			}
			if (Alternates != null)
			{
				AssetLocation f = Alternates.Select((CompositeTexture ct) => ct.WildCardNoFiles).FirstOrDefault();
				if (f != null)
				{
					return f;
				}
			}
			return null;
		}
	}

	/// <summary>
	/// Creates a new empty composite texture
	/// </summary>
	public CompositeTexture()
	{
	}

	/// <summary>
	/// Creates a new empty composite texture with given base texture
	/// </summary>
	/// <param name="Base"></param>
	public CompositeTexture(AssetLocation Base)
	{
		this.Base = Base;
	}

	/// <summary>
	/// Creates a deep copy of the texture
	/// </summary>
	/// <returns></returns>
	public CompositeTexture Clone()
	{
		CompositeTexture[] alternatesClone = null;
		if (Alternates != null)
		{
			alternatesClone = new CompositeTexture[Alternates.Length];
			for (int k = 0; k < alternatesClone.Length; k++)
			{
				alternatesClone[k] = Alternates[k].CloneWithoutAlternates();
			}
		}
		CompositeTexture[] tilesClone = null;
		if (Tiles != null)
		{
			tilesClone = new CompositeTexture[Tiles.Length];
			for (int j = 0; j < tilesClone.Length; j++)
			{
				tilesClone[j] = tilesClone[j].CloneWithoutAlternates();
			}
		}
		CompositeTexture ct = new CompositeTexture
		{
			Base = Base.Clone(),
			Alternates = alternatesClone,
			Tiles = tilesClone,
			Rotation = Rotation,
			Alpha = Alpha,
			TilesWidth = TilesWidth
		};
		if (BlendedOverlays != null)
		{
			ct.BlendedOverlays = new BlendedOverlayTexture[BlendedOverlays.Length];
			for (int i = 0; i < ct.BlendedOverlays.Length; i++)
			{
				ct.BlendedOverlays[i] = BlendedOverlays[i].Clone();
			}
		}
		return ct;
	}

	internal CompositeTexture CloneWithoutAlternates()
	{
		CompositeTexture ct = new CompositeTexture
		{
			Base = Base.Clone(),
			Rotation = Rotation,
			Alpha = Alpha,
			TilesWidth = TilesWidth
		};
		if (BlendedOverlays != null)
		{
			ct.BlendedOverlays = new BlendedOverlayTexture[BlendedOverlays.Length];
			for (int j = 0; j < ct.BlendedOverlays.Length; j++)
			{
				ct.BlendedOverlays[j] = BlendedOverlays[j].Clone();
			}
		}
		if (Tiles != null)
		{
			ct.Tiles = new CompositeTexture[Tiles.Length];
			for (int i = 0; i < ct.Tiles.Length; i++)
			{
				ct.Tiles[i] = ct.Tiles[i].CloneWithoutAlternates();
			}
		}
		return ct;
	}

	/// <summary>
	/// Tests whether this is a basic CompositeTexture with an asset location only, no rotation, alpha, alternates or overlays
	/// </summary>
	/// <returns></returns>
	public bool IsBasic()
	{
		if (Rotation != 0 || Alpha != 255)
		{
			return false;
		}
		if (Alternates == null && BlendedOverlays == null)
		{
			return Tiles == null;
		}
		return false;
	}

	/// <summary>
	/// Expands the Composite Texture to a texture atlas friendly version and populates the Baked field. This method is called by the texture atlas managers.
	/// Won't have any effect if called after the texture atlasses have been created.
	/// </summary>
	public void Bake(IAssetManager assetManager)
	{
		if (Baked == null)
		{
			Baked = Bake(assetManager, this);
		}
	}

	/// <summary>
	/// Expands the Composite Texture to a texture atlas friendly version and populates the Baked field. This method can be called after the game world has loaded.
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="intoAtlas">The atlas to insert the baked texture.</param>
	public void RuntimeBake(ICoreClientAPI capi, ITextureAtlasAPI intoAtlas)
	{
		Baked = Bake(capi.Assets, this);
		RuntimeInsert(capi, intoAtlas, Baked);
		if (Baked.BakedVariants != null)
		{
			BakedCompositeTexture[] bakedVariants = Baked.BakedVariants;
			foreach (BakedCompositeTexture val in bakedVariants)
			{
				RuntimeInsert(capi, intoAtlas, val);
			}
		}
	}

	private bool RuntimeInsert(ICoreClientAPI capi, ITextureAtlasAPI intoAtlas, BakedCompositeTexture btex)
	{
		BitmapRef bmp = capi.Assets.Get(btex.BakedName).ToBitmap(capi);
		if (intoAtlas.InsertTexture(bmp, out var textureSubId, out var _))
		{
			btex.TextureSubId = textureSubId;
			capi.Render.RemoveTexture(btex.BakedName);
			return true;
		}
		bmp.Dispose();
		return false;
	}

	/// <summary>
	/// Expands a CompositeTexture to a texture atlas friendly version and populates the Baked field
	/// </summary>
	/// <param name="assetManager"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	public static BakedCompositeTexture Bake(IAssetManager assetManager, CompositeTexture ct)
	{
		BakedCompositeTexture bct = new BakedCompositeTexture();
		ct.WildCardNoFiles = null;
		if (ct.Base.EndsWithWildCard)
		{
			if (wildcardsCache == null)
			{
				wildcardsCache = new Dictionary<AssetLocation, List<IAsset>>();
			}
			if (!wildcardsCache.TryGetValue(ct.Base, out var assets))
			{
				List<IAsset> list = (wildcardsCache[ct.Base] = assetManager.GetManyInCategory("textures", ct.Base.Path.Substring(0, ct.Base.Path.Length - 1), ct.Base.Domain));
				assets = list;
			}
			if (assets.Count == 0)
			{
				ct.WildCardNoFiles = ct.Base;
				ct.Base = new AssetLocation("unknown");
			}
			else if (assets.Count == 1)
			{
				ct.Base = assets[0].Location.CloneWithoutPrefixAndEnding("textures/".Length);
			}
			else
			{
				int origLength = ((ct.Alternates != null) ? ct.Alternates.Length : 0);
				CompositeTexture[] alternates = new CompositeTexture[origLength + assets.Count - 1];
				if (ct.Alternates != null)
				{
					Array.Copy(ct.Alternates, alternates, ct.Alternates.Length);
				}
				if (basicTexturesCache == null)
				{
					basicTexturesCache = new Dictionary<AssetLocation, CompositeTexture>();
				}
				for (int l = 0; l < assets.Count; l++)
				{
					AssetLocation newLocation = assets[l].Location.CloneWithoutPrefixAndEnding("textures/".Length);
					if (l == 0)
					{
						ct.Base = newLocation;
						continue;
					}
					CompositeTexture act2;
					if (ct.Rotation == 0 && ct.Alpha == 255)
					{
						if (!basicTexturesCache.TryGetValue(newLocation, out act2))
						{
							CompositeTexture compositeTexture2 = (basicTexturesCache[newLocation] = new CompositeTexture(newLocation));
							act2 = compositeTexture2;
						}
					}
					else
					{
						act2 = new CompositeTexture(newLocation);
						act2.Rotation = ct.Rotation;
						act2.Alpha = ct.Alpha;
					}
					alternates[origLength + l - 1] = act2;
				}
				ct.Alternates = alternates;
			}
		}
		bct.BakedName = ct.Base.Clone();
		if (ct.BlendedOverlays != null)
		{
			bct.TextureFilenames = new AssetLocation[ct.BlendedOverlays.Length + 1];
			bct.TextureFilenames[0] = ct.Base;
			for (int k = 0; k < ct.BlendedOverlays.Length; k++)
			{
				BlendedOverlayTexture bov = ct.BlendedOverlays[k];
				bct.TextureFilenames[k + 1] = bov.Base;
				AssetLocation bakedName = bct.BakedName;
				string[] obj = new string[5] { bakedName.Path, "++", null, null, null };
				int blendMode = (int)bov.BlendMode;
				obj[2] = blendMode.ToString();
				obj[3] = "~";
				obj[4] = bov.Base.ToShortString();
				bakedName.Path = string.Concat(obj);
			}
		}
		else
		{
			bct.TextureFilenames = new AssetLocation[1] { ct.Base };
		}
		if (ct.Rotation != 0)
		{
			if (ct.Rotation != 90 && ct.Rotation != 180 && ct.Rotation != 270)
			{
				throw new Exception(string.Concat("Texture definition ", ct.Base, " has a rotation thats not 0, 90, 180 or 270. These are the only allowed values!"));
			}
			AssetLocation bakedName2 = bct.BakedName;
			bakedName2.Path = bakedName2.Path + "@" + ct.Rotation;
		}
		if (ct.Alpha != 255)
		{
			if (ct.Alpha < 0 || ct.Alpha > 255)
			{
				throw new Exception(string.Concat("Texture definition ", ct.Base, " has a alpha value outside the 0..255 range."));
			}
			AssetLocation bakedName3 = bct.BakedName;
			bakedName3.Path = bakedName3.Path + "å" + ct.Alpha;
		}
		if (ct.Alternates != null)
		{
			bct.BakedVariants = new BakedCompositeTexture[ct.Alternates.Length + 1];
			bct.BakedVariants[0] = bct;
			for (int j = 0; j < ct.Alternates.Length; j++)
			{
				bct.BakedVariants[j + 1] = Bake(assetManager, ct.Alternates[j]);
			}
		}
		if (ct.Tiles != null)
		{
			List<BakedCompositeTexture> tiles = new List<BakedCompositeTexture>();
			for (int i = 0; i < ct.Tiles.Length; i++)
			{
				if (ct.Tiles[i].Base.EndsWithWildCard)
				{
					if (wildcardsCache == null)
					{
						wildcardsCache = new Dictionary<AssetLocation, List<IAsset>>();
					}
					string basePath = ct.Base.Path.Substring(0, ct.Base.Path.Length - 1);
					List<IAsset> list = (wildcardsCache[ct.Base] = assetManager.GetManyInCategory("textures", basePath, ct.Base.Domain));
					List<IAsset> source = list;
					int len = "textures".Length + basePath.Length + "/".Length;
					List<IAsset> sortedassets = source.OrderBy((IAsset asset) => asset.Location.Path.Substring(len).RemoveFileEnding().ToInt()).ToList();
					for (int m = 0; m < sortedassets.Count; m++)
					{
						CompositeTexture act = new CompositeTexture(sortedassets[m].Location.CloneWithoutPrefixAndEnding("textures/".Length));
						act.Rotation = ct.Rotation;
						act.Alpha = ct.Alpha;
						BakedCompositeTexture bt = Bake(assetManager, act);
						bt.TilesWidth = ct.TilesWidth;
						tiles.Add(bt);
					}
				}
				else
				{
					BakedCompositeTexture bt2 = Bake(assetManager, ct.Tiles[i]);
					bt2.TilesWidth = ct.TilesWidth;
					tiles.Add(bt2);
				}
			}
			bct.BakedTiles = tiles.ToArray();
		}
		return bct;
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder();
		sb.Append(Base.ToString());
		sb.Append("@");
		sb.Append(Rotation);
		sb.Append("a");
		sb.Append(Alpha);
		if (Alternates != null)
		{
			sb.Append("alts:");
			CompositeTexture[] alternates = Alternates;
			for (int i = 0; i < alternates.Length; i++)
			{
				alternates[i].ToString(sb);
				sb.Append(",");
			}
		}
		if (BlendedOverlays != null)
		{
			sb.Append("ovs:");
			BlendedOverlayTexture[] blendedOverlays = BlendedOverlays;
			for (int i = 0; i < blendedOverlays.Length; i++)
			{
				blendedOverlays[i].ToString(sb);
				sb.Append(",");
			}
		}
		return sb.ToString();
	}

	public void ToString(StringBuilder sb)
	{
		sb.Append(Base.ToString());
		sb.Append("@");
		sb.Append(Rotation);
		sb.Append("a");
		sb.Append(Alpha);
		if (Alternates != null)
		{
			sb.Append("alts:");
			CompositeTexture[] alternates = Alternates;
			foreach (CompositeTexture val2 in alternates)
			{
				sb.Append(val2.ToString());
				sb.Append(",");
			}
		}
		if (BlendedOverlays != null)
		{
			sb.Append("ovs:");
			BlendedOverlayTexture[] blendedOverlays = BlendedOverlays;
			foreach (BlendedOverlayTexture val in blendedOverlays)
			{
				sb.Append(val.ToString());
				sb.Append(",");
			}
		}
	}

	public void FillPlaceholder(string search, string replace)
	{
		Base.Path = Base.Path.Replace(search, replace);
		if (BlendedOverlays != null)
		{
			BlendedOverlays.Foreach(delegate(BlendedOverlayTexture ov)
			{
				ov.Base.Path = ov.Base.Path.Replace(search, replace);
			});
		}
		if (Alternates != null)
		{
			Alternates.Foreach(delegate(CompositeTexture alt)
			{
				alt.FillPlaceholder(search, replace);
			});
		}
		if (Tiles != null)
		{
			Tiles.Foreach(delegate(CompositeTexture tile)
			{
				tile.FillPlaceholder(search, replace);
			});
		}
	}
}
