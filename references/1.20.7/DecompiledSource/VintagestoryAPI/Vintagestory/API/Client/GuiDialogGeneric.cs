using System;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

/// <summary>
/// Mainly used for block entity based guis
/// </summary>
public abstract class GuiDialogGeneric : GuiDialog
{
	/// <summary>
	/// The title of the Dialog.
	/// </summary>
	public string DialogTitle;

	/// <summary>
	/// Should this Dialog de-register itself once closed?
	/// </summary>
	public override bool UnregisterOnClose => true;

	/// <summary>
	/// The tree attributes for this dialog.
	/// </summary>
	public virtual ITreeAttribute Attributes { get; protected set; }

	public override string ToggleKeyCombinationCode => null;

	/// <summary>
	/// Constructor for a generic Dialog.
	/// </summary>
	/// <param name="DialogTitle">The title of the dialog.</param>
	/// <param name="capi">The Client API</param>
	public GuiDialogGeneric(string DialogTitle, ICoreClientAPI capi)
		: base(capi)
	{
		this.DialogTitle = DialogTitle;
	}

	/// <summary>
	/// Recomposes the dialog with it's set of elements.
	/// </summary>
	public virtual void Recompose()
	{
		foreach (GuiComposer value in Composers.Values)
		{
			value.ReCompose();
		}
	}

	/// <summary>
	/// Unfocuses the elements in each composer.
	/// </summary>
	public virtual void UnfocusElements()
	{
		foreach (GuiComposer value in Composers.Values)
		{
			value.UnfocusOwnElements();
		}
	}

	/// <summary>
	/// Focuses a specific element in the single composer.
	/// </summary>
	/// <param name="index">Index of the element.</param>
	public virtual void FocusElement(int index)
	{
		base.SingleComposer.FocusElement(index);
	}

	/// <summary>
	/// Checks if the player is in range of the block.
	/// </summary>
	/// <param name="blockEntityPos">The block's position.</param>
	/// <returns>In range or no?</returns>
	public virtual bool IsInRangeOfBlock(BlockPos blockEntityPos)
	{
		Cuboidf[] boxes = capi.World.BlockAccessor.GetBlock(blockEntityPos).GetSelectionBoxes(capi.World.BlockAccessor, blockEntityPos);
		double dist = 99.0;
		int i = 0;
		while (boxes != null && i < boxes.Length)
		{
			Cuboidf box = boxes[i];
			Vec3d playerEye = capi.World.Player.Entity.Pos.XYZ.Add(capi.World.Player.Entity.LocalEyePos);
			dist = Math.Min(dist, box.ToDouble().Translate(blockEntityPos.X, blockEntityPos.InternalY, blockEntityPos.Z).ShortestDistanceFrom(playerEye));
			i++;
		}
		return dist <= (double)capi.World.Player.WorldData.PickingRange + 0.5;
	}
}
