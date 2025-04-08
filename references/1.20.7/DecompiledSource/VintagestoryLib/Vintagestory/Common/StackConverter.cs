using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Common;

public static class StackConverter
{
	public static ItemStack FromPacket(Packet_ItemStack fromPacket, IWorldAccessor resolver)
	{
		TreeAttribute attributes = new TreeAttribute();
		if (fromPacket.Attributes != null)
		{
			BinaryReader reader = new BinaryReader(new MemoryStream(fromPacket.Attributes));
			attributes.FromBytes(reader);
		}
		return new ItemStack(fromPacket.ItemId, (EnumItemClass)fromPacket.ItemClass, fromPacket.StackSize, attributes, resolver);
	}

	public static Packet_ItemStack ToPacket(ItemStack stack)
	{
		MemoryStream ms = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(ms);
		stack.Attributes.ToBytes(writer);
		return new Packet_ItemStack
		{
			ItemClass = (int)stack.Class,
			ItemId = stack.Id,
			StackSize = stack.StackSize,
			Attributes = ms.ToArray()
		};
	}
}
