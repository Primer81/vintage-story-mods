using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class BlockEntityContainerOpen
{
	public string BlockEntity;

	public string DialogTitle;

	public byte Columns;

	public TreeAttribute Tree;

	public static byte[] ToBytes(string entityName, string dialogTitle, byte columns, InventoryBase inventory)
	{
		using MemoryStream ms = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(ms);
		writer.Write(entityName);
		writer.Write(dialogTitle);
		writer.Write(columns);
		TreeAttribute tree = new TreeAttribute();
		inventory.ToTreeAttributes(tree);
		tree.ToBytes(writer);
		return ms.ToArray();
	}

	public static BlockEntityContainerOpen FromBytes(byte[] data)
	{
		BlockEntityContainerOpen inst = new BlockEntityContainerOpen();
		using MemoryStream ms = new MemoryStream(data);
		BinaryReader reader = new BinaryReader(ms);
		inst.BlockEntity = reader.ReadString();
		inst.DialogTitle = reader.ReadString();
		inst.Columns = reader.ReadByte();
		inst.Tree = new TreeAttribute();
		inst.Tree.FromBytes(reader);
		return inst;
	}
}
