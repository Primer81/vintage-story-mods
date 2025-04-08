using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public abstract class BlockEntityFruitTreePart : BlockEntity, ITexPositionSource
{
	public EnumFoliageState FoliageState = EnumFoliageState.Plain;

	protected ICoreClientAPI capi;

	protected MeshData sticksMesh;

	protected MeshData leavesMesh;

	public int[] LeafParticlesColor;

	public int[] BlossomParticlesColor;

	public string TreeType;

	public int Height;

	public Vec3i RootOff;

	protected bool listenerOk;

	protected Shape nowTesselatingShape;

	public int fruitingSide;

	protected string foliageDictCacheKey;

	protected BlockFruitTreeFoliage blockFoliage;

	public BlockFruitTreeBranch blockBranch;

	public BlockFacing GrowthDir = BlockFacing.UP;

	public EnumTreePartType PartType = EnumTreePartType.Cutting;

	public AssetLocation harvestingSound;

	protected bool harvested;

	private long listenerId;

	private FruitTreeRootBH rootBh;

	public EnumFruitTreeState FruitTreeState
	{
		get
		{
			if (rootBh == null)
			{
				return EnumFruitTreeState.Empty;
			}
			if (rootBh.propsByType.TryGetValue(TreeType, out var val))
			{
				return val.State;
			}
			return EnumFruitTreeState.Empty;
		}
	}

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public double Progress => rootBh.GetCurrentStateProgress(TreeType);

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			IDictionary<string, CompositeTexture> textures = base.Block.Textures;
			AssetLocation texturePath = null;
			if ((this is BlockEntityFruitTreeBranch || this is BlockEntityFruitTreeFoliage) && FoliageState == EnumFoliageState.Dead && (textureCode == "bark" || textureCode == "treetrunk"))
			{
				textureCode = "deadtree";
			}
			DynFoliageProperties props = null;
			blockFoliage.foliageProps?.TryGetValue(TreeType, out props);
			if (props != null)
			{
				string key = textureCode + "-" + FoliageUtil.FoliageStates[(int)FoliageState];
				TextureAtlasPosition texPos = props.GetOrLoadTexture(capi, key);
				if (texPos != null)
				{
					return texPos;
				}
				texPos = props.GetOrLoadTexture(capi, textureCode);
				if (texPos != null)
				{
					return texPos;
				}
			}
			if (textures.TryGetValue(textureCode, out var tex))
			{
				texturePath = tex.Baked.BakedName;
			}
			if (texturePath == null && textures.TryGetValue("all", out tex))
			{
				texturePath = tex.Baked.BakedName;
			}
			if (texturePath == null)
			{
				nowTesselatingShape?.Textures.TryGetValue(textureCode, out texturePath);
			}
			if (texturePath == null)
			{
				return capi.BlockTextureAtlas.UnknownTexturePosition;
			}
			return getOrCreateTexPos(texturePath);
		}
	}

	protected TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
	{
		TextureAtlasPosition texpos = capi.BlockTextureAtlas[texturePath];
		if (texpos == null && !capi.BlockTextureAtlas.GetOrInsertTexture(texturePath, out var _, out texpos))
		{
			capi.World.Logger.Warning(string.Concat("For render in fruit tree block ", base.Block.Code, ", defined texture {1}, no such texture found."), texturePath);
			return capi.BlockTextureAtlas.UnknownTexturePosition;
		}
		return texpos;
	}

	public abstract void GenMesh();

	public virtual bool GenFoliageMesh(bool withSticks, out MeshData foliageMesh, out MeshData sticksMesh)
	{
		foliageMesh = null;
		sticksMesh = null;
		ICoreAPI api = Api;
		if ((api != null && api.Side == EnumAppSide.Server) || TreeType == null || TreeType == "" || blockFoliage?.foliageProps == null)
		{
			return false;
		}
		DynFoliageProperties foliageProps = blockFoliage.foliageProps[TreeType];
		LeafParticlesColor = capi.BlockTextureAtlas.GetRandomColors(getOrCreateTexPos(foliageProps.LeafParticlesTexture.Base));
		BlossomParticlesColor = capi.BlockTextureAtlas.GetRandomColors(getOrCreateTexPos(foliageProps.BlossomParticlesTexture.Base));
		Dictionary<int, MeshData[]> meshesByKey = ObjectCacheUtil.GetOrCreate(Api, foliageDictCacheKey, () => new Dictionary<int, MeshData[]>());
		int meshCacheKey = getHashCodeLeaves();
		if (meshesByKey.TryGetValue(meshCacheKey, out var meshes))
		{
			sticksMesh = meshes[0];
			foliageMesh = meshes[1];
			return true;
		}
		meshes = new MeshData[2];
		string shapekey2 = "foliage-ver";
		BlockFacing growthDir = GrowthDir;
		if (growthDir != null && growthDir.IsHorizontal)
		{
			shapekey2 = "foliage-hor-" + GrowthDir?.Code[0];
		}
		if (blockBranch?.Shapes == null || !blockBranch.Shapes.TryGetValue(shapekey2, out var shapeData2))
		{
			return false;
		}
		nowTesselatingShape = shapeData2.Shape;
		List<string> selectiveElements2 = new List<string>();
		bool everGreen = false;
		FruitTreeProperties props = null;
		FruitTreeRootBH fruitTreeRootBH = rootBh;
		if (fruitTreeRootBH != null && fruitTreeRootBH.propsByType.TryGetValue(TreeType, out props))
		{
			everGreen = props.CycleType == EnumTreeCycleType.Evergreen;
		}
		if (withSticks)
		{
			selectiveElements2.Add("sticks/*");
			capi.Tesselator.TesselateShape("fruittreefoliage", nowTesselatingShape, out meshes[0], this, new Vec3f(shapeData2.CShape.rotateX, shapeData2.CShape.rotateY, shapeData2.CShape.rotateZ), 0, 0, 0, null, selectiveElements2.ToArray());
		}
		selectiveElements2.Clear();
		if (FoliageState == EnumFoliageState.Flowering)
		{
			selectiveElements2.Add("blossom/*");
		}
		if (FoliageState != EnumFoliageState.Dead && FoliageState != 0 && (FoliageState != EnumFoliageState.Flowering || everGreen))
		{
			nowTesselatingShape.WalkElements("leaves/*", delegate(ShapeElement elem)
			{
				elem.SeasonColorMap = foliageProps.SeasonColorMap;
				elem.ClimateColorMap = foliageProps.ClimateColorMap;
			});
			selectiveElements2.Add("leaves/*");
		}
		float rndydeg = (float)GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, 3) * 22.5f - 22.5f;
		capi.Tesselator.TesselateShape("fruittreefoliage", nowTesselatingShape, out meshes[1], this, new Vec3f(shapeData2.CShape.rotateX, shapeData2.CShape.rotateY + rndydeg, shapeData2.CShape.rotateZ), 0, 0, 0, null, selectiveElements2.ToArray());
		sticksMesh = meshes[0];
		foliageMesh = meshes[1];
		if (FoliageState == EnumFoliageState.Fruiting || FoliageState == EnumFoliageState.Ripe)
		{
			string shapekey = "fruit-" + TreeType;
			if ((FoliageState != EnumFoliageState.Ripe || !blockBranch.Shapes.TryGetValue(shapekey + "-ripe", out var shapeData)) && !blockBranch.Shapes.TryGetValue(shapekey, out shapeData))
			{
				return false;
			}
			nowTesselatingShape = shapeData.Shape;
			List<string> selectiveElements = new List<string>();
			for (int i = 0; i < 4; i++)
			{
				char f = BlockFacing.HORIZONTALS[i].Code[0];
				if ((fruitingSide & (1 << i)) > 0)
				{
					ReadOnlySpan<char> readOnlySpan = "fruits-";
					char reference = f;
					selectiveElements.Add(string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference), "/*"));
				}
			}
			GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, 3);
			capi.Tesselator.TesselateShape("fruittreefoliage", nowTesselatingShape, out var fruitMesh, this, new Vec3f(shapeData.CShape.rotateX, shapeData.CShape.rotateY, shapeData.CShape.rotateZ), 0, 0, 0, null, selectiveElements.ToArray());
			foliageMesh.AddMeshData(fruitMesh);
		}
		meshesByKey[meshCacheKey] = meshes;
		return true;
	}

	protected virtual int getHashCodeLeaves()
	{
		return (GrowthDir.Code[0] + "-" + TreeType + "-" + FoliageState.ToString() + "-" + fruitingSide + "-" + ((float)GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, 3) * 22.5f - 22.5f)).GetHashCode();
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		foliageDictCacheKey = "fruitTreeFoliageMeshes" + base.Block.Code.ToShortString();
		capi = api as ICoreClientAPI;
		listenerId = RegisterGameTickListener(trySetup, 1000);
		if (api.Side == EnumAppSide.Client)
		{
			string code = base.Block.Attributes["harvestingSound"].AsString("sounds/block/plant");
			if (code != null)
			{
				harvestingSound = AssetLocation.Create(code, base.Block.Code.Domain);
			}
			GenMesh();
		}
	}

	private void trySetup(float dt)
	{
		if (!(RootOff != null) || RootOff.IsZero || Api.World.BlockAccessor.GetChunkAtBlockPos(Pos.AddCopy(RootOff)) != null)
		{
			getRootBhSetupListener();
			UnregisterGameTickListener(listenerId);
			listenerId = 0L;
		}
	}

	protected bool getRootBhSetupListener()
	{
		if (RootOff == null || RootOff.IsZero)
		{
			rootBh = GetBehavior<FruitTreeRootBH>();
		}
		else
		{
			rootBh = (Api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(RootOff)) as BlockEntityFruitTreeBranch)?.GetBehavior<FruitTreeRootBH>();
		}
		if (TreeType == null)
		{
			Api.World.Logger.Error("Coding error. Fruit tree without fruit tree type @" + Pos);
			return false;
		}
		if (rootBh != null && rootBh.propsByType.TryGetValue(TreeType, out var val))
		{
			switch (val.State)
			{
			case EnumFruitTreeState.EnterDormancy:
				FoliageState = EnumFoliageState.Plain;
				break;
			case EnumFruitTreeState.Dormant:
				FoliageState = EnumFoliageState.DormantNoLeaves;
				harvested = false;
				break;
			case EnumFruitTreeState.DormantVernalized:
				FoliageState = EnumFoliageState.DormantNoLeaves;
				harvested = false;
				break;
			case EnumFruitTreeState.Flowering:
				FoliageState = EnumFoliageState.Flowering;
				harvested = false;
				break;
			case EnumFruitTreeState.Fruiting:
				FoliageState = EnumFoliageState.Fruiting;
				harvested = false;
				break;
			case EnumFruitTreeState.Ripe:
				FoliageState = (harvested ? EnumFoliageState.Plain : EnumFoliageState.Ripe);
				break;
			case EnumFruitTreeState.Empty:
				FoliageState = EnumFoliageState.Plain;
				break;
			case EnumFruitTreeState.Young:
				FoliageState = EnumFoliageState.Plain;
				break;
			case EnumFruitTreeState.Dead:
				FoliageState = EnumFoliageState.Dead;
				break;
			}
			if (Api.Side == EnumAppSide.Server)
			{
				rootBh.propsByType[TreeType].OnFruitingStateChange += RootBh_OnFruitingStateChange;
			}
			listenerOk = true;
			return true;
		}
		return false;
	}

	protected void RootBh_OnFruitingStateChange(EnumFruitTreeState nowState)
	{
		switch (nowState)
		{
		case EnumFruitTreeState.EnterDormancy:
			FoliageState = EnumFoliageState.Plain;
			harvested = false;
			break;
		case EnumFruitTreeState.Dormant:
			FoliageState = EnumFoliageState.DormantNoLeaves;
			harvested = false;
			break;
		case EnumFruitTreeState.DormantVernalized:
			FoliageState = EnumFoliageState.DormantNoLeaves;
			harvested = false;
			break;
		case EnumFruitTreeState.Flowering:
			FoliageState = EnumFoliageState.Flowering;
			harvested = false;
			break;
		case EnumFruitTreeState.Fruiting:
			FoliageState = EnumFoliageState.Fruiting;
			harvested = false;
			break;
		case EnumFruitTreeState.Ripe:
			if (!harvested)
			{
				FoliageState = EnumFoliageState.Ripe;
			}
			break;
		case EnumFruitTreeState.Empty:
			FoliageState = EnumFoliageState.Plain;
			break;
		case EnumFruitTreeState.Young:
			FoliageState = EnumFoliageState.Plain;
			break;
		case EnumFruitTreeState.Dead:
			FoliageState = EnumFoliageState.Dead;
			break;
		}
		calcFruitingSide();
		MarkDirty();
	}

	protected void calcFruitingSide()
	{
		fruitingSide = 0;
		for (int i = 0; i < 4; i++)
		{
			BlockFacing.HORIZONTALS[i].IterateThruFacingOffsets(Pos);
			if (Api.World.BlockAccessor.GetBlock(Pos).Id == 0)
			{
				fruitingSide |= 1 << i;
			}
		}
		Pos.East();
	}

	public void OnGrown()
	{
		if (!listenerOk)
		{
			getRootBhSetupListener();
		}
		GenMesh();
		calcFruitingSide();
	}

	public bool OnBlockInteractStart(IPlayer byPlayer, BlockSelection blockSel)
	{
		if (FoliageState == EnumFoliageState.Ripe && PartType != 0)
		{
			Api.World.PlaySoundAt(harvestingSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
			return true;
		}
		return false;
	}

	public bool OnBlockInteractStep(float secondsUsed, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (PartType == EnumTreePartType.Stem)
		{
			return false;
		}
		(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
		if (Api.World.Rand.NextDouble() < 0.1)
		{
			Api.World.PlaySoundAt(harvestingSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
		}
		if (FoliageState == EnumFoliageState.Ripe)
		{
			return (double)secondsUsed < 1.3;
		}
		return false;
	}

	public void OnBlockInteractStop(float secondsUsed, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!((double)secondsUsed > 1.1) || FoliageState != EnumFoliageState.Ripe)
		{
			return;
		}
		FoliageState = EnumFoliageState.Plain;
		MarkDirty(redrawOnClient: true);
		harvested = true;
		AssetLocation loc = AssetLocation.Create(base.Block.Attributes["branchBlock"].AsString(), base.Block.Code.Domain);
		BlockDropItemStack[] fruitStacks = (Api.World.GetBlock(loc) as BlockFruitTreeBranch).TypeProps[TreeType].FruitStacks;
		foreach (BlockDropItemStack drop in fruitStacks)
		{
			ItemStack stack = drop.GetNextItemStack();
			if (stack != null)
			{
				if (!byPlayer.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
				{
					Api.World.SpawnItemEntity(stack, byPlayer.Entity.Pos.XYZ.Add(0.0, 0.5, 0.0));
				}
				if (drop.LastDrop)
				{
					break;
				}
			}
		}
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		base.OnBlockBroken(byPlayer);
		if (FoliageState != EnumFoliageState.Ripe)
		{
			return;
		}
		AssetLocation loc = AssetLocation.Create(base.Block.Attributes["branchBlock"].AsString(), base.Block.Code.Domain);
		BlockDropItemStack[] fruitStacks = (Api.World.GetBlock(loc) as BlockFruitTreeBranch).TypeProps[TreeType].FruitStacks;
		foreach (BlockDropItemStack drop in fruitStacks)
		{
			ItemStack stack = drop.GetNextItemStack();
			if (stack != null)
			{
				Api.World.SpawnItemEntity(stack, Pos);
				if (drop.LastDrop)
				{
					break;
				}
			}
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (FoliageState == EnumFoliageState.Dead && PartType != EnumTreePartType.Cutting)
		{
			dsc.AppendLine("<font color=\"#ff8080\">" + Lang.Get("Dead tree.") + "</font>");
		}
		if (rootBh != null && rootBh.propsByType.Count > 0 && TreeType != null && PartType != EnumTreePartType.Cutting)
		{
			FruitTreeProperties props = rootBh?.propsByType[TreeType];
			if (props.State == EnumFruitTreeState.Ripe)
			{
				double days3 = props.lastStateChangeTotalDays + (double)props.RipeDays - rootBh.LastRootTickTotalDays;
				dsc.AppendLine(Lang.Get("Fresh fruit for about {0:0.#} days.", days3));
			}
			if (props.State == EnumFruitTreeState.Fruiting)
			{
				double days2 = props.lastStateChangeTotalDays + (double)props.FruitingDays - rootBh.LastRootTickTotalDays;
				dsc.AppendLine(Lang.Get("Ripe in about {0:0.#} days, weather permitting.", days2));
			}
			if (props.State == EnumFruitTreeState.Flowering)
			{
				double days = props.lastStateChangeTotalDays + (double)props.FloweringDays - rootBh.LastRootTickTotalDays;
				dsc.AppendLine(Lang.Get("Flowering for about {0:0.#} days, weather permitting.", days));
			}
			dsc.AppendLine(Lang.Get("treestate", Lang.Get("treestate-" + props.State.ToString().ToLowerInvariant())));
		}
		base.GetBlockInfo(forPlayer, dsc);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		EnumFoliageState prevState = FoliageState;
		PartType = (EnumTreePartType)tree.GetInt("partType");
		FoliageState = (EnumFoliageState)tree.GetInt("foliageState");
		GrowthDir = BlockFacing.ALLFACES[tree.GetInt("growthDir")];
		TreeType = tree.GetString("treeType");
		Height = tree.GetInt("height");
		fruitingSide = tree.GetInt("fruitingSide", fruitingSide);
		harvested = tree.GetBool("harvested");
		if (tree.HasAttribute("rootOffX"))
		{
			RootOff = new Vec3i(tree.GetInt("rootOffX"), tree.GetInt("rootOffY"), tree.GetInt("rootOffZ"));
		}
		if (Api != null && Api.Side == EnumAppSide.Client && prevState != FoliageState)
		{
			MarkDirty(redrawOnClient: true);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("partType", (int)PartType);
		tree.SetInt("foliageState", (int)FoliageState);
		tree.SetInt("growthDir", GrowthDir.Index);
		tree.SetString("treeType", TreeType);
		tree.SetInt("height", Height);
		tree.SetInt("fruitingSide", fruitingSide);
		tree.SetBool("harvested", harvested);
		if (RootOff != null)
		{
			tree.SetInt("rootOffX", RootOff.X);
			tree.SetInt("rootOffY", RootOff.Y);
			tree.SetInt("rootOffZ", RootOff.Z);
		}
	}
}
