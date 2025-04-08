using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

/// <summary>
/// Base class for dialogs bound to block entities.
/// </summary>
public abstract class GuiDialogBlockEntity : GuiDialogGeneric
{
	public bool IsDuplicate { get; }

	public InventoryBase Inventory { get; }

	public BlockPos BlockEntityPosition { get; }

	/// <summary>
	/// Gets the opening sound for the dialog being opened, or null if none.
	/// </summary>
	public virtual AssetLocation OpenSound { get; set; }

	/// <summary>
	/// Gets the opening sound for the dialog being opened, or null if none.
	/// </summary>
	public virtual AssetLocation CloseSound { get; set; }

	/// <summary>
	/// Gets the Y offset of the dialog in-world if floaty GUIs is turned on.
	/// 0.5 is the center of the block and larger means it will float higher up.
	/// </summary>
	protected virtual double FloatyDialogPosition => 0.75;

	/// <summary>
	/// Gets the Y align of the dialog if floaty GUIs is turned on.
	/// 0.5 means the dialog is centered on <see cref="P:Vintagestory.API.Client.GuiDialogBlockEntity.FloatyDialogPosition" />.
	/// 0 is top-aligned while 1 is bottom-aligned.
	/// </summary>
	protected virtual double FloatyDialogAlign => 0.75;

	public override bool PrefersUngrabbedMouse => false;

	/// <param name="dialogTitle">The title of this dialogue. Ex: "Chest"</param>
	/// <param name="inventory">The inventory associated with this block entity.</param>
	/// <param name="blockEntityPos">The position of this block entity.</param>
	/// <param name="capi">The Client API</param>
	public GuiDialogBlockEntity(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi)
		: base(dialogTitle, capi)
	{
		IsDuplicate = capi.World.Player.InventoryManager.Inventories.ContainsValue(inventory);
		if (!IsDuplicate)
		{
			Inventory = inventory;
			BlockEntityPosition = blockEntityPos;
		}
	}

	/// <param name="dialogTitle">The title of this dialogue. Ex: "Chest"</param>
	/// <param name="blockEntityPos">The position of this block entity.</param>
	/// <param name="capi">The Client API</param>
	public GuiDialogBlockEntity(string dialogTitle, BlockPos blockEntityPos, ICoreClientAPI capi)
		: base(dialogTitle, capi)
	{
		IsDuplicate = capi.OpenedGuis.FirstOrDefault((object dlg) => (dlg as GuiDialogBlockEntity)?.BlockEntityPosition == blockEntityPos) != null;
		if (!IsDuplicate)
		{
			BlockEntityPosition = blockEntityPos;
		}
	}

	/// <summary>
	/// This occurs right before the frame is pushed to the screen.
	/// </summary>
	/// <param name="dt">The time elapsed.</param>
	public override void OnFinalizeFrame(float dt)
	{
		base.OnFinalizeFrame(dt);
		if (!IsInRangeOfBlock(BlockEntityPosition))
		{
			capi.Event.EnqueueMainThreadTask(delegate
			{
				TryClose();
			}, "closedlg");
		}
	}

	/// <summary>
	/// Render's the object in Orthographic mode.
	/// </summary>
	/// <param name="deltaTime">The time elapsed.</param>
	public override void OnRenderGUI(float deltaTime)
	{
		if (capi.Settings.Bool["immersiveMouseMode"])
		{
			Vec3d pos = MatrixToolsd.Project(new Vec3d((double)BlockEntityPosition.X + 0.5, (double)BlockEntityPosition.Y + FloatyDialogPosition, (double)BlockEntityPosition.Z + 0.5), capi.Render.PerspectiveProjectionMat, capi.Render.PerspectiveViewMat, capi.Render.FrameWidth, capi.Render.FrameHeight);
			if (pos.Z < 0.0)
			{
				return;
			}
			base.SingleComposer.Bounds.Alignment = EnumDialogArea.None;
			base.SingleComposer.Bounds.fixedOffsetX = 0.0;
			base.SingleComposer.Bounds.fixedOffsetY = 0.0;
			base.SingleComposer.Bounds.absFixedX = pos.X - base.SingleComposer.Bounds.OuterWidth / 2.0;
			base.SingleComposer.Bounds.absFixedY = (double)capi.Render.FrameHeight - pos.Y - base.SingleComposer.Bounds.OuterHeight * FloatyDialogAlign;
			base.SingleComposer.Bounds.absMarginX = 0.0;
			base.SingleComposer.Bounds.absMarginY = 0.0;
		}
		base.OnRenderGUI(deltaTime);
	}

	/// <summary>
	/// We tunnel our packet through a block entity packet so the block entity can handle all the network stuff
	/// </summary>
	/// <param name="p"></param>
	protected void DoSendPacket(object p)
	{
		capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.InternalY, BlockEntityPosition.Z, p);
	}

	/// <summary>
	/// Called whenever the scrollbar or mouse wheel is used.
	/// </summary>
	/// <param name="value">The new value of the scrollbar.</param>
	protected void OnNewScrollbarvalue(float value)
	{
		ElementBounds bounds = base.SingleComposer.GetSlotGrid("slotgrid").Bounds;
		bounds.fixedY = 10.0 - GuiElementItemSlotGridBase.unscaledSlotPadding - (double)value;
		bounds.CalcWorldBounds();
	}

	/// <summary>
	/// Occurs whenever the X icon in the top right corner of the GUI (not the window) is pressed.
	/// </summary>
	protected void CloseIconPressed()
	{
		TryClose();
	}

	/// <summary>
	/// Called whenver the GUI is opened.
	/// </summary>
	public override void OnGuiOpened()
	{
		if (Inventory != null)
		{
			Inventory.Open(capi.World.Player);
			capi.World.Player.InventoryManager.OpenInventory(Inventory);
		}
		capi.Gui.PlaySound(OpenSound, randomizePitch: true);
	}

	/// <summary>
	/// Attempts to open this gui.
	/// </summary>
	/// <returns>Whether the attempt was successful.</returns>
	public override bool TryOpen()
	{
		if (IsDuplicate)
		{
			return false;
		}
		return base.TryOpen();
	}

	/// <summary>
	/// Called when the GUI is closed.
	/// </summary>
	public override void OnGuiClosed()
	{
		if (Inventory != null)
		{
			Inventory.Close(capi.World.Player);
			capi.World.Player.InventoryManager.CloseInventory(Inventory);
		}
		capi.Network.SendBlockEntityPacket(BlockEntityPosition, 1001);
		capi.Gui.PlaySound(CloseSound, randomizePitch: true);
	}

	/// <summary>
	/// Reloads the values of the GUI.
	/// </summary>
	public void ReloadValues()
	{
	}
}
