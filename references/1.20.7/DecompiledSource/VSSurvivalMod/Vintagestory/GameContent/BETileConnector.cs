using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class BETileConnector : BlockEntity
{
	public string Constraints = "*";

	private ICoreClientAPI capi;

	public void OnInteract(IPlayer byPlayer)
	{
		if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative && Api is ICoreClientAPI api)
		{
			capi = api;
			GuiDialogTiledDungeon dlg = new GuiDialogTiledDungeon("Dungeon Tile Constraint", Constraints, capi);
			dlg.TryOpen();
			dlg.OnClosed += delegate
			{
				DidCloseDialog(dlg);
			};
		}
	}

	private void DidCloseDialog(GuiDialogTiledDungeon dialog)
	{
		ITreeAttribute attr = dialog.Attributes;
		if (attr.GetInt("save") == 0)
		{
			return;
		}
		using MemoryStream ms = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(ms);
		attr.ToBytes(writer);
		capi.Network.SendBlockEntityPacket(Pos, 0, ms.ToArray());
	}

	public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
	{
		if (packetid == 0)
		{
			TreeAttribute tree = new TreeAttribute();
			tree.FromBytes(data);
			Constraints = (tree["constraints"] as StringAttribute).value;
			MarkDirty();
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree["constraints"] = new StringAttribute(Constraints);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		Constraints = (tree["constraints"] as StringAttribute).value;
	}
}
