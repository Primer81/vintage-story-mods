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
			List<ModelTransform> tf = new List<ModelTransform>(new ModelTransform[5] { oldCollectible.GuiTransform, oldCollectible.FpHandTransform, oldCollectible.TpHandTransform, oldCollectible.TpOffHandTransform, oldCollectible.GroundTransform });
			foreach (TransformConfig extraTf in extraTransforms)
			{
				JsonObject attributes = oldCollectible.Attributes;
				tf.Add((attributes != null && attributes[extraTf.AttributeName].Exists) ? oldCollectible.Attributes?[extraTf.AttributeName].AsObject<ModelTransform>() : new ModelTransform().EnsureDefaultValues());
			}
			return tf[target];
		}
		set
		{
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
				TransformConfig extraTf = extraTransforms[target - 5];
				if (oldCollectible.Attributes == null)
				{
					oldCollectible.Attributes = new JsonObject(new JObject());
				}
				oldCollectible.Attributes.Token[extraTf.AttributeName] = JToken.FromObject(value);
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
		string type = args[0] as string;
		switch (type)
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
		int index = -1;
		for (int i = 0; i < extraTransforms.Count; i++)
		{
			if (extraTransforms[i].AttributeName == type)
			{
				index = i;
			}
		}
		if (index >= 0)
		{
			target = index + 5;
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
		ElementBounds line = ElementBounds.Fixed(0.0, 22.0, 500.0, 20.0);
		ElementBounds inputBnds = ElementBounds.Fixed(0.0, 11.0, 230.0, 30.0);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftTop).WithFixedAlignmentOffset(110.0 + GuiStyle.DialogToScreenPadding, GuiStyle.DialogToScreenPadding);
		ElementBounds tabBounds = ElementBounds.Fixed(-320.0, 35.0, 300.0, 500.0);
		ElementBounds textAreaBounds = ElementBounds.FixedSize(500.0, 200.0);
		ElementBounds clippingBounds = ElementBounds.FixedSize(500.0, 200.0);
		ElementBounds btnBounds = ElementBounds.FixedSize(200.0, 20.0).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0);
		List<GuiTab> tabs = new List<GuiTab>
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
		int i = 5;
		double padtop = GuiElement.scaled(15.0);
		foreach (TransformConfig extraTf in extraTransforms)
		{
			tabs.Add(new GuiTab
			{
				DataInt = i++,
				Name = extraTf.Title,
				PaddingTop = padtop
			});
			padtop = 0.0;
		}
		base.SingleComposer = capi.Gui.CreateCompo("transformeditor", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar("Transform Editor (" + target + ")", OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddVerticalTabs(tabs.ToArray(), tabBounds, OnTabClicked, "verticalTabs")
			.AddStaticText("Translation X", CairoFont.WhiteDetailText(), line = line.FlatCopy().WithFixedWidth(230.0))
			.AddNumberInput(inputBnds = inputBnds.BelowCopy(), OnTranslateX, CairoFont.WhiteDetailText(), "translatex")
			.AddStaticText("Origin X", CairoFont.WhiteDetailText(), line.RightCopy(40.0))
			.AddNumberInput(inputBnds.RightCopy(40.0), OnOriginX, CairoFont.WhiteDetailText(), "originx")
			.AddStaticText("Translation Y", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 33.0))
			.AddNumberInput(inputBnds = inputBnds.BelowCopy(0.0, 22.0), OnTranslateY, CairoFont.WhiteDetailText(), "translatey")
			.AddStaticText("Origin Y", CairoFont.WhiteDetailText(), line.RightCopy(40.0))
			.AddNumberInput(inputBnds.RightCopy(40.0), OnOriginY, CairoFont.WhiteDetailText(), "originy")
			.AddStaticText("Translation Z", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 32.0))
			.AddNumberInput(inputBnds = inputBnds.BelowCopy(0.0, 22.0), OnTranslateZ, CairoFont.WhiteDetailText(), "translatez")
			.AddStaticText("Origin Z", CairoFont.WhiteDetailText(), line.RightCopy(40.0))
			.AddNumberInput(inputBnds.RightCopy(40.0), OnOriginZ, CairoFont.WhiteDetailText(), "originz")
			.AddStaticText("Rotation X", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 33.0).WithFixedWidth(500.0))
			.AddSlider(OnRotateX, inputBnds = inputBnds.BelowCopy(0.0, 22.0).WithFixedWidth(500.0), "rotatex")
			.AddStaticText("Rotation Y", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 32.0))
			.AddSlider(OnRotateY, inputBnds = inputBnds.BelowCopy(0.0, 22.0), "rotatey")
			.AddStaticText("Rotation Z", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 32.0))
			.AddSlider(OnRotateZ, inputBnds = inputBnds.BelowCopy(0.0, 22.0), "rotatez")
			.AddStaticText("Scale", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 32.0))
			.AddSlider(OnScale, inputBnds = inputBnds.BelowCopy(0.0, 22.0), "scale")
			.AddSwitch(onFlipXAxis, inputBnds = inputBnds.BelowCopy(0.0, 10.0), "flipx", 20.0)
			.AddStaticText("Flip on X-Axis", CairoFont.WhiteDetailText(), inputBnds.RightCopy(10.0, 1.0).WithFixedWidth(200.0))
			.AddStaticText("Json Code", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 72.0))
			.BeginClip(clippingBounds.FixedUnder(inputBnds, 37.0))
			.AddTextArea(textAreaBounds, null, CairoFont.WhiteSmallText(), "textarea")
			.EndClip()
			.AddSmallButton("Close & Apply", OnApplyJson, btnBounds = btnBounds.FlatCopy().FixedUnder(clippingBounds, 15.0))
			.AddSmallButton("Copy JSON", OnCopyJson, btnBounds = btnBounds.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0))
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
		base.SingleComposer.GetVerticalTab("verticalTabs").SetValue(tabs.IndexOf((GuiTab tab) => tab.DataInt == target), triggerHandler: false);
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
		StringBuilder json = new StringBuilder();
		ModelTransform def = new ModelTransform();
		string indent = "\t\t";
		switch (target)
		{
		case 4:
			json.Append("\tgroundTransform: {\n");
			def = ((oldCollectible is Block) ? ModelTransform.BlockDefaultFp() : ModelTransform.ItemDefaultFp());
			break;
		case 0:
			json.Append("\tguiTransform: {\n");
			def = ((oldCollectible is Block) ? ModelTransform.BlockDefaultGui() : ModelTransform.ItemDefaultGui());
			break;
		case 2:
			json.Append("\ttpHandTransform: {\n");
			def = ((oldCollectible is Block) ? ModelTransform.BlockDefaultTp() : ModelTransform.ItemDefaultTp());
			break;
		case 3:
			json.Append("\ttpOffHandTransform: {\n");
			def = ((oldCollectible is Block) ? ModelTransform.BlockDefaultTp() : ModelTransform.ItemDefaultTp());
			break;
		}
		if (target >= 5 && extraTransforms.Count >= target - 5)
		{
			TransformConfig extraTf = extraTransforms[target - 5];
			json.Append("\tattributes: {\n");
			json.Append("\t\t" + extraTf.AttributeName + ": {\n");
			indent = "\t\t\t";
			def = new ModelTransform().EnsureDefaultValues();
		}
		bool added = false;
		if (currentTransform.Translation.X != def.Translation.X || currentTransform.Translation.Y != def.Translation.Y || currentTransform.Translation.Z != def.Translation.Z)
		{
			json.Append(string.Format(GlobalConstants.DefaultCultureInfo, indent + "translation: {{ x: {0}, y: {1}, z: {2} }}", currentTransform.Translation.X, currentTransform.Translation.Y, currentTransform.Translation.Z));
			added = true;
		}
		if (currentTransform.Rotation.X != def.Rotation.X || currentTransform.Rotation.Y != def.Rotation.Y || currentTransform.Rotation.Z != def.Rotation.Z)
		{
			if (added)
			{
				json.Append(",\n");
			}
			json.Append(string.Format(GlobalConstants.DefaultCultureInfo, indent + "rotation: {{ x: {0}, y: {1}, z: {2} }}", currentTransform.Rotation.X, currentTransform.Rotation.Y, currentTransform.Rotation.Z));
			added = true;
		}
		if (currentTransform.Origin.X != def.Origin.X || currentTransform.Origin.Y != def.Origin.Y || currentTransform.Origin.Z != def.Origin.Z)
		{
			if (added)
			{
				json.Append(",\n");
			}
			json.Append(string.Format(GlobalConstants.DefaultCultureInfo, indent + "origin: {{ x: {0}, y: {1}, z: {2} }}", currentTransform.Origin.X, currentTransform.Origin.Y, currentTransform.Origin.Z));
			added = true;
		}
		if (currentTransform.ScaleXYZ.X != def.ScaleXYZ.X)
		{
			if (added)
			{
				json.Append(",\n");
			}
			if (currentTransform.ScaleXYZ.X != currentTransform.ScaleXYZ.Y || currentTransform.ScaleXYZ.X != currentTransform.ScaleXYZ.Z)
			{
				json.Append(string.Format(GlobalConstants.DefaultCultureInfo, indent + "scaleXyz: {{ x: {0}, y: {1}, z: {2} }}", currentTransform.ScaleXYZ.X, currentTransform.ScaleXYZ.Y, currentTransform.ScaleXYZ.Z));
			}
			else
			{
				json.Append(string.Format(GlobalConstants.DefaultCultureInfo, indent + "scale: {0}", currentTransform.ScaleXYZ.X));
			}
		}
		if (target >= 5)
		{
			json.Append("\n\t\t}");
		}
		json.Append("\n\t}");
		string jsonstr = json.ToString();
		TreeAttribute tree = new TreeAttribute();
		tree.SetString("json", jsonstr);
		capi.Event.PushEvent("genjsontransform", tree);
		return tree.GetString("json");
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
