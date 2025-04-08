using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockTapestry : Block
{
	private ICoreClientAPI capi;

	private BlockFacing orientation;

	private bool noLoreEvent;

	private string loreCode;

	public static string[][] tapestryGroups;

	private static Dictionary<string, TVec2i[]> neighbours2x1 = new Dictionary<string, TVec2i[]>
	{
		{
			"1",
			new TVec2i[1]
			{
				new TVec2i(1, 0, "2")
			}
		},
		{
			"2",
			new TVec2i[1]
			{
				new TVec2i(-1, 0, "1")
			}
		}
	};

	private static Dictionary<string, TVec2i[]> neighbours1x2 = new Dictionary<string, TVec2i[]>
	{
		{
			"1",
			new TVec2i[1]
			{
				new TVec2i(0, -1, "2")
			}
		},
		{
			"2",
			new TVec2i[1]
			{
				new TVec2i(0, 1, "1")
			}
		}
	};

	private static Dictionary<string, TVec2i[]> neighbours1x3 = new Dictionary<string, TVec2i[]>
	{
		{
			"1",
			new TVec2i[2]
			{
				new TVec2i(0, -1, "2"),
				new TVec2i(0, -2, "3")
			}
		},
		{
			"2",
			new TVec2i[2]
			{
				new TVec2i(0, 1, "1"),
				new TVec2i(0, -1, "3")
			}
		},
		{
			"3",
			new TVec2i[2]
			{
				new TVec2i(0, 2, "1"),
				new TVec2i(0, 1, "2")
			}
		}
	};

	private static Dictionary<string, TVec2i[]> neighbours3x1 = new Dictionary<string, TVec2i[]>
	{
		{
			"1",
			new TVec2i[2]
			{
				new TVec2i(1, 0, "2"),
				new TVec2i(2, 0, "3")
			}
		},
		{
			"2",
			new TVec2i[2]
			{
				new TVec2i(-1, 0, "1"),
				new TVec2i(1, 0, "3")
			}
		},
		{
			"3",
			new TVec2i[2]
			{
				new TVec2i(-2, 0, "1"),
				new TVec2i(-1, 0, "2")
			}
		}
	};

	private static Dictionary<string, TVec2i[]> neighbours4x1 = new Dictionary<string, TVec2i[]>
	{
		{
			"1",
			new TVec2i[3]
			{
				new TVec2i(1, 0, "2"),
				new TVec2i(2, 0, "3"),
				new TVec2i(3, 0, "4")
			}
		},
		{
			"2",
			new TVec2i[3]
			{
				new TVec2i(-1, 0, "1"),
				new TVec2i(1, 0, "3"),
				new TVec2i(2, 0, "4")
			}
		},
		{
			"3",
			new TVec2i[3]
			{
				new TVec2i(-2, 0, "1"),
				new TVec2i(-1, 0, "2"),
				new TVec2i(1, 0, "4")
			}
		},
		{
			"4",
			new TVec2i[3]
			{
				new TVec2i(-3, 0, "1"),
				new TVec2i(-2, 0, "2"),
				new TVec2i(-1, 0, "3")
			}
		}
	};

	private static Dictionary<string, TVec2i[]> neighbours2x2 = new Dictionary<string, TVec2i[]>
	{
		{
			"11",
			new TVec2i[3]
			{
				new TVec2i(1, 0, "12"),
				new TVec2i(0, -1, "21"),
				new TVec2i(1, -1, "22")
			}
		},
		{
			"12",
			new TVec2i[3]
			{
				new TVec2i(-1, 0, "11"),
				new TVec2i(0, -1, "22"),
				new TVec2i(-1, -1, "21")
			}
		},
		{
			"21",
			new TVec2i[3]
			{
				new TVec2i(0, 1, "11"),
				new TVec2i(1, 0, "22"),
				new TVec2i(1, 1, "12")
			}
		},
		{
			"22",
			new TVec2i[3]
			{
				new TVec2i(0, 1, "12"),
				new TVec2i(-1, 0, "21"),
				new TVec2i(-1, 1, "11")
			}
		}
	};

	public string LoreCode => loreCode;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		capi = api as ICoreClientAPI;
		orientation = BlockFacing.FromCode(Variant["side"]);
		loreCode = Attributes["loreCode"].AsString("tapestry");
		noLoreEvent = Attributes.IsTrue("noLoreEvent");
		if (tapestryGroups == null)
		{
			tapestryGroups = Attributes["tapestryGroups"].AsObject<string[][]>();
		}
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		base.OnUnloaded(api);
		tapestryGroups = null;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
		Dictionary<string, MultiTextureMeshRef> tapestryMeshes = ObjectCacheUtil.GetOrCreate(capi, "tapestryMeshesInventory", () => new Dictionary<string, MultiTextureMeshRef>());
		renderinfo.NormalShaded = false;
		string type = itemstack.Attributes.GetString("type", "");
		if (!tapestryMeshes.TryGetValue(type, out var meshref))
		{
			MeshData mesh = genMesh(rotten: false, type, 0, inventory: true);
			meshref = (tapestryMeshes[type] = capi.Render.UploadMultiTextureMesh(mesh));
		}
		renderinfo.ModelRef = meshref;
	}

	public static string GetBaseCode(string type)
	{
		if (type.Length == 0)
		{
			return null;
		}
		int substr = 0;
		if (char.IsDigit(type[type.Length - 1]))
		{
			substr++;
		}
		if (char.IsDigit(type[type.Length - 2]))
		{
			substr++;
		}
		return type.Substring(0, type.Length - substr);
	}

	public override void OnBeingLookedAt(IPlayer byPlayer, BlockSelection blockSel, bool firstTick)
	{
		if (noLoreEvent || !firstTick || api.Side != EnumAppSide.Server)
		{
			return;
		}
		BlockEntityTapestry beTas = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityTapestry;
		if (beTas.Rotten || beTas.Type == null)
		{
			return;
		}
		string baseCode = GetBaseCode(beTas.Type);
		if (baseCode == null)
		{
			return;
		}
		int id = GetLoreChapterId(baseCode);
		if (id >= 0)
		{
			string size = Attributes["sizes"][baseCode].AsString();
			Dictionary<string, TVec2i[]> neighbours = size switch
			{
				"2x1" => neighbours2x1, 
				"1x2" => neighbours1x2, 
				"1x3" => neighbours1x3, 
				"3x1" => neighbours3x1, 
				"4x1" => neighbours4x1, 
				"2x2" => neighbours2x2, 
				_ => throw new Exception("invalid tapestry json config - missing size attribute for size '" + size + "'"), 
			};
			string intComp = beTas.Type.Substring(baseCode.Length);
			TVec2i[] vecs = neighbours[intComp];
			if (isComplete(blockSel.Position, baseCode, vecs))
			{
				ModJournal modSystem = api.ModLoader.GetModSystem<ModJournal>();
				LoreDiscovery discovery = new LoreDiscovery
				{
					Code = LoreCode,
					ChapterIds = new List<int> { id }
				};
				modSystem.TryDiscoverLore(discovery, byPlayer as IServerPlayer);
			}
		}
	}

	public int GetLoreChapterId(string baseCode)
	{
		if (!Attributes["loreChapterIds"][baseCode].Exists)
		{
			throw new Exception("incomplete tapestry json configuration - missing lore piece id");
		}
		return Attributes["loreChapterIds"][baseCode].AsInt();
	}

	private bool isComplete(BlockPos position, string baseCode, TVec2i[] vecs)
	{
		foreach (TVec2i vec in vecs)
		{
			Vec3i offs;
			switch (orientation.Index)
			{
			case 0:
				offs = new Vec3i(vec.X, vec.Y, 0);
				break;
			case 1:
				offs = new Vec3i(0, vec.Y, vec.X);
				break;
			case 2:
				offs = new Vec3i(-vec.X, vec.Y, 0);
				break;
			case 3:
				offs = new Vec3i(0, vec.Y, -vec.X);
				break;
			default:
				return false;
			}
			if (!(api.World.BlockAccessor.GetBlockEntity(position.AddCopy(offs.X, offs.Y, offs.Z)) is BlockEntityTapestry bet))
			{
				return false;
			}
			string nbaseCode = GetBaseCode(bet.Type);
			if (nbaseCode != baseCode)
			{
				return false;
			}
			if (bet.Rotten)
			{
				return false;
			}
			if (bet.Type.Substring(nbaseCode.Length) != vec.IntComp)
			{
				return false;
			}
		}
		return true;
	}

	public MeshData genMesh(bool rotten, string type, int rotVariant, bool inventory = false)
	{
		TapestryTextureSource txs = new TapestryTextureSource(capi, rotten, type, rotVariant);
		Shape shape = capi.TesselatorManager.GetCachedShape(inventory ? ShapeInventory.Base : Shape.Base);
		capi.Tesselator.TesselateShape("tapestryblock", shape, out var mesh, txs, null, 0, 0, 0);
		return mesh;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		ItemStack[] stacks = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
		BlockEntityTapestry bet = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityTapestry;
		if (bet.Rotten)
		{
			return new ItemStack[0];
		}
		stacks[0].Attributes.SetString("type", bet?.Type);
		return stacks;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		BlockEntityTapestry bet = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityTapestry;
		ItemStack itemStack = new ItemStack(this);
		itemStack.Attributes.SetString("type", bet?.Type);
		itemStack.Attributes.SetBool("rotten", bet?.Rotten ?? false);
		return itemStack;
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		string type = itemStack.Attributes.GetString("type", "");
		return Lang.Get("tapestry-name", Lang.GetMatching("tapestry-" + type));
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		BlockEntityTapestry bet = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityTapestry;
		if (bet != null && bet.Rotten)
		{
			return Lang.Get("Rotten Tapestry");
		}
		string type = bet?.Type;
		return Lang.Get("tapestry-name", Lang.GetMatching("tapestry-" + type));
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		dsc.AppendLine(GetWordedSection(inSlot, world));
		if (withDebugInfo)
		{
			string type = inSlot.Itemstack.Attributes.GetString("type", "");
			dsc.AppendLine(type);
		}
	}

	public string GetWordedSection(ItemSlot slot, IWorldAccessor world)
	{
		string type = slot.Itemstack.Attributes.GetString("type", "");
		string baseCode = GetBaseCode(type);
		if (baseCode == null)
		{
			return "unknown";
		}
		string size = Attributes["sizes"][baseCode].AsString();
		string intComp = type.Substring(baseCode.Length);
		switch (size)
		{
		case "1x1":
			return "";
		case "2x1":
			if (!(intComp == "1"))
			{
				if (intComp == "2")
				{
					return Lang.Get("Section: Right Half");
				}
				return "unknown";
			}
			return Lang.Get("Section: Left Half");
		case "1x2":
			if (!(intComp == "1"))
			{
				if (intComp == "2")
				{
					return Lang.Get("Section: Bottom Half");
				}
				return "unknown";
			}
			return Lang.Get("Section: Top Half");
		case "3x1":
			return intComp switch
			{
				"1" => Lang.Get("Section: Left third"), 
				"2" => Lang.Get("Section: Center third"), 
				"3" => Lang.Get("Section: Right third"), 
				_ => "unknown", 
			};
		case "1x3":
			return intComp switch
			{
				"1" => Lang.Get("Section: Top third"), 
				"2" => Lang.Get("Section: Middle third"), 
				"3" => Lang.Get("Section: Bottom third"), 
				_ => "unknown", 
			};
		case "4x1":
			return intComp switch
			{
				"1" => Lang.Get("Section: Top quarter"), 
				"2" => Lang.Get("Section: Top middle quarter"), 
				"3" => Lang.Get("Section: Bottom middle quarter"), 
				"4" => Lang.Get("Section: Bottom quarter"), 
				_ => "unknown", 
			};
		case "2x2":
			return intComp switch
			{
				"11" => Lang.Get("Section: Top Left Quarter"), 
				"21" => Lang.Get("Section: Bottom Left Quarter"), 
				"12" => Lang.Get("Section: Top Right Quarter"), 
				"22" => Lang.Get("Section: Bottom Right Quarter"), 
				_ => "unknown", 
			};
		default:
			throw new Exception("invalid tapestry json config - missing size attribute for size '" + size + "'");
		}
	}
}
