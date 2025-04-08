using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class VtmlUtil
{
	/// <summary>
	/// You can register your own tag converters here
	/// </summary>
	public static Dictionary<string, Tag2RichTextDelegate> TagConverters = new Dictionary<string, Tag2RichTextDelegate>();

	private static CairoFont monospacedFont = new CairoFont(16.0, "Consolas", new double[4] { 1.0, 1.0, 1.0, 1.0 });

	public static RichTextComponentBase[] Richtextify(ICoreClientAPI capi, string vtmlCode, CairoFont baseFont, Action<LinkTextComponent> didClickLink = null)
	{
		List<RichTextComponentBase> elems = new List<RichTextComponentBase>();
		Stack<CairoFont> fontStack = new Stack<CairoFont>();
		fontStack.Push(baseFont);
		VtmlToken[] tokens = VtmlParser.Tokenize(capi.Logger, vtmlCode);
		Richtextify(capi, tokens, ref elems, fontStack, didClickLink);
		return elems.ToArray();
	}

	private static void Richtextify(ICoreClientAPI capi, VtmlToken[] tokens, ref List<RichTextComponentBase> elems, Stack<CairoFont> fontStack, Action<LinkTextComponent> didClickLink)
	{
		for (int i = 0; i < tokens.Length; i++)
		{
			Richtextify(capi, tokens[i], ref elems, fontStack, didClickLink);
		}
	}

	private static void Richtextify(ICoreClientAPI capi, VtmlToken token, ref List<RichTextComponentBase> elems, Stack<CairoFont> fontStack, Action<LinkTextComponent> didClickLink)
	{
		if (token is VtmlTagToken)
		{
			VtmlTagToken tagToken = token as VtmlTagToken;
			switch (tagToken.Name)
			{
			case "br":
				elems.Add(new RichTextComponent(capi, "\r\n", fontStack.Peek()));
				break;
			case "hk":
			case "hotkey":
			{
				string hotkeyName = tagToken.ContentText;
				if (hotkeyName == "leftmouse")
				{
					hotkeyName = "primarymouse";
				}
				if (hotkeyName == "rightmouse")
				{
					hotkeyName = "secondarymouse";
				}
				if (hotkeyName == "toolmode")
				{
					hotkeyName = "toolmodeselect";
				}
				HotkeyComponent hcmp = new HotkeyComponent(capi, hotkeyName, fontStack.Peek());
				hcmp.PaddingLeft -= 1.0;
				hcmp.PaddingRight += 3.0;
				elems.Add(hcmp);
				break;
			}
			case "i":
			{
				CairoFont font = fontStack.Peek().Clone();
				font.Slant = FontSlant.Italic;
				fontStack.Push(font);
				foreach (VtmlToken val4 in tagToken.ChildElements)
				{
					Richtextify(capi, val4, ref elems, fontStack, didClickLink);
				}
				fontStack.Pop();
				break;
			}
			case "a":
			{
				LinkTextComponent cmp = new LinkTextComponent(capi, tagToken.ContentText, fontStack.Peek(), didClickLink);
				if (!tagToken.Attributes.TryGetValue("href", out cmp.Href))
				{
					capi.Logger.Warning("Language file includes an <a /> link missing href");
				}
				elems.Add(cmp);
				break;
			}
			case "icon":
			{
				tagToken.Attributes.TryGetValue("name", out var iconName);
				tagToken.Attributes.TryGetValue("path", out var iconPath);
				if (iconName == null)
				{
					iconName = tagToken.ContentText;
				}
				IconComponent iconcmp = new IconComponent(capi, iconName, iconPath, fontStack.Peek());
				iconcmp.BoundsPerLine[0].Ascent = fontStack.Peek().GetFontExtents().Ascent;
				elems.Add(iconcmp);
				break;
			}
			case "itemstack":
			{
				FontExtents fontExtents = fontStack.Peek().GetFontExtents();
				float size = (float)fontExtents.Height;
				EnumFloat floatType = EnumFloat.Inline;
				if (tagToken.Attributes.TryGetValue("floattype", out var floattypestr) && !Enum.TryParse<EnumFloat>(floattypestr, out floatType))
				{
					floatType = EnumFloat.Inline;
				}
				tagToken.Attributes.TryGetValue("code", out var codes);
				if (!tagToken.Attributes.TryGetValue("type", out var type))
				{
					type = "block";
				}
				if (codes == null)
				{
					codes = tagToken.ContentText;
				}
				List<ItemStack> stacks = new List<ItemStack>();
				string[] array = codes.Split('|');
				foreach (string code in array)
				{
					CollectibleObject cobj = ((!(type == "item")) ? ((CollectibleObject)capi.World.GetBlock(new AssetLocation(code))) : ((CollectibleObject)capi.World.GetItem(new AssetLocation(code))));
					if (cobj == null)
					{
						cobj = capi.World.GetBlock(0);
					}
					stacks.Add(new ItemStack(cobj));
				}
				float sizemul = 1.3f;
				if (tagToken.Attributes.TryGetValue("rsize", out var sizemulstr))
				{
					sizemul *= sizemulstr.ToFloat();
				}
				SlideshowItemstackTextComponent stckcmp = new SlideshowItemstackTextComponent(capi, stacks.ToArray(), size / RuntimeEnv.GUIScale, floatType);
				stckcmp.Background = true;
				stckcmp.renderSize *= sizemul;
				stckcmp.VerticalAlign = EnumVerticalAlign.Middle;
				stckcmp.BoundsPerLine[0].Ascent = fontExtents.Ascent;
				if (tagToken.Attributes.TryGetValue("offx", out var offxstr))
				{
					stckcmp.offX = GuiElement.scaled(offxstr.ToFloat());
				}
				if (tagToken.Attributes.TryGetValue("offy", out var offystr))
				{
					stckcmp.offY = GuiElement.scaled(offystr.ToFloat());
				}
				elems.Add(stckcmp);
				break;
			}
			case "font":
				fontStack.Push(getFont(tagToken, fontStack));
				foreach (VtmlToken val3 in tagToken.ChildElements)
				{
					Richtextify(capi, val3, ref elems, fontStack, didClickLink);
				}
				fontStack.Pop();
				break;
			case "clear":
				elems.Add(new ClearFloatTextComponent(capi));
				break;
			case "code":
			{
				double[] color = fontStack.Peek().Color;
				int hsv = ColorUtil.Rgb2Hsv((float)color[0], (float)color[1], (float)color[2]) | -16777216;
				hsv >>= 8;
				int rgbint = ColorUtil.Hsv2Rgb((hsv & 0xFF00) + ((hsv & 0xFF) << 16) + ((hsv >> 16) & 0xFF));
				double[] newcolor = new double[4];
				newcolor[3] = 1.0;
				newcolor[2] = (double)(rgbint & 0xFF) / 255.0;
				newcolor[1] = (double)((rgbint >> 8) & 0xFF) / 255.0;
				newcolor[0] = (double)((rgbint >> 16) & 0xFF) / 255.0;
				fontStack.Push(monospacedFont.Clone().WithColor(newcolor));
				foreach (VtmlToken val2 in tagToken.ChildElements)
				{
					Richtextify(capi, val2, ref elems, fontStack, didClickLink);
				}
				fontStack.Pop();
				break;
			}
			case "strong":
				fontStack.Push(fontStack.Peek().Clone().WithWeight(FontWeight.Bold));
				foreach (VtmlToken val in tagToken.ChildElements)
				{
					Richtextify(capi, val, ref elems, fontStack, didClickLink);
				}
				fontStack.Pop();
				break;
			}
			if (tagToken.Name != null && TagConverters.ContainsKey(tagToken.Name))
			{
				RichTextComponentBase elem = TagConverters[tagToken.Name](capi, tagToken, fontStack, didClickLink);
				if (elem != null)
				{
					elems.Add(elem);
				}
			}
		}
		else
		{
			VtmlTextToken textToken = token as VtmlTextToken;
			elems.Add(new RichTextComponent(capi, textToken.Text, fontStack.Peek()));
		}
	}

	private static CairoFont getFont(VtmlTagToken tag, Stack<CairoFont> fontStack)
	{
		double size = 0.0;
		double lineHeight = 1.0;
		string fontName = "";
		EnumTextOrientation orient = EnumTextOrientation.Left;
		double[] color = ColorUtil.WhiteArgbDouble;
		FontWeight weight = FontWeight.Normal;
		CairoFont prevFont = fontStack.Pop();
		if (!tag.Attributes.ContainsKey("size") || !double.TryParse(tag.Attributes["size"], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out size))
		{
			size = prevFont.UnscaledFontsize;
		}
		if (tag.Attributes.ContainsKey("scale"))
		{
			string scale = tag.Attributes["scale"];
			if (scale.EndsWith("%") && double.TryParse(scale.Substring(0, scale.Length - 1), NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var scaled))
			{
				size = prevFont.UnscaledFontsize * scaled / 100.0;
			}
		}
		fontName = (tag.Attributes.ContainsKey("family") ? tag.Attributes["family"] : prevFont.Fontname);
		if (!tag.Attributes.ContainsKey("color") || !parseHexColor(tag.Attributes["color"], out color))
		{
			color = prevFont.Color;
		}
		if (tag.Attributes.ContainsKey("opacity") && double.TryParse(tag.Attributes["opacity"], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var opacity))
		{
			color = (double[])color.Clone();
			color[3] *= opacity;
		}
		weight = ((!tag.Attributes.ContainsKey("weight")) ? prevFont.FontWeight : ((tag.Attributes["weight"] == "bold") ? FontWeight.Bold : FontWeight.Normal));
		if (!tag.Attributes.ContainsKey("lineheight") || !double.TryParse(tag.Attributes["lineheight"], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out lineHeight))
		{
			lineHeight = prevFont.LineHeightMultiplier;
		}
		if (tag.Attributes.ContainsKey("align"))
		{
			switch (tag.Attributes["align"])
			{
			case "left":
				orient = EnumTextOrientation.Left;
				break;
			case "right":
				orient = EnumTextOrientation.Right;
				break;
			case "center":
				orient = EnumTextOrientation.Center;
				break;
			case "justify":
				orient = EnumTextOrientation.Justify;
				break;
			}
		}
		else
		{
			orient = prevFont.Orientation;
		}
		fontStack.Push(prevFont);
		return new CairoFont(size, fontName, color).WithWeight(weight).WithLineHeightMultiplier(lineHeight).WithOrientation(orient);
	}

	public static bool parseHexColor(string colorText, out double[] color)
	{
		System.Drawing.Color cl;
		try
		{
			cl = ColorTranslator.FromHtml(colorText);
		}
		catch (Exception)
		{
			color = new double[4] { 0.0, 0.0, 0.0, 1.0 };
			return false;
		}
		if (cl == System.Drawing.Color.Empty)
		{
			color = null;
			return false;
		}
		color = new double[4]
		{
			(double)(int)cl.R / 255.0,
			(double)(int)cl.G / 255.0,
			(double)(int)cl.B / 255.0,
			(double)(int)cl.A / 255.0
		};
		return true;
	}

	public static string toHexColor(double[] color)
	{
		return "#" + ((int)(color[0] * 255.0)).ToString("X2") + ((int)(color[1] * 255.0)).ToString("X2") + ((int)(color[2] * 255.0)).ToString("X2");
	}
}
