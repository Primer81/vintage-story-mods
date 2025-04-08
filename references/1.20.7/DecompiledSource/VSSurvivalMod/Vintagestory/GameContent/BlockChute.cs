using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockChute : Block, IBlockItemFlow
{
	public string Type { get; set; }

	public string Side { get; set; }

	public string Vertical { get; set; }

	public string[] PullFaces => Attributes["pullFaces"].AsArray(new string[0]);

	public string[] PushFaces => Attributes["pushFaces"].AsArray(new string[0]);

	public string[] AcceptFaces => Attributes["acceptFromFaces"].AsArray(new string[0]);

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		string t = Variant["type"];
		Type = ((t != null) ? string.Intern(t) : null);
		string s = Variant["side"];
		Side = ((s != null) ? string.Intern(s) : null);
		string v = Variant["vertical"];
		Vertical = ((v != null) ? string.Intern(v) : null);
	}

	public bool HasItemFlowConnectorAt(BlockFacing facing)
	{
		if (!PullFaces.Contains(facing.Code) && !PushFaces.Contains(facing.Code))
		{
			return AcceptFaces.Contains(facing.Code);
		}
		return true;
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		BlockChute blockToPlace = null;
		BlockFacing[] facings = OrientForPlacement(world.BlockAccessor, byPlayer, blockSel);
		if (Type == "elbow" || Type == "3way")
		{
			string vertical = ((facings[1] == BlockFacing.UP) ? "down" : "up");
			BlockFacing horizontal = facings[0];
			if (vertical == "up" && (Type == "3way" || horizontal == BlockFacing.NORTH || horizontal == BlockFacing.SOUTH))
			{
				horizontal = horizontal.Opposite;
			}
			AssetLocation code = CodeWithVariants(new string[2] { "vertical", "side" }, new string[2] { vertical, horizontal.Code });
			blockToPlace = api.World.GetBlock(code) as BlockChute;
			int i = 0;
			while (blockToPlace != null && !blockToPlace.CanStay(world, blockSel.Position))
			{
				if (i >= BlockFacing.HORIZONTALS.Length)
				{
					blockToPlace = null;
					break;
				}
				blockToPlace = api.World.GetBlock(CodeWithVariants(new string[2] { "vertical", "side" }, new string[2]
				{
					vertical,
					BlockFacing.HORIZONTALS[i++].Code
				})) as BlockChute;
			}
		}
		else if (Type == "t")
		{
			string variant = ((facings[0].Axis == EnumAxis.X) ? "we" : "ns");
			if (blockSel.Face.IsVertical)
			{
				ReadOnlySpan<char> readOnlySpan = "ud-";
				char reference = facings[0].Opposite.Code[0];
				variant = string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference));
			}
			blockToPlace = api.World.GetBlock(CodeWithVariant("side", variant)) as BlockChute;
			if (!blockToPlace.CanStay(world, blockSel.Position))
			{
				blockToPlace = api.World.GetBlock(CodeWithVariant("side", (facings[0].Axis == EnumAxis.X) ? "we" : "ns")) as BlockChute;
			}
		}
		else if (Type == "straight")
		{
			string variant2 = ((facings[0].Axis == EnumAxis.X) ? "we" : "ns");
			if (blockSel.Face.IsVertical)
			{
				variant2 = "ud";
			}
			blockToPlace = api.World.GetBlock(CodeWithVariant("side", variant2)) as BlockChute;
		}
		else if (Type == "cross")
		{
			string variant3 = ((facings[0].Axis != 0) ? "ns" : "we");
			if (blockSel.Face.IsVertical)
			{
				variant3 = "ground";
			}
			blockToPlace = api.World.GetBlock(CodeWithVariant("side", variant3)) as BlockChute;
		}
		if (blockToPlace != null && blockToPlace.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode) && blockToPlace.CanStay(world, blockSel.Position))
		{
			world.BlockAccessor.SetBlock(blockToPlace.BlockId, blockSel.Position);
			world.Logger.Audit("{0} placed a chute at {1}", byPlayer.PlayerName, blockSel.Position);
			return true;
		}
		if (Type == "cross")
		{
			blockToPlace = api.World.GetBlock(CodeWithVariant("side", "ground")) as BlockChute;
		}
		if (blockToPlace != null && blockToPlace.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode) && blockToPlace.CanStay(world, blockSel.Position))
		{
			world.BlockAccessor.SetBlock(blockToPlace.BlockId, blockSel.Position);
			world.Logger.Audit("{0} placed a chute at {1}", byPlayer.PlayerName, blockSel.Position);
			return true;
		}
		return false;
	}

	protected virtual BlockFacing[] OrientForPlacement(IBlockAccessor worldmap, IPlayer player, BlockSelection bs)
	{
		BlockFacing[] facings = Block.SuggestedHVOrientation(player, bs);
		BlockPos pos = bs.Position;
		BlockFacing horizontal = null;
		BlockFacing face = bs.Face.Opposite;
		BlockFacing vert = null;
		if (face.IsHorizontal)
		{
			if (HasConnector(worldmap, pos.AddCopy(face), bs.Face, out vert))
			{
				horizontal = face;
			}
			else
			{
				face = face.GetCW();
				if (HasConnector(worldmap, pos.AddCopy(face), face.Opposite, out vert))
				{
					horizontal = face;
				}
				else if (HasConnector(worldmap, pos.AddCopy(face.Opposite), face, out vert))
				{
					horizontal = face.Opposite;
				}
				else if (HasConnector(worldmap, pos.AddCopy(bs.Face), bs.Face.Opposite, out vert))
				{
					horizontal = bs.Face;
				}
			}
			if (Type == "3way" && horizontal != null)
			{
				face = horizontal.GetCW();
				BlockFacing unused2 = null;
				if (HasConnector(worldmap, pos.AddCopy(face), face.Opposite, out unused2) && !HasConnector(worldmap, pos.AddCopy(face.Opposite), face, out unused2))
				{
					horizontal = face;
				}
			}
		}
		else
		{
			vert = face;
			bool moreThanOne = false;
			horizontal = (HasConnector(worldmap, pos.EastCopy(), BlockFacing.WEST, out vert) ? BlockFacing.EAST : null);
			if (HasConnector(worldmap, pos.WestCopy(), BlockFacing.EAST, out vert))
			{
				moreThanOne = horizontal != null;
				horizontal = BlockFacing.WEST;
			}
			if (HasConnector(worldmap, pos.NorthCopy(), BlockFacing.SOUTH, out vert))
			{
				moreThanOne = horizontal != null;
				horizontal = BlockFacing.NORTH;
			}
			if (HasConnector(worldmap, pos.SouthCopy(), BlockFacing.NORTH, out vert))
			{
				moreThanOne = horizontal != null;
				horizontal = BlockFacing.SOUTH;
			}
			if (moreThanOne)
			{
				horizontal = null;
			}
		}
		if (vert == null)
		{
			BlockFacing unused = null;
			bool up = HasConnector(worldmap, pos.UpCopy(), BlockFacing.DOWN, out unused);
			bool down = HasConnector(worldmap, pos.DownCopy(), BlockFacing.UP, out unused);
			if (up && !down)
			{
				vert = BlockFacing.UP;
			}
			else if (down && !up)
			{
				vert = BlockFacing.DOWN;
			}
		}
		if (vert != null)
		{
			facings[1] = vert;
		}
		facings[0] = horizontal ?? facings[0].Opposite;
		return facings;
	}

	protected virtual bool HasConnector(IBlockAccessor ba, BlockPos pos, BlockFacing face, out BlockFacing vert)
	{
		if (ba.GetBlock(pos) is BlockChute chute)
		{
			if (chute.HasItemFlowConnectorAt(BlockFacing.UP) && !chute.HasItemFlowConnectorAt(BlockFacing.DOWN))
			{
				vert = BlockFacing.DOWN;
			}
			else if (chute.HasItemFlowConnectorAt(BlockFacing.DOWN) && !chute.HasItemFlowConnectorAt(BlockFacing.UP))
			{
				vert = BlockFacing.UP;
			}
			else
			{
				vert = null;
			}
			return chute.HasItemFlowConnectorAt(face);
		}
		vert = null;
		return ba.GetBlock(pos).GetBlockEntity<BlockEntityContainer>(pos) != null;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		if (!CanStay(world, pos))
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
	}

	private bool CanStay(IWorldAccessor world, BlockPos pos)
	{
		BlockPos npos = new BlockPos();
		IBlockAccessor ba = world.BlockAccessor;
		if (PullFaces != null)
		{
			string[] pullFaces = PullFaces;
			int num = 0;
			while (num < pullFaces.Length)
			{
				BlockFacing face2 = BlockFacing.FromCode(pullFaces[num]);
				Block block2 = world.BlockAccessor.GetBlock(npos.Set(pos).Add(face2));
				if (!block2.CanAttachBlockAt(world.BlockAccessor, this, pos, face2))
				{
					IBlockItemFlow obj = block2 as IBlockItemFlow;
					if ((obj == null || !obj.HasItemFlowConnectorAt(face2.Opposite)) && ba.GetBlock(pos).GetBlockEntity<BlockEntityContainer>(npos) == null)
					{
						num++;
						continue;
					}
				}
				return true;
			}
		}
		if (PushFaces != null)
		{
			string[] pullFaces = PushFaces;
			int num = 0;
			while (num < pullFaces.Length)
			{
				BlockFacing face = BlockFacing.FromCode(pullFaces[num]);
				Block block = world.BlockAccessor.GetBlock(npos.Set(pos).Add(face));
				if (!block.CanAttachBlockAt(world.BlockAccessor, this, pos, face))
				{
					IBlockItemFlow obj2 = block as IBlockItemFlow;
					if ((obj2 == null || !obj2.HasItemFlowConnectorAt(face.Opposite)) && ba.GetBlock(pos).GetBlockEntity<BlockEntityContainer>(npos) == null)
					{
						num++;
						continue;
					}
				}
				return true;
			}
		}
		return false;
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return new BlockDropItemStack[1]
		{
			new BlockDropItemStack(handbookStack)
		};
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		Block block = null;
		if (Type == "elbow" || Type == "3way")
		{
			block = api.World.GetBlock(CodeWithVariants(new string[2] { "vertical", "side" }, new string[2] { "down", "east" }));
		}
		if (Type == "t" || Type == "straight")
		{
			block = api.World.GetBlock(CodeWithVariant("side", "ns"));
		}
		if (Type == "cross")
		{
			block = api.World.GetBlock(CodeWithVariant("side", "ground"));
		}
		return new ItemStack[1]
		{
			new ItemStack(block)
		};
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return GetDrops(world, pos, null)[0];
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		int dir = GameMath.Mod(angle / 90, 4);
		switch (Type)
		{
		case "elbow":
		{
			BlockFacing facing3 = BlockFacing.FromCode(Side);
			return CodeWithVariant("side", BlockFacing.HORIZONTALS[GameMath.Mod(facing3.Index + dir + 2, 4)].Code.ToLower());
		}
		case "3way":
		{
			BlockFacing facing2 = BlockFacing.FromCode(Side);
			return CodeWithVariant("side", BlockFacing.HORIZONTALS[GameMath.Mod(facing2.Index + dir, 4)].Code.ToLower());
		}
		case "t":
		{
			if ((Side.Equals("ns") || Side.Equals("we")) && (dir == 1 || dir == 3))
			{
				return CodeWithVariant("side", Side.Equals("ns") ? "we" : "ns");
			}
			BlockFacing facing = Side switch
			{
				"ud-n" => BlockFacing.NORTH, 
				"ud-e" => BlockFacing.EAST, 
				"ud-s" => BlockFacing.SOUTH, 
				"ud-w" => BlockFacing.WEST, 
				_ => BlockFacing.NORTH, 
			};
			ReadOnlySpan<char> readOnlySpan = "ud-";
			char reference = BlockFacing.HORIZONTALS[GameMath.Mod(facing.Index + dir, 4)].Code.ToLower()[0];
			return CodeWithVariant("side", string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference)));
		}
		case "straight":
			if (Side.Equals("ud") || dir == 0 || dir == 2)
			{
				return Code;
			}
			return CodeWithVariant("side", Side.Equals("ns") ? "we" : "ns");
		case "cross":
			if ((Side.Equals("ns") || Side.Equals("we")) && (dir == 1 || dir == 3))
			{
				return CodeWithVariant("side", Side.Equals("ns") ? "we" : "ns");
			}
			return Code;
		default:
			return Code;
		}
	}

	public override AssetLocation GetVerticallyFlippedBlockCode()
	{
		return base.GetVerticallyFlippedBlockCode();
	}
}
