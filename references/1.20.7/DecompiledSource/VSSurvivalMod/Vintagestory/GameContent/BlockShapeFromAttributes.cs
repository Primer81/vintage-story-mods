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

public abstract class BlockShapeFromAttributes : Block, IWrenchOrientable, ITextureFlippable
{
	protected bool colSelBoxEditMode;

	protected bool transformEditMode;

	protected float rotInterval = (float)Math.PI / 8f;

	public IDictionary<string, CompositeTexture> blockTextures;

	public Dictionary<string, OrderedDictionary<string, CompositeTexture>> OverrideTextureGroups;

	protected Dictionary<string, MeshData> meshDictionary;

	protected string inventoryMeshDictionary;

	protected string blockForLogging;

	public bool AllowRandomizeDims = true;

	public SkillItem[] extraWrenchModes;

	private byte[] noLight = new byte[3];

	public abstract string ClassType { get; }

	public abstract IEnumerable<IShapeTypeProps> AllTypes { get; }

	public abstract void LoadTypes();

	public abstract IShapeTypeProps GetTypeProps(string code, ItemStack stack, BEBehaviorShapeFromAttributes be);

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		extraWrenchModes = new SkillItem[3]
		{
			new SkillItem
			{
				Code = new AssetLocation("ns"),
				Name = Lang.Get("Offset 1 Voxel North/South")
			},
			new SkillItem
			{
				Code = new AssetLocation("ew"),
				Name = Lang.Get("Offset 1 Voxel East/West")
			},
			new SkillItem
			{
				Code = new AssetLocation("ud"),
				Name = Lang.Get("Offset 1 Voxel Up/Down")
			}
		};
		if (api is ICoreClientAPI capi)
		{
			extraWrenchModes[0].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/movens.svg"), 48, 48, 5, -1));
			extraWrenchModes[1].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/moveew.svg"), 48, 48, 5, -1));
			extraWrenchModes[2].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/moveud.svg"), 48, 48, 5, -1));
			meshDictionary = ObjectCacheUtil.GetOrCreate(api, ClassType + "Meshes", () => new Dictionary<string, MeshData>());
			inventoryMeshDictionary = ClassType + "MeshesInventory";
			blockForLogging = ClassType + "block";
			capi.Event.RegisterEventBusListener(OnEventBusEvent);
			foreach (IShapeTypeProps type in AllTypes)
			{
				if (Textures.TryGetValue(type.Code + ":" + type.FirstTexture, out var ct))
				{
					type.TexPos = capi.BlockTextureAtlas[ct.Baked.BakedName];
				}
			}
			blockTextures = Attributes["textures"].AsObject<IDictionary<string, CompositeTexture>>();
		}
		else
		{
			LoadTypes();
			OverrideTextureGroups = Attributes["overrideTextureGroups"].AsObject<Dictionary<string, OrderedDictionary<string, CompositeTexture>>>();
		}
		AllowRandomizeDims = Attributes?["randomizeDimensions"].AsBool(defaultValue: true) ?? false;
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		base.OnUnloaded(api);
		if (extraWrenchModes != null)
		{
			extraWrenchModes[0].Dispose();
			extraWrenchModes[1].Dispose();
			extraWrenchModes[2].Dispose();
		}
		if (!(api is ICoreClientAPI capi) || inventoryMeshDictionary == null)
		{
			return;
		}
		Dictionary<string, MultiTextureMeshRef> clutterMeshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(capi, inventoryMeshDictionary);
		if (clutterMeshRefs == null)
		{
			return;
		}
		foreach (MultiTextureMeshRef value in clutterMeshRefs.Values)
		{
			value.Dispose();
		}
		ObjectCacheUtil.Delete(capi, inventoryMeshDictionary);
	}

	public override void OnCollectTextures(ICoreAPI api, ITextureLocationDictionary textureDict)
	{
		OverrideTextureGroups = Attributes["overrideTextureGroups"].AsObject<Dictionary<string, OrderedDictionary<string, CompositeTexture>>>();
		base.api = api;
		LoadTypes();
		foreach (IShapeTypeProps cprops in AllTypes)
		{
			cprops.ShapeResolved = api.Assets.TryGet(cprops.ShapePath)?.ToObject<Shape>();
			if (cprops.ShapeResolved == null)
			{
				api.Logger.Error("Block {0}: Could not find {1}, type {2} shape '{3}'.", Code, ClassType, cprops.Code, cprops.ShapePath);
				continue;
			}
			FastSmallDictionary<string, CompositeTexture> textures = new FastSmallDictionary<string, CompositeTexture>(1);
			textureDict.CollectAndBakeTexturesFromShape(cprops.ShapeResolved, textures, cprops.ShapePath);
			cprops.FirstTexture = textures.GetFirstKey();
			foreach (KeyValuePair<string, CompositeTexture> pair in textures)
			{
				Textures.Add(cprops.Code + ":" + pair.Key, pair.Value);
			}
		}
		if (OverrideTextureGroups != null)
		{
			foreach (KeyValuePair<string, OrderedDictionary<string, CompositeTexture>> group in OverrideTextureGroups)
			{
				string sourceString = string.Concat("Block ", Code, ": override texture group ", group.Key);
				foreach (KeyValuePair<string, CompositeTexture> val in group.Value)
				{
					val.Value.Bake(api.Assets);
					val.Value.Baked.TextureSubId = textureDict.GetOrAddTextureLocation(new AssetLocationAndSource(val.Value.Baked.BakedName, sourceString, Code));
				}
			}
		}
		base.OnCollectTextures(api, textureDict);
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			blockBehaviors[i].OnNeighbourBlockChange(world, pos, neibpos, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				return;
			}
		}
		if (handled == EnumHandling.PassThrough && (this == snowCovered1 || this == snowCovered2 || this == snowCovered3) && pos.X == neibpos.X && pos.Z == neibpos.Z && pos.Y + 1 == neibpos.Y && world.BlockAccessor.GetBlock(neibpos).Id != 0)
		{
			world.BlockAccessor.ExchangeBlock(notSnowCovered.Id, pos);
			world.BlockAccessor.MarkBlockDirty(pos);
			world.BlockAccessor.MarkBlockEntityDirty(pos);
		}
	}

	public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
	{
		if (extra is string && (string)extra == "melt")
		{
			if (this == snowCovered3)
			{
				world.BlockAccessor.ExchangeBlock(snowCovered2.Id, pos);
			}
			else if (this == snowCovered2)
			{
				world.BlockAccessor.ExchangeBlock(snowCovered1.Id, pos);
			}
			else if (this == snowCovered1)
			{
				world.BlockAccessor.ExchangeBlock(notSnowCovered.Id, pos);
			}
			world.BlockAccessor.MarkBlockDirty(pos);
			world.BlockAccessor.MarkBlockEntityDirty(pos);
		}
	}

	private void OnEventBusEvent(string eventName, ref EnumHandling handling, IAttribute data)
	{
		switch (eventName)
		{
		case "oncloseeditselboxes":
		case "oneditselboxes":
		case "onapplyselboxes":
			onSelBoxEditorEvent(eventName, data);
			break;
		}
		switch (eventName)
		{
		case "oncloseedittransforms":
		case "onedittransforms":
		case "onapplytransforms":
		case "genjsontransform":
			onTfEditorEvent(eventName, data);
			break;
		}
	}

	private void onTfEditorEvent(string eventName, IAttribute data)
	{
		ItemSlot slot = (api as ICoreClientAPI).World.Player.InventoryManager.ActiveHotbarSlot;
		if (slot.Empty)
		{
			return;
		}
		string type = slot.Itemstack.Attributes.GetString("type");
		IShapeTypeProps cprops = GetTypeProps(type, slot.Itemstack, null);
		if (cprops == null || (transformEditMode && eventName == "onedittransforms") || eventName == "genjsontransform")
		{
			return;
		}
		transformEditMode = eventName == "onedittransforms";
		if (transformEditMode)
		{
			if (cprops.GuiTransform == null)
			{
				cprops.GuiTransform = ModelTransform.BlockDefaultGui();
			}
			GuiTransform = cprops.GuiTransform;
			if (cprops.FpTtransform == null)
			{
				cprops.FpTtransform = ModelTransform.BlockDefaultFp();
			}
			FpHandTransform = cprops.FpTtransform;
			if (cprops.TpTransform == null)
			{
				cprops.TpTransform = ModelTransform.BlockDefaultTp();
			}
			TpHandTransform = cprops.TpTransform;
			if (cprops.GroundTransform == null)
			{
				cprops.GroundTransform = ModelTransform.BlockDefaultGround();
			}
			GroundTransform = cprops.GroundTransform;
		}
		if (eventName == "onapplytransforms")
		{
			cprops.GuiTransform = GuiTransform;
			cprops.FpTtransform = FpHandTransform;
			cprops.TpTransform = TpHandTransform;
			cprops.GroundTransform = GroundTransform;
		}
		if (eventName == "oncloseedittransforms")
		{
			GuiTransform = ModelTransform.BlockDefaultGui();
			FpHandTransform = ModelTransform.BlockDefaultFp();
			TpHandTransform = ModelTransform.BlockDefaultTp();
			GroundTransform = ModelTransform.BlockDefaultGround();
		}
	}

	private void onSelBoxEditorEvent(string eventName, IAttribute data)
	{
		TreeAttribute tree = data as TreeAttribute;
		if (tree?.GetInt("nowblockid") != Id)
		{
			return;
		}
		colSelBoxEditMode = eventName == "oneditselboxes";
		BlockPos pos = tree.GetBlockPos("pos");
		if (colSelBoxEditMode)
		{
			BEBehaviorShapeFromAttributes bect2 = GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
			IShapeTypeProps cprops2 = GetTypeProps(bect2?.Type, null, bect2);
			if (cprops2 != null)
			{
				if (cprops2.ColSelBoxes == null)
				{
					cprops2.ColSelBoxes = new Cuboidf[1] { Cuboidf.Default() };
				}
				SelectionBoxes = cprops2.ColSelBoxes;
			}
		}
		if (eventName == "onapplyselboxes")
		{
			BEBehaviorShapeFromAttributes bect = GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
			IShapeTypeProps cprops = GetTypeProps(bect?.Type, null, bect);
			if (cprops != null)
			{
				cprops.ColSelBoxes = SelectionBoxes;
				SelectionBoxes = new Cuboidf[1] { Cuboidf.Default() };
			}
		}
	}

	public override Cuboidf[] GetParticleCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return GetCollisionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BEBehaviorShapeFromAttributes bect = GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
		if (bect == null)
		{
			return base.GetCollisionBoxes(blockAccessor, pos);
		}
		IShapeTypeProps cprops = GetTypeProps(bect.Type, null, bect);
		return getCollisionBoxes(blockAccessor, pos, bect, cprops);
	}

	private Cuboidf[] getCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, BEBehaviorShapeFromAttributes bect, IShapeTypeProps cprops)
	{
		if (cprops?.ColSelBoxes == null)
		{
			return base.GetCollisionBoxes(blockAccessor, pos);
		}
		if (colSelBoxEditMode)
		{
			return cprops.ColSelBoxes;
		}
		long hashkey = ((long)(bect.offsetX * 255f + 255f) << 45) | ((long)(bect.offsetY * 255f + 255f) << 36) | ((long)(bect.offsetZ * 255f + 255f) << 27) | ((long)((bect.rotateY + ((bect.rotateY < 0f) ? ((float)Math.PI * 2f) : 0f)) * (180f / (float)Math.PI)) << 18) | ((long)((bect.rotateX + ((bect.rotateX < 0f) ? ((float)Math.PI * 2f) : 0f)) * (180f / (float)Math.PI)) << 9) | (long)((bect.rotateZ + ((bect.rotateZ < 0f) ? ((float)Math.PI * 2f) : 0f)) * (180f / (float)Math.PI));
		if (cprops.ColSelBoxesByHashkey.TryGetValue(hashkey, out var cuboids))
		{
			return cuboids;
		}
		cuboids = (cprops.ColSelBoxesByHashkey[hashkey] = new Cuboidf[cprops.ColSelBoxes.Length]);
		for (int i = 0; i < cuboids.Length; i++)
		{
			cuboids[i] = cprops.ColSelBoxes[i].RotatedCopy(bect.rotateX * (180f / (float)Math.PI), bect.rotateY * (180f / (float)Math.PI), bect.rotateZ * (180f / (float)Math.PI), new Vec3d(0.5, 0.5, 0.5)).ClampTo(Vec3f.Zero, Vec3f.One).OffsetCopy(bect.offsetX, bect.offsetY, bect.offsetZ);
		}
		return cuboids;
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BEBehaviorShapeFromAttributes bect = GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
		if (bect == null)
		{
			return base.GetCollisionBoxes(blockAccessor, pos);
		}
		IShapeTypeProps cprops = GetTypeProps(bect.Type, null, bect);
		if (cprops?.SelBoxes == null)
		{
			return getCollisionBoxes(blockAccessor, pos, bect, cprops);
		}
		if (colSelBoxEditMode)
		{
			return cprops.ColSelBoxes ?? cprops.SelBoxes;
		}
		long hashkey = ((long)(bect.offsetX * 255f + 255f) << 45) | ((long)(bect.offsetY * 255f + 255f) << 36) | ((long)(bect.offsetZ * 255f + 255f) << 27) | ((long)((bect.rotateY + ((bect.rotateY < 0f) ? ((float)Math.PI * 2f) : 0f)) * (180f / (float)Math.PI)) << 18) | ((long)((bect.rotateX + ((bect.rotateX < 0f) ? ((float)Math.PI * 2f) : 0f)) * (180f / (float)Math.PI)) << 9) | (long)((bect.rotateZ + ((bect.rotateZ < 0f) ? ((float)Math.PI * 2f) : 0f)) * (180f / (float)Math.PI));
		if (cprops.SelBoxesByHashkey == null)
		{
			cprops.SelBoxesByHashkey = new Dictionary<long, Cuboidf[]>();
		}
		if (cprops.SelBoxesByHashkey.TryGetValue(hashkey, out var cuboids))
		{
			return cuboids;
		}
		cuboids = (cprops.SelBoxesByHashkey[hashkey] = new Cuboidf[cprops.SelBoxes.Length]);
		for (int i = 0; i < cuboids.Length; i++)
		{
			cuboids[i] = cprops.SelBoxes[i].RotatedCopy(bect.rotateX * (180f / (float)Math.PI), bect.rotateY * (180f / (float)Math.PI), bect.rotateZ * (180f / (float)Math.PI), new Vec3d(0.5, 0.5, 0.5)).ClampTo(Vec3f.Zero, Vec3f.One).OffsetCopy(bect.offsetX, bect.offsetY, bect.offsetZ);
		}
		return cuboids;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
		Dictionary<string, MultiTextureMeshRef> clutterMeshRefs = ObjectCacheUtil.GetOrCreate(capi, inventoryMeshDictionary, () => new Dictionary<string, MultiTextureMeshRef>());
		string type = itemstack.Attributes.GetString("type", "");
		IShapeTypeProps cprops = GetTypeProps(type, itemstack, null);
		if (cprops == null)
		{
			return;
		}
		float rotX = itemstack.Attributes.GetFloat("rotX");
		float rotY = itemstack.Attributes.GetFloat("rotY");
		float rotZ = itemstack.Attributes.GetFloat("rotZ");
		string otcode = itemstack.Attributes.GetString("overrideTextureCode");
		string hashkey = cprops.HashKey + "-" + rotX + "-" + rotY + "-" + rotZ + "-" + otcode;
		if (!clutterMeshRefs.TryGetValue(hashkey, out var meshref))
		{
			MeshData mesh = GetOrCreateMesh(cprops, null, otcode);
			mesh = mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), rotX, rotY, rotZ);
			meshref = (clutterMeshRefs[hashkey] = capi.Render.UploadMultiTextureMesh(mesh));
		}
		renderinfo.ModelRef = meshref;
		if (transformEditMode)
		{
			return;
		}
		switch (target)
		{
		case EnumItemRenderTarget.Ground:
			if (cprops.GroundTransform != null)
			{
				renderinfo.Transform = cprops.GroundTransform;
			}
			break;
		case EnumItemRenderTarget.Gui:
			if (cprops.GuiTransform != null)
			{
				renderinfo.Transform = cprops.GuiTransform;
			}
			break;
		case EnumItemRenderTarget.HandTp:
			if (cprops.TpTransform != null)
			{
				renderinfo.Transform = cprops.TpTransform;
			}
			break;
		case EnumItemRenderTarget.HandFp:
		case EnumItemRenderTarget.HandTpOff:
			break;
		}
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack stack = base.OnPickBlock(world, pos);
		BEBehaviorShapeFromAttributes bect = GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
		if (bect != null)
		{
			stack.Attributes.SetString("type", bect.Type);
			if (bect.overrideTextureCode != null)
			{
				stack.Attributes.SetString("overrideTextureCode", bect.overrideTextureCode);
			}
		}
		return stack;
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num)
		{
			BEBehaviorShapeFromAttributes bect = GetBEBehavior<BEBehaviorShapeFromAttributes>(blockSel.Position);
			if (bect != null)
			{
				BlockPos targetPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
				double y = byPlayer.Entity.Pos.X - ((double)targetPos.X + blockSel.HitPosition.X);
				double dz = (double)(float)byPlayer.Entity.Pos.Z - ((double)targetPos.Z + blockSel.HitPosition.Z);
				float roundRad = (float)(int)Math.Round((float)Math.Atan2(y, dz) / rotInterval) * rotInterval;
				bect.rotateX = byItemStack.Attributes.GetFloat("rotX");
				bect.rotateY = byItemStack.Attributes.GetFloat("rotY", roundRad);
				bect.rotateZ = byItemStack.Attributes.GetFloat("rotZ");
				string otcode = byItemStack.Attributes.GetString("overrideTextureCode");
				if (otcode != null)
				{
					bect.overrideTextureCode = otcode;
				}
				bect.OnBlockPlaced(byItemStack);
			}
		}
		return num;
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		BEBehaviorShapeFromAttributes bes = GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
		if (bes != null)
		{
			IShapeTypeProps cprops = GetTypeProps(bes.Type, null, bes);
			if (cprops == null)
			{
				base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
				return;
			}
			blockModelData = GetOrCreateMesh(cprops, null, bes.overrideTextureCode).Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), bes.rotateX, bes.rotateY + cprops.Rotation.Y * ((float)Math.PI / 180f), bes.rotateZ);
			decalModelData = GetOrCreateMesh(cprops, decalTexSource, bes.overrideTextureCode).Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), bes.rotateX, bes.rotateY + cprops.Rotation.Y * ((float)Math.PI / 180f), bes.rotateZ);
		}
		else
		{
			base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
		}
	}

	public virtual MeshData GetOrCreateMesh(IShapeTypeProps cprops, ITexPositionSource overrideTexturesource = null, string overrideTextureCode = null)
	{
		Dictionary<string, MeshData> cMeshes = meshDictionary;
		ICoreClientAPI capi = api as ICoreClientAPI;
		if (overrideTexturesource != null || !cMeshes.TryGetValue(cprops.Code + "-" + overrideTextureCode, out var mesh))
		{
			mesh = new MeshData(4, 3);
			Shape shape = cprops.ShapeResolved;
			ITexPositionSource texSource = overrideTexturesource;
			if (texSource == null)
			{
				ShapeTextureSource stexSource = new ShapeTextureSource(capi, shape, cprops.ShapePath.ToString());
				texSource = stexSource;
				if (blockTextures != null)
				{
					foreach (KeyValuePair<string, CompositeTexture> val2 in blockTextures)
					{
						if (val2.Value.Baked == null)
						{
							val2.Value.Bake(capi.Assets);
						}
						stexSource.textures[val2.Key] = val2.Value;
					}
				}
				if (cprops.Textures != null)
				{
					foreach (KeyValuePair<string, CompositeTexture> val in cprops.Textures)
					{
						CompositeTexture ctex2 = val.Value.Clone();
						ctex2.Bake(capi.Assets);
						stexSource.textures[val.Key] = ctex2;
					}
				}
				if (overrideTextureCode != null && cprops.TextureFlipCode != null && OverrideTextureGroups[cprops.TextureFlipGroupCode].TryGetValue(overrideTextureCode, out var ctex))
				{
					ctex.Bake(capi.Assets);
					stexSource.textures[cprops.TextureFlipCode] = ctex;
				}
			}
			if (shape == null)
			{
				return mesh;
			}
			capi.Tesselator.TesselateShape(blockForLogging, shape, out mesh, texSource, null, 0, 0, 0);
			if (cprops.TexPos == null)
			{
				api.Logger.Warning("No texture previously loaded for clutter block " + cprops.Code);
				cprops.TexPos = (texSource as ShapeTextureSource)?.firstTexPos;
				cprops.TexPos.RndColors = new int[30];
			}
			if (overrideTexturesource == null)
			{
				cMeshes[cprops.Code + "-" + overrideTextureCode] = mesh;
			}
		}
		return mesh;
	}

	public override byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
	{
		if (pos == null)
		{
			string type = stack.Attributes.GetString("type", "");
			return GetTypeProps(type, stack, null)?.LightHsv ?? noLight;
		}
		BEBehaviorShapeFromAttributes bect = blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorShapeFromAttributes>();
		return GetTypeProps(bect?.Type, null, bect)?.LightHsv ?? noLight;
	}

	public override int GetColor(ICoreClientAPI capi, BlockPos pos)
	{
		BEBehaviorShapeFromAttributes bect = GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
		IShapeTypeProps cprops = GetTypeProps(bect?.Type, null, bect);
		if (cprops?.TexPos != null)
		{
			return cprops.TexPos.AvgColor;
		}
		return base.GetColor(capi, pos);
	}

	public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		BEBehaviorShapeFromAttributes bect = GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
		return GetTypeProps(bect?.Type, null, bect)?.CanAttachBlockAt(new Vec3f(bect.rotateX, bect.rotateY, bect.rotateZ), blockFace, attachmentArea) ?? base.CanAttachBlockAt(blockAccessor, block, pos, blockFace, attachmentArea);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		BEBehaviorShapeFromAttributes bect = GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
		IShapeTypeProps cprops = GetTypeProps(bect?.Type, null, bect);
		if (cprops?.TexPos != null)
		{
			return cprops.TexPos.RndColors[(rndIndex < 0) ? capi.World.Rand.Next(cprops.TexPos.RndColors.Length) : rndIndex];
		}
		return base.GetRandomColor(capi, pos, facing, rndIndex);
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		string type = itemStack.Attributes.GetString("type", "");
		return Lang.GetMatching(Code.Domain + ":" + ClassType + "-" + type.Replace("/", "-"));
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		BEBehaviorShapeFromAttributes bect = GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
		if (bect != null && bect.overrideTextureCode != null)
		{
			string name = Lang.GetMatchingIfExists(bect.GetFullCode() + "-" + bect.overrideTextureCode);
			if (name != null)
			{
				return name;
			}
		}
		return Lang.GetMatching(bect?.GetFullCode() ?? "Unknown");
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		BEBehaviorShapeFromAttributes bect = GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
		return base.GetPlacedBlockInfo(world, pos, forPlayer) + Lang.GetMatchingIfExists(Code.Domain + ":" + ClassType + "desc-" + bect?.Type?.Replace("/", "-"));
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		string type = inSlot.Itemstack.Attributes.GetString("type", "");
		string desc = Lang.GetIfExists(Code.Domain + ":" + ClassType + "desc-" + type.Replace("/", "-"));
		if (desc != null)
		{
			dsc.AppendLine(desc);
		}
	}

	public void Rotate(EntityAgent byEntity, BlockSelection blockSel, int dir)
	{
		GetBEBehavior<BEBehaviorShapeFromAttributes>(blockSel.Position).Rotate(byEntity, blockSel, dir);
	}

	public void FlipTexture(BlockPos pos, string newTextureCode)
	{
		BEBehaviorShapeFromAttributes bEBehavior = GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
		bEBehavior.overrideTextureCode = newTextureCode;
		bEBehavior.loadMesh();
		bEBehavior.Blockentity.MarkDirty(redrawOnClient: true);
	}

	public OrderedDictionary<string, CompositeTexture> GetAvailableTextures(BlockPos pos)
	{
		BEBehaviorShapeFromAttributes bect = GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
		IShapeTypeProps cprops = GetTypeProps(bect?.Type, null, bect);
		if (cprops != null && cprops.TextureFlipGroupCode != null)
		{
			return OverrideTextureGroups[cprops.TextureFlipGroupCode];
		}
		return null;
	}

	public virtual string BaseCodeForName()
	{
		return Code.Domain + ":" + ClassType + "-";
	}
}
