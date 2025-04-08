using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityFruitTreeBranch : BlockEntityFruitTreePart
{
	public int SideGrowth;

	public Vec3i ParentOff;

	public int GrowTries;

	public double lastGrowthAttemptTotalDays;

	private MeshData branchMesh;

	private Cuboidf[] colSelBoxes;

	public float? FastForwardGrowth;

	private bool initialized;

	private static Dictionary<string, int[]> facingRemapByShape = new Dictionary<string, int[]>
	{
		{
			"stem",
			new int[6] { 0, 1, 2, 3, 4, 5 }
		},
		{
			"branch-ud",
			new int[6] { 0, 1, 2, 3, 4, 5 }
		},
		{
			"branch-n",
			new int[6] { 4, 3, 5, 1, 0, 2 }
		},
		{
			"branch-s",
			new int[6] { 4, 3, 5, 1, 0, 2 }
		},
		{
			"branch-w",
			new int[6] { 0, 5, 2, 4, 3, 1 }
		},
		{
			"branch-e",
			new int[6] { 0, 5, 2, 4, 3, 1 }
		},
		{
			"branch-ud-end",
			new int[6] { 0, 1, 2, 3, 4, 5 }
		},
		{
			"branch-n-end",
			new int[6] { 4, 3, 5, 1, 0, 2 }
		},
		{
			"branch-s-end",
			new int[6] { 4, 3, 5, 1, 0, 2 }
		},
		{
			"branch-w-end",
			new int[6] { 0, 5, 2, 4, 3, 1 }
		},
		{
			"branch-e-end",
			new int[6] { 0, 5, 2, 4, 3, 1 }
		}
	};

	public override void Initialize(ICoreAPI api)
	{
		Api = api;
		Block block = base.Block;
		if (block != null && (block.Attributes?["foliageBlock"].Exists).GetValueOrDefault())
		{
			blockFoliage = api.World.GetBlock(AssetLocation.Create(base.Block.Attributes["foliageBlock"].AsString(), base.Block.Code.Domain)) as BlockFruitTreeFoliage;
			blockBranch = base.Block as BlockFruitTreeBranch;
			initCustomBehaviors(null, callInitialize: false);
			base.Initialize(api);
			if (FastForwardGrowth.HasValue && Api.Side == EnumAppSide.Server)
			{
				lastGrowthAttemptTotalDays = Api.World.Calendar.TotalDays - 20.0 - (double)(FastForwardGrowth.Value * 600f);
				InitTreeRoot(TreeType, callInitialize: true);
				FastForwardGrowth = null;
			}
			updateProperties();
		}
	}

	public Cuboidf[] GetColSelBox()
	{
		if (GrowthDir.Axis == EnumAxis.Y)
		{
			return base.Block.CollisionBoxes;
		}
		return colSelBoxes;
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		InitTreeRoot(byItemStack?.Attributes?.GetString("type"), byItemStack != null, byItemStack);
	}

	internal void InteractDebug()
	{
		if (RootOff.Y != 0)
		{
			return;
		}
		FruitTreeRootBH rootBe = (Api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(RootOff)) as BlockEntityFruitTreeBranch)?.GetBehavior<FruitTreeRootBH>();
		if (rootBe != null)
		{
			foreach (KeyValuePair<string, FruitTreeProperties> val in rootBe.propsByType)
			{
				val.Value.State = (EnumFruitTreeState)((int)(val.Value.State + 1) % 8);
			}
		}
		MarkDirty(redrawOnClient: true);
	}

	public void InitTreeRoot(string treeType, bool callInitialize, ItemStack parentPlantStack = null)
	{
		if (initialized)
		{
			return;
		}
		initialized = true;
		GrowthDir = BlockFacing.UP;
		PartType = ((!(parentPlantStack?.Collectible.Variant["type"] == "cutting")) ? EnumTreePartType.Branch : EnumTreePartType.Cutting);
		RootOff = new Vec3i();
		if (TreeType == null)
		{
			TreeType = treeType;
		}
		if (PartType == EnumTreePartType.Cutting && parentPlantStack != null)
		{
			BlockEntityFruitTreeBranch belowBe = Api.World.BlockAccessor.GetBlockEntity(Pos.DownCopy()) as BlockEntityFruitTreeBranch;
			if (Api.World.BlockAccessor.GetBlock(Pos.DownCopy()).Fertility <= 0 && belowBe == null)
			{
				BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
				foreach (BlockFacing facing in hORIZONTALS)
				{
					if (Api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(facing)) is BlockEntityFruitTreeBranch nbe)
					{
						GrowthDir = facing.Opposite;
						RootOff = nbe.RootOff.AddCopy(facing);
						FruitTreeRootBH behavior = Api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(RootOff)).GetBehavior<FruitTreeRootBH>();
						behavior.RegisterTreeType(treeType);
						behavior.propsByType[TreeType].OnFruitingStateChange += base.RootBh_OnFruitingStateChange;
						GenMesh();
					}
				}
			}
		}
		updateProperties();
		initCustomBehaviors(parentPlantStack, callInitialize);
		FruitTreeGrowingBranchBH bh = GetBehavior<FruitTreeGrowingBranchBH>();
		if (bh == null)
		{
			return;
		}
		bh.VDrive = 3f + (float)Api.World.Rand.NextDouble();
		bh.HDrive = 1f;
		if (treeType != null)
		{
			Vec3i rootOff = RootOff;
			if ((object)rootOff != null && rootOff.IsZero)
			{
				FruitTreeProperties props = GetBehavior<FruitTreeRootBH>().propsByType[TreeType];
				bh.HDrive *= props.RootSizeMul;
				bh.VDrive *= props.RootSizeMul;
			}
		}
	}

	public override void OnBlockBroken(IPlayer byPlayer)
	{
		base.OnBlockBroken(byPlayer);
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing facing in aLLFACES)
		{
			BlockPos npos = Pos.AddCopy(facing);
			Block nblock = Api.World.BlockAccessor.GetBlock(npos);
			if (nblock == blockFoliage)
			{
				bool isSupported = false;
				BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
				foreach (BlockFacing nfacing in hORIZONTALS)
				{
					BlockPos nnpos = npos.AddCopy(nfacing);
					if (!(nnpos == Pos) && Api.World.BlockAccessor.GetBlock(nnpos) == blockBranch)
					{
						isSupported = true;
						break;
					}
				}
				if (!isSupported)
				{
					Api.World.BlockAccessor.BreakBlock(npos, byPlayer);
				}
			}
			if (!(nblock is BlockFruitTreeBranch) || !facing.IsHorizontal)
			{
				continue;
			}
			BlockEntity be = Api.World.BlockAccessor.GetBlockEntity(npos);
			if (be != null)
			{
				FruitTreeGrowingBranchBH bh = be.GetBehavior<FruitTreeGrowingBranchBH>();
				if (bh == null)
				{
					bh = new FruitTreeGrowingBranchBH(this);
					bh.Initialize(Api, null);
					be.Behaviors.Add(bh);
				}
				bh.OnNeighbourBranchRemoved(facing.Opposite);
			}
		}
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		FruitTreeRootBH rootBe = (Api?.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(RootOff)) as BlockEntityFruitTreeBranch)?.GetBehavior<FruitTreeRootBH>();
		if (rootBe != null)
		{
			rootBe.BlocksRemoved++;
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
	}

	public void updateProperties()
	{
		if (GrowthDir.Axis != EnumAxis.Y)
		{
			float rotX = ((GrowthDir.Axis == EnumAxis.Z) ? 90 : 0);
			float rotZ = ((GrowthDir.Axis == EnumAxis.X) ? 90 : 0);
			if (base.Block == null || base.Block.CollisionBoxes == null)
			{
				Api?.World.Logger.Warning("BEFruitTreeBranch:updatedProperties() Block {0} or its collision box is null? Block might have incorrect hitboxes now.", base.Block?.Code);
			}
			else
			{
				colSelBoxes = new Cuboidf[1] { base.Block.CollisionBoxes[0].Clone().RotatedCopy(rotX, 0f, rotZ, new Vec3d(0.5, 0.5, 0.5)) };
			}
		}
		GenMesh();
	}

	public override void GenMesh()
	{
		branchMesh = GenMeshes();
	}

	public MeshData GenMeshes()
	{
		if (capi == null)
		{
			return null;
		}
		if (Api.Side != EnumAppSide.Client || TreeType == null || TreeType == "")
		{
			return null;
		}
		string cacheKey = "fruitTreeMeshes" + base.Block.Code.ToShortString();
		Dictionary<int, MeshData> meshes = ObjectCacheUtil.GetOrCreate(Api, cacheKey, () => new Dictionary<int, MeshData>());
		leavesMesh = null;
		if (PartType == EnumTreePartType.Branch && Height > 0)
		{
			GenFoliageMesh(withSticks: false, out leavesMesh, out var _);
		}
		string shapekey = "stem";
		switch (PartType)
		{
		case EnumTreePartType.Cutting:
			if (GrowthDir.Axis == EnumAxis.Y)
			{
				shapekey = "cutting-ud";
			}
			if (GrowthDir.Axis == EnumAxis.X)
			{
				shapekey = "cutting-we";
			}
			if (GrowthDir.Axis == EnumAxis.Z)
			{
				shapekey = "cutting-ns";
			}
			break;
		case EnumTreePartType.Branch:
			if (GrowthDir.Axis == EnumAxis.Y)
			{
				shapekey = "branch-ud";
			}
			else
			{
				ReadOnlySpan<char> readOnlySpan = "branch-";
				char reference = GrowthDir.Code[0];
				shapekey = string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference));
			}
			if (!(Api.World.BlockAccessor.GetBlock(Pos.AddCopy(GrowthDir)) is BlockFruitTreeBranch))
			{
				shapekey += "-end";
			}
			break;
		case EnumTreePartType.Leaves:
			shapekey = "leaves";
			break;
		}
		int meshkey = getHashCode(shapekey);
		if (meshes.TryGetValue(meshkey, out var mesh))
		{
			return mesh;
		}
		CompositeShape cshape = base.Block?.Attributes?["shapes"][shapekey].AsObject<CompositeShape>(null, base.Block.Code.Domain);
		if (cshape == null)
		{
			return null;
		}
		nowTesselatingShape = Shape.TryGet(Api, cshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/"));
		if (nowTesselatingShape == null)
		{
			return null;
		}
		List<string> selectiveElements = null;
		if (PartType != EnumTreePartType.Cutting)
		{
			selectiveElements = new List<string>(new string[2] { "stem", "root/branch" });
			if (PartType != EnumTreePartType.Leaves)
			{
				int[] remap = facingRemapByShape[shapekey];
				for (int i = 0; i < 8; i++)
				{
					if ((SideGrowth & (1 << i)) > 0)
					{
						char f = BlockFacing.ALLFACES[remap[i]].Code[0];
						List<string> list = selectiveElements;
						ReadOnlySpan<char> readOnlySpan2 = "branch-";
						char reference = f;
						list.Add(string.Concat(readOnlySpan2, new ReadOnlySpan<char>(in reference)));
						List<string> list2 = selectiveElements;
						ReadOnlySpan<char> readOnlySpan3 = "root/branch-";
						reference = f;
						list2.Add(string.Concat(readOnlySpan3, new ReadOnlySpan<char>(in reference)));
					}
				}
			}
		}
		capi.Tesselator.TesselateShape("fruittreebranch", nowTesselatingShape, out mesh, this, new Vec3f(cshape.rotateX, cshape.rotateY, cshape.rotateZ), 0, 0, 0, null, selectiveElements?.ToArray());
		mesh.ClimateColorMapIds.Fill((byte)0);
		mesh.SeasonColorMapIds.Fill((byte)0);
		return meshes[meshkey] = mesh;
	}

	private int getHashCode(string shapekey)
	{
		return (SideGrowth + "-" + PartType.ToString() + "-" + FoliageState.ToString() + "-" + GrowthDir.Index + "-" + shapekey + "-" + TreeType).GetHashCode();
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		if (!Api.World.EntityDebugMode)
		{
			return;
		}
		dsc.AppendLine("LeavesState: " + FoliageState);
		dsc.AppendLine("TreeType: " + TreeType);
		dsc.Append("SideGrowth: ");
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing facing in aLLFACES)
		{
			if ((SideGrowth & (1 << facing.Index)) > 0)
			{
				dsc.Append(facing.Code[0]);
			}
		}
		dsc.AppendLine();
		FruitTreeRootBH rootBe = (Api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(RootOff)) as BlockEntityFruitTreeBranch)?.GetBehavior<FruitTreeRootBH>();
		if (rootBe == null)
		{
			return;
		}
		foreach (KeyValuePair<string, FruitTreeProperties> val in rootBe.propsByType)
		{
			dsc.AppendLine(val.Key + " " + val.Value.State);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		GrowTries = tree.GetInt("growTries");
		if (tree.HasAttribute("rootOffX"))
		{
			RootOff = new Vec3i(tree.GetInt("rootOffX"), tree.GetInt("rootOffY"), tree.GetInt("rootOffZ"));
		}
		initCustomBehaviors(null, callInitialize: false);
		base.FromTreeAttributes(tree, worldForResolving);
		SideGrowth = tree.GetInt("sideGrowth");
		if (tree.HasAttribute("parentX"))
		{
			ParentOff = new Vec3i(tree.GetInt("parentX"), tree.GetInt("parentY"), tree.GetInt("parentZ"));
		}
		FastForwardGrowth = null;
		if (tree.HasAttribute("fastForwardGrowth"))
		{
			FastForwardGrowth = tree.GetFloat("fastForwardGrowth");
		}
		lastGrowthAttemptTotalDays = tree.GetDouble("lastGrowthAttemptTotalDays");
		if (Api != null)
		{
			updateProperties();
		}
	}

	private void initCustomBehaviors(ItemStack parentPlantStack, bool callInitialize)
	{
		Vec3i rootOff = RootOff;
		if ((object)rootOff != null && rootOff.IsZero && GetBehavior<FruitTreeRootBH>() == null)
		{
			FruitTreeRootBH bh2 = new FruitTreeRootBH(this, parentPlantStack);
			if (callInitialize)
			{
				bh2.Initialize(Api, null);
			}
			Behaviors.Add(bh2);
		}
		if (GrowTries < 60 && GetBehavior<FruitTreeGrowingBranchBH>() == null)
		{
			FruitTreeGrowingBranchBH bh = new FruitTreeGrowingBranchBH(this);
			if (callInitialize)
			{
				bh.Initialize(Api, null);
			}
			Behaviors.Add(bh);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("sideGrowth", SideGrowth);
		if (ParentOff != null)
		{
			tree.SetInt("parentX", ParentOff.X);
			tree.SetInt("parentY", ParentOff.Y);
			tree.SetInt("parentZ", ParentOff.Z);
		}
		tree.SetInt("growTries", GrowTries);
		if (FastForwardGrowth.HasValue)
		{
			tree.SetFloat("fastForwardGrowth", FastForwardGrowth.Value);
		}
		tree.SetDouble("lastGrowthAttemptTotalDays", lastGrowthAttemptTotalDays);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		mesher.AddMeshData(branchMesh);
		if (leavesMesh != null)
		{
			mesher.AddMeshData(leavesMesh);
		}
		return true;
	}
}
