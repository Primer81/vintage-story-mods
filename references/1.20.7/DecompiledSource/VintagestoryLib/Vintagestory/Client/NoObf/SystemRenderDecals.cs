using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class SystemRenderDecals : ClientSystem
{
	private MeshDataPool decalPool;

	public MeshData[][] decalModeldatas;

	private LoadedTexture decalTextureAtlas;

	private int nextDecalId = 1;

	private Size2i decalAtlasSize = new Size2i(512, 512);

	internal static int decalPoolSize = 200;

	internal Dictionary<int, BlockDecal> decals = new Dictionary<int, BlockDecal>(decalPoolSize);

	internal TextureAtlasPosition[] DecalTextureAtlasPositionsByTextureSubId;

	internal Dictionary<string, int> TextureNameToIdMapping;

	private Vec3d decalOrigin = new Vec3d();

	private float[] floatpool = new float[4];

	private bool[] leavesWaveTileSide = new bool[6];

	public override string Name => "rede";

	public SystemRenderDecals(ClientMain game)
		: base(game)
	{
		game.api.ChatCommands.GetOrCreate("debug").BeginSubCommand("spawndecal").WithDescription("Spawn a decal at position")
			.HandleWith(OnSpawnDecal)
			.EndSubCommand();
		game.eventManager.OnPlayerBreakingBlock.Add(OnPlayerBreakingBlock);
		game.eventManager.OnUnBreakingBlock.Add(OnUnBreakingBlock);
		game.eventManager.OnPlayerBrokenBlock.Add(OnPlayerBrokenBlock);
		game.RegisterGameTickListener(OnGameTick, 500);
		game.eventManager.OnBlockChanged.Add(OnBlockChanged);
		game.eventManager.OnReloadShapes += TesselateDecalsFromBlockShapes;
		game.eventManager.RegisterRenderer(OnRenderFrame3D, EnumRenderStage.AfterOIT, "decals", 0.5);
	}

	public override void OnBlockTexturesLoaded()
	{
		InitAtlasAndModelPool();
		TesselateDecalsFromBlockShapes();
	}

	private void TesselateDecalsFromBlockShapes()
	{
		decalModeldatas = new MeshData[game.Blocks.Count][];
	}

	private void tesselateBlockDecal(int blockId)
	{
		Block block = game.Blocks[blockId];
		TextureSource texSource = new TextureSource(game, decalAtlasSize, block);
		texSource.isDecalUv = true;
		int altShapeCount = ((block.Shape.BakedAlternates != null) ? block.Shape.BakedAlternates.Length : 0);
		try
		{
			if (altShapeCount > 0)
			{
				decalModeldatas[block.BlockId] = new MeshData[block.Shape.BakedAlternates.Length];
				for (int i = 0; i < block.Shape.BakedAlternates.Length; i++)
				{
					MeshData altModeldata = null;
					game.TesselatorManager.Tesselator.TesselateBlock(block, block.Shape.BakedAlternates[i % altShapeCount], out altModeldata, texSource);
					addLod0Mesh(altModeldata, block, texSource, i);
					decalModeldatas[block.BlockId][i] = altModeldata;
				}
			}
			else
			{
				MeshData altModeldata2 = null;
				game.TesselatorManager.Tesselator.TesselateBlock(block, block.Shape, out altModeldata2, texSource);
				addLod0Mesh(altModeldata2, block, texSource, 0);
				decalModeldatas[block.BlockId] = new MeshData[1] { altModeldata2 };
			}
			MeshData[] array = decalModeldatas[block.BlockId];
			foreach (MeshData mesh in array)
			{
				addZOffset(block, mesh);
			}
		}
		catch (Exception e)
		{
			game.Platform.Logger.Error("Exception thrown when trying to tesselate block for decal system {0}. Will use invisible decal.", block);
			game.Platform.Logger.Error(e);
			decalModeldatas[block.BlockId] = new MeshData[1]
			{
				new MeshData(4, 6)
			};
		}
	}

	private void addZOffset(Block block, MeshData mesh)
	{
		int zoffs = block.VertexFlags.ZOffset << 8;
		for (int i = 0; i < mesh.FlagsCount; i++)
		{
			mesh.Flags[i] |= zoffs;
		}
	}

	private void addLod0Mesh(MeshData altModeldata, Block block, TextureSource texSource, int alternateIndex)
	{
		if (block.Lod0Shape != null)
		{
			game.TesselatorManager.Tesselator.TesselateBlock(block, block.Lod0Shape.BakedAlternates[alternateIndex], out var lod0DecalMesh, texSource);
			altModeldata.AddMeshData(lod0DecalMesh);
		}
	}

	private TextCommandResult OnSpawnDecal(TextCommandCallingArgs textCommandCallingArgs)
	{
		if (game.BlockSelection != null)
		{
			AddBlockBreakDecal(game.BlockSelection.Position, 3);
		}
		return TextCommandResult.Success();
	}

	private void OnGameTick(float dt)
	{
	}

	private void OnBlockChanged(BlockPos pos, Block oldBlock)
	{
		if (decals.Count == 0)
		{
			return;
		}
		List<int> foundDecals = new List<int>();
		foreach (KeyValuePair<int, BlockDecal> val in decals)
		{
			if (val.Value.pos.Equals(pos))
			{
				foundDecals.Add(val.Key);
			}
		}
		foreach (int decalid in foundDecals)
		{
			BlockDecal decal = decals[decalid];
			if (decal.PoolLocation != null)
			{
				decalPool.RemoveLocation(decal.PoolLocation);
			}
			decal.PoolLocation = null;
			decals.Remove(decalid);
		}
	}

	private void OnPlayerBrokenBlock(BlockDamage blockDamage)
	{
		if (blockDamage.DecalId != 0)
		{
			decals.TryGetValue(blockDamage.DecalId, out var decal);
			if (decal != null && decal.PoolLocation != null)
			{
				decalPool.RemoveLocation(decal.PoolLocation);
				decal.PoolLocation = null;
			}
			decals.Remove(blockDamage.DecalId);
		}
	}

	private void OnUnBreakingBlock(BlockDamage blockDamage)
	{
		if (blockDamage.DecalId != 0)
		{
			if (blockDamage.RemainingResistance >= blockDamage.Block.GetResistance(game.BlockAccessor, blockDamage.Position))
			{
				OnPlayerBrokenBlock(blockDamage);
			}
			else
			{
				OnPlayerBreakingBlock(blockDamage);
			}
		}
	}

	private void OnPlayerBreakingBlock(BlockDamage blockDamage)
	{
		float resi = blockDamage.Block.GetResistance(game.BlockAccessor, blockDamage.Position);
		if (blockDamage.RemainingResistance == resi)
		{
			return;
		}
		if (blockDamage.DecalId == 0 || !decals.ContainsKey(blockDamage.DecalId))
		{
			BlockDecal decal = AddBlockBreakDecal(blockDamage.Position, 0);
			if (decal != null)
			{
				blockDamage.DecalId = decal.DecalId;
			}
			return;
		}
		BlockDecal decal2 = decals[blockDamage.DecalId];
		int stages = 10;
		int animationStage = decal2.AnimationStage;
		int stage = (int)((float)stages * (resi - blockDamage.RemainingResistance) / resi);
		decal2.AnimationStage = GameMath.Clamp(stage, 1, stages - 1);
		decal2.LastModifiedMilliseconds = game.ElapsedMilliseconds;
		if (animationStage != decal2.AnimationStage)
		{
			UpdateDecal(decal2);
		}
	}

	internal BlockDecal AddBlockBreakDecal(BlockPos pos, int stage)
	{
		BlockDecal decal = new BlockDecal
		{
			AnimationStage = stage,
			DecalId = nextDecalId++,
			pos = pos.Copy(),
			LastModifiedMilliseconds = game.ElapsedMilliseconds
		};
		if (UpdateDecal(decal))
		{
			decals.Add(decal.DecalId, decal);
			return decal;
		}
		return null;
	}

	internal bool UpdateDecal(BlockDecal decal)
	{
		if (decal.PoolLocation != null)
		{
			decalPool.RemoveLocation(decal.PoolLocation);
		}
		TextureNameToIdMapping.TryGetValue("destroy_stage_" + decal.AnimationStage + ".png", out var textureSubId);
		Block block = game.WorldMap.RelaxedBlockAccess.GetBlock(decal.pos);
		if (block.BlockId == 0)
		{
			decal.PoolLocation = null;
			decals.Remove(decal.DecalId);
			return false;
		}
		if (decalModeldatas[block.BlockId] == null)
		{
			tesselateBlockDecal(block.BlockId);
		}
		MeshData decalModelData;
		MeshData blockModelData;
		if (block.HasAlternates)
		{
			int k2 = GameMath.MurmurHash3(decal.pos.X, (block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? decal.pos.Y : 0, decal.pos.Z);
			int index = GameMath.Mod(k2, decalModeldatas[block.BlockId].Length);
			decalModelData = decalModeldatas[block.BlockId][index].Clone();
			int index2 = GameMath.Mod(k2, game.TesselatorManager.altblockModelDatasLod1[block.BlockId].Length);
			blockModelData = game.TesselatorManager.altblockModelDatasLod1[block.BlockId][index2];
			if (block.Lod0Shape != null)
			{
				blockModelData = blockModelData.Clone();
				blockModelData.AddMeshData(game.TesselatorManager.altblockModelDatasLod0[block.BlockId][index2]);
			}
		}
		else
		{
			decalModelData = decalModeldatas[block.BlockId][0].Clone();
			blockModelData = game.TesselatorManager.blockModelDatas[block.BlockId];
			if (block.Lod0Shape != null)
			{
				blockModelData = blockModelData.Clone();
				blockModelData.AddMeshData(block.Lod0Mesh);
			}
		}
		TextureSource texSource = new TextureSource(game, decalAtlasSize, block);
		texSource.isDecalUv = true;
		block.GetDecal(game, decal.pos, texSource, ref decalModelData, ref blockModelData);
		decalModelData.CustomFloats = new CustomMeshDataPartFloat(4 * decalModelData.VerticesCount)
		{
			InterleaveSizes = new int[3] { 2, 2, 2 },
			InterleaveStride = 24,
			InterleaveOffsets = new int[3] { 0, 8, 16 }
		};
		if (decalModelData.VerticesCount == 0)
		{
			decal.PoolLocation = null;
			return false;
		}
		double offX = 0.0;
		double offZ = 0.0;
		if (block.RandomDrawOffset != 0)
		{
			offX = (float)(GameMath.oaatHash(decal.pos.X, 0, decal.pos.Z) % 12) / (24f + 12f * (float)block.RandomDrawOffset);
			offZ = (float)(GameMath.oaatHash(decal.pos.X, 1, decal.pos.Z) % 12) / (24f + 12f * (float)block.RandomDrawOffset);
		}
		if (block.RandomizeRotations)
		{
			int rnd = GameMath.MurmurHash3Mod(-decal.pos.X, (block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? decal.pos.Y : 0, decal.pos.Z, TesselationMetaData.randomRotations.Length);
			decalModelData = decalModelData.MatrixTransform(TesselationMetaData.randomRotMatrices[rnd], floatpool);
		}
		int lightrgbs = game.WorldMap.GetLightRGBsAsInt(decal.pos.X, decal.pos.Y, decal.pos.Z);
		for (int k = 0; k < 6; k++)
		{
			BlockFacing face = BlockFacing.ALLFACES[k];
			Block nblock = game.BlockAccessor.GetBlockOnSide(decal.pos, face);
			leavesWaveTileSide[k] = !nblock.SideSolid[face.Opposite.Index] || nblock.BlockMaterial == EnumBlockMaterial.Leaves;
		}
		byte sunBright = (byte)(lightrgbs >> 24);
		byte blockR = (byte)(lightrgbs >> 16);
		byte blockG = (byte)(lightrgbs >> 8);
		byte blockB = (byte)lightrgbs;
		int uvIndex = 0;
		int rgbaIndex = 0;
		int xyzIndex = 0;
		for (int j = 0; j < decalModelData.VerticesCount; j++)
		{
			TextureAtlasPosition decalTexPos = DecalTextureAtlasPositionsByTextureSubId[textureSubId];
			decalModelData.Uv[uvIndex++] += decalTexPos.x1;
			decalModelData.Uv[uvIndex++] += decalTexPos.y1;
			if (blockModelData.UvCount > 2 * j + 1)
			{
				decalModelData.CustomFloats.Add(blockModelData.Uv[2 * j]);
				decalModelData.CustomFloats.Add(blockModelData.Uv[2 * j + 1]);
			}
			decalModelData.CustomFloats.Add(decalTexPos.x2 - decalTexPos.x1);
			decalModelData.CustomFloats.Add(decalTexPos.y2 - decalTexPos.y1);
			decalModelData.CustomFloats.Add(decalTexPos.x1);
			decalModelData.CustomFloats.Add(decalTexPos.y1);
			decalModelData.Rgba[rgbaIndex++] = blockR;
			decalModelData.Rgba[rgbaIndex++] = blockG;
			decalModelData.Rgba[rgbaIndex++] = blockB;
			decalModelData.Rgba[rgbaIndex++] = sunBright;
			decalModelData.Flags[j] = decalModelData.Flags[j];
		}
		block.OnDecalTesselation(game, decalModelData, decal.pos);
		for (int i = 0; i < decalModelData.VerticesCount; i++)
		{
			decalModelData.xyz[xyzIndex++] += (float)((double)decal.pos.X + offX - decalOrigin.X);
			decalModelData.xyz[xyzIndex++] += (float)((double)decal.pos.Y - decalOrigin.Y);
			decalModelData.xyz[xyzIndex++] += (float)((double)decal.pos.Z + offZ - decalOrigin.Z);
		}
		Sphere boundingSphere = Sphere.BoundingSphereForCube(decal.pos.X, decal.pos.Y, decal.pos.Z, 1f);
		if ((decal.PoolLocation = decalPool.TryAdd(game.api, decalModelData, null, 0, boundingSphere)) == null)
		{
			return false;
		}
		return true;
	}

	internal void UpdateAllDecals()
	{
		foreach (BlockDecal decal in new List<BlockDecal>(decals.Values))
		{
			UpdateDecal(decal);
		}
	}

	public void OnRenderFrame3D(float deltaTime)
	{
		Vec3d playerPos = game.EntityPlayer.CameraPos;
		if (decalOrigin.SquareDistanceTo(playerPos) > 1000000f)
		{
			decalOrigin = playerPos.Clone();
			UpdateAllDecals();
		}
		if (decals.Count > 0)
		{
			game.GlPushMatrix();
			game.GlLoadMatrix(game.MainCamera.CameraMatrixOrigin);
			game.Platform.GlToggleBlend(on: true);
			game.Platform.GlDisableCullFace();
			ShaderProgramDecals shaderProgramDecals = ShaderPrograms.Decals;
			shaderProgramDecals.Use();
			shaderProgramDecals.WindWaveCounter = game.shUniforms.WindWaveCounter;
			shaderProgramDecals.WindWaveCounterHighFreq = game.shUniforms.WindWaveCounterHighFreq;
			shaderProgramDecals.BlockTexture2D = game.BlockAtlasManager.AtlasTextures[0].TextureId;
			shaderProgramDecals.DecalTexture2D = decalTextureAtlas.TextureId;
			shaderProgramDecals.RgbaFogIn = game.AmbientManager.BlendedFogColor;
			shaderProgramDecals.RgbaAmbientIn = game.AmbientManager.BlendedAmbientColor;
			shaderProgramDecals.FogDensityIn = game.AmbientManager.BlendedFogDensity;
			shaderProgramDecals.FogMinIn = game.AmbientManager.BlendedFogMin;
			shaderProgramDecals.Origin = new Vec3f((float)(decalOrigin.X - playerPos.X), (float)(decalOrigin.Y - playerPos.Y), (float)(decalOrigin.Z - playerPos.Z));
			shaderProgramDecals.ProjectionMatrix = game.CurrentProjectionMatrix;
			shaderProgramDecals.ModelViewMatrix = game.CurrentModelViewMatrix;
			decalPool.Draw(game.api, game.frustumCuller, EnumFrustumCullMode.CullInstant);
			shaderProgramDecals.Stop();
			game.Platform.GlToggleBlend(on: true);
			game.Platform.GlEnableCullFace();
			game.GlPopMatrix();
		}
	}

	private void InitAtlasAndModelPool()
	{
		List<IAsset> assets = game.Platform.AssetManager.GetManyInCategory("textures", "decal/");
		int size = game.textureSize * (int)Math.Ceiling(Math.Sqrt(assets.Count));
		decalAtlasSize = new Size2i(size, size);
		game.Logger.Notification("Texture size is {0} so decal atlas size of {1}x{2} should suffice", game.textureSize, decalAtlasSize.Width, decalAtlasSize.Height);
		TextureAtlas decalAtlas = new TextureAtlas(decalAtlasSize.Width, decalAtlasSize.Height, 0f, 0f);
		DecalTextureAtlasPositionsByTextureSubId = new TextureAtlasPosition[assets.Count];
		TextureNameToIdMapping = new Dictionary<string, int>();
		for (int i = 0; i < assets.Count; i++)
		{
			if (!decalAtlas.InsertTexture(i, game.api, assets[i]))
			{
				throw new Exception("Texture decal atlas overflow. Did you create a high res texture pack without setting the correct textureSize value in the modinfo.json?");
			}
			TextureNameToIdMapping[assets[i].Name] = i;
		}
		decalTextureAtlas = decalAtlas.Upload(game);
		game.Platform.BuildMipMaps(decalTextureAtlas.TextureId);
		decalAtlas.PopulateAtlasPositions(DecalTextureAtlasPositionsByTextureSubId, 0);
		int quantityVertices = decalPoolSize * 24 * 10;
		CustomMeshDataPartFloat customMeshDataPartFloat = new CustomMeshDataPartFloat();
		customMeshDataPartFloat.Instanced = false;
		customMeshDataPartFloat.StaticDraw = true;
		customMeshDataPartFloat.InterleaveSizes = new int[3] { 2, 2, 2 };
		customMeshDataPartFloat.InterleaveStride = 24;
		customMeshDataPartFloat.InterleaveOffsets = new int[3] { 0, 8, 16 };
		customMeshDataPartFloat.Count = quantityVertices;
		CustomMeshDataPartFloat blockUvFloats = customMeshDataPartFloat;
		decalPool = MeshDataPool.AllocateNewPool(game.api, quantityVertices, (int)((float)quantityVertices * 1.5f), 2 * decalPoolSize, blockUvFloats);
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}

	public override void Dispose(ClientMain game)
	{
		decalPool?.Dispose(game.api);
		decalTextureAtlas?.Dispose();
	}
}
