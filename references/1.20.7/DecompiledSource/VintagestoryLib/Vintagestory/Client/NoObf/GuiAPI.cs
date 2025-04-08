using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class GuiAPI : IGuiAPI
{
	private ClientMain game;

	public GuiManager guimgr;

	private TextTextureUtil textutil;

	private IconUtil iconutil;

	private TextDrawUtil prober;

	private SvgLoader svgLoader;

	public TextTextureUtil TextTexture => textutil;

	public IconUtil Icons => iconutil;

	public TextDrawUtil Text => prober;

	public List<GuiDialog> LoadedGuis => game.LoadedGuis;

	List<GuiDialog> IGuiAPI.OpenedGuis => game.OpenedGuis;

	public MeshRef QuadMeshRef => game.quadModel;

	public ElementBounds WindowBounds => new ElementWindowBounds();

	public GuiAPI(ClientMain game, ICoreClientAPI capi)
	{
		this.game = game;
		prober = new TextDrawUtil();
		textutil = new TextTextureUtil(capi);
		iconutil = new IconUtil(capi);
		svgLoader = new SvgLoader(capi);
	}

	public GuiComposer CreateCompo(string dialogName, ElementBounds bounds)
	{
		return game.GuiComposers.Create(dialogName, bounds);
	}

	public void DeleteTexture(int textureid)
	{
		game.Platform.GLDeleteTexture(textureid);
	}

	public void DrawSvg(IAsset svgAsset, ImageSurface intoSurface, int posx, int posy, int width = 0, int height = 0, int? color = 0)
	{
		svgLoader.DrawSvg(svgAsset, intoSurface, posx, posy, width, height, color);
	}

	public void DrawSvg(IAsset svgAsset, ImageSurface intoSurface, Matrix matrix, int posx, int posy, int width = 0, int height = 0, int? color = 0)
	{
		svgLoader.DrawSvg(svgAsset, intoSurface, matrix, posx, posy, width, height, color);
	}

	public LoadedTexture LoadSvg(AssetLocation loc, int textureWidth, int textureHeight, int width = 0, int height = 0, int? color = null)
	{
		IAsset asset = game.AssetManager.TryGet(loc);
		if (asset == null)
		{
			return null;
		}
		return svgLoader.LoadSvg(asset, textureWidth, textureHeight, width, height, color);
	}

	public LoadedTexture LoadSvgWithPadding(AssetLocation loc, int textureWidth, int textureHeight, int padding = 0, int? color = 0)
	{
		return LoadSvg(loc, textureWidth + 2 * padding, textureHeight + 2 * padding, textureWidth, textureHeight, color);
	}

	public int LoadCairoTexture(ImageSurface surface, bool linearMag)
	{
		return game.Platform.LoadCairoTexture(surface, linearMag);
	}

	public void LoadOrUpdateCairoTexture(ImageSurface surface, bool linearMag, ref LoadedTexture intoTexture)
	{
		game.Platform.LoadOrUpdateCairoTexture(surface, linearMag, ref intoTexture);
	}

	public Vec2i GetDialogPosition(string key)
	{
		return ClientSettings.Inst.GetDialogPosition(key);
	}

	public void SetDialogPosition(string key, Vec2i pos)
	{
		ClientSettings.Inst.SetDialogPosition(key, pos);
	}

	public void PlaySound(string soundname, bool randomizePitch = false, float volume = 1f)
	{
		game.PlaySound(new AssetLocation("sounds/" + soundname), randomizePitch, volume);
	}

	public void PlaySound(AssetLocation soundname, bool randomizePitch = false, float volume = 1f)
	{
		game.PlaySound(soundname, randomizePitch, volume);
	}

	public void RequestFocus(GuiDialog guiDialog)
	{
		guimgr.RequestFocus(guiDialog);
	}

	public void TriggerDialogOpened(GuiDialog guiDialog)
	{
		game.eventManager?.TriggerDialogOpened(guiDialog);
	}

	public void TriggerDialogClosed(GuiDialog guiDialog)
	{
		game.eventManager?.TriggerDialogClosed(guiDialog);
	}

	public void RegisterDialog(params GuiDialog[] dialogs)
	{
		game.RegisterDialog(dialogs);
	}

	public List<ElementBounds> GetDialogBoundsInArea(EnumDialogArea area)
	{
		List<ElementBounds> bounds = new List<ElementBounds>();
		foreach (GuiDialog openedGui in game.OpenedGuis)
		{
			foreach (GuiComposer composer in openedGui.Composers.Values)
			{
				if (composer.Bounds.Alignment == area)
				{
					bounds.Add(composer.Bounds);
				}
			}
		}
		return bounds;
	}

	public void OpenLink(string href)
	{
		game.EnqueueMainThreadTask(delegate
		{
			new GuiDialogConfirm(game.api, Lang.Get("Open below external link in a browser?") + "\n\n\n" + href, delegate(bool val)
			{
				if (val)
				{
					NetUtil.OpenUrlInBrowser(href);
				}
			}).TryOpen();
		}, "openlink");
	}
}
