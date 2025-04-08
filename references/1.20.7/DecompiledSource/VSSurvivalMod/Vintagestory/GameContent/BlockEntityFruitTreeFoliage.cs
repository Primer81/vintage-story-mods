using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityFruitTreeFoliage : BlockEntityFruitTreePart
{
	public override void Initialize(ICoreAPI api)
	{
		blockFoliage = base.Block as BlockFruitTreeFoliage;
		string code = base.Block.Attributes?["branchBlock"]?.AsString();
		if (code == null)
		{
			api.World.BlockAccessor.SetBlock(0, Pos);
			return;
		}
		blockBranch = api.World.GetBlock(AssetLocation.Create(code, base.Block.Code.Domain)) as BlockFruitTreeBranch;
		base.Initialize(api);
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		GenMesh();
	}

	public override void GenMesh()
	{
		base.GenFoliageMesh(withSticks: true, out leavesMesh, out sticksMesh);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		if (Api.World.EntityDebugMode)
		{
			dsc.AppendLine("TreeType: " + TreeType);
			dsc.AppendLine("FoliageState: " + FoliageState);
			dsc.AppendLine("Growthdir: " + GrowthDir);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		EnumFoliageState prevState = FoliageState;
		if (Api != null && Api.Side == EnumAppSide.Client)
		{
			GenMesh();
			if (prevState != FoliageState)
			{
				MarkDirty(redrawOnClient: true);
			}
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (sticksMesh == null)
		{
			return true;
		}
		mesher.AddMeshData(leavesMesh);
		mesher.AddMeshData(CopyRndSticksMesh(sticksMesh));
		return true;
	}

	private MeshData CopyRndSticksMesh(MeshData mesh)
	{
		float rndoffx = (float)(GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, 100) - 50) / 500f;
		float rndoffy = (float)(GameMath.MurmurHash3Mod(Pos.X, -Pos.Y, Pos.Z, 100) - 50) / 500f;
		float rndoffz = (float)(GameMath.MurmurHash3Mod(Pos.X, Pos.Y, -Pos.Z, 100) - 50) / 500f;
		float rndrotx = ((float)GameMath.MurmurHash3Mod(-Pos.X, -Pos.Y, Pos.Z, 100) - 50f) / 150f;
		float rndrotz = ((float)GameMath.MurmurHash3Mod(-Pos.X, -Pos.Y, -Pos.Z, 100) - 50f) / 150f;
		float branchAngle1 = rndrotx;
		float branchAngle2 = rndrotz;
		Vec3f origin = null;
		switch (GrowthDir.Index)
		{
		case 0:
			origin = new Vec3f(0.5f, 0.25f, 1.3125f);
			branchAngle1 = 0f - rndrotx;
			break;
		case 1:
			branchAngle1 = rndrotz;
			branchAngle2 = 0f - rndrotx;
			origin = new Vec3f(-0.3125f, 0.25f, 0.5f);
			break;
		case 2:
			origin = new Vec3f(0.5f, 0.25f, -0.3125f);
			break;
		case 3:
			origin = new Vec3f(1.3125f, 0.25f, 0.5f);
			branchAngle1 = 0f;
			branchAngle2 = rndrotx;
			break;
		case 4:
			origin = new Vec3f(0.5f, 0f, 0.5f);
			rndoffy = 0f;
			break;
		}
		return mesh?.Clone().Translate(rndoffx, rndoffy, rndoffz).Rotate(origin, branchAngle1, 0f, branchAngle2);
	}
}
