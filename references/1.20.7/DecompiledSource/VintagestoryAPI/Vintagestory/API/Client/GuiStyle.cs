using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

/// <summary>
/// A class containing common values for elements before scaling is applied.
/// </summary>
public static class GuiStyle
{
	/// <summary>
	/// The padding between the element and the dialogue. 20f.
	/// </summary>
	public static double ElementToDialogPadding;

	/// <summary>
	/// The padding between other things.  5f.
	/// </summary>
	public static double HalfPadding;

	/// <summary>
	/// The padding between the dialogue and the screen. 10f.
	/// </summary>
	public static double DialogToScreenPadding;

	/// <summary>
	/// The height of the title bar. 30.
	/// </summary>
	public static double TitleBarHeight;

	/// <summary>
	/// The radius of the dialogue background. 1.
	/// </summary>
	public static double DialogBGRadius;

	/// <summary>
	/// The radius of the element background. 1.
	/// </summary>
	public static double ElementBGRadius;

	/// <summary>
	/// The size of the large font. 40.
	/// </summary>
	public static double LargeFontSize;

	/// <summary>
	/// The size of the normal fonts.  Used for text boxes. 30.
	/// </summary>
	public static double NormalFontSize;

	/// <summary>
	/// The fonts that are slightly smaller than normal fonts. 24.
	/// </summary>
	public static double SubNormalFontSize;

	/// <summary>
	/// The smaller fonts. 20.
	/// </summary>
	public static double SmallishFontSize;

	/// <summary>
	/// The smallest font size used in the game that isn't used with itemstacks. 16.
	/// </summary>
	public static double SmallFontSize;

	/// <summary>
	/// The font size used for specific details like Item Stack size info. 14.
	/// </summary>
	public static double DetailFontSize;

	/// <summary>
	/// The decorative font type. "Lora".
	/// </summary>
	public static string DecorativeFontName;

	/// <summary>
	/// The standard font "Montserrat".
	/// </summary>
	public static string StandardFontName;

	/// <summary>
	/// Set by the client, loaded from the clientsettings.json. Used by ElementBounds to add a margin for left/right aligned dialogs
	/// </summary>
	public static int LeftDialogMargin;

	/// <summary>
	/// Set by the client, loaded from the clientsettings.json. Used by ElementBounds to add a margin for left/right aligned dialogs
	/// </summary>
	public static int RightDialogMargin;

	public static double[] ColorTime1;

	public static double[] ColorTime2;

	public static double[] ColorRust1;

	public static double[] ColorRust2;

	public static double[] ColorRust3;

	public static double[] ColorWood;

	public static double[] ColorParchment;

	public static double[] ColorSchematic;

	public static double[] ColorRot1;

	public static double[] ColorRot2;

	public static double[] ColorRot3;

	public static double[] ColorRot4;

	public static double[] ColorRot5;

	public static double[] DialogSlotBackColor;

	public static double[] DialogSlotFrontColor;

	/// <summary>
	/// The light background color for dialogs.
	/// </summary>
	public static double[] DialogLightBgColor;

	/// <summary>
	/// The default background color for dialogs.
	/// </summary>
	public static double[] DialogDefaultBgColor;

	/// <summary>
	/// The strong background color for dialogs.
	/// </summary>
	public static double[] DialogStrongBgColor;

	/// <summary>
	/// The default dialog border color
	/// </summary>
	public static double[] DialogBorderColor;

	/// <summary>
	/// The Highlight color for dialogs.
	/// </summary>
	public static double[] DialogHighlightColor;

	/// <summary>
	/// The alternate background color for dialogs.
	/// </summary>
	public static double[] DialogAlternateBgColor;

	/// <summary>
	/// The default text color for any given dialog.
	/// </summary>
	public static double[] DialogDefaultTextColor;

	/// <summary>
	/// A color for a darker brown.
	/// </summary>
	public static double[] DarkBrownColor;

	/// <summary>
	/// The color of the 1..9 numbers on the hotbar slots
	/// </summary>
	public static double[] HotbarNumberTextColor;

	public static double[] DiscoveryTextColor;

	public static double[] SuccessTextColor;

	public static string SuccessTextColorHex;

	public static string ErrorTextColorHex;

	/// <summary>
	/// The color of the error text.
	/// </summary>
	public static double[] ErrorTextColor;

	/// <summary>
	/// The color of the error text.
	/// </summary>
	public static double[] WarningTextColor;

	/// <summary>
	/// The color of the the link text.
	/// </summary>
	public static double[] LinkTextColor;

	/// <summary>
	/// A light brown text color.
	/// </summary>
	public static double[] ButtonTextColor;

	/// <summary>
	/// A hover color for the light brown text.
	/// </summary>
	public static double[] ActiveButtonTextColor;

	/// <summary>
	/// The text color for a disabled object.
	/// </summary>
	public static double[] DisabledTextColor;

	/// <summary>
	/// The color of the actively selected slot overlay
	/// </summary>
	public static double[] ActiveSlotColor;

	/// <summary>
	/// The color of the health bar.
	/// </summary>
	public static double[] HealthBarColor;

	/// <summary>
	/// The color of the oxygen bar
	/// </summary>
	public static double[] OxygenBarColor;

	/// <summary>
	/// The color of the food bar.
	/// </summary>
	public static double[] FoodBarColor;

	/// <summary>
	/// The color of the XP bar.
	/// </summary>
	public static double[] XPBarColor;

	/// <summary>
	/// The color of the title bar.
	/// </summary>
	public static double[] TitleBarColor;

	/// <summary>
	/// The color of the macro icon.
	/// </summary>
	public static double[] MacroIconColor;

	/// <summary>
	/// A 100 step gradient from green to red, to be used to show durability, damage or any state other of decay
	/// </summary>
	public static int[] DamageColorGradient;

	static GuiStyle()
	{
		ElementToDialogPadding = 20.0;
		HalfPadding = 5.0;
		DialogToScreenPadding = 10.0;
		TitleBarHeight = 31.0;
		DialogBGRadius = 1.0;
		ElementBGRadius = 1.0;
		LargeFontSize = 40.0;
		NormalFontSize = 30.0;
		SubNormalFontSize = 24.0;
		SmallishFontSize = 20.0;
		SmallFontSize = 16.0;
		DetailFontSize = 14.0;
		DecorativeFontName = "Lora";
		StandardFontName = "sans-serif";
		ColorTime1 = new double[4]
		{
			56.0 / 255.0,
			232.0 / 255.0,
			61.0 / 85.0,
			1.0
		};
		ColorTime2 = new double[4]
		{
			79.0 / 255.0,
			98.0 / 255.0,
			94.0 / 255.0,
			1.0
		};
		ColorRust1 = new double[4]
		{
			208.0 / 255.0,
			91.0 / 255.0,
			4.0 / 85.0,
			1.0
		};
		ColorRust2 = new double[4]
		{
			143.0 / 255.0,
			47.0 / 255.0,
			0.0,
			1.0
		};
		ColorRust3 = new double[4]
		{
			116.0 / 255.0,
			49.0 / 255.0,
			4.0 / 255.0,
			1.0
		};
		ColorWood = new double[4]
		{
			44.0 / 85.0,
			92.0 / 255.0,
			67.0 / 255.0,
			1.0
		};
		ColorParchment = new double[4]
		{
			79.0 / 85.0,
			206.0 / 255.0,
			152.0 / 255.0,
			1.0
		};
		ColorSchematic = new double[4]
		{
			1.0,
			226.0 / 255.0,
			194.0 / 255.0,
			1.0
		};
		ColorRot1 = new double[4]
		{
			98.0 / 255.0,
			23.0 / 85.0,
			13.0 / 51.0,
			1.0
		};
		ColorRot2 = new double[4]
		{
			0.4,
			22.0 / 51.0,
			112.0 / 255.0,
			1.0
		};
		ColorRot3 = new double[4]
		{
			98.0 / 255.0,
			74.0 / 255.0,
			64.0 / 255.0,
			1.0
		};
		ColorRot4 = new double[4]
		{
			0.17647058823529413,
			7.0 / 51.0,
			11.0 / 85.0,
			1.0
		};
		ColorRot5 = new double[4]
		{
			5.0 / 51.0,
			1.0 / 17.0,
			13.0 / 255.0,
			1.0
		};
		DialogSlotBackColor = ColorSchematic;
		DialogSlotFrontColor = ColorWood;
		DialogLightBgColor = ColorUtil.Hex2Doubles("#403529", 0.75);
		DialogDefaultBgColor = ColorUtil.Hex2Doubles("#403529", 0.8);
		DialogStrongBgColor = ColorUtil.Hex2Doubles("#403529", 1.0);
		DialogBorderColor = new double[4] { 0.0, 0.0, 0.0, 0.3 };
		DialogHighlightColor = ColorUtil.Hex2Doubles("#a88b6c", 0.9);
		DialogAlternateBgColor = ColorUtil.Hex2Doubles("#b5aea6", 0.93);
		DialogDefaultTextColor = ColorUtil.Hex2Doubles("#e9ddce", 1.0);
		DarkBrownColor = ColorUtil.Hex2Doubles("#5a4530", 1.0);
		HotbarNumberTextColor = ColorUtil.Hex2Doubles("#5a4530", 0.5);
		DiscoveryTextColor = ColorParchment;
		SuccessTextColor = new double[4] { 0.5, 1.0, 0.5, 1.0 };
		SuccessTextColorHex = "#80ff80";
		ErrorTextColorHex = "#ff8080";
		ErrorTextColor = new double[4] { 1.0, 0.5, 0.5, 1.0 };
		WarningTextColor = new double[4]
		{
			242.0 / 255.0,
			67.0 / 85.0,
			131.0 / 255.0,
			1.0
		};
		LinkTextColor = new double[4] { 0.5, 0.5, 1.0, 1.0 };
		ButtonTextColor = new double[4]
		{
			224.0 / 255.0,
			69.0 / 85.0,
			11.0 / 15.0,
			1.0
		};
		ActiveButtonTextColor = new double[4]
		{
			197.0 / 255.0,
			137.0 / 255.0,
			24.0 / 85.0,
			1.0
		};
		DisabledTextColor = new double[4] { 1.0, 1.0, 1.0, 0.35 };
		ActiveSlotColor = new double[4]
		{
			98.0 / 255.0,
			197.0 / 255.0,
			73.0 / 85.0,
			1.0
		};
		HealthBarColor = new double[4] { 0.659, 0.0, 0.0, 1.0 };
		OxygenBarColor = new double[4] { 0.659, 0.659, 1.0, 1.0 };
		FoodBarColor = new double[4] { 0.482, 0.521, 0.211, 1.0 };
		XPBarColor = new double[4] { 0.745, 0.61, 0.0, 1.0 };
		TitleBarColor = new double[4] { 0.0, 0.0, 0.0, 0.2 };
		MacroIconColor = new double[4] { 1.0, 1.0, 1.0, 1.0 };
		int[] colors = new int[11]
		{
			ColorUtil.Hex2Int("#A7251F"),
			ColorUtil.Hex2Int("#F01700"),
			ColorUtil.Hex2Int("#F04900"),
			ColorUtil.Hex2Int("#F07100"),
			ColorUtil.Hex2Int("#F0D100"),
			ColorUtil.Hex2Int("#F0ED00"),
			ColorUtil.Hex2Int("#E2F000"),
			ColorUtil.Hex2Int("#AAF000"),
			ColorUtil.Hex2Int("#71F000"),
			ColorUtil.Hex2Int("#33F000"),
			ColorUtil.Hex2Int("#00F06B")
		};
		DamageColorGradient = new int[100];
		for (int i = 0; i < 10; i++)
		{
			for (int j = 0; j < 10; j++)
			{
				DamageColorGradient[10 * i + j] = ColorUtil.ColorOverlay(colors[i], colors[i + 1], (float)j / 10f);
			}
		}
	}
}
