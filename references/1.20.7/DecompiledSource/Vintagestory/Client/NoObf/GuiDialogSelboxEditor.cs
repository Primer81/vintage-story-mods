#region Assembly VintagestoryLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

internal class GuiDialogSelboxEditor : GuiDialog
{
    private Cuboidf[] originalSelBoxes;

    private Block nowBlock;

    private BlockPos nowPos;

    private Cuboidf[] currentSelBoxes;

    private int boxIndex;

    private string[] coordnames = new string[6] { "x1", "y1", "z1", "x2", "y2", "z2" };

    private bool isChanging;

    public override string ToggleKeyCombinationCode => null;

    public override bool PrefersUngrabbedMouse => true;

    public GuiDialogSelboxEditor(ICoreClientAPI capi)
        : base(capi)
    {
        capi.ChatCommands.GetOrCreate("dev").BeginSubCommand("bsedit").WithRootAlias("bsedit")
            .WithDescription("Opens the block selection editor")
            .HandleWith(CmdSelectionBoxEditor)
            .EndSubCommand();
    }

    private TextCommandResult CmdSelectionBoxEditor(TextCommandCallingArgs textCommandCallingArgs)
    {
        TryOpen();
        return TextCommandResult.Success();
    }

    public override void OnGuiOpened()
    {
        BlockSelection currentBlockSelection = capi.World.Player.CurrentBlockSelection;
        boxIndex = 0;
        if (currentBlockSelection == null)
        {
            capi.World.Player.ShowChatNotification("Look at a block first");
            capi.Event.EnqueueMainThreadTask(delegate
            {
                TryClose();
            }, "closegui");
            return;
        }

        nowPos = currentBlockSelection.Position.Copy();
        nowBlock = capi.World.BlockAccessor.GetBlock(currentBlockSelection.Position);
        TreeAttribute treeAttribute = new TreeAttribute();
        treeAttribute.SetInt("nowblockid", nowBlock.Id);
        treeAttribute.SetBlockPos("pos", currentBlockSelection.Position);
        capi.Event.PushEvent("oneditselboxes", treeAttribute);
        if (nowBlock.SelectionBoxes != null)
        {
            originalSelBoxes = new Cuboidf[nowBlock.SelectionBoxes.Length];
            for (int i = 0; i < originalSelBoxes.Length; i++)
            {
                originalSelBoxes[i] = nowBlock.SelectionBoxes[i].Clone();
            }
        }

        currentSelBoxes = nowBlock.SelectionBoxes;
        ComposeDialog();
    }

    private void ComposeDialog()
    {
        ClearComposers();
        ElementBounds elementBounds = ElementBounds.Fixed(0.0, 21.0, 500.0, 20.0);
        ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 11.0, 500.0, 30.0);
        ElementBounds elementBounds3 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        elementBounds3.BothSizing = ElementSizing.FitToChildren;
        ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftTop).WithFixedAlignmentOffset(60.0 + GuiStyle.DialogToScreenPadding, GuiStyle.DialogToScreenPadding);
        ElementBounds bounds2 = ElementBounds.Fixed(-320.0, 35.0, 300.0, 300.0);
        GuiTab[] tabs = new GuiTab[6]
        {
            new GuiTab
            {
                DataInt = 0,
                Name = "Hitbox 1"
            },
            new GuiTab
            {
                DataInt = 1,
                Name = "Hitbox 2"
            },
            new GuiTab
            {
                DataInt = 2,
                Name = "Hitbox 3"
            },
            new GuiTab
            {
                DataInt = 3,
                Name = "Hitbox 4"
            },
            new GuiTab
            {
                DataInt = 4,
                Name = "Hitbox 5"
            },
            new GuiTab
            {
                DataInt = 5,
                Name = "Hitbox 6"
            }
        };
        isChanging = true;
        base.SingleComposer = capi.Gui.CreateCompo("transformeditor", bounds).AddShadedDialogBG(elementBounds3).AddDialogTitleBar("Block Hitbox Editor (" + nowBlock.GetHeldItemName(new ItemStack(nowBlock)) + ")", OnTitleBarClose)
            .BeginChildElements(elementBounds3)
            .AddVerticalTabs(tabs, bounds2, OnTabClicked, "verticalTabs")
            .AddStaticText("X1", CairoFont.WhiteDetailText(), elementBounds = elementBounds.FlatCopy().WithFixedWidth(230.0))
            .AddNumberInput(elementBounds2 = elementBounds2.BelowCopy().WithFixedWidth(230.0), delegate (string val)
            {
                onCoordVal(val, 0);
            }, CairoFont.WhiteDetailText(), "x1")
            .AddStaticText("X2", CairoFont.WhiteDetailText(), elementBounds.RightCopy(40.0))
            .AddNumberInput(elementBounds2.RightCopy(40.0), delegate (string val)
            {
                onCoordVal(val, 3);
            }, CairoFont.WhiteDetailText(), "x2")
            .AddStaticText("Y1", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 33.0))
            .AddNumberInput(elementBounds2 = elementBounds2.BelowCopy(0.0, 22.0), delegate (string val)
            {
                onCoordVal(val, 1);
            }, CairoFont.WhiteDetailText(), "y1")
            .AddStaticText("Y2", CairoFont.WhiteDetailText(), elementBounds.RightCopy(40.0))
            .AddNumberInput(elementBounds2.RightCopy(40.0), delegate (string val)
            {
                onCoordVal(val, 4);
            }, CairoFont.WhiteDetailText(), "y2")
            .AddStaticText("Z1", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 32.0))
            .AddNumberInput(elementBounds2 = elementBounds2.BelowCopy(0.0, 22.0), delegate (string val)
            {
                onCoordVal(val, 2);
            }, CairoFont.WhiteDetailText(), "z1")
            .AddStaticText("Z2", CairoFont.WhiteDetailText(), elementBounds.RightCopy(40.0))
            .AddNumberInput(elementBounds2.RightCopy(40.0), delegate (string val)
            {
                onCoordVal(val, 5);
            }, CairoFont.WhiteDetailText(), "z2")
            .AddStaticText("ΔX", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 38.0).WithFixedWidth(50.0))
            .AddNumberInput(elementBounds2 = elementBounds2.BelowCopy(0.0, 28.0).WithFixedWidth(50.0), delegate (string val)
            {
                onDeltaVal(val, 0);
            }, CairoFont.WhiteDetailText(), "dx")
            .AddStaticText("ΔY", CairoFont.WhiteDetailText(), elementBounds = elementBounds.RightCopy(5.0))
            .AddNumberInput(elementBounds2 = elementBounds2.RightCopy(5.0), delegate (string val)
            {
                onDeltaVal(val, 1);
            }, CairoFont.WhiteDetailText(), "dy")
            .AddStaticText("ΔZ", CairoFont.WhiteDetailText(), elementBounds = elementBounds.RightCopy(5.0))
            .AddNumberInput(elementBounds2 = elementBounds2.RightCopy(5.0), delegate (string val)
            {
                onDeltaVal(val, 2);
            }, CairoFont.WhiteDetailText(), "dz")
            .AddStaticText("Json Code", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(-110.0, 36.0).WithFixedWidth(500.0))
            .BeginClip(elementBounds2.BelowCopy(-110.0, 26.0).WithFixedHeight(200.0).WithFixedWidth(500.0))
            .AddTextArea(elementBounds2 = elementBounds2.BelowCopy(-110.0, 26.0).WithFixedHeight(200.0).WithFixedWidth(500.0), null, CairoFont.WhiteSmallText(), "textarea")
            .EndClip()
            .AddSmallButton("Close & Apply", OnApplyJson, elementBounds2 = elementBounds2.BelowCopy(0.0, 20.0).WithFixedSize(200.0, 20.0).WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedPadding(10.0, 2.0))
            .AddSmallButton("Copy JSON", OnCopyJson, elementBounds2 = elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0))
            .EndChildElements()
            .Compose();
        Cuboidf cuboidf = new Cuboidf();
        if (boxIndex < currentSelBoxes.Length)
        {
            cuboidf = currentSelBoxes[boxIndex];
        }
        else
        {
            while (boxIndex >= currentSelBoxes.Length)
            {
                currentSelBoxes = currentSelBoxes.Append(new Cuboidf());
            }

            nowBlock.SelectionBoxes = currentSelBoxes;
        }

        for (int i = 0; i < coordnames.Length; i++)
        {
            base.SingleComposer.GetNumberInput(coordnames[i]).SetValue(cuboidf[i]);
            base.SingleComposer.GetNumberInput(coordnames[i]).Interval = 0.0625f;
        }

        base.SingleComposer.GetNumberInput("dx").Interval = 0.0625f;
        base.SingleComposer.GetNumberInput("dy").Interval = 0.0625f;
        base.SingleComposer.GetNumberInput("dz").Interval = 0.0625f;
        base.SingleComposer.GetVerticalTab("verticalTabs").SetValue(boxIndex, triggerHandler: false);
        isChanging = false;
    }

    private void OnTabClicked(int index, GuiTab tab)
    {
        boxIndex = index;
        ComposeDialog();
    }

    private bool OnApplyJson()
    {
        originalSelBoxes = currentSelBoxes;
        TreeAttribute treeAttribute = new TreeAttribute();
        treeAttribute.SetInt("nowblockid", nowBlock.Id);
        treeAttribute.SetBlockPos("pos", nowPos);
        capi.Event.PushEvent("onapplyselboxes", treeAttribute);
        TryClose();
        return true;
    }

    private bool OnCopyJson()
    {
        ScreenManager.Platform.XPlatInterface.SetClipboardText(getJson());
        return true;
    }

    private void updateJson()
    {
        base.SingleComposer.GetTextArea("textarea").SetValue(getJson());
    }

    private string getJson()
    {
        List<Cuboidf> list = new List<Cuboidf>();
        for (int i = 0; i < currentSelBoxes.Length; i++)
        {
            if (!currentSelBoxes[i].Empty)
            {
                list.Add(currentSelBoxes[i]);
            }
        }

        if (list.Count == 0)
        {
            return "";
        }

        if (list.Count == 1)
        {
            Cuboidf cuboidf = currentSelBoxes[0];
            return string.Format(GlobalConstants.DefaultCultureInfo, "\tselectionBox: {{ x1: {0}, y1: {1}, z1: {2}, x2: {3}, y2: {4}, z2: {5} }}\n", cuboidf.X1, cuboidf.Y1, cuboidf.Z1, cuboidf.X2, cuboidf.Y2, cuboidf.Z2);
        }

        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("\tselectionBoxes: [\n");
        foreach (Cuboidf item in list)
        {
            stringBuilder.Append(string.Format(GlobalConstants.DefaultCultureInfo, "\t\t{{ x1: {0}, y1: {1}, z1: {2}, x2: {3}, y2: {4}, z2: {5} }},\n", item.X1, item.Y1, item.Z1, item.X2, item.Y2, item.Z2));
        }

        stringBuilder.Append("\t]");
        return stringBuilder.ToString();
    }

    private void onCoordVal(string val, int index)
    {
        if (!isChanging)
        {
            isChanging = true;
            float.TryParse(val, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result);
            currentSelBoxes[boxIndex][index] = result;
            updateJson();
            isChanging = false;
        }
    }

    private void onDeltaVal(string val, int index)
    {
        if (!isChanging)
        {
            isChanging = true;
            float.TryParse(val, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result);
            Cuboidf cuboidf = currentSelBoxes[boxIndex];
            switch (index)
            {
                case 0:
                    cuboidf.X1 += result;
                    cuboidf.X2 += result;
                    base.SingleComposer.GetNumberInput("dx").SetValue("");
                    break;
                case 1:
                    cuboidf.Y1 += result;
                    cuboidf.Y2 += result;
                    base.SingleComposer.GetNumberInput("dy").SetValue("");
                    break;
                case 2:
                    cuboidf.Z1 += result;
                    cuboidf.Z2 += result;
                    base.SingleComposer.GetNumberInput("dz").SetValue("");
                    break;
            }

            for (int i = 0; i < coordnames.Length; i++)
            {
                base.SingleComposer.GetNumberInput(coordnames[i]).SetValue(cuboidf[i]);
                base.SingleComposer.GetNumberInput(coordnames[i]).Interval = 0.0625f;
            }

            updateJson();
            isChanging = false;
        }
    }

    private void OnTitleBarClose()
    {
        TryClose();
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();
        currentSelBoxes = originalSelBoxes;
        TreeAttribute treeAttribute = new TreeAttribute();
        treeAttribute.SetInt("nowblockid", nowBlock.Id);
        treeAttribute.SetBlockPos("pos", nowPos);
        capi.Event.PushEvent("oncloseeditselboxes", treeAttribute);
    }

    public override void OnMouseWheel(MouseWheelEventArgs args)
    {
        base.OnMouseWheel(args);
        args.SetHandled();
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
