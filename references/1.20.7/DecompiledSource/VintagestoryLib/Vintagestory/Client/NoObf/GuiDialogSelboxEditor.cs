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
		BlockSelection blockSel = capi.World.Player.CurrentBlockSelection;
		boxIndex = 0;
		if (blockSel == null)
		{
			capi.World.Player.ShowChatNotification("Look at a block first");
			capi.Event.EnqueueMainThreadTask(delegate
			{
				TryClose();
			}, "closegui");
			return;
		}
		nowPos = blockSel.Position.Copy();
		nowBlock = capi.World.BlockAccessor.GetBlock(blockSel.Position);
		TreeAttribute tree = new TreeAttribute();
		tree.SetInt("nowblockid", nowBlock.Id);
		tree.SetBlockPos("pos", blockSel.Position);
		capi.Event.PushEvent("oneditselboxes", tree);
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
		ElementBounds line = ElementBounds.Fixed(0.0, 21.0, 500.0, 20.0);
		ElementBounds input = ElementBounds.Fixed(0.0, 11.0, 500.0, 30.0);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftTop).WithFixedAlignmentOffset(60.0 + GuiStyle.DialogToScreenPadding, GuiStyle.DialogToScreenPadding);
		ElementBounds tabBounds = ElementBounds.Fixed(-320.0, 35.0, 300.0, 300.0);
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
		base.SingleComposer = capi.Gui.CreateCompo("transformeditor", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar("Block Hitbox Editor (" + nowBlock.GetHeldItemName(new ItemStack(nowBlock)) + ")", OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddVerticalTabs(tabs, tabBounds, OnTabClicked, "verticalTabs")
			.AddStaticText("X1", CairoFont.WhiteDetailText(), line = line.FlatCopy().WithFixedWidth(230.0))
			.AddNumberInput(input = input.BelowCopy().WithFixedWidth(230.0), delegate(string val)
			{
				onCoordVal(val, 0);
			}, CairoFont.WhiteDetailText(), "x1")
			.AddStaticText("X2", CairoFont.WhiteDetailText(), line.RightCopy(40.0))
			.AddNumberInput(input.RightCopy(40.0), delegate(string val)
			{
				onCoordVal(val, 3);
			}, CairoFont.WhiteDetailText(), "x2")
			.AddStaticText("Y1", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 33.0))
			.AddNumberInput(input = input.BelowCopy(0.0, 22.0), delegate(string val)
			{
				onCoordVal(val, 1);
			}, CairoFont.WhiteDetailText(), "y1")
			.AddStaticText("Y2", CairoFont.WhiteDetailText(), line.RightCopy(40.0))
			.AddNumberInput(input.RightCopy(40.0), delegate(string val)
			{
				onCoordVal(val, 4);
			}, CairoFont.WhiteDetailText(), "y2")
			.AddStaticText("Z1", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 32.0))
			.AddNumberInput(input = input.BelowCopy(0.0, 22.0), delegate(string val)
			{
				onCoordVal(val, 2);
			}, CairoFont.WhiteDetailText(), "z1")
			.AddStaticText("Z2", CairoFont.WhiteDetailText(), line.RightCopy(40.0))
			.AddNumberInput(input.RightCopy(40.0), delegate(string val)
			{
				onCoordVal(val, 5);
			}, CairoFont.WhiteDetailText(), "z2")
			.AddStaticText("ΔX", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 38.0).WithFixedWidth(50.0))
			.AddNumberInput(input = input.BelowCopy(0.0, 28.0).WithFixedWidth(50.0), delegate(string val)
			{
				onDeltaVal(val, 0);
			}, CairoFont.WhiteDetailText(), "dx")
			.AddStaticText("ΔY", CairoFont.WhiteDetailText(), line = line.RightCopy(5.0))
			.AddNumberInput(input = input.RightCopy(5.0), delegate(string val)
			{
				onDeltaVal(val, 1);
			}, CairoFont.WhiteDetailText(), "dy")
			.AddStaticText("ΔZ", CairoFont.WhiteDetailText(), line = line.RightCopy(5.0))
			.AddNumberInput(input = input.RightCopy(5.0), delegate(string val)
			{
				onDeltaVal(val, 2);
			}, CairoFont.WhiteDetailText(), "dz")
			.AddStaticText("Json Code", CairoFont.WhiteDetailText(), line = line.BelowCopy(-110.0, 36.0).WithFixedWidth(500.0))
			.BeginClip(input.BelowCopy(-110.0, 26.0).WithFixedHeight(200.0).WithFixedWidth(500.0))
			.AddTextArea(input = input.BelowCopy(-110.0, 26.0).WithFixedHeight(200.0).WithFixedWidth(500.0), null, CairoFont.WhiteSmallText(), "textarea")
			.EndClip()
			.AddSmallButton("Close & Apply", OnApplyJson, input = input.BelowCopy(0.0, 20.0).WithFixedSize(200.0, 20.0).WithAlignment(EnumDialogArea.LeftFixed)
				.WithFixedPadding(10.0, 2.0))
			.AddSmallButton("Copy JSON", OnCopyJson, input = input.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0))
			.EndChildElements()
			.Compose();
		Cuboidf hitbox = new Cuboidf();
		if (boxIndex < currentSelBoxes.Length)
		{
			hitbox = currentSelBoxes[boxIndex];
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
			base.SingleComposer.GetNumberInput(coordnames[i]).SetValue(hitbox[i]);
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
		TreeAttribute tree = new TreeAttribute();
		tree.SetInt("nowblockid", nowBlock.Id);
		tree.SetBlockPos("pos", nowPos);
		capi.Event.PushEvent("onapplyselboxes", tree);
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
		List<Cuboidf> nonEmptyBoxes = new List<Cuboidf>();
		for (int i = 0; i < currentSelBoxes.Length; i++)
		{
			if (!currentSelBoxes[i].Empty)
			{
				nonEmptyBoxes.Add(currentSelBoxes[i]);
			}
		}
		if (nonEmptyBoxes.Count == 0)
		{
			return "";
		}
		if (nonEmptyBoxes.Count == 1)
		{
			Cuboidf box = currentSelBoxes[0];
			return string.Format(GlobalConstants.DefaultCultureInfo, "\tselectionBox: {{ x1: {0}, y1: {1}, z1: {2}, x2: {3}, y2: {4}, z2: {5} }}\n", box.X1, box.Y1, box.Z1, box.X2, box.Y2, box.Z2);
		}
		StringBuilder json = new StringBuilder();
		json.Append("\tselectionBoxes: [\n");
		foreach (Cuboidf box2 in nonEmptyBoxes)
		{
			json.Append(string.Format(GlobalConstants.DefaultCultureInfo, "\t\t{{ x1: {0}, y1: {1}, z1: {2}, x2: {3}, y2: {4}, z2: {5} }},\n", box2.X1, box2.Y1, box2.Z1, box2.X2, box2.Y2, box2.Z2));
		}
		json.Append("\t]");
		return json.ToString();
	}

	private void onCoordVal(string val, int index)
	{
		if (!isChanging)
		{
			isChanging = true;
			float.TryParse(val, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var value);
			currentSelBoxes[boxIndex][index] = value;
			updateJson();
			isChanging = false;
		}
	}

	private void onDeltaVal(string val, int index)
	{
		if (!isChanging)
		{
			isChanging = true;
			float.TryParse(val, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var value);
			Cuboidf hitbox = currentSelBoxes[boxIndex];
			switch (index)
			{
			case 0:
				hitbox.X1 += value;
				hitbox.X2 += value;
				base.SingleComposer.GetNumberInput("dx").SetValue("");
				break;
			case 1:
				hitbox.Y1 += value;
				hitbox.Y2 += value;
				base.SingleComposer.GetNumberInput("dy").SetValue("");
				break;
			case 2:
				hitbox.Z1 += value;
				hitbox.Z2 += value;
				base.SingleComposer.GetNumberInput("dz").SetValue("");
				break;
			}
			for (int i = 0; i < coordnames.Length; i++)
			{
				base.SingleComposer.GetNumberInput(coordnames[i]).SetValue(hitbox[i]);
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
		TreeAttribute tree = new TreeAttribute();
		tree.SetInt("nowblockid", nowBlock.Id);
		tree.SetBlockPos("pos", nowPos);
		capi.Event.PushEvent("oncloseeditselboxes", tree);
	}

	public override void OnMouseWheel(MouseWheelEventArgs args)
	{
		base.OnMouseWheel(args);
		args.SetHandled();
	}
}
