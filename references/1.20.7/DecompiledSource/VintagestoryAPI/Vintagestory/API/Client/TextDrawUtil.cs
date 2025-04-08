using System;
using System.Collections.Generic;
using System.Text;
using Cairo;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class TextDrawUtil
{
	private int caretPos;

	private bool gotLinebreak;

	private bool gotSpace;

	public TextLine[] Lineize(Context ctx, string text, double boxwidth, double lineHeightMultiplier = 1.0, EnumLinebreakBehavior linebreak = EnumLinebreakBehavior.Default, bool keepLinebreakChar = false)
	{
		return Lineize(ctx, text, linebreak, new TextFlowPath[1]
		{
			new TextFlowPath(boxwidth)
		}, 0.0, 0.0, lineHeightMultiplier, keepLinebreakChar);
	}

	public int GetQuantityTextLines(CairoFont font, string text, double boxWidth, EnumLinebreakBehavior linebreak = EnumLinebreakBehavior.Default)
	{
		return GetQuantityTextLines(font, text, linebreak, new TextFlowPath[1]
		{
			new TextFlowPath(boxWidth)
		});
	}

	public double GetMultilineTextHeight(CairoFont font, string text, double boxWidth, EnumLinebreakBehavior linebreak = EnumLinebreakBehavior.Default)
	{
		return (double)GetQuantityTextLines(font, text, boxWidth, linebreak) * GetLineHeight(font);
	}

	public TextLine[] Lineize(CairoFont font, string fulltext, double boxWidth, EnumLinebreakBehavior linebreak = EnumLinebreakBehavior.Default, bool keepLinebreakChar = false)
	{
		return Lineize(font, fulltext, linebreak, new TextFlowPath[1]
		{
			new TextFlowPath(boxWidth)
		}, 0.0, 0.0, keepLinebreakChar);
	}

	/// <summary>
	/// Use Matrix transformation to move the draw position
	/// </summary>
	/// <param name="ctx">The context of the text.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="text">The text itself.</param>
	/// <param name="boxWidth">The width of the box containing the text.</param>
	/// <param name="orientation">The orientation of the text.</param>
	public void AutobreakAndDrawMultilineText(Context ctx, CairoFont font, string text, double boxWidth, EnumTextOrientation orientation = EnumTextOrientation.Left)
	{
		AutobreakAndDrawMultilineText(ctx, font, text, 0.0, 0.0, new TextFlowPath[1]
		{
			new TextFlowPath(boxWidth)
		}, orientation);
	}

	/// <summary>
	/// Draws the text with matrix transformations.
	/// </summary>
	/// <param name="ctx">The context of the text.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="text">The text itself.</param>
	/// <param name="posX">The X position of the text.</param>
	/// <param name="posY">The Y position of the text.</param>
	/// <param name="boxWidth">The width of the box containing the text.</param>
	/// <param name="orientation">The orientation of the text.</param>
	/// <returns>The new height of the text.</returns>
	public double AutobreakAndDrawMultilineTextAt(Context ctx, CairoFont font, string text, double posX, double posY, double boxWidth, EnumTextOrientation orientation = EnumTextOrientation.Left)
	{
		ctx.Save();
		Matrix i = ctx.Matrix;
		i.Translate((int)posX, (int)posY);
		ctx.Matrix = i;
		double result = AutobreakAndDrawMultilineText(ctx, font, text, 0.0, 0.0, new TextFlowPath[1]
		{
			new TextFlowPath(boxWidth)
		}, orientation);
		ctx.Restore();
		return result;
	}

	/// <summary>
	/// Draws the text with pre-set breaks.
	/// </summary>
	/// <param name="ctx">The context of the text.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="lines">The lines of text.</param>
	/// <param name="posX">The X position of the text.</param>
	/// <param name="posY">The Y position of the text.</param>
	/// <param name="boxWidth">The width of the box containing the text.</param>
	/// <param name="orientation">The orientation of the text.</param>
	public void DrawMultilineTextAt(Context ctx, CairoFont font, TextLine[] lines, double posX, double posY, double boxWidth, EnumTextOrientation orientation = EnumTextOrientation.Left)
	{
		ctx.Save();
		Matrix i = ctx.Matrix;
		i.Translate(posX, posY);
		ctx.Matrix = i;
		font.SetupContext(ctx);
		DrawMultilineText(ctx, font, lines, orientation);
		ctx.Restore();
	}

	/// <summary>
	/// Gets the height of the font to calculate the height of the line.
	/// </summary>
	/// <param name="font">The font to calculate from.</param>
	/// <returns>The height of the line.</returns>
	public double GetLineHeight(CairoFont font)
	{
		return font.GetFontExtents().Height * font.LineHeightMultiplier;
	}

	/// <summary>
	/// Gets the number of lines of text.
	/// </summary>
	/// <param name="font">The font of the text.</param>
	/// <param name="text">The text itself.</param>
	/// <param name="linebreak"></param>
	/// <param name="flowPath">The path for the text.</param>
	/// <param name="lineY">The height of the line</param>
	/// <returns>The number of lines.</returns>
	public int GetQuantityTextLines(CairoFont font, string text, EnumLinebreakBehavior linebreak, TextFlowPath[] flowPath, double lineY = 0.0)
	{
		if (text == null || text.Length == 0)
		{
			return 0;
		}
		ImageSurface surface = new ImageSurface(Format.Argb32, 1, 1);
		Context ctx = new Context(surface);
		font.SetupContext(ctx);
		int result = Lineize(ctx, text, linebreak, flowPath, 0.0, lineY, font.LineHeightMultiplier).Length;
		ctx.Dispose();
		surface.Dispose();
		return result;
	}

	/// <summary>
	/// Get the final height of the text.
	/// </summary>
	/// <param name="font">The font of the text.</param>
	/// <param name="text">The text itself.</param>
	/// <param name="linebreak"></param>
	/// <param name="flowPath">The path for the text.</param>
	/// <param name="lineY">The height of the line</param>
	/// <returns>The final height of the text.</returns>
	public double GetMultilineTextHeight(CairoFont font, string text, EnumLinebreakBehavior linebreak, TextFlowPath[] flowPath, double lineY = 0.0)
	{
		return (double)GetQuantityTextLines(font, text, linebreak, flowPath, lineY) * GetLineHeight(font);
	}

	/// <summary>
	/// Turns the supplied text into line of text constrained by supplied flow path and starting at supplied start coordinates
	/// </summary>
	/// <param name="font">The font of the text.</param>
	/// <param name="fulltext">The text of the lines.</param>
	/// <param name="linebreak"></param>
	/// <param name="flowPath">The flow direction of text.</param>
	/// <param name="startOffsetX">The offset start position for X</param>
	/// <param name="startY">The offset start position for Y</param>
	/// <param name="keepLinebreakChar"></param>
	/// <returns>The text broken up into lines.</returns>
	public TextLine[] Lineize(CairoFont font, string fulltext, EnumLinebreakBehavior linebreak, TextFlowPath[] flowPath, double startOffsetX = 0.0, double startY = 0.0, bool keepLinebreakChar = false)
	{
		if (fulltext == null || fulltext.Length == 0)
		{
			return new TextLine[0];
		}
		ImageSurface surface = new ImageSurface(Format.Argb32, 1, 1);
		Context ctx = new Context(surface);
		font.SetupContext(ctx);
		TextLine[] result = Lineize(ctx, fulltext, linebreak, flowPath, startOffsetX, startY, font.LineHeightMultiplier, keepLinebreakChar);
		ctx.Dispose();
		surface.Dispose();
		return result;
	}

	/// <summary>
	/// Turns the supplied text into line of text constrained by supplied flow path and starting at supplied start coordinates
	/// </summary>
	/// <param name="ctx">Contexts of the GUI.</param>
	/// <param name="text">The text to be split</param>
	/// <param name="linebreak"></param>
	/// <param name="flowPath">Sets the general flow of text.</param>
	/// <param name="startOffsetX">The offset start position for X</param>
	/// <param name="startY">The offset start position for Y</param>
	/// <param name="lineHeightMultiplier"></param>
	/// <param name="keepLinebreakChar"></param>
	/// <returns>The text broken up into lines.</returns>
	public TextLine[] Lineize(Context ctx, string text, EnumLinebreakBehavior linebreak, TextFlowPath[] flowPath, double startOffsetX = 0.0, double startY = 0.0, double lineHeightMultiplier = 1.0, bool keepLinebreakChar = false)
	{
		if (text == null || text.Length == 0)
		{
			return new TextLine[0];
		}
		if (linebreak == EnumLinebreakBehavior.Default)
		{
			linebreak = Lang.AvailableLanguages[Lang.CurrentLocale].LineBreakBehavior;
		}
		StringBuilder lineTextBldr = new StringBuilder();
		List<TextLine> lines = new List<TextLine>();
		caretPos = 0;
		double lineheight = ctx.FontExtents.Height * lineHeightMultiplier;
		double curX = startOffsetX;
		double curY = startY;
		string word;
		TextFlowPath currentSection;
		double usableWidth;
		while ((word = getNextWord(text, linebreak)) != null)
		{
			string spc = ((gotLinebreak || caretPos >= text.Length || !gotSpace) ? "" : " ");
			double nextWidth = ctx.TextExtents(lineTextBldr?.ToString() + word + spc).Width;
			currentSection = GetCurrentFlowPathSection(flowPath, curY);
			if (currentSection == null)
			{
				Console.WriteLine("Flow path underflow. Something in the text flow system is incorrectly programmed.");
				currentSection = new TextFlowPath(500.0);
			}
			usableWidth = currentSection.X2 - currentSection.X1 - curX;
			if (nextWidth >= usableWidth)
			{
				if (word.Length > 0 && lineTextBldr.Length == 0 && startOffsetX == 0.0)
				{
					int tries = 500;
					while (word.Length > 0 && nextWidth >= usableWidth && tries-- > 0)
					{
						word = word.Substring(0, word.Length - 1);
						nextWidth = ctx.TextExtents(lineTextBldr?.ToString() + word + spc).Width;
						caretPos--;
					}
					lineTextBldr.Append(word);
					word = "";
				}
				string linetext2 = lineTextBldr.ToString();
				double withoutWidth2 = ctx.TextExtents(linetext2).Width;
				lines.Add(new TextLine
				{
					Text = linetext2,
					Bounds = new LineRectangled(currentSection.X1 + curX, curY, withoutWidth2, lineheight)
					{
						Ascent = ctx.FontExtents.Ascent
					},
					LeftSpace = 0.0,
					RightSpace = usableWidth - withoutWidth2
				});
				lineTextBldr.Clear();
				curY += lineheight;
				curX = 0.0;
				if (gotLinebreak)
				{
					currentSection = GetCurrentFlowPathSection(flowPath, curY);
				}
			}
			lineTextBldr.Append(word);
			if (gotSpace)
			{
				lineTextBldr.Append(" ");
			}
			if (currentSection == null)
			{
				currentSection = new TextFlowPath();
			}
			if (gotLinebreak)
			{
				if (keepLinebreakChar)
				{
					lineTextBldr.Append("\n");
				}
				string linetext = lineTextBldr.ToString();
				double withoutWidth = ctx.TextExtents(linetext).Width;
				lines.Add(new TextLine
				{
					Text = linetext,
					Bounds = new LineRectangled(currentSection.X1 + curX, curY, withoutWidth, lineheight)
					{
						Ascent = ctx.FontExtents.Ascent
					},
					LeftSpace = 0.0,
					NextOffsetX = curX,
					RightSpace = usableWidth - withoutWidth
				});
				lineTextBldr.Clear();
				curY += lineheight;
				curX = 0.0;
			}
		}
		currentSection = GetCurrentFlowPathSection(flowPath, curY);
		if (currentSection == null)
		{
			currentSection = new TextFlowPath();
		}
		usableWidth = currentSection.X2 - currentSection.X1 - curX;
		string lineTextStr = lineTextBldr.ToString();
		double endWidth = ctx.TextExtents(lineTextStr).Width;
		lines.Add(new TextLine
		{
			Text = lineTextStr,
			Bounds = new LineRectangled(currentSection.X1 + curX, curY, endWidth, lineheight)
			{
				Ascent = ctx.FontExtents.Ascent
			},
			LeftSpace = 0.0,
			NextOffsetX = curX,
			RightSpace = usableWidth - endWidth
		});
		return lines.ToArray();
	}

	private TextFlowPath GetCurrentFlowPathSection(TextFlowPath[] flowPath, double posY)
	{
		for (int i = 0; i < flowPath.Length; i++)
		{
			if (flowPath[i].Y1 <= posY && flowPath[i].Y2 >= posY)
			{
				return flowPath[i];
			}
		}
		return null;
	}

	private string getNextWord(string fulltext, EnumLinebreakBehavior linebreakBh)
	{
		if (caretPos >= fulltext.Length)
		{
			return null;
		}
		StringBuilder word = new StringBuilder();
		gotLinebreak = false;
		gotSpace = false;
		bool autobreak = linebreakBh != EnumLinebreakBehavior.None;
		while (caretPos < fulltext.Length)
		{
			char chr = fulltext[caretPos];
			caretPos++;
			if (chr == ' ' && autobreak)
			{
				gotSpace = true;
				break;
			}
			switch (chr)
			{
			case '\t':
				if (word.Length > 0)
				{
					caretPos--;
					break;
				}
				return "  ";
			case '\r':
				gotLinebreak = true;
				if (caretPos <= fulltext.Length - 1 && fulltext[caretPos] == '\n')
				{
					caretPos++;
				}
				break;
			case '\n':
				gotLinebreak = true;
				break;
			default:
				goto IL_00cf;
			}
			break;
			IL_00cf:
			word.Append(chr);
			if (linebreakBh == EnumLinebreakBehavior.AfterCharacter)
			{
				break;
			}
		}
		return word.ToString();
	}

	public double AutobreakAndDrawMultilineText(Context ctx, CairoFont font, string text, double lineX, double lineY, TextFlowPath[] flowPath, EnumTextOrientation orientation = EnumTextOrientation.Left, EnumLinebreakBehavior linebreak = EnumLinebreakBehavior.AfterWord)
	{
		TextLine[] lines = Lineize(font, text, linebreak, flowPath, lineX, lineY);
		DrawMultilineText(ctx, font, lines, orientation);
		if (lines.Length == 0)
		{
			return 0.0;
		}
		return lines[^1].Bounds.Y + lines[^1].Bounds.Height;
	}

	/// <summary>
	/// lineX is set to 0 after the second line, lineY is advanced by line height for each line
	/// </summary>
	/// <param name="ctx">The context of the text.</param>
	/// <param name="lines">The preformatted lines of the text.</param>
	/// <param name="font">The font of the text</param>
	/// <param name="orientation">The orientation of text (Default: Left)</param>
	public void DrawMultilineText(Context ctx, CairoFont font, TextLine[] lines, EnumTextOrientation orientation = EnumTextOrientation.Left)
	{
		font.SetupContext(ctx);
		double offsetX = 0.0;
		foreach (TextLine textLine in lines)
		{
			if (textLine.Text.Length != 0)
			{
				if (orientation == EnumTextOrientation.Center)
				{
					offsetX = (textLine.LeftSpace + textLine.RightSpace) / 2.0;
				}
				if (orientation == EnumTextOrientation.Right)
				{
					offsetX = textLine.LeftSpace + textLine.RightSpace;
				}
				DrawTextLine(ctx, font, textLine.Text, offsetX + textLine.Bounds.X, textLine.Bounds.Y);
			}
		}
	}

	/// <summary>
	/// Draws a line of text on the screen.
	/// </summary>
	/// <param name="ctx">The context of the text.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="offsetX">The X offset for the text start position. (Default: 0)</param>
	/// <param name="offsetY">The Y offset for the text start position. (Default: 0)</param>
	/// <param name="textPathMode">Whether or not to use TextPathMode.</param>
	public void DrawTextLine(Context ctx, CairoFont font, string text, double offsetX = 0.0, double offsetY = 0.0, bool textPathMode = false)
	{
		if (text == null || text.Length == 0)
		{
			return;
		}
		ctx.MoveTo((int)offsetX, (int)(offsetY + ctx.FontExtents.Ascent));
		if (textPathMode)
		{
			ctx.TextPath(text);
		}
		else if (font.StrokeWidth > 0.0)
		{
			ctx.TextPath(text);
			ctx.LineWidth = font.StrokeWidth;
			ctx.SetSourceRGBA(font.StrokeColor);
			ctx.StrokePreserve();
			ctx.SetSourceRGBA(font.Color);
			ctx.Fill();
		}
		else
		{
			ctx.ShowText(text);
			if (font.RenderTwice)
			{
				ctx.ShowText(text);
			}
		}
	}
}
