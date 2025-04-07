#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

//
// Summary:
//     A class containing common values for elements before scaling is applied.
public static class GuiStyle
{
    //
    // Summary:
    //     The padding between the element and the dialogue. 20f.
    public static double ElementToDialogPadding;

    //
    // Summary:
    //     The padding between other things. 5f.
    public static double HalfPadding;

    //
    // Summary:
    //     The padding between the dialogue and the screen. 10f.
    public static double DialogToScreenPadding;

    //
    // Summary:
    //     The height of the title bar. 30.
    public static double TitleBarHeight;

    //
    // Summary:
    //     The radius of the dialogue background. 1.
    public static double DialogBGRadius;

    //
    // Summary:
    //     The radius of the element background. 1.
    public static double ElementBGRadius;

    //
    // Summary:
    //     The size of the large font. 40.
    public static double LargeFontSize;

    //
    // Summary:
    //     The size of the normal fonts. Used for text boxes. 30.
    public static double NormalFontSize;

    //
    // Summary:
    //     The fonts that are slightly smaller than normal fonts. 24.
    public static double SubNormalFontSize;

    //
    // Summary:
    //     The smaller fonts. 20.
    public static double SmallishFontSize;

    //
    // Summary:
    //     The smallest font size used in the game that isn't used with itemstacks. 16.
    public static double SmallFontSize;

    //
    // Summary:
    //     The font size used for specific details like Item Stack size info. 14.
    public static double DetailFontSize;

    //
    // Summary:
    //     The decorative font type. "Lora".
    public static string DecorativeFontName;

    //
    // Summary:
    //     The standard font "Montserrat".
    public static string StandardFontName;

    //
    // Summary:
    //     Set by the client, loaded from the clientsettings.json. Used by ElementBounds
    //     to add a margin for left/right aligned dialogs
    public static int LeftDialogMargin;

    //
    // Summary:
    //     Set by the client, loaded from the clientsettings.json. Used by ElementBounds
    //     to add a margin for left/right aligned dialogs
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

    //
    // Summary:
    //     The light background color for dialogs.
    public static double[] DialogLightBgColor;

    //
    // Summary:
    //     The default background color for dialogs.
    public static double[] DialogDefaultBgColor;

    //
    // Summary:
    //     The strong background color for dialogs.
    public static double[] DialogStrongBgColor;

    //
    // Summary:
    //     The default dialog border color
    public static double[] DialogBorderColor;

    //
    // Summary:
    //     The Highlight color for dialogs.
    public static double[] DialogHighlightColor;

    //
    // Summary:
    //     The alternate background color for dialogs.
    public static double[] DialogAlternateBgColor;

    //
    // Summary:
    //     The default text color for any given dialog.
    public static double[] DialogDefaultTextColor;

    //
    // Summary:
    //     A color for a darker brown.
    public static double[] DarkBrownColor;

    //
    // Summary:
    //     The color of the 1..9 numbers on the hotbar slots
    public static double[] HotbarNumberTextColor;

    public static double[] DiscoveryTextColor;

    public static double[] SuccessTextColor;

    public static string SuccessTextColorHex;

    public static string ErrorTextColorHex;

    //
    // Summary:
    //     The color of the error text.
    public static double[] ErrorTextColor;

    //
    // Summary:
    //     The color of the error text.
    public static double[] WarningTextColor;

    //
    // Summary:
    //     The color of the the link text.
    public static double[] LinkTextColor;

    //
    // Summary:
    //     A light brown text color.
    public static double[] ButtonTextColor;

    //
    // Summary:
    //     A hover color for the light brown text.
    public static double[] ActiveButtonTextColor;

    //
    // Summary:
    //     The text color for a disabled object.
    public static double[] DisabledTextColor;

    //
    // Summary:
    //     The color of the actively selected slot overlay
    public static double[] ActiveSlotColor;

    //
    // Summary:
    //     The color of the health bar.
    public static double[] HealthBarColor;

    //
    // Summary:
    //     The color of the oxygen bar
    public static double[] OxygenBarColor;

    //
    // Summary:
    //     The color of the food bar.
    public static double[] FoodBarColor;

    //
    // Summary:
    //     The color of the XP bar.
    public static double[] XPBarColor;

    //
    // Summary:
    //     The color of the title bar.
    public static double[] TitleBarColor;

    //
    // Summary:
    //     The color of the macro icon.
    public static double[] MacroIconColor;

    //
    // Summary:
    //     A 100 step gradient from green to red, to be used to show durability, damage
    //     or any state other of decay
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
        int[] array = new int[11]
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
                DamageColorGradient[10 * i + j] = ColorUtil.ColorOverlay(array[i], array[i + 1], (float)j / 10f);
            }
        }
    }
}
#if false // Decompilation log
'180' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Could not find by name: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Found single assembly: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\protobuf-net.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Specialized.dll'
------------------
Resolve: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NetworkInformation.dll'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\cairo-sharp.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ObjectModel.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.GraphicsLibraryFramework.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Found single assembly: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Microsoft.Data.Sqlite.dll'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Desktop.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NameResolution.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.NonGeneric.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
