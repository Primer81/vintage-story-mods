#region Assembly VintagestoryLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

internal class GuiDialogMacroEditor : GuiDialog
{
    private List<SkillItem> skillItems;

    private int rows = 2;

    private int cols = 8;

    private int selectedIndex = -1;

    private IMacroBase currentMacro;

    private SkillItem currentSkillitem;

    private HotkeyCapturer hotkeyCapturer = new HotkeyCapturer();

    internal IMacroBase SelectedMacro
    {
        get
        {
            (capi.World as ClientMain).macroManager.MacrosByIndex.TryGetValue(selectedIndex, out var value);
            return value;
        }
    }

    public override string ToggleKeyCombinationCode => "macroeditor";

    public override bool PrefersUngrabbedMouse => true;

    public GuiDialogMacroEditor(ICoreClientAPI capi)
        : base(capi)
    {
        skillItems = new List<SkillItem>();
        ComposeDialog();
    }

    private void LoadSkillList()
    {
        skillItems.Clear();
        for (int i = 0; i < cols * rows; i++)
        {
            (capi.World as ClientMain).macroManager.MacrosByIndex.TryGetValue(i, out var value);
            SkillItem item;
            if (value == null)
            {
                item = new SkillItem();
            }
            else
            {
                if (value.iconTexture == null)
                {
                    (value as Macro).GenTexture(capi, (int)GuiElementPassiveItemSlot.unscaledSlotSize);
                }

                item = new SkillItem
                {
                    Code = new AssetLocation(value.Code),
                    Name = value.Name,
                    Hotkey = value.KeyCombination,
                    Texture = value.iconTexture
                };
            }

            skillItems.Add(item);
        }
    }

    private void ComposeDialog()
    {
        LoadSkillList();
        selectedIndex = 0;
        currentSkillitem = skillItems[0];
        int num = 5;
        double num2 = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
        double num3 = (double)cols * num2;
        ElementBounds elementBounds = ElementBounds.Fixed(0.0, 30.0, num3, (double)rows * num2);
        ElementBounds elementBounds2 = elementBounds.ForkBoundingParent(3.0, 6.0, 3.0, 3.0);
        double fixedWidth = num3 / 2.0 - 5.0;
        ElementBounds elementBounds3 = ElementBounds.FixedSize(fixedWidth, 30.0).FixedUnder(elementBounds2, num + 10);
        ElementBounds elementBounds4 = ElementBounds.Fixed(num3 / 2.0 + 8.0, 0.0, fixedWidth, 30.0).FixedUnder(elementBounds2, num + 10);
        ElementBounds elementBounds5 = ElementBounds.FixedSize(fixedWidth, 30.0).FixedUnder(elementBounds3, num - 10);
        ElementBounds elementBounds6 = ElementBounds.Fixed(num3 / 2.0 + 8.0, 0.0, fixedWidth, 30.0).FixedUnder(elementBounds4, num - 10);
        ElementBounds elementBounds7 = ElementBounds.FixedSize(300.0, 30.0).FixedUnder(elementBounds5, num + 10);
        ElementBounds elementBounds8 = ElementBounds.Fixed(0.0, 0.0, num3 - 20.0, 100.0);
        ElementBounds elementBounds9 = ElementBounds.Fixed(0.0, 0.0, num3 - 20.0 - 1.0, 99.0).FixedUnder(elementBounds7, num - 10);
        ElementBounds bounds = elementBounds9.CopyOffsetedSibling(elementBounds9.fixedWidth + 6.0, -1.0).WithFixedWidth(20.0).FixedGrow(0.0, 2.0);
        ElementBounds bounds2 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds9, 6 + 2 * num).WithAlignment(EnumDialogArea.LeftFixed)
            .WithFixedPadding(10.0, 2.0);
        ElementBounds bounds3 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds9, 6 + 2 * num).WithAlignment(EnumDialogArea.RightFixed)
            .WithFixedPadding(10.0, 2.0);
        ElementBounds elementBounds10 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        elementBounds10.BothSizing = ElementSizing.FitToChildren;
        ElementBounds bounds4 = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
        if (base.SingleComposer != null)
        {
            base.SingleComposer.Dispose();
        }

        base.SingleComposer = capi.Gui.CreateCompo("texteditordialog", bounds4).AddShadedDialogBG(elementBounds10).AddDialogTitleBar(Lang.Get("Macro Editor"), OnTitleBarClose)
            .BeginChildElements(elementBounds10)
            .AddInset(elementBounds2, 3)
            .BeginChildElements()
            .AddSkillItemGrid(skillItems, cols, rows, OnSlotClick, elementBounds, "skillitemgrid")
            .EndChildElements()
            .AddStaticText(Lang.Get("macroname"), CairoFont.WhiteSmallText(), elementBounds3)
            .AddStaticText(Lang.Get("macrohotkey"), CairoFont.WhiteSmallText(), elementBounds4)
            .AddTextInput(elementBounds5, OnMacroNameChanged, CairoFont.TextInput(), "macroname")
            .AddInset(elementBounds6, 2, 0.7f)
            .AddDynamicText("", CairoFont.TextInput(), elementBounds6.FlatCopy().WithFixedPadding(3.0, 3.0).WithFixedOffset(3.0, 3.0), "hotkey")
            .AddStaticText(Lang.Get("macrocommands"), CairoFont.WhiteSmallText(), elementBounds7)
            .BeginClip(elementBounds9)
            .AddTextArea(elementBounds8, OnCommandCodeChanged, CairoFont.TextInput().WithFontSize(16f), "commands")
            .EndClip()
            .AddVerticalScrollbar(OnNewScrollbarvalue, bounds, "scrollbar")
            .AddSmallButton(Lang.Get("Delete"), OnClearMacro, bounds2)
            .AddSmallButton(Lang.Get("Save"), OnSaveMacro, bounds3)
            .EndChildElements()
            .Compose();
        base.SingleComposer.GetTextArea("commands").OnCursorMoved = OnTextAreaCursorMoved;
        base.SingleComposer.GetScrollbar("scrollbar").SetHeights((float)elementBounds8.fixedHeight - 1f, (float)elementBounds8.fixedHeight);
        base.SingleComposer.GetSkillItemGrid("skillitemgrid").selectedIndex = 0;
        OnSlotClick(0);
        base.SingleComposer.UnfocusOwnElements();
    }

    private void OnTextAreaCursorMoved(double posX, double posY)
    {
        //IL_0015: Unknown result type (might be due to invalid IL or missing references)
        //IL_001a: Unknown result type (might be due to invalid IL or missing references)
        FontExtents fontExtents = base.SingleComposer.GetTextArea("commands").Font.GetFontExtents();
        double height = ((FontExtents)(ref fontExtents)).Height;
        base.SingleComposer.GetScrollbar("scrollbar").EnsureVisible(posX, posY);
        base.SingleComposer.GetScrollbar("scrollbar").EnsureVisible(posX, posY + height + 5.0);
    }

    private void OnCommandCodeChanged(string newCode)
    {
        GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
        base.SingleComposer.GetScrollbar("scrollbar").SetNewTotalHeight((float)textArea.Bounds.OuterHeight);
    }

    private void OnMacroNameChanged(string newname)
    {
    }

    private void OnSlotClick(int index)
    {
        GuiElementTextInput textInput = base.SingleComposer.GetTextInput("macroname");
        GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
        GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("hotkey");
        base.SingleComposer.GetSkillItemGrid("skillitemgrid").selectedIndex = index;
        selectedIndex = index;
        currentSkillitem = skillItems[index];
        if ((capi.World as ClientMain).macroManager.MacrosByIndex.ContainsKey(index))
        {
            currentMacro = SelectedMacro;
        }
        else
        {
            currentMacro = new Macro();
            currentSkillitem = new SkillItem();
        }

        textInput.SetValue(currentSkillitem.Name);
        textArea.LoadValue(textArea.Lineize(string.Join("\r\n", currentMacro.Commands)));
        if (currentSkillitem.Hotkey != null)
        {
            dynamicText.SetNewText(currentSkillitem.Hotkey?.ToString() ?? "");
        }
        else
        {
            dynamicText.SetNewText("");
        }

        base.SingleComposer.GetScrollbar("scrollbar").SetNewTotalHeight((float)textArea.Bounds.OuterHeight);
    }

    public override void OnGuiOpened()
    {
        base.OnGuiOpened();
        ComposeDialog();
    }

    private bool OnClearMacro()
    {
        if (selectedIndex < 0)
        {
            return true;
        }

        (capi.World as ClientMain).macroManager.DeleteMacro(selectedIndex);
        GuiElementTextInput textInput = base.SingleComposer.GetTextInput("macroname");
        GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
        GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("hotkey");
        textInput.SetValue("");
        textArea.SetValue("");
        dynamicText.SetNewText("");
        currentMacro = new Macro();
        currentSkillitem = new SkillItem();
        LoadSkillList();
        return true;
    }

    private bool OnSaveMacro()
    {
        if (selectedIndex < 0 || currentMacro == null)
        {
            return true;
        }

        GuiElementTextInput textInput = base.SingleComposer.GetTextInput("macroname");
        GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
        currentMacro.Name = textInput.GetText();
        if (currentMacro.Name.Length == 0)
        {
            currentMacro.Name = "Macro " + (selectedIndex + 1);
            textInput.SetValue(currentMacro.Name);
        }

        currentMacro.Commands = textArea.GetLines().ToArray();
        for (int i = 0; i < currentMacro.Commands.Length; i++)
        {
            currentMacro.Commands[i] = currentMacro.Commands[i].TrimEnd('\n', '\r');
        }

        currentMacro.Index = selectedIndex;
        currentMacro.Code = Regex.Replace(currentMacro.Name.Replace(" ", "_"), "[^a-z0-9_-]+", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        currentMacro.GenTexture(capi, (int)GuiElementPassiveItemSlot.unscaledSlotSize);
        MacroManager macroManager = (capi.World as ClientMain).macroManager;
        base.SingleComposer.GetTextInput("macroname").Font.WithColor(GuiStyle.DialogDefaultTextColor);
        if (macroManager.MacrosByIndex.Values.FirstOrDefault((IMacroBase m) => m.Code == currentMacro.Code && m.Index != selectedIndex) != null)
        {
            capi.TriggerIngameError(this, "duplicatemacro", Lang.Get("A macro of this name exists already, please choose another name"));
            base.SingleComposer.GetTextInput("macroname").Font.WithColor(GuiStyle.ErrorTextColor);
            return false;
        }

        macroManager.SetMacro(selectedIndex, currentMacro);
        LoadSkillList();
        return true;
    }

    private void OnTitleBarClose()
    {
        TryClose();
    }

    private void OnNewScrollbarvalue(float value)
    {
        GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
        textArea.Bounds.fixedY = 1f - value;
        textArea.Bounds.CalcWorldBounds();
    }

    public override void OnMouseDown(MouseEvent args)
    {
        base.OnMouseDown(args);
        if (selectedIndex >= 0)
        {
            GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("hotkey");
            dynamicText.Font.Color = new double[4] { 1.0, 1.0, 1.0, 0.9 };
            dynamicText.RecomposeText();
            if (dynamicText.Bounds.PointInside(args.X, args.Y))
            {
                dynamicText.SetNewText("?");
                hotkeyCapturer.BeginCapture();
            }
            else
            {
                CancelCapture();
            }
        }
    }

    public override void OnKeyUp(KeyEvent args)
    {
        if (!hotkeyCapturer.OnKeyUp(args, delegate
        {
            if (currentMacro != null)
            {
                currentMacro.KeyCombination = hotkeyCapturer.CapturedKeyComb;
                GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("hotkey");
                if (ScreenManager.hotkeyManager.IsHotKeyRegistered(currentMacro.KeyCombination))
                {
                    dynamicText.Font.Color = GuiStyle.ErrorTextColor;
                }
                else
                {
                    dynamicText.Font.Color = new double[4] { 1.0, 1.0, 1.0, 0.9 };
                }

                dynamicText.SetNewText(hotkeyCapturer.CapturedKeyComb.ToString(), autoHeight: false, forceRedraw: true);
            }
        }))
        {
            base.OnKeyUp(args);
        }
    }

    public override void OnKeyDown(KeyEvent args)
    {
        if (hotkeyCapturer.OnKeyDown(args))
        {
            if (hotkeyCapturer.IsCapturing())
            {
                base.SingleComposer.GetDynamicText("hotkey").SetNewText(hotkeyCapturer.CapturingKeyComb.ToString());
            }
            else
            {
                CancelCapture();
            }
        }
        else
        {
            base.OnKeyDown(args);
        }
    }

    private void CancelCapture()
    {
        GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("hotkey");
        if (SelectedMacro?.KeyCombination != null)
        {
            dynamicText.SetNewText(SelectedMacro.KeyCombination.ToString());
        }

        hotkeyCapturer.EndCapture();
    }

    public override bool CaptureAllInputs()
    {
        return hotkeyCapturer.IsCapturing();
    }

    public override void Dispose()
    {
        base.Dispose();
        if (skillItems == null)
        {
            return;
        }

        foreach (SkillItem skillItem in skillItems)
        {
            skillItem?.Dispose();
        }
    }
}
#if false // Decompilation log
'168' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Could not find by name: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
------------------
Resolve: 'CommandLine, Version=2.9.1.0, Culture=neutral, PublicKeyToken=5a870481e358d379'
Could not find by name: 'CommandLine, Version=2.9.1.0, Culture=neutral, PublicKeyToken=5a870481e358d379'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Could not find by name: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.Http, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Http, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Http.dll'
------------------
Resolve: 'Open.Nat, Version=1.0.0.0, Culture=neutral, PublicKeyToken=f22a6a4582336c76'
Could not find by name: 'Open.Nat, Version=1.0.0.0, Culture=neutral, PublicKeyToken=f22a6a4582336c76'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'Mono.Nat, Version=3.0.0.0, Culture=neutral, PublicKeyToken=6c9468a3c21bc6d1'
Could not find by name: 'Mono.Nat, Version=3.0.0.0, Culture=neutral, PublicKeyToken=6c9468a3c21bc6d1'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Net.Sockets, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Sockets, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Sockets.dll'
------------------
Resolve: 'Mono.Cecil, Version=0.11.5.0, Culture=neutral, PublicKeyToken=50cebf1cceb9d05e'
Could not find by name: 'Mono.Cecil, Version=0.11.5.0, Culture=neutral, PublicKeyToken=50cebf1cceb9d05e'
------------------
Resolve: 'Microsoft.CodeAnalysis, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'Microsoft.CodeAnalysis, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'Microsoft.CodeAnalysis.CSharp, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'Microsoft.CodeAnalysis.CSharp, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'System.Collections.Immutable, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Immutable, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\gwlar\.nuget\packages\system.collections.immutable\8.0.0\lib\net7.0\System.Collections.Immutable.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'ICSharpCode.SharpZipLib, Version=1.4.2.13, Culture=neutral, PublicKeyToken=1b03e6acf1164f73'
Could not find by name: 'ICSharpCode.SharpZipLib, Version=1.4.2.13, Culture=neutral, PublicKeyToken=1b03e6acf1164f73'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.IO.Compression, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.IO.Compression, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.IO.Compression.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Could not find by name: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: '0Harmony, Version=2.3.5.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: '0Harmony, Version=2.3.5.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\0Harmony.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'OpenTK.Audio.OpenAL, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Audio.OpenAL, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'OpenTK.Mathematics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Mathematics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'OpenTK.Windowing.Common, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Windowing.Common, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'DnsClient, Version=1.7.0.0, Culture=neutral, PublicKeyToken=4574bb5573c51424'
Could not find by name: 'DnsClient, Version=1.7.0.0, Culture=neutral, PublicKeyToken=4574bb5573c51424'
------------------
Resolve: 'System.IO.Pipes, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.IO.Pipes, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.IO.Pipes.dll'
------------------
Resolve: 'OpenTK.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Diagnostics.FileVersionInfo, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.FileVersionInfo, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.FileVersionInfo.dll'
------------------
Resolve: 'System.ComponentModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.dll'
------------------
Resolve: 'csvorbis, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=eaa89a1626beb708'
Could not find by name: 'csvorbis, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=eaa89a1626beb708'
------------------
Resolve: 'csogg, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=cbfcc0aaeece6bdb'
Could not find by name: 'csogg, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=cbfcc0aaeece6bdb'
------------------
Resolve: 'System.Diagnostics.TraceSource, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.TraceSource, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.TraceSource.dll'
------------------
Resolve: 'xplatforminterface, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'xplatforminterface, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'Microsoft.Win32.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'Microsoft.Win32.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\Microsoft.Win32.Primitives.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
