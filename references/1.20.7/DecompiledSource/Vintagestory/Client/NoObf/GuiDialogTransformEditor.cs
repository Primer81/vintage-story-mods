#region Assembly VintagestoryLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class GuiDialogTransformEditor : GuiDialog
{
    private ModelTransform originalTransform;

    private CollectibleObject oldCollectible;

    private ModelTransform currentTransform = new ModelTransform();

    private int target = 2;

    public static List<TransformConfig> extraTransforms = new List<TransformConfig>
    {
        new TransformConfig
        {
            AttributeName = "toolrackTransform",
            Title = "On Tool rack"
        },
        new TransformConfig
        {
            AttributeName = "onmoldrackTransform",
            Title = "On vertical rack"
        },
        new TransformConfig
        {
            AttributeName = "onDisplayTransform",
            Title = "In display case"
        },
        new TransformConfig
        {
            AttributeName = "groundStorageTransform",
            Title = "Placed on ground"
        },
        new TransformConfig
        {
            AttributeName = "onshelfTransform",
            Title = "On shelf"
        },
        new TransformConfig
        {
            AttributeName = "onscrollrackTransform",
            Title = "On scroll rack"
        },
        new TransformConfig
        {
            AttributeName = "onAntlerMountTransform",
            Title = "On antler mount"
        },
        new TransformConfig
        {
            AttributeName = "onTongTransform",
            Title = "On tongs"
        }
    };

    public ModelTransform TargetTransform
    {
        get
        {
            List<ModelTransform> list = new List<ModelTransform>(new ModelTransform[5] { oldCollectible.GuiTransform, oldCollectible.FpHandTransform, oldCollectible.TpHandTransform, oldCollectible.TpOffHandTransform, oldCollectible.GroundTransform });
            foreach (TransformConfig extraTransform in extraTransforms)
            {
                JsonObject attributes = oldCollectible.Attributes;
                list.Add((attributes != null && attributes[extraTransform.AttributeName].Exists) ? oldCollectible.Attributes?[extraTransform.AttributeName].AsObject<ModelTransform>() : new ModelTransform().EnsureDefaultValues());
            }

            return list[target];
        }
        set
        {
            //IL_0091: Unknown result type (might be due to invalid IL or missing references)
            //IL_009b: Expected O, but got Unknown
            switch (target)
            {
                case 4:
                    oldCollectible.GroundTransform = value;
                    return;
                case 0:
                    oldCollectible.GuiTransform = value;
                    return;
                case 2:
                    oldCollectible.TpHandTransform = value;
                    return;
                case 3:
                    oldCollectible.TpOffHandTransform = value;
                    return;
            }

            if (extraTransforms.Count >= target - 5)
            {
                TransformConfig transformConfig = extraTransforms[target - 5];
                if (oldCollectible.Attributes == null)
                {
                    oldCollectible.Attributes = new JsonObject((JToken)new JObject());
                }

                oldCollectible.Attributes.Token[(object)transformConfig.AttributeName] = JToken.FromObject((object)value);
            }
        }
    }

    public override string ToggleKeyCombinationCode => null;

    public override bool PrefersUngrabbedMouse => true;

    public GuiDialogTransformEditor(ICoreClientAPI capi)
        : base(capi)
    {
        (capi.World as ClientMain).eventManager.OnActiveSlotChanged.Add(OnActiveSlotChanged);
        capi.ChatCommands.GetOrCreate("dev").WithDescription("Gamedev tools").BeginSubCommand("tfedit")
            .WithRootAlias("tfedit")
            .WithDescription("Opens the Transform Editor")
            .WithArgs(capi.ChatCommands.Parsers.OptionalWordRange("type", "fp", "tp", "tpo", "gui", "ground"))
            .HandleWith(CmdTransformEditor)
            .EndSubCommand();
    }

    private TextCommandResult CmdTransformEditor(TextCommandCallingArgs args)
    {
        string text = args[0] as string;
        switch (text)
        {
            case "gui":
                target = 0;
                break;
            case "tp":
                target = 2;
                break;
            case "tpo":
                target = 3;
                break;
            case "ground":
                target = 4;
                break;
        }

        int num = -1;
        for (int i = 0; i < extraTransforms.Count; i++)
        {
            if (extraTransforms[i].AttributeName == text)
            {
                num = i;
            }
        }

        if (num >= 0)
        {
            target = num + 5;
        }

        if (capi.World.Player.InventoryManager.ActiveHotbarSlot?.Itemstack == null)
        {
            return TextCommandResult.Success("Put something in your active slot first");
        }

        TryOpen();
        return TextCommandResult.Success();
    }

    public override void OnGuiOpened()
    {
        capi.Event.PushEvent("onedittransforms");
        currentTransform = new ModelTransform();
        currentTransform.Rotation = new Vec3f();
        currentTransform.Translation = new Vec3f();
        oldCollectible = capi.World.Player.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible;
        originalTransform = TargetTransform;
        TargetTransform = (currentTransform = originalTransform.Clone());
        ComposeDialog();
    }

    private void OnActiveSlotChanged()
    {
        if (IsOpened())
        {
            TargetTransform = originalTransform;
            if (capi.World.Player.InventoryManager.ActiveHotbarSlot?.Itemstack == null)
            {
                TryClose();
                capi.World.Player.ShowChatNotification("Put something in your active slot");
                return;
            }

            oldCollectible = capi.World.Player.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible;
            originalTransform = TargetTransform;
            currentTransform = originalTransform.Clone();
            TargetTransform = currentTransform;
        }
    }

    private void ComposeDialog()
    {
        ClearComposers();
        ElementBounds elementBounds = ElementBounds.Fixed(0.0, 22.0, 500.0, 20.0);
        ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 11.0, 230.0, 30.0);
        ElementBounds elementBounds3 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        elementBounds3.BothSizing = ElementSizing.FitToChildren;
        ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftTop).WithFixedAlignmentOffset(110.0 + GuiStyle.DialogToScreenPadding, GuiStyle.DialogToScreenPadding);
        ElementBounds bounds2 = ElementBounds.Fixed(-320.0, 35.0, 300.0, 500.0);
        ElementBounds bounds3 = ElementBounds.FixedSize(500.0, 200.0);
        ElementBounds elementBounds4 = ElementBounds.FixedSize(500.0, 200.0);
        ElementBounds elementBounds5 = ElementBounds.FixedSize(200.0, 20.0).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0);
        List<GuiTab> list = new List<GuiTab>
        {
            new GuiTab
            {
                DataInt = 0,
                Name = "Gui"
            },
            new GuiTab
            {
                DataInt = 2,
                Name = "Main Hand"
            },
            new GuiTab
            {
                DataInt = 3,
                Name = "Off Hand"
            },
            new GuiTab
            {
                DataInt = 4,
                Name = "Ground"
            }
        };
        int num = 5;
        double paddingTop = GuiElement.scaled(15.0);
        foreach (TransformConfig extraTransform in extraTransforms)
        {
            list.Add(new GuiTab
            {
                DataInt = num++,
                Name = extraTransform.Title,
                PaddingTop = paddingTop
            });
            paddingTop = 0.0;
        }

        base.SingleComposer = capi.Gui.CreateCompo("transformeditor", bounds).AddShadedDialogBG(elementBounds3).AddDialogTitleBar("Transform Editor (" + target + ")", OnTitleBarClose)
            .BeginChildElements(elementBounds3)
            .AddVerticalTabs(list.ToArray(), bounds2, OnTabClicked, "verticalTabs")
            .AddStaticText("Translation X", CairoFont.WhiteDetailText(), elementBounds = elementBounds.FlatCopy().WithFixedWidth(230.0))
            .AddNumberInput(elementBounds2 = elementBounds2.BelowCopy(), OnTranslateX, CairoFont.WhiteDetailText(), "translatex")
            .AddStaticText("Origin X", CairoFont.WhiteDetailText(), elementBounds.RightCopy(40.0))
            .AddNumberInput(elementBounds2.RightCopy(40.0), OnOriginX, CairoFont.WhiteDetailText(), "originx")
            .AddStaticText("Translation Y", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 33.0))
            .AddNumberInput(elementBounds2 = elementBounds2.BelowCopy(0.0, 22.0), OnTranslateY, CairoFont.WhiteDetailText(), "translatey")
            .AddStaticText("Origin Y", CairoFont.WhiteDetailText(), elementBounds.RightCopy(40.0))
            .AddNumberInput(elementBounds2.RightCopy(40.0), OnOriginY, CairoFont.WhiteDetailText(), "originy")
            .AddStaticText("Translation Z", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 32.0))
            .AddNumberInput(elementBounds2 = elementBounds2.BelowCopy(0.0, 22.0), OnTranslateZ, CairoFont.WhiteDetailText(), "translatez")
            .AddStaticText("Origin Z", CairoFont.WhiteDetailText(), elementBounds.RightCopy(40.0))
            .AddNumberInput(elementBounds2.RightCopy(40.0), OnOriginZ, CairoFont.WhiteDetailText(), "originz")
            .AddStaticText("Rotation X", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 33.0).WithFixedWidth(500.0))
            .AddSlider(OnRotateX, elementBounds2 = elementBounds2.BelowCopy(0.0, 22.0).WithFixedWidth(500.0), "rotatex")
            .AddStaticText("Rotation Y", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 32.0))
            .AddSlider(OnRotateY, elementBounds2 = elementBounds2.BelowCopy(0.0, 22.0), "rotatey")
            .AddStaticText("Rotation Z", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 32.0))
            .AddSlider(OnRotateZ, elementBounds2 = elementBounds2.BelowCopy(0.0, 22.0), "rotatez")
            .AddStaticText("Scale", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 32.0))
            .AddSlider(OnScale, elementBounds2 = elementBounds2.BelowCopy(0.0, 22.0), "scale")
            .AddSwitch(onFlipXAxis, elementBounds2 = elementBounds2.BelowCopy(0.0, 10.0), "flipx", 20.0)
            .AddStaticText("Flip on X-Axis", CairoFont.WhiteDetailText(), elementBounds2.RightCopy(10.0, 1.0).WithFixedWidth(200.0))
            .AddStaticText("Json Code", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 72.0))
            .BeginClip(elementBounds4.FixedUnder(elementBounds2, 37.0))
            .AddTextArea(bounds3, null, CairoFont.WhiteSmallText(), "textarea")
            .EndClip()
            .AddSmallButton("Close & Apply", OnApplyJson, elementBounds5 = elementBounds5.FlatCopy().FixedUnder(elementBounds4, 15.0))
            .AddSmallButton("Copy JSON", OnCopyJson, elementBounds5 = elementBounds5.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0))
            .EndChildElements()
            .Compose();
        base.SingleComposer.GetTextInput("translatex").SetValue(currentTransform.Translation.X.ToString(GlobalConstants.DefaultCultureInfo));
        base.SingleComposer.GetTextInput("translatey").SetValue(currentTransform.Translation.Y.ToString(GlobalConstants.DefaultCultureInfo));
        base.SingleComposer.GetTextInput("translatez").SetValue(currentTransform.Translation.Z.ToString(GlobalConstants.DefaultCultureInfo));
        base.SingleComposer.GetTextInput("originx").SetValue(currentTransform.Origin.X.ToString(GlobalConstants.DefaultCultureInfo));
        base.SingleComposer.GetTextInput("originy").SetValue(currentTransform.Origin.Y.ToString(GlobalConstants.DefaultCultureInfo));
        base.SingleComposer.GetTextInput("originz").SetValue(currentTransform.Origin.Z.ToString(GlobalConstants.DefaultCultureInfo));
        base.SingleComposer.GetSlider("rotatex").SetValues((int)currentTransform.Rotation.X, -180, 180, 1);
        base.SingleComposer.GetSlider("rotatey").SetValues((int)currentTransform.Rotation.Y, -180, 180, 1);
        base.SingleComposer.GetSlider("rotatez").SetValues((int)currentTransform.Rotation.Z, -180, 180, 1);
        base.SingleComposer.GetSlider("scale").SetValues((int)Math.Abs(100f * currentTransform.ScaleXYZ.X), 25, 600, 1);
        base.SingleComposer.GetSwitch("flipx").On = currentTransform.ScaleXYZ.X < 0f;
        base.SingleComposer.GetVerticalTab("verticalTabs").SetValue(list.IndexOf((GuiTab tab) => tab.DataInt == target), triggerHandler: false);
    }

    private void onFlipXAxis(bool on)
    {
        currentTransform.ScaleXYZ.X *= -1f;
        updateJson();
    }

    private void OnOriginX(string val)
    {
        currentTransform.Origin.X = val.ToFloat();
        updateJson();
    }

    private void OnOriginY(string val)
    {
        currentTransform.Origin.Y = val.ToFloat();
        updateJson();
    }

    private void OnOriginZ(string val)
    {
        currentTransform.Origin.Z = val.ToFloat();
        updateJson();
    }

    private void OnTabClicked(int index, GuiTab tab)
    {
        TargetTransform = originalTransform;
        target = tab.DataInt;
        OnGuiOpened();
    }

    private bool OnApplyJson()
    {
        TargetTransform = (originalTransform = currentTransform);
        currentTransform = null;
        capi.Event.PushEvent("onapplytransforms");
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
        if (target >= 5)
        {
            TargetTransform = currentTransform;
        }

        base.SingleComposer.GetTextArea("textarea").SetValue(getJson());
    }

    private string getJson()
    {
        StringBuilder stringBuilder = new StringBuilder();
        ModelTransform modelTransform = new ModelTransform();
        string text = "\t\t";
        switch (target)
        {
            case 4:
                stringBuilder.Append("\tgroundTransform: {\n");
                modelTransform = ((oldCollectible is Block) ? ModelTransform.BlockDefaultFp() : ModelTransform.ItemDefaultFp());
                break;
            case 0:
                stringBuilder.Append("\tguiTransform: {\n");
                modelTransform = ((oldCollectible is Block) ? ModelTransform.BlockDefaultGui() : ModelTransform.ItemDefaultGui());
                break;
            case 2:
                stringBuilder.Append("\ttpHandTransform: {\n");
                modelTransform = ((oldCollectible is Block) ? ModelTransform.BlockDefaultTp() : ModelTransform.ItemDefaultTp());
                break;
            case 3:
                stringBuilder.Append("\ttpOffHandTransform: {\n");
                modelTransform = ((oldCollectible is Block) ? ModelTransform.BlockDefaultTp() : ModelTransform.ItemDefaultTp());
                break;
        }

        if (target >= 5 && extraTransforms.Count >= target - 5)
        {
            TransformConfig transformConfig = extraTransforms[target - 5];
            stringBuilder.Append("\tattributes: {\n");
            stringBuilder.Append("\t\t" + transformConfig.AttributeName + ": {\n");
            text = "\t\t\t";
            modelTransform = new ModelTransform().EnsureDefaultValues();
        }

        bool flag = false;
        if (currentTransform.Translation.X != modelTransform.Translation.X || currentTransform.Translation.Y != modelTransform.Translation.Y || currentTransform.Translation.Z != modelTransform.Translation.Z)
        {
            stringBuilder.Append(string.Format(GlobalConstants.DefaultCultureInfo, text + "translation: {{ x: {0}, y: {1}, z: {2} }}", currentTransform.Translation.X, currentTransform.Translation.Y, currentTransform.Translation.Z));
            flag = true;
        }

        if (currentTransform.Rotation.X != modelTransform.Rotation.X || currentTransform.Rotation.Y != modelTransform.Rotation.Y || currentTransform.Rotation.Z != modelTransform.Rotation.Z)
        {
            if (flag)
            {
                stringBuilder.Append(",\n");
            }

            stringBuilder.Append(string.Format(GlobalConstants.DefaultCultureInfo, text + "rotation: {{ x: {0}, y: {1}, z: {2} }}", currentTransform.Rotation.X, currentTransform.Rotation.Y, currentTransform.Rotation.Z));
            flag = true;
        }

        if (currentTransform.Origin.X != modelTransform.Origin.X || currentTransform.Origin.Y != modelTransform.Origin.Y || currentTransform.Origin.Z != modelTransform.Origin.Z)
        {
            if (flag)
            {
                stringBuilder.Append(",\n");
            }

            stringBuilder.Append(string.Format(GlobalConstants.DefaultCultureInfo, text + "origin: {{ x: {0}, y: {1}, z: {2} }}", currentTransform.Origin.X, currentTransform.Origin.Y, currentTransform.Origin.Z));
            flag = true;
        }

        if (currentTransform.ScaleXYZ.X != modelTransform.ScaleXYZ.X)
        {
            if (flag)
            {
                stringBuilder.Append(",\n");
            }

            if (currentTransform.ScaleXYZ.X != currentTransform.ScaleXYZ.Y || currentTransform.ScaleXYZ.X != currentTransform.ScaleXYZ.Z)
            {
                stringBuilder.Append(string.Format(GlobalConstants.DefaultCultureInfo, text + "scaleXyz: {{ x: {0}, y: {1}, z: {2} }}", currentTransform.ScaleXYZ.X, currentTransform.ScaleXYZ.Y, currentTransform.ScaleXYZ.Z));
            }
            else
            {
                stringBuilder.Append(string.Format(GlobalConstants.DefaultCultureInfo, text + "scale: {0}", currentTransform.ScaleXYZ.X));
            }
        }

        if (target >= 5)
        {
            stringBuilder.Append("\n\t\t}");
        }

        stringBuilder.Append("\n\t}");
        string value = stringBuilder.ToString();
        TreeAttribute treeAttribute = new TreeAttribute();
        treeAttribute.SetString("json", value);
        capi.Event.PushEvent("genjsontransform", treeAttribute);
        return treeAttribute.GetString("json");
    }

    private bool OnScale(int val)
    {
        currentTransform.Scale = (float)val / 100f;
        if (base.SingleComposer.GetSwitch("flipx").On)
        {
            currentTransform.ScaleXYZ.X *= -1f;
        }

        updateJson();
        return true;
    }

    private bool OnRotateX(int deg)
    {
        currentTransform.Rotation.X = deg;
        updateJson();
        return true;
    }

    private bool OnRotateY(int deg)
    {
        currentTransform.Rotation.Y = deg;
        updateJson();
        return true;
    }

    private bool OnRotateZ(int deg)
    {
        currentTransform.Rotation.Z = deg;
        updateJson();
        return true;
    }

    private void OnTranslateX(string val)
    {
        currentTransform.Translation.X = val.ToFloat();
        updateJson();
    }

    private void OnTranslateY(string val)
    {
        currentTransform.Translation.Y = val.ToFloat();
        updateJson();
    }

    private void OnTranslateZ(string val)
    {
        currentTransform.Translation.Z = val.ToFloat();
        updateJson();
    }

    private void OnTitleBarClose()
    {
        TryClose();
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();
        if (oldCollectible != null)
        {
            TargetTransform = originalTransform;
        }

        capi.Event.PushEvent("oncloseedittransforms");
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
