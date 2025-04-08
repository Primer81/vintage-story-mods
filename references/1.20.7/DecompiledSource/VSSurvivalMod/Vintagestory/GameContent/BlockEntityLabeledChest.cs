using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityLabeledChest : BlockEntityGenericTypedContainer
{
	private string text = "";

	private ChestLabelRenderer labelrenderer;

	private int color;

	private int tempColor;

	private ItemStack tempStack;

	private float fontSize = 20f;

	private GuiDialogBlockEntityTextInput editDialog;

	public override float MeshAngle
	{
		get
		{
			return base.MeshAngle;
		}
		set
		{
			labelrenderer?.SetRotation(value);
			base.MeshAngle = value;
		}
	}

	public override string DialogTitle
	{
		get
		{
			if (text == null || text.Length == 0)
			{
				return Lang.Get("Chest Contents");
			}
			return text.Replace("\r", "").Replace("\n", " ").Substring(0, Math.Min(text.Length, 15));
		}
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (api is ICoreClientAPI)
		{
			labelrenderer = new ChestLabelRenderer(Pos, api as ICoreClientAPI);
			labelrenderer.SetRotation(MeshAngle);
			labelrenderer.SetNewText(text, color);
		}
	}

	public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
	{
		if (byPlayer != null && (byPlayer.Entity?.Controls?.ShiftKey).GetValueOrDefault())
		{
			ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
			if (hotbarSlot != null && (hotbarSlot.Itemstack?.ItemAttributes?["pigment"]?["color"].Exists).GetValueOrDefault())
			{
				JsonObject jsonObject = hotbarSlot.Itemstack.ItemAttributes["pigment"]["color"];
				int r = jsonObject["red"].AsInt();
				int g = jsonObject["green"].AsInt();
				int b = jsonObject["blue"].AsInt();
				tempColor = ColorUtil.ToRgba(255, r, g, b);
				tempStack = hotbarSlot.TakeOut(1);
				hotbarSlot.MarkDirty();
				if (Api is ICoreServerAPI sapi)
				{
					sapi.Network.SendBlockEntityPacket((IServerPlayer)byPlayer, Pos, 1001);
				}
				return true;
			}
		}
		return base.OnPlayerRightClick(byPlayer, blockSel);
	}

	public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
	{
		if (packetid == 1002)
		{
			EditSignPacket packet = SerializerUtil.Deserialize<EditSignPacket>(data);
			text = packet.Text;
			fontSize = packet.FontSize;
			color = tempColor;
			MarkDirty(redrawOnClient: true);
			Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
			if (Api.World.Rand.NextDouble() < 0.85)
			{
				player.InventoryManager.TryGiveItemstack(tempStack);
			}
		}
		if (packetid == 1003 && tempStack != null)
		{
			player.InventoryManager.TryGiveItemstack(tempStack);
			tempStack = null;
		}
		base.OnReceivedClientPacket(player, packetid, data);
	}

	public override void OnReceivedServerPacket(int packetid, byte[] data)
	{
		if (packetid == 1001)
		{
			if (editDialog != null && editDialog.IsOpened())
			{
				return;
			}
			editDialog = new GuiDialogBlockEntityTextInput(Lang.Get("Edit Label text"), Pos, text, Api as ICoreClientAPI, new TextAreaConfig
			{
				MaxWidth = 130,
				MaxHeight = 160
			}.CopyWithFontSize(fontSize));
			editDialog.OnTextChanged = DidChangeTextClientSide;
			editDialog.OnCloseCancel = delegate
			{
				labelrenderer?.SetNewText(text, color);
				(Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos, 1003);
			};
			editDialog.TryOpen();
		}
		if (packetid == 1000)
		{
			EditSignPacket packet = SerializerUtil.Deserialize<EditSignPacket>(data);
			if (labelrenderer != null)
			{
				labelrenderer.fontSize = packet.FontSize;
				labelrenderer.SetNewText(packet.Text, color);
			}
		}
		base.OnReceivedServerPacket(packetid, data);
	}

	private void DidChangeTextClientSide(string text)
	{
		if (editDialog != null)
		{
			fontSize = editDialog.FontSize;
			labelrenderer.fontSize = fontSize;
			labelrenderer?.SetNewText(text, tempColor);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		color = tree.GetInt("color");
		text = tree.GetString("text");
		fontSize = tree.GetFloat("fontSize", 20f);
		labelrenderer?.SetNewText(text, color);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("color", color);
		tree.SetString("text", text);
		tree.SetFloat("fontSize", fontSize);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (labelrenderer != null)
		{
			labelrenderer.Dispose();
			labelrenderer = null;
		}
		editDialog?.TryClose();
		editDialog?.Dispose();
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		base.OnBlockBroken(byPlayer);
		labelrenderer?.Dispose();
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		labelrenderer?.Dispose();
		editDialog?.TryClose();
		editDialog?.Dispose();
	}
}
