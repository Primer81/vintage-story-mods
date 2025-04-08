using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class LiquidItemStackRenderer : ModSystem
{
	private ICoreClientAPI capi;

	private Dictionary<string, LoadedTexture> litreTextTextures;

	private CairoFont stackSizeFont;

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		stackSizeFont = CairoFont.WhiteSmallText().WithFontSize((float)GuiStyle.DetailFontSize);
		stackSizeFont.FontWeight = FontWeight.Bold;
		stackSizeFont.Color = new double[4] { 1.0, 1.0, 1.0, 1.0 };
		stackSizeFont.StrokeColor = new double[4] { 0.0, 0.0, 0.0, 1.0 };
		stackSizeFont.StrokeWidth = (double)RuntimeEnv.GUIScale + 0.25;
		litreTextTextures = new Dictionary<string, LoadedTexture>();
		api.Settings.AddWatcher("guiScale", delegate(float newvalue)
		{
			stackSizeFont.StrokeWidth = (double)newvalue + 0.25;
			foreach (KeyValuePair<string, LoadedTexture> litreTextTexture in litreTextTextures)
			{
				litreTextTexture.Value.Dispose();
			}
			litreTextTextures.Clear();
		});
		api.Event.LeaveWorld += Event_LeaveWorld;
		api.Event.LevelFinalize += Event_LevelFinalize;
	}

	private void Event_LevelFinalize()
	{
		foreach (CollectibleObject obj in capi.World.Collectibles)
		{
			JsonObject attributes = obj.Attributes;
			if (attributes != null && attributes["waterTightContainerProps"].Exists)
			{
				RegisterLiquidStackRenderer(obj);
			}
		}
		capi.Logger.VerboseDebug("Done scanning for liquid containers ");
	}

	private void Event_LeaveWorld()
	{
		foreach (KeyValuePair<string, LoadedTexture> litreTextTexture in litreTextTextures)
		{
			litreTextTexture.Value.Dispose();
		}
		litreTextTextures.Clear();
	}

	public void RegisterLiquidStackRenderer(CollectibleObject obj)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj cannot be null");
		}
		JsonObject attributes = obj.Attributes;
		if (attributes != null)
		{
			_ = attributes["waterTightContainerProps"].Exists;
			if (0 == 0)
			{
				capi.Event.RegisterItemstackRenderer(obj, RenderLiquidItemStackGui, EnumItemRenderTarget.Gui);
				return;
			}
		}
		throw new ArgumentException("This collectible object has no waterTightContainerProps");
	}

	public void RenderLiquidItemStackGui(ItemSlot inSlot, ItemRenderInfo renderInfo, Matrixf modelMat, double posX, double posY, double posZ, float size, int color, bool rotate = false, bool showStackSize = true)
	{
		ItemStack itemstack = inSlot.Itemstack;
		WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(itemstack);
		capi.Render.RenderMultiTextureMesh(renderInfo.ModelRef, "tex2d");
		if (showStackSize)
		{
			float litreFloat = (float)itemstack.StackSize / props.ItemsPerLitre;
			string litres = ((!((double)litreFloat < 0.1)) ? Lang.Get("{0:0.##} L", litreFloat) : Lang.Get("{0} mL", (int)(litreFloat * 1000f)));
			float mul = size / (float)GuiElement.scaled(25.600000381469727);
			LoadedTexture texttex = GetOrCreateLitreTexture(litres, size, mul);
			capi.Render.GlToggleBlend(blend: true, EnumBlendMode.PremultipliedAlpha);
			capi.Render.Render2DLoadedTexture(texttex, (int)(posX + (double)size + 1.0 - (double)texttex.Width - GuiElement.scaled(1.0)), (int)(posY + (double)size - (double)texttex.Height + (double)mul * GuiElement.scaled(3.0) - GuiElement.scaled(5.0)), (int)posZ + 60);
			capi.Render.GlToggleBlend(blend: true);
		}
	}

	public LoadedTexture GetOrCreateLitreTexture(string litres, float size, float fontSizeMultiplier = 1f)
	{
		string key = litres + "-" + fontSizeMultiplier + "-1";
		if (!litreTextTextures.TryGetValue(key, out var texture))
		{
			CairoFont font = stackSizeFont.Clone();
			font.UnscaledFontsize *= fontSizeMultiplier;
			double width = font.GetTextExtents(litres).Width;
			double ratio = Math.Min(1.0, (double)(1.5f * size) / width);
			font.UnscaledFontsize *= ratio;
			return litreTextTextures[key] = capi.Gui.TextTexture.GenTextTexture(litres, font);
		}
		return texture;
	}
}
