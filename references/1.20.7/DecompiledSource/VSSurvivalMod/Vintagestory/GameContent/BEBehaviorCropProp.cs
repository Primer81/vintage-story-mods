using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BEBehaviorCropProp : BlockEntityBehavior, ITexPositionSource
{
	public int Stage = 1;

	public string Type;

	private MeshData mesh;

	private CropPropConfig config;

	private ICoreClientAPI capi;

	private Shape nowTesselatingShape;

	private ITexPositionSource defaultSource;

	private bool dead;

	private Block cropBlock;

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			int textureSubId;
			if (config.Textures != null && config.Textures.ContainsKey(textureCode))
			{
				capi.BlockTextureAtlas.GetOrInsertTexture(config.Textures[textureCode], out textureSubId, out var texPosb);
				return texPosb;
			}
			capi.BlockTextureAtlas.GetOrInsertTexture(nowTesselatingShape.Textures[textureCode], out textureSubId, out var texPos);
			return texPos;
		}
	}

	public BEBehaviorCropProp(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		if (Api.Side == EnumAppSide.Server)
		{
			Blockentity.RegisterGameTickListener(onTick8s, 800, Api.World.Rand.Next(100));
			if (Type != null)
			{
				loadConfig();
				onTick8s(0f);
				mesh = null;
			}
		}
		else if (Type != null)
		{
			loadConfig();
			mesh = null;
		}
	}

	private void loadConfig()
	{
		if (Type == null)
		{
			return;
		}
		config = base.Block.Attributes["types"][dead ? "dead" : Type].AsObject<CropPropConfig>();
		if (config.Shape != null)
		{
			config.Shape.Base.Path = config.Shape.Base.Path.Replace("{stage}", Stage.ToString() ?? "").Replace("{type}", Type);
		}
		if (config.Textures == null)
		{
			return;
		}
		foreach (CompositeTexture val in config.Textures.Values)
		{
			val.Base.Path = val.Base.Path.Replace("{stage}", Stage.ToString() ?? "").Replace("{type}", Type);
		}
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		if (byItemStack != null)
		{
			Type = byItemStack.Attributes.GetString("type");
		}
		loadConfig();
		onTick8s(0f);
		mesh = null;
	}

	private void loadMesh()
	{
		if (Api == null || Api.Side != EnumAppSide.Client)
		{
			return;
		}
		capi = Api as ICoreClientAPI;
		if (Type != null)
		{
			cropBlock = Api.World.GetBlock(new AssetLocation("crop-" + Type + "-" + Stage));
			string key = getCacheKey();
			Dictionary<string, MeshData> cache = ObjectCacheUtil.GetOrCreate(Api, "croppropmeshes", () => new Dictionary<string, MeshData>());
			if (cache.TryGetValue(key, out var meshData))
			{
				this.mesh = meshData;
				return;
			}
			MeshData mesh = genMesh(cropBlock);
			key = getCacheKey();
			MeshData meshData3 = (cache[key] = mesh);
			this.mesh = meshData3;
		}
	}

	private string getCacheKey()
	{
		if (config.BakedAlternatesLength < 0)
		{
			return cropBlock.Id + "--1";
		}
		int rndIndex = GameMath.MurmurHash3Mod(base.Pos.X, base.Pos.Y, base.Pos.Z, config.BakedAlternatesLength);
		return cropBlock.Id + "-" + rndIndex;
	}

	private MeshData genMesh(Block cropBlock)
	{
		CompositeShape cshape = config.Shape;
		if (cshape == null)
		{
			if (cropBlock.Shape.Alternates == null)
			{
				mesh = capi.TesselatorManager.GetDefaultBlockMesh(cropBlock).Clone();
				mesh.Translate(0f, -0.0625f, 0f);
				return mesh;
			}
			cshape = cropBlock.Shape;
		}
		else
		{
			cshape.LoadAlternates(capi.Assets, capi.Logger);
		}
		if (cshape.BakedAlternates != null)
		{
			config.BakedAlternatesLength = cshape.BakedAlternates.Length;
			cshape = cshape.BakedAlternates[GameMath.MurmurHash3Mod(base.Pos.X, base.Pos.Y, base.Pos.Z, cshape.BakedAlternates.Length)];
		}
		nowTesselatingShape = capi.Assets.TryGet(cshape.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json")).ToObject<Shape>();
		capi.Tesselator.TesselateShape("croprop", base.Block.Code, cshape, out mesh, this, 0, 0, 0);
		mesh.Translate(0f, -0.0625f, 0f);
		return mesh;
	}

	private void onTick8s(float dt)
	{
		if (config != null)
		{
			float yearRel = Api.World.Calendar.YearRel;
			float len = (config.MonthEnd - config.MonthStart) / 12f;
			int nextStage = GameMath.Clamp((int)((yearRel - (config.MonthStart - 1f) / 12f) / len * (float)config.Stages), 1, config.Stages);
			float temp = Api.World.BlockAccessor.GetClimateAt(base.Pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, Api.World.Calendar.TotalDays).Temperature;
			bool nowDead = !dead && temp < -2f;
			bool nowAlive = dead && temp > 15f;
			if (nowDead)
			{
				dead = true;
			}
			if (nowAlive)
			{
				dead = false;
			}
			if (Stage != nextStage || nowDead || nowAlive)
			{
				Stage = nextStage;
				loadConfig();
				loadMesh();
				Blockentity.MarkDirty(redrawOnClient: true);
			}
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		int curStage = Stage;
		bool curDead = dead;
		Type = tree.GetString("code");
		Stage = tree.GetInt("stage");
		dead = tree.GetBool("dead");
		if (Stage != curStage || dead != curDead)
		{
			loadConfig();
			loadMesh();
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetString("code", Type);
		tree.SetInt("stage", Stage);
		tree.SetBool("dead", dead);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (mesh == null)
		{
			loadMesh();
		}
		Block block = cropBlock;
		float[] matrix = ((block != null && block.RandomizeRotations) ? TesselationMetaData.randomRotMatrices[GameMath.MurmurHash3Mod(-base.Pos.X, (cropBlock.RandomizeAxes == EnumRandomizeAxes.XYZ) ? base.Pos.Y : 0, base.Pos.Z, TesselationMetaData.randomRotations.Length)] : null);
		mesher.AddMeshData(mesh, matrix);
		return true;
	}
}
