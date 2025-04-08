using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockSpawner : Block
{
	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntitySpawner be && byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			be.OnInteract(byPlayer);
			return true;
		}
		return false;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack stack = base.OnPickBlock(world, pos);
		BESpawnerData spawnerdata = (world.BlockAccessor.GetBlockEntity(pos) as BlockEntitySpawner)?.Data;
		if (spawnerdata != null)
		{
			stack.Attributes.SetBytes("spawnerData", SerializerUtil.Serialize(spawnerdata));
		}
		return stack;
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		byte[] data = inSlot.Itemstack.Attributes.GetBytes("spawnerData");
		if (data != null)
		{
			try
			{
				BESpawnerData spawnderdata = SerializerUtil.Deserialize<BESpawnerData>(data);
				if (spawnderdata.EntityCodes == null)
				{
					dsc.AppendLine("Spawns: Nothing");
				}
				else
				{
					string names = "";
					string[] entityCodes = spawnderdata.EntityCodes;
					foreach (string val in entityCodes)
					{
						if (names.Length > 0)
						{
							names += ", ";
						}
						names += Lang.Get("item-creature-" + val);
					}
					dsc.AppendLine("Spawns: " + names);
				}
				dsc.AppendLine("Area: " + spawnderdata.SpawnArea);
				dsc.AppendLine("Interval: " + spawnderdata.InGameHourInterval);
				dsc.AppendLine("Max count: " + spawnderdata.MaxCount);
			}
			catch
			{
			}
		}
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
	}
}
