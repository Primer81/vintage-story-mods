using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Util;

namespace Vintagestory.Client.NoObf;

public class TextureAtlasManager : AsyncHelper.Multithreaded, ITextureAtlasAPI, ITextureLocationDictionary
{
	internal const int UnknownTextureSubId = 0;

	public static FrameBufferRef atlasFramebuffer;

	private static float[] equalWeight = new float[4] { 0.25f, 0.25f, 0.25f, 0.25f };

	private static int[] pixelsTmp = new int[4];

	public List<TextureAtlas> Atlasses = new List<TextureAtlas>();

	public List<LoadedTexture> AtlasTextures;

	public TextureAtlasPosition[] TextureAtlasPositionsByTextureSubId;

	public TextureAtlasPosition UnknownTexturePos;

	protected OrderedDictionary<AssetLocation, int> textureNamesDict = new OrderedDictionary<AssetLocation, int>();

	protected int nextTextureSubId;

	protected int reloadIteration;

	protected ClientMain game;

	protected Random rand = new Random();

	protected int textureSubId;

	protected HashSet<string> textureCodes = new HashSet<string>();

	private string itemclass;

	private TextureAtlas currentAtlas;

	private Dictionary<AssetLocation, BitmapRef> overlayTextures = new Dictionary<AssetLocation, BitmapRef>();

	private bool genMipmapsQueued;

	private bool autoRegenMipMaps = true;

	public int Count => textureNamesDict.Count;

	public TextureAtlasPosition UnknownTexturePosition => UnknownTexturePos;

	public int this[AssetLocationAndSource textureLoc] => textureNamesDict[textureLoc];

	public Size2i Size { get; set; }

	public TextureAtlasPosition this[AssetLocation textureLocation]
	{
		get
		{
			if (textureNamesDict.TryGetValue(textureLocation, out var textureSubId))
			{
				return TextureAtlasPositionsByTextureSubId[textureSubId];
			}
			return null;
		}
	}

	public float SubPixelPaddingX
	{
		get
		{
			float subPixelPadding = 0f;
			if (itemclass == "items")
			{
				subPixelPadding = ClientSettings.ItemAtlasSubPixelPadding / (float)Size.Width;
			}
			if (itemclass == "blocks")
			{
				subPixelPadding = ClientSettings.BlockAtlasSubPixelPadding / (float)Size.Width;
			}
			if (itemclass == "entities")
			{
				subPixelPadding = 0f;
			}
			return subPixelPadding;
		}
	}

	public float SubPixelPaddingY
	{
		get
		{
			float subPixelPadding = 0f;
			if (itemclass == "items")
			{
				subPixelPadding = ClientSettings.ItemAtlasSubPixelPadding / (float)Size.Height;
			}
			if (itemclass == "blocks")
			{
				subPixelPadding = ClientSettings.BlockAtlasSubPixelPadding / (float)Size.Height;
			}
			if (itemclass == "entities")
			{
				subPixelPadding = 0f;
			}
			return subPixelPadding;
		}
	}

	public TextureAtlasPosition[] Positions => TextureAtlasPositionsByTextureSubId;

	List<LoadedTexture> ITextureAtlasAPI.AtlasTextures => AtlasTextures;

	public TextureAtlasManager(ClientMain game)
	{
		this.game = game;
		int maxTextureSize = game.Platform.GlGetMaxTextureSize();
		Size = new Size2i(GameMath.Clamp(maxTextureSize, 512, ClientSettings.MaxTextureAtlasWidth), GameMath.Clamp(maxTextureSize, 512, ClientSettings.MaxTextureAtlasHeight));
		textureNamesDict[new AssetLocationAndSource("unknown")] = textureSubId++;
	}

	public TextureAtlas CreateNewAtlas(string itemclass)
	{
		this.itemclass = itemclass;
		currentAtlas = new TextureAtlas(Size.Width, Size.Height, SubPixelPaddingX, SubPixelPaddingY);
		addCommonTextures();
		Atlasses.Add(currentAtlas);
		return currentAtlas;
	}

	public TextureAtlas RuntimeCreateNewAtlas(string itemclass)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Attempting to create an additional texture atlas outside of the main thread. This is not possible as we have only one OpenGL context!");
		}
		TextureAtlas textureAtlas = CreateNewAtlas(itemclass);
		LoadedTexture atlasTexture = textureAtlas.Upload(game);
		AtlasTextures.Add(atlasTexture);
		return textureAtlas;
	}

	private void addCommonTextures()
	{
		foreach (KeyValuePair<AssetLocation, int> val in textureNamesDict)
		{
			AssetLocationAndSource textureName = val.Key as AssetLocationAndSource;
			if (textureName.AddToAllAtlasses)
			{
				IAsset asset = game.AssetManager.TryGet(textureName);
				currentAtlas.InsertTexture(val.Value, game.api, asset);
			}
		}
	}

	public bool AddTextureLocation(AssetLocationAndSource loc)
	{
		if (textureNamesDict.ContainsKey(loc))
		{
			return false;
		}
		textureNamesDict[loc] = textureSubId++;
		return true;
	}

	public void SetTextureLocation(AssetLocationAndSource loc)
	{
		textureNamesDict[loc] = textureSubId++;
	}

	public int GetOrAddTextureLocation(AssetLocationAndSource loc)
	{
		if (!textureNamesDict.TryGetValue(loc, out var result))
		{
			result = textureSubId++;
			textureNamesDict[loc] = result;
		}
		return result;
	}

	public bool ContainsKey(AssetLocation loc)
	{
		return textureNamesDict.ContainsKey(loc);
	}

	public void GenFramebuffer()
	{
		DisposeFrameBuffer();
		atlasFramebuffer = game.Platform.CreateFramebuffer(new FramebufferAttrs("Render2DLoadedTexture", Size.Width, Size.Height)
		{
			Attachments = new FramebufferAttrsAttachment[1]
			{
				new FramebufferAttrsAttachment
				{
					AttachmentType = EnumFramebufferAttachment.ColorAttachment0,
					Texture = new RawTexture
					{
						Width = Size.Width,
						Height = Size.Height,
						TextureId = AtlasTextures[0].TextureId
					}
				}
			}
		});
	}

	public void RenderTextureIntoAtlas(int atlasTextureId, LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, float targetX, float targetY, float alphaTest = 0f)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Attempting to insert a texture into the atlas outside of the main thread. This is not possible as we have only one OpenGL context!");
		}
		game.RenderTextureIntoFrameBuffer(atlasTextureId, fromTexture, sourceX, sourceY, sourceWidth, sourceHeight, atlasFramebuffer, targetX, targetY, alphaTest);
	}

	public bool GetOrInsertTexture(AssetLocation path, out int textureSubId, out TextureAtlasPosition texPos, CreateTextureDelegate onCreate = null, float alphaTest = 0f)
	{
		return GetOrInsertTexture(new AssetLocationAndSource(path), out textureSubId, out texPos, onCreate, alphaTest);
	}

	public bool GetOrInsertTexture(AssetLocationAndSource loc, out int textureSubId, out TextureAtlasPosition texPos, CreateTextureDelegate onCreate = null, float alphaTest = 0f)
	{
		if (onCreate == null)
		{
			onCreate = delegate
			{
				IBitmap bitmap = LoadCompositeBitmap(loc);
				if (bitmap.Width == 0 && bitmap.Height == 0)
				{
					game.Logger.Warning("GetOrInsertTexture() on path {0}: Bitmap width and height is 0! Either missing or corrupt image file. Will use unknown texture.", loc);
				}
				return bitmap;
			};
		}
		if (textureNamesDict.TryGetValue(loc, out textureSubId))
		{
			texPos = TextureAtlasPositionsByTextureSubId[textureSubId];
			if (texPos.reloadIteration != reloadIteration)
			{
				IBitmap bmp = onCreate();
				if (bmp == null)
				{
					return false;
				}
				runtimeUpdateTexture(bmp, texPos, alphaTest);
			}
			return true;
		}
		texPos = null;
		textureSubId = 0;
		IBitmap bmp2 = onCreate();
		if (bmp2 == null)
		{
			return false;
		}
		bool num = InsertTexture(bmp2, out textureSubId, out texPos, alphaTest);
		if (num)
		{
			textureNamesDict[loc] = textureSubId;
		}
		return num;
	}

	[Obsolete("Use GetOrInsertTexture() instead. It's more efficient to load the bmp only if the texture was not found in the cache")]
	public bool InsertTextureCached(AssetLocation path, IBitmap bmp, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f)
	{
		AssetLocationAndSource loc = new AssetLocationAndSource(path);
		if (textureNamesDict.TryGetValue(loc, out textureSubId))
		{
			texPos = TextureAtlasPositionsByTextureSubId[textureSubId];
			if (texPos.reloadIteration != reloadIteration)
			{
				runtimeUpdateTexture(bmp, texPos, alphaTest);
			}
			return true;
		}
		bool num = InsertTexture(bmp, out textureSubId, out texPos, alphaTest);
		if (num)
		{
			textureNamesDict[loc] = textureSubId;
		}
		return num;
	}

	public bool InsertTexture(IBitmap bmp, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Attempting to insert a texture into the atlas outside of the main thread. This is not possible as we have only one OpenGL context!");
		}
		if (!AllocateTextureSpace(bmp.Width, bmp.Height, out textureSubId, out texPos))
		{
			return false;
		}
		runtimeUpdateTexture(bmp, texPos, alphaTest);
		return true;
	}

	private void runtimeUpdateTexture(IBitmap bmp, TextureAtlasPosition texPos, float alphaTest = 0f)
	{
		if (alphaTest < 0.0001f)
		{
			game.Platform.LoadIntoTexture(bmp, texPos.atlasTextureId, (int)(texPos.x1 * (float)Size.Width), (int)(texPos.y1 * (float)Size.Height));
		}
		else
		{
			bool glScissorFlagEnabled = game.Platform.GlScissorFlagEnabled;
			if (glScissorFlagEnabled)
			{
				game.Platform.GlScissorFlag(enable: false);
			}
			game.Platform.GlToggleBlend(on: false);
			LoadedTexture tex = new LoadedTexture(game.api, game.Platform.LoadTexture(bmp), bmp.Width, bmp.Height);
			RenderTextureIntoAtlas(texPos.atlasTextureId, tex, 0f, 0f, bmp.Width, bmp.Height, texPos.x1 * (float)Size.Width, texPos.y1 * (float)Size.Height, alphaTest);
			tex.Dispose();
			if (glScissorFlagEnabled)
			{
				game.Platform.GlScissorFlag(enable: true);
			}
		}
		if (autoRegenMipMaps && !genMipmapsQueued)
		{
			genMipmapsQueued = true;
			game.EnqueueMainThreadTask(delegate
			{
				game.EnqueueMainThreadTask(delegate
				{
					RegenMipMaps(texPos.atlasNumber);
					genMipmapsQueued = false;
				}, "genmipmaps");
			}, "genmipmaps");
		}
		int wdt = bmp.Width;
		int hgt = bmp.Height;
		pixelsTmp[0] = bmp.GetPixel((int)(0.35f * (float)wdt), (int)(0.35f * (float)hgt)).ToArgb();
		pixelsTmp[1] = bmp.GetPixel((int)(0.65f * (float)wdt), (int)(0.35f * (float)hgt)).ToArgb();
		pixelsTmp[2] = bmp.GetPixel((int)(0.35f * (float)wdt), (int)(0.65f * (float)hgt)).ToArgb();
		pixelsTmp[3] = bmp.GetPixel((int)(0.65f * (float)wdt), (int)(0.65f * (float)hgt)).ToArgb();
		texPos.AvgColor = ColorUtil.ReverseColorBytes(ColorUtil.ColorAverage(pixelsTmp, equalWeight));
		texPos.RndColors = new int[30];
		for (int i = 0; i < 30; i++)
		{
			int color = 0;
			for (int j = 0; j < 15; j++)
			{
				color = bmp.GetPixel((int)(rand.NextDouble() * (double)wdt), (int)(rand.NextDouble() * (double)hgt)).ToArgb();
				if (((color >> 24) & 0xFF) > 5)
				{
					break;
				}
			}
			texPos.RndColors[i] = color;
		}
	}

	public void RegenMipMaps(int atlasNumber)
	{
		game.Platform.BuildMipMaps(AtlasTextures[atlasNumber].TextureId);
	}

	public bool InsertTexture(BitmapRef bmp, AssetLocationAndSource loc, out int textureSubIdOut)
	{
		if (bmp.Width > Size.Width || bmp.Height > Size.Height)
		{
			throw new InvalidOperationException("Cannot insert texture larger than the atlas itself");
		}
		textureSubIdOut = GetOrAddTextureLocation(loc);
		bool added = currentAtlas.InsertTexture(textureSubIdOut, bmp.Width, bmp.Height, bmp.Pixels);
		if (!added)
		{
			RuntimeCreateNewAtlas(itemclass);
			return currentAtlas.InsertTexture(textureSubIdOut, bmp.Width, bmp.Height, bmp.Pixels);
		}
		return added;
	}

	public bool AllocateTextureSpace(int width, int height, out int textureSubId, out TextureAtlasPosition texPos)
	{
		if (width > Size.Width || height > Size.Height)
		{
			throw new InvalidOperationException("Cannot create allocate texture space larger than the atlas itself");
		}
		textureSubId = ++nextTextureSubId;
		TextureAtlasPosition tp = null;
		int i = 0;
		foreach (TextureAtlas atlass in Atlasses)
		{
			tp = atlass.AllocateTextureSpace(textureSubId, width, height);
			if (tp != null)
			{
				break;
			}
			i++;
		}
		if (tp == null)
		{
			tp = RuntimeCreateNewAtlas(itemclass).AllocateTextureSpace(textureSubId, width, height);
		}
		tp.atlasNumber = (byte)i;
		tp.atlasTextureId = AtlasTextures[i].TextureId;
		texPos = tp;
		TextureAtlasPositionsByTextureSubId = TextureAtlasPositionsByTextureSubId.Append(texPos);
		return true;
	}

	public void FreeTextureSpace(int textureSubId)
	{
		using List<TextureAtlas>.Enumerator enumerator = Atlasses.GetEnumerator();
		while (enumerator.MoveNext() && !enumerator.Current.FreeTextureSpace(textureSubId))
		{
		}
	}

	public virtual void PopulateTextureAtlassesFromTextures()
	{
		TextureAtlasPositionsByTextureSubId = new TextureAtlasPosition[textureNamesDict.Count];
		nextTextureSubId = textureNamesDict.Count - 1;
		BakedBitmap[] bitmaps = new BakedBitmap[textureNamesDict.Count];
		if (itemclass != "entities")
		{
			StartWorkerThread(delegate
			{
				LoadBitmaps(bitmaps);
			});
		}
		LoadBitmaps(bitmaps);
		while (WorkerThreadsInProgress() && !game.disposed)
		{
			Thread.Sleep(10);
		}
		addCommonTextures();
		foreach (KeyValuePair<AssetLocation, int> val in textureNamesDict)
		{
			int textureSubId = val.Value;
			BakedBitmap bcBmp = bitmaps[textureSubId];
			if (bcBmp != null && !(val.Key as AssetLocationAndSource).AddToAllAtlasses && !currentAtlas.InsertTexture(textureSubId, bcBmp.Width, bcBmp.Height, bcBmp.TexturePixels))
			{
				CreateNewAtlas(itemclass);
				if (!currentAtlas.InsertTexture(textureSubId, bcBmp.Width, bcBmp.Height, bcBmp.TexturePixels))
				{
					throw new Exception("Texture bigger than max supported texture size!");
				}
			}
		}
		FinishedOverlays();
	}

	private void LoadBitmaps(BakedBitmap[] bitmaps)
	{
		foreach (KeyValuePair<AssetLocation, int> val in textureNamesDict)
		{
			AssetLocationAndSource textureName = val.Key as AssetLocationAndSource;
			if (AsyncHelper.CanProceedOnThisThread(ref textureName.loadedAlready))
			{
				int textureSubId = val.Value;
				BakedBitmap bcBmp = LoadCompositeBitmap(game, textureName, overlayTextures);
				bitmaps[textureSubId] = bcBmp;
			}
		}
	}

	public virtual void ComposeTextureAtlasses_StageA()
	{
		AtlasTextures = new List<LoadedTexture>();
		foreach (TextureAtlas atlass in Atlasses)
		{
			LoadedTexture texture = atlass.Upload(game);
			AtlasTextures.Add(texture);
		}
		game.Platform.Logger.Notification("Composed {0} {1}x{2} " + itemclass + " texture atlases from {3} textures", AtlasTextures.Count, Size.Width, Size.Height, textureNamesDict.Count);
	}

	public virtual void ComposeTextureAtlasses_StageB()
	{
		foreach (TextureAtlas atlas in Atlasses)
		{
			game.Platform.BuildMipMaps(atlas.textureId);
		}
	}

	public virtual void ComposeTextureAtlasses_StageC()
	{
		int atlasId = 0;
		foreach (TextureAtlas atlass in Atlasses)
		{
			atlass.PopulateAtlasPositions(TextureAtlasPositionsByTextureSubId, atlasId++);
		}
		UnknownTexturePos = TextureAtlasPositionsByTextureSubId[0];
		for (int texSubId = 0; texSubId < TextureAtlasPositionsByTextureSubId.Length; texSubId++)
		{
			TextureAtlasPosition texPos = TextureAtlasPositionsByTextureSubId[texSubId];
			TextureAtlas atlas = Atlasses[texPos.atlasNumber];
			float texWidth = texPos.x2 - texPos.x1;
			float texHeight = texPos.y2 - texPos.y1;
			pixelsTmp[0] = atlas.GetPixel(texPos.x1 + 0.35f * texWidth, texPos.y1 + 0.35f * texHeight);
			pixelsTmp[1] = atlas.GetPixel(texPos.x1 + 0.65f * texWidth, texPos.y1 + 0.35f * texHeight);
			pixelsTmp[2] = atlas.GetPixel(texPos.x1 + 0.35f * texWidth, texPos.y1 + 0.65f * texHeight);
			pixelsTmp[3] = atlas.GetPixel(texPos.x1 + 0.65f * texWidth, texPos.y1 + 0.65f * texHeight);
			texPos.AvgColor = ColorUtil.ReverseColorBytes(ColorUtil.ColorAverage(pixelsTmp, equalWeight));
			texPos.RndColors = new int[30];
			for (int i = 0; i < 30; i++)
			{
				int color = 0;
				for (int j = 0; j < 15; j++)
				{
					color = atlas.GetPixel((float)((double)texPos.x1 + rand.NextDouble() * (double)texWidth), (float)((double)texPos.y1 + rand.NextDouble() * (double)texHeight));
					if (((color >> 24) & 0xFF) > 5)
					{
						break;
					}
				}
				texPos.RndColors[i] = color;
			}
		}
		foreach (TextureAtlas atlass2 in Atlasses)
		{
			atlass2.DisposePixels();
		}
	}

	public virtual TextureAtlasManager ReloadTextures()
	{
		reloadIteration++;
		foreach (TextureAtlas atlas in Atlasses)
		{
			atlas.ReinitPixels();
			foreach (KeyValuePair<AssetLocation, int> val2 in textureNamesDict)
			{
				TextureAtlasPosition tpos2 = TextureAtlasPositionsByTextureSubId[val2.Value];
				AssetLocationAndSource textureName2 = val2.Key as AssetLocationAndSource;
				if (textureName2.AddToAllAtlasses)
				{
					game.AssetManager.TryGet(textureName2);
					BitmapRef colormap = game.Platform.CreateBitmapFromPng(game.AssetManager.Get(textureName2));
					atlas.UpdateTexture(tpos2, colormap.Pixels);
				}
			}
		}
		foreach (KeyValuePair<AssetLocation, int> val in textureNamesDict)
		{
			TextureAtlasPosition tpos = TextureAtlasPositionsByTextureSubId[val.Value];
			AssetLocationAndSource textureName = val.Key as AssetLocationAndSource;
			if (textureName.AddToAllAtlasses)
			{
				continue;
			}
			int[] pixels;
			if (textureName.loadedAlready == 2)
			{
				pixels = game.Platform.CreateBitmapFromPng(game.AssetManager.Get(textureName)).Pixels;
			}
			else
			{
				BakedBitmap bcBmp = LoadCompositeBitmap(game, textureName, overlayTextures);
				int w = (int)Math.Round((tpos.x2 - tpos.x1 + 2f * SubPixelPaddingX) * (float)Size.Width);
				int h = (int)Math.Round((tpos.y2 - tpos.y1 + 2f * SubPixelPaddingY) * (float)Size.Height);
				if (w != bcBmp.Width || h != bcBmp.Height)
				{
					game.Platform.Logger.Error("Texture {0} changed in size ({1}x{2} => {3}x{4}). Runtime reload with changing texture sizes is not supported. Will not update.", textureName, w, h, bcBmp.Width, bcBmp.Height);
					continue;
				}
				pixels = bcBmp.Pixels;
			}
			Atlasses[tpos.atlasNumber].UpdateTexture(tpos, pixels);
		}
		FinishedOverlays();
		for (int i = 0; i < Atlasses.Count; i++)
		{
			LoadedTexture texAtlas = AtlasTextures[i];
			Atlasses[i].DrawToTexture(game.Platform, texAtlas);
			Atlasses[i].DisposePixels();
		}
		return this;
	}

	private void FinishedOverlays()
	{
		foreach (BitmapRef value in overlayTextures.Values)
		{
			value?.Dispose();
		}
		overlayTextures.Clear();
	}

	public virtual TextureAtlasManager PauseRegenMipmaps()
	{
		autoRegenMipMaps = false;
		return this;
	}

	public virtual TextureAtlasManager ResumeRegenMipmaps()
	{
		autoRegenMipMaps = true;
		for (int i = 0; i < Atlasses.Count; i++)
		{
			RegenMipMaps(i);
		}
		return this;
	}

	public IBitmap LoadCompositeBitmap(AssetLocationAndSource path)
	{
		return LoadCompositeBitmap(game, path);
	}

	public static BakedBitmap LoadCompositeBitmap(ClientMain game, string compositeTextureName)
	{
		return LoadCompositeBitmap(game, new AssetLocationAndSource(compositeTextureName));
	}

	public static AssetLocationAndSource ToTextureAssetLocation(AssetLocationAndSource loc)
	{
		AssetLocationAndSource assetLocationAndSource = new AssetLocationAndSource(loc.Domain, "textures/" + loc.Path, loc.Source);
		assetLocationAndSource.Path = assetLocationAndSource.Path.Replace("@90", "").Replace("@180", "").Replace("@270", "");
		assetLocationAndSource.Path = Regex.Replace(assetLocationAndSource.Path, "å\\d+", "");
		assetLocationAndSource.WithPathAppendixOnce(".png");
		return assetLocationAndSource;
	}

	public static int getRotation(AssetLocationAndSource loc)
	{
		if (loc.Path.Contains("@90"))
		{
			return 90;
		}
		if (loc.Path.Contains("@180"))
		{
			return 180;
		}
		if (loc.Path.Contains("@270"))
		{
			return 270;
		}
		return 0;
	}

	public static int getAlpha(AssetLocationAndSource tex)
	{
		int index = tex.Path.IndexOf('å');
		if (index < 0)
		{
			return 255;
		}
		return tex.Path.Substring(index + 1, Math.Min(tex.Path.Length - index - 1, 3)).ToInt(255);
	}

	public static BakedBitmap LoadCompositeBitmap(ClientMain game, AssetLocationAndSource compositeTextureLocation)
	{
		return LoadCompositeBitmap(game, compositeTextureLocation, null);
	}

	public static BakedBitmap LoadCompositeBitmap(ClientMain game, AssetLocationAndSource compositeTextureLocation, Dictionary<AssetLocation, BitmapRef> cache)
	{
		BakedBitmap bcBmp = new BakedBitmap();
		int rot = getRotation(compositeTextureLocation);
		int alpha = getAlpha(compositeTextureLocation);
		if (!compositeTextureLocation.Path.Contains("++"))
		{
			bcBmp.TexturePixels = LoadBitmapPixels(game, compositeTextureLocation, rot, alpha, null, out var readWidth, out var readHeight);
			bcBmp.Width = readWidth;
			bcBmp.Height = readHeight;
			return bcBmp;
		}
		string[] parts = compositeTextureLocation.ToString().Split(new string[1] { "++" }, StringSplitOptions.None);
		for (int i = 0; i < parts.Length; i++)
		{
			string[] subparts = parts[i].Split('~');
			EnumColorBlendMode mode = ((subparts.Length > 1) ? ((EnumColorBlendMode)subparts[0].ToInt()) : EnumColorBlendMode.Normal);
			AssetLocation loc = AssetLocation.Create((subparts.Length > 1) ? subparts[1] : subparts[0], compositeTextureLocation.Domain);
			if (rot != 0)
			{
				loc.WithPathAppendixOnce("@" + rot);
			}
			AssetLocationAndSource partTexture = new AssetLocationAndSource(loc, compositeTextureLocation.Source);
			int readWidth2;
			int readHeight2;
			int[] texturePixels = LoadBitmapPixels(game, partTexture, rot, alpha, cache, out readWidth2, out readHeight2);
			if (bcBmp.TexturePixels == null)
			{
				bcBmp.TexturePixels = texturePixels;
				bcBmp.Width = readWidth2;
				bcBmp.Height = readHeight2;
			}
			else if (bcBmp.Width != readWidth2 || bcBmp.Height != readHeight2)
			{
				game.Platform.Logger.Warning("Textureoverlay {0} ({2}x{3} pixel) is not the same width and height as base texture in composite texture {1} ({4}x{5} pixel), ignoring.", partTexture, compositeTextureLocation, readWidth2, readHeight2, bcBmp.Width, bcBmp.Height);
			}
			else
			{
				for (int p = 0; p < bcBmp.TexturePixels.Length; p++)
				{
					bcBmp.TexturePixels[p] = ColorBlend.Blend(mode, bcBmp.TexturePixels[p], texturePixels[p]);
				}
			}
		}
		return bcBmp;
	}

	private static int[] LoadBitmapPixels(ClientMain game, AssetLocationAndSource source, int rot, int alpha, Dictionary<AssetLocation, BitmapRef> cache, out int readWidth, out int readHeight)
	{
		BitmapRef bmp;
		if (cache != null)
		{
			lock (cache)
			{
				if (!cache.TryGetValue(source, out bmp))
				{
					bmp = LoadBitmap(game, ToTextureAssetLocation(source));
					cache.Add(source, bmp);
				}
			}
		}
		else
		{
			bmp = LoadBitmap(game, ToTextureAssetLocation(source));
		}
		if (bmp == null)
		{
			readWidth = 0;
			readHeight = 0;
			return null;
		}
		int[] pixelsTransformed = bmp.GetPixelsTransformed(rot, alpha);
		bool cw = rot % 180 == 90;
		readWidth = (cw ? bmp.Height : bmp.Width);
		readHeight = (cw ? bmp.Width : bmp.Height);
		if (cache == null)
		{
			bmp.Dispose();
		}
		return pixelsTransformed;
	}

	public static BitmapRef LoadBitmap(ClientMain game, AssetLocationAndSource textureLoc)
	{
		if (textureLoc == null)
		{
			return null;
		}
		IAsset asset = null;
		try
		{
			asset = game.AssetManager.TryGet(textureLoc);
			byte[] fileData;
			if (asset == null)
			{
				game.Logger.Warning("Texture asset '{0}' not found (defined in {1}).", textureLoc, textureLoc.Source);
				fileData = game.AssetManager.Get("textures/unknown.png").Data;
			}
			else
			{
				fileData = asset.Data;
			}
			BitmapRef bmp = game.Platform.CreateBitmapFromPng(fileData, fileData.Length);
			if (bmp.Width / 4 * 4 != bmp.Width)
			{
				game.Platform.Logger.Warning("Texture {0} width is not divisible by 4, will probably glitch when mipmapped", textureLoc);
			}
			else if (bmp.Height / 4 * 4 != bmp.Height)
			{
				game.Platform.Logger.Warning("Texture {0} height is not divisible by 4, will probably glitch when mipmapped", textureLoc);
			}
			return bmp;
		}
		catch (Exception)
		{
			game.Logger.Notification("The quest as to why Fulgen crashes here.");
			game.Logger.Notification("textureLoc={0}", textureLoc);
			game.Logger.Notification("asset={0}", asset);
			throw;
		}
	}

	public virtual void LoadShapeTextureCodes(Shape shape)
	{
		textureCodes.Clear();
		if (shape != null)
		{
			ShapeElement[] elements = shape.Elements;
			foreach (ShapeElement elem in elements)
			{
				AddTexturesForElement(elem);
			}
		}
	}

	private void AddTexturesForElement(ShapeElement elem)
	{
		ShapeElementFace[] facesResolved = elem.FacesResolved;
		foreach (ShapeElementFace face in facesResolved)
		{
			if (face != null && face.Texture.Length > 0)
			{
				textureCodes.Add(face.Texture);
			}
		}
		if (elem.Children != null)
		{
			ShapeElement[] children = elem.Children;
			foreach (ShapeElement child in children)
			{
				AddTexturesForElement(child);
			}
		}
	}

	public void ResolveTextureDict(FastSmallDictionary<string, CompositeTexture> texturesDict)
	{
		if (texturesDict.TryGetValue("sides", out var ct))
		{
			texturesDict.AddIfNotPresent("west", ct);
			texturesDict.AddIfNotPresent("east", ct);
			texturesDict.AddIfNotPresent("north", ct);
			texturesDict.AddIfNotPresent("south", ct);
			texturesDict.AddIfNotPresent("up", ct);
			texturesDict.AddIfNotPresent("down", ct);
		}
		if (texturesDict.TryGetValue("horizontals", out ct))
		{
			texturesDict.AddIfNotPresent("west", ct);
			texturesDict.AddIfNotPresent("east", ct);
			texturesDict.AddIfNotPresent("north", ct);
			texturesDict.AddIfNotPresent("south", ct);
		}
		if (texturesDict.TryGetValue("verticals", out ct))
		{
			texturesDict.AddIfNotPresent("up", ct);
			texturesDict.AddIfNotPresent("down", ct);
		}
		if (texturesDict.TryGetValue("westeast", out ct))
		{
			texturesDict.AddIfNotPresent("west", ct);
			texturesDict.AddIfNotPresent("east", ct);
		}
		if (texturesDict.TryGetValue("northsouth", out ct))
		{
			texturesDict.AddIfNotPresent("north", ct);
			texturesDict.AddIfNotPresent("south", ct);
		}
		if (!texturesDict.TryGetValue("all", out ct))
		{
			return;
		}
		texturesDict.Remove("all");
		foreach (string textureCode in textureCodes)
		{
			texturesDict.AddIfNotPresent(textureCode, ct);
		}
	}

	public virtual void Dispose()
	{
		foreach (TextureAtlas atlass in Atlasses)
		{
			atlass.DisposePixels();
		}
		if (AtlasTextures != null)
		{
			for (int i = 0; i < AtlasTextures.Count; i++)
			{
				AtlasTextures[i].Dispose();
			}
			AtlasTextures.Clear();
			DisposeFrameBuffer();
		}
	}

	private void DisposeFrameBuffer()
	{
		if (atlasFramebuffer != null)
		{
			game.Platform.DisposeFrameBuffer(atlasFramebuffer, disposeTextures: false);
		}
		atlasFramebuffer = null;
	}

	public int GetRandomColor(int textureSubId)
	{
		return TextureAtlasPositionsByTextureSubId[textureSubId].RndColors[rand.Next(30)];
	}

	public int GetRandomColor(int textureSubId, int rndIndex)
	{
		TextureAtlasPosition texPos = TextureAtlasPositionsByTextureSubId[textureSubId];
		return GetRandomColor(texPos, rndIndex);
	}

	public int GetRandomColor(TextureAtlasPosition texPos, int rndIndex)
	{
		if (rndIndex < 0)
		{
			rndIndex = rand.Next(30);
		}
		return texPos.RndColors[rndIndex];
	}

	public int[] GetRandomColors(TextureAtlasPosition texPos)
	{
		return texPos.RndColors;
	}

	public int GetAverageColor(int textureSubId)
	{
		return TextureAtlasPositionsByTextureSubId[textureSubId].AvgColor;
	}

	public bool InsertTexture(byte[] bytes, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f)
	{
		BitmapExternal bitmap = game.api.Render.BitmapCreateFromPng(bytes);
		return InsertTexture(bitmap, out textureSubId, out texPos, alphaTest);
	}

	public bool InsertTextureCached(AssetLocation path, byte[] bytes, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f)
	{
		BitmapExternal bitmap = game.api.Render.BitmapCreateFromPng(bytes);
		return GetOrInsertTexture(path, out textureSubId, out texPos, () => bitmap, alphaTest);
	}

	public bool GetOrInsertTexture(CompositeTexture ct, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f)
	{
		ct.Bake(game.AssetManager);
		AssetLocationAndSource alocs = new AssetLocationAndSource(ct.Baked.BakedName, "Shape file ", ct.Base);
		return GetOrInsertTexture(ct.Baked.BakedName, out textureSubId, out texPos, () => LoadCompositeBitmap(game, alocs), alphaTest);
	}

	public virtual void CollectAndBakeTexturesFromShape(Shape compositeShape, IDictionary<string, CompositeTexture> targetDict, AssetLocation baseLoc)
	{
		throw new NotImplementedException();
	}
}
