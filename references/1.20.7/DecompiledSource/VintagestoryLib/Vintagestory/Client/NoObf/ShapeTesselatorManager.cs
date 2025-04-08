using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Server;

namespace Vintagestory.Client.NoObf;

public class ShapeTesselatorManager : AsyncHelper.Multithreaded, ITesselatorManager
{
	public OrderedDictionary<AssetLocation, UnloadableShape> shapes;

	public OrderedDictionary<AssetLocation, IAsset> objs;

	public OrderedDictionary<AssetLocation, GltfType> gltfs;

	public MeshData[] blockModelDatas;

	public MeshData[][] altblockModelDatasLod0;

	public MeshData[][] altblockModelDatasLod1;

	public MeshData[][] altblockModelDatasLod2;

	public MultiTextureMeshRef[] blockModelRefsInventory;

	public MultiTextureMeshRef[] itemModelRefsInventory;

	public MultiTextureMeshRef[][] altItemModelRefsInventory;

	public MeshData unknownItemModelData = QuadMeshUtilExt.GetCustomQuadModelData(0f, 0f, 0f, 1f, 1f, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

	public MultiTextureMeshRef unknownItemModelRef;

	public MeshData unknownBlockModelData = CubeMeshUtil.GetCubeOnlyScaleXyz(0.5f, 0.5f, new Vec3f(0.5f, 0.5f, 0.5f));

	public MultiTextureMeshRef unknownBlockModelRef;

	private ClientMain game;

	internal volatile int finishedAsyncBlockTesselation;

	[ThreadStatic]
	private static ShapeTesselator TLTesselator;

	private UnloadableShape basicCubeShape;

	private bool itemloadingdone;

	private Dictionary<AssetLocation, UnloadableShape> shapes2;

	private Dictionary<AssetLocation, UnloadableShape> shapes3;

	private Dictionary<AssetLocation, UnloadableShape> shapes4;

	private Dictionary<AssetLocation, UnloadableShape> itemshapes;

	public ShapeTesselator Tesselator => TLTesselator ?? (TLTesselator = new ShapeTesselator(game, shapes, objs, gltfs));

	public MeshData GetDefaultBlockMesh(Block block)
	{
		return blockModelDatas[block.BlockId];
	}

	internal ITesselatorAPI GetNewTesselator()
	{
		return new ShapeTesselator(game, shapes, objs, gltfs);
	}

	public ShapeTesselatorManager(ClientMain game)
	{
		this.game = game;
		ClientEventManager em = game.eventManager;
		if (em != null)
		{
			em.OnReloadShapes += TesselateBlocksAndItems;
		}
	}

	public ShapeTesselatorManager(ServerMain server)
	{
	}

	private void TesselateBlocksAndItems()
	{
		PrepareToLoadShapes();
		LoadItemShapesAsync(game.Items);
		LoadBlockShapes(game.Blocks);
		TLTesselator = new ShapeTesselator(game, shapes, objs, gltfs);
		TesselateBlocks_Pre();
		TyronThreadPool.QueueTask(delegate
		{
			TesselateBlocks_Async(game.Blocks);
		});
		for (int i = 0; i < game.Blocks.Count; i = TesselateBlocksForInventory(game.Blocks, i, game.Blocks.Count))
		{
		}
		for (int i = 0; i < game.Items.Count; i = TesselateItems(game.Items, i, game.Items.Count))
		{
		}
		while (finishedAsyncBlockTesselation != 2 && !game.disposed)
		{
			Thread.Sleep(30);
		}
		LoadDone();
	}

	public void LoadDone()
	{
		if (ClientSettings.OptimizeRamMode != 2)
		{
			return;
		}
		foreach (KeyValuePair<AssetLocation, UnloadableShape> val in shapes)
		{
			if (val.Value != basicCubeShape)
			{
				val.Value.Unload();
			}
		}
	}

	public void PrepareToLoadShapes()
	{
		ResetThreading();
		shapes = new OrderedDictionary<AssetLocation, UnloadableShape>();
		shapes2 = new Dictionary<AssetLocation, UnloadableShape>();
		shapes3 = new Dictionary<AssetLocation, UnloadableShape>();
		shapes4 = new Dictionary<AssetLocation, UnloadableShape>();
		itemshapes = new Dictionary<AssetLocation, UnloadableShape>();
		objs = new OrderedDictionary<AssetLocation, IAsset>();
		gltfs = new OrderedDictionary<AssetLocation, GltfType>();
		shapes[new AssetLocation("block/basic/cube")] = BasicCube(game.api);
	}

	internal void LoadItemShapesAsync(IList<Item> items)
	{
		itemloadingdone = false;
		TyronThreadPool.QueueTask(delegate
		{
			LoadItemShapes(items);
		});
	}

	internal Dictionary<AssetLocation, UnloadableShape> LoadItemShapes(IList<Item> items)
	{
		try
		{
			HashSet<AssetLocationAndSource> shapelocations = new HashSet<AssetLocationAndSource>();
			for (int i = 0; i < items.Count; i++)
			{
				if (game.disposed)
				{
					return itemshapes;
				}
				Item item = items[i];
				if (item == null || item.Shape == null)
				{
					continue;
				}
				CompositeShape shape = item.Shape;
				if (!shape.VoxelizeTexture)
				{
					shapelocations.Add(new AssetLocationAndSource(shape.Base, "Shape for item ", item.Code));
					shape.LoadAlternates(game.api.Assets, game.Logger);
				}
				if (shape.BakedAlternates != null)
				{
					for (int k = 0; k < shape.BakedAlternates.Length; k++)
					{
						if (game.disposed)
						{
							return itemshapes;
						}
						if (!shape.BakedAlternates[k].VoxelizeTexture)
						{
							shapelocations.Add(new AssetLocationAndSource(shape.BakedAlternates[k].Base, "Alternate shape for item ", item.Code, k));
						}
					}
				}
				if (shape.Overlays == null)
				{
					continue;
				}
				for (int j = 0; j < shape.Overlays.Length; j++)
				{
					if (game.disposed)
					{
						return itemshapes;
					}
					if (!shape.Overlays[j].VoxelizeTexture)
					{
						shapelocations.Add(new AssetLocationAndSource(shape.Overlays[j].Base, "Overlay shape for item ", item.Code, j));
					}
				}
			}
			game.Platform.Logger.VerboseDebug("[LoadShapes] Searched through items...");
			LoadShapes(shapelocations, itemshapes, "for items");
		}
		finally
		{
			itemloadingdone = true;
		}
		return itemshapes;
	}

	internal OrderedDictionary<AssetLocation, UnloadableShape> LoadBlockShapes(IList<Block> blocks)
	{
		game.Platform.Logger.VerboseDebug("[LoadShapes] Searching through blocks...");
		int maxBlockId = blocks.Count;
		IDisposable[] refs = blockModelRefsInventory;
		DisposeArray(refs);
		blockModelDatas = new MeshData[maxBlockId + 1];
		altblockModelDatasLod0 = new MeshData[maxBlockId + 1][];
		altblockModelDatasLod1 = new MeshData[maxBlockId + 1][];
		altblockModelDatasLod2 = new MeshData[maxBlockId + 1][];
		blockModelRefsInventory = new MultiTextureMeshRef[maxBlockId + 1];
		CompositeShape basicCube = new CompositeShape
		{
			Base = new AssetLocation("block/basic/cube")
		};
		int availableCores = Environment.ProcessorCount / 2 - 3;
		availableCores = Math.Min(availableCores, 4);
		availableCores = Math.Max(availableCores, 2);
		TargetSet[] sets = new TargetSet[1];
		int count = 0;
		for (int j = 0; j < sets.Length; j++)
		{
			TargetSet set2 = new TargetSet();
			sets[j] = set2;
			int start = j * blocks.Count / sets.Length;
			int end = (j + 1) * blocks.Count / sets.Length;
			if (j < sets.Length - 1)
			{
				TyronThreadPool.QueueTask(delegate
				{
					CollectBlockShapes(blocks, start, end, set2, basicCube, ref count);
				}, "collectblockshapes");
			}
			else
			{
				CollectBlockShapes(blocks, start, end, set2, basicCube, ref count);
			}
		}
		HashSet<AssetLocationAndSource> shapelocations = new HashSet<AssetLocationAndSource>();
		HashSet<AssetLocationAndSource> objlocations = new HashSet<AssetLocationAndSource>();
		HashSet<AssetLocationAndSource> gltflocations = new HashSet<AssetLocationAndSource>();
		shapelocations.Add(new AssetLocationAndSource(basicCube.Base));
		foreach (TargetSet set in sets)
		{
			while (!set.finished && !game.disposed)
			{
				Thread.Sleep(10);
			}
			foreach (AssetLocationAndSource val3 in set.shapelocations)
			{
				shapelocations.Add(val3);
			}
			foreach (AssetLocationAndSource val2 in set.objlocations)
			{
				objlocations.Add(val2);
			}
			foreach (AssetLocationAndSource val in set.gltflocations)
			{
				gltflocations.Add(val);
			}
		}
		game.Platform.Logger.VerboseDebug("[LoadShapes] Searched through " + count + " blocks");
		while (WorkerThreadsInProgress() && !game.disposed)
		{
			Thread.Sleep(10);
		}
		game.Platform.Logger.VerboseDebug("[LoadShapes] Starting to parse block shapes...");
		if (availableCores >= 2)
		{
			StartWorkerThread(delegate
			{
				LoadShapes(shapelocations, shapes2, "(2nd block loading thread)");
			});
		}
		if (availableCores >= 3)
		{
			StartWorkerThread(delegate
			{
				LoadShapes(shapelocations, shapes3, "(3rd block loading thread)");
			});
		}
		if (availableCores >= 4)
		{
			StartWorkerThread(delegate
			{
				LoadShapes(shapelocations, shapes4, "(4th block loading thread)");
			});
		}
		LoadShapes(objlocations, gltflocations);
		LoadShapes(shapelocations, shapes, "for " + count + " blocks" + ((availableCores > 1) ? ", some others done offthread" : ""));
		FinalizeLoading();
		return shapes;
	}

	private void CollectBlockShapes(IList<Block> blocks, int start, int maxCount, TargetSet targetSet, CompositeShape basicCube, ref int totalCount)
	{
		int count = 0;
		try
		{
			for (int i = start; i < maxCount; i++)
			{
				if (game.disposed)
				{
					break;
				}
				Block block = blocks[i];
				if (block.Code == null)
				{
					continue;
				}
				count++;
				if (block.Shape == null || block.Shape.Base.Path.Length == 0)
				{
					block.Shape = basicCube;
				}
				else
				{
					CompositeShape shape = block.Shape;
					shape.LoadAlternates(game.api.Assets, game.Logger);
					targetSet.Add(shape, "Shape for block ", block.Code);
					if (shape.BakedAlternates != null)
					{
						for (int n = 0; n < shape.BakedAlternates.Length; n++)
						{
							if (game.disposed)
							{
								return;
							}
							CompositeShape alternateShape3 = shape.BakedAlternates[n];
							if (alternateShape3 != null && !(alternateShape3.Base == null))
							{
								targetSet.Add(alternateShape3, "Alternate shape for block ", block.Code, n);
							}
						}
					}
					if (block.Shape.Overlays != null)
					{
						for (int m = 0; m < block.Shape.Overlays.Length; m++)
						{
							if (game.disposed)
							{
								return;
							}
							CompositeShape overlayshape2 = block.Shape.Overlays[m];
							if (overlayshape2 != null && !(overlayshape2.Base == null))
							{
								targetSet.Add(overlayshape2, "Overlay shape for block ", block.Code, m);
							}
						}
					}
				}
				if (block.ShapeInventory != null)
				{
					if (game.disposed)
					{
						break;
					}
					targetSet.Add(block.ShapeInventory, "Inventory shape for block ", block.Code);
					if (block.ShapeInventory.Overlays != null)
					{
						for (int l = 0; l < block.ShapeInventory.Overlays.Length; l++)
						{
							if (game.disposed)
							{
								return;
							}
							CompositeShape overlayshape = block.ShapeInventory.Overlays[l];
							if (overlayshape != null && !(overlayshape.Base == null))
							{
								targetSet.Add(overlayshape, "Inventory overlay shape for block ", block.Code, l);
							}
						}
					}
				}
				if (block.Lod0Shape != null)
				{
					if (game.disposed)
					{
						break;
					}
					block.Lod0Shape.LoadAlternates(game.api.Assets, game.Logger);
					targetSet.Add(block.Lod0Shape, "Lod0 shape for block ", block.Code);
					if (block.Lod0Shape.BakedAlternates != null)
					{
						for (int k = 0; k < block.Lod0Shape.BakedAlternates.Length; k++)
						{
							if (game.disposed)
							{
								return;
							}
							CompositeShape alternateShape2 = block.Lod0Shape.BakedAlternates[k];
							if (alternateShape2 != null && !(alternateShape2.Base == null))
							{
								targetSet.Add(alternateShape2, "Alternate lod 0 for block ", block.Code, k);
							}
						}
					}
				}
				if (block.Lod2Shape == null)
				{
					continue;
				}
				if (game.disposed)
				{
					break;
				}
				block.Lod2Shape.LoadAlternates(game.api.Assets, game.Logger);
				targetSet.Add(block.Lod2Shape, "Lod2 shape for block ", block.Code);
				if (block.Lod2Shape.BakedAlternates == null)
				{
					continue;
				}
				for (int j = 0; j < block.Lod2Shape.BakedAlternates.Length; j++)
				{
					if (game.disposed)
					{
						return;
					}
					CompositeShape alternateShape = block.Lod2Shape.BakedAlternates[j];
					if (alternateShape != null && !(alternateShape.Base == null))
					{
						targetSet.Add(alternateShape, "Alternate lod 2 for block ", block.Code, j);
					}
				}
			}
		}
		finally
		{
			targetSet.finished = true;
			Interlocked.Add(ref totalCount, count);
		}
	}

	internal void FinalizeLoading()
	{
		while (!itemloadingdone || (WorkerThreadsInProgress() && !game.disposed))
		{
			Thread.Sleep(10);
		}
		ILogger logger = game.Platform.Logger;
		shapes.AddRange(shapes2, logger);
		shapes2.Clear();
		shapes2 = null;
		shapes.AddRange(shapes3, logger);
		shapes3.Clear();
		shapes3 = null;
		shapes.AddRange(shapes4, logger);
		shapes4.Clear();
		shapes4 = null;
		shapes.AddRange(itemshapes, logger);
		itemshapes = null;
		game.DoneBlockAndItemShapeLoading = true;
		logger.Notification("Collected {0} shapes to tesselate.", shapes.Count);
	}

	internal void LoadShapes(HashSet<AssetLocationAndSource> shapelocations, IDictionary<AssetLocation, UnloadableShape> shapes, string typeForLog)
	{
		int count = 0;
		foreach (AssetLocationAndSource srcandLoc in shapelocations)
		{
			if (game.disposed)
			{
				break;
			}
			if (AsyncHelper.CanProceedOnThisThread(ref srcandLoc.loadedAlready))
			{
				count++;
				UnloadableShape shape = new UnloadableShape();
				shape.Loaded = true;
				if (!shape.Load(game, srcandLoc))
				{
					shapes[srcandLoc] = basicCubeShape;
				}
				else
				{
					shapes[srcandLoc] = shape;
				}
			}
		}
		game.Platform.Logger.VerboseDebug("[LoadShapes] parsed " + count + " shapes from JSON " + typeForLog);
	}

	internal void LoadShapes(HashSet<AssetLocationAndSource> objlocations, HashSet<AssetLocationAndSource> gltflocations)
	{
		int count = 0;
		foreach (AssetLocationAndSource srcandLoc2 in objlocations)
		{
			if (game.disposed)
			{
				return;
			}
			AssetLocation newLocation2 = srcandLoc2.CopyWithPathPrefixAndAppendixOnce("shapes/", ".obj");
			IAsset asset = ScreenManager.Platform.AssetManager.TryGet(newLocation2);
			if (game.disposed)
			{
				return;
			}
			if (asset == null)
			{
				game.Platform.Logger.Warning("Did not find required obj {0} anywhere. (defined in {1})", newLocation2, srcandLoc2.Source);
			}
			else
			{
				objs[srcandLoc2] = asset;
				count++;
			}
		}
		foreach (AssetLocationAndSource srcandLoc in gltflocations)
		{
			if (game.disposed)
			{
				return;
			}
			AssetLocation newLocation = srcandLoc.CopyWithPathPrefixAndAppendixOnce("shapes/", ".gltf");
			IAsset asset = ScreenManager.Platform.AssetManager.TryGet(newLocation);
			if (game.disposed)
			{
				return;
			}
			if (asset == null)
			{
				game.Platform.Logger.Warning("Did not find required gltf {0} anywhere. (defined in {1})", newLocation, srcandLoc.Source);
			}
			else
			{
				gltfs[srcandLoc] = asset.ToObject<GltfType>();
				count++;
			}
		}
		if (count > 0)
		{
			game.Platform.Logger.VerboseDebug("[LoadShapes] loaded " + count + " block shapes in obj and gltf formats");
		}
	}

	private UnloadableShape BasicCube(ICoreAPI api)
	{
		if (basicCubeShape == null)
		{
			AssetLocation pathLoc = new AssetLocation("shapes/block/basic/cube.json");
			IAsset asset = api.Assets.TryGet(pathLoc);
			if (asset == null)
			{
				throw new Exception("Shape shapes/block/basic/cube.json not found, it is required to run the game");
			}
			ShapeElement.locationForLogging = pathLoc;
			basicCubeShape = asset.ToObject<UnloadableShape>();
			basicCubeShape.Loaded = true;
		}
		return basicCubeShape;
	}

	public void LoadEntityShapesAsync(IEnumerable<EntityProperties> entities, ICoreAPI api)
	{
		OnWorkerThread(delegate
		{
			LoadEntityShapes(entities, api);
		});
	}

	public void LoadEntityShapes(IEnumerable<EntityProperties> entities, ICoreAPI api)
	{
		Dictionary<AssetLocation, Shape> entityShapes = new Dictionary<AssetLocation, Shape>();
		entityShapes[new AssetLocation("block/basic/cube")] = BasicCube(api);
		api.Logger.VerboseDebug("Entity shape loading starting ...");
		foreach (EntityProperties val in entities)
		{
			if (game != null && game.disposed)
			{
				return;
			}
			if (val != null && val.Client != null)
			{
				try
				{
					LoadShape(val, api, entityShapes);
				}
				catch (Exception)
				{
					api.Logger.Error("Error while attempting to load shape file for entity: " + val.Code.ToShortString());
					throw;
				}
			}
		}
		api.Logger.VerboseDebug("Entity shape loading completed");
	}

	private void LoadShape(EntityProperties entity, ICoreAPI api, Dictionary<AssetLocation, Shape> entityShapes)
	{
		EntityClientProperties clientProperties = entity.Client;
		Shape shape = (clientProperties.LoadedShape = LoadEntityShape(clientProperties.Shape, entity.Code, api, entityShapes));
		if (api is ICoreServerAPI)
		{
			shape?.FreeRAMServer();
		}
		CompositeShape[] alternates = clientProperties.Shape?.Alternates;
		if (alternates == null)
		{
			return;
		}
		Shape[] loadedAlternates = (clientProperties.LoadedAlternateShapes = new Shape[alternates.Length]);
		for (int i = 0; i < alternates.Length; i++)
		{
			if (game != null && game.disposed)
			{
				break;
			}
			shape = (loadedAlternates[i] = LoadEntityShape(alternates[i], entity.Code, api, entityShapes));
			if (api is ICoreServerAPI)
			{
				shape?.FreeRAMServer();
			}
		}
	}

	private Shape LoadEntityShape(CompositeShape cShape, AssetLocation entityTypeForLogging, ICoreAPI api, Dictionary<AssetLocation, Shape> entityShapes)
	{
		if (cShape == null)
		{
			return null;
		}
		if (cShape.Base == null || cShape.Base.Path.Length == 0)
		{
			if (cShape == null || !cShape.VoxelizeTexture)
			{
				api.Logger.Warning("No entity shape supplied for entity {0}, using cube shape", entityTypeForLogging);
			}
			cShape.Base = new AssetLocation("block/basic/cube");
			return basicCubeShape;
		}
		if (entityShapes.TryGetValue(cShape.Base, out var entityShape))
		{
			if (entityShape == null)
			{
				api.Logger.Error("Entity shape for entity {0} not found or errored, was supposed to be at shapes/{1}.json. Entity will be invisible!", entityTypeForLogging, cShape.Base);
			}
			return entityShape;
		}
		AssetLocation shapePath = cShape.Base.CopyWithPath("shapes/" + cShape.Base.Path + ".json");
		entityShape = Shape.TryGet(api, shapePath);
		entityShapes[cShape.Base] = entityShape;
		if (entityShape == null)
		{
			api.Logger.Error("Entity shape for entity {0} not found or errored, was supposed to be at {1}. Entity will be invisible!", entityTypeForLogging, shapePath);
			return null;
		}
		entityShape.ResolveReferences(api.Logger, cShape.Base.ToString());
		if (api.Side == EnumAppSide.Client)
		{
			CacheInvTransforms(entityShape.Elements);
		}
		return entityShape;
	}

	private static void CacheInvTransforms(ShapeElement[] elements)
	{
		if (elements != null)
		{
			foreach (ShapeElement obj in elements)
			{
				obj.CacheInverseTransformMatrix();
				CacheInvTransforms(obj.Children);
			}
		}
	}

	public void TesselateBlocks_Pre()
	{
		if (unknownBlockModelRef == null)
		{
			unknownBlockModelRef = game.api.renderapi.UploadMultiTextureMesh(unknownBlockModelData);
		}
		if (shapes == null)
		{
			throw new Exception("Can't tesselate, shapes not loaded yet!");
		}
		finishedAsyncBlockTesselation = 0;
	}

	public int TesselateBlocksForInventory(IList<Block> blocks, int offset, int maxCount)
	{
		Stopwatch sw = new Stopwatch();
		sw.Start();
		int i = offset;
		int cnt = 0;
		for (; i < maxCount; i++)
		{
			Block block = blocks[i];
			if (!(block.Code == null) && block.BlockId != 0)
			{
				MeshData modeldataInv = TesselateBlockForInventory(block);
				blockModelRefsInventory[block.BlockId]?.Dispose();
				blockModelRefsInventory[block.BlockId] = game.api.renderapi.UploadMultiTextureMesh(modeldataInv);
				if (cnt++ % 4 == 0 && sw.ElapsedMilliseconds >= 60)
				{
					i++;
					break;
				}
			}
		}
		if (i == blocks.Count)
		{
			BlockTesselationHalfCompleted();
		}
		return i;
	}

	public void TesselateBlocksForInventory_ASync(IList<Block> blocks)
	{
		if (TLTesselator != null)
		{
			throw new Exception("A previous threadpool thread did not call ThreadDispose() when finished with the TesselatorManager");
		}
		MeshData[] meshes = new MeshData[blocks.Count];
		try
		{
			for (int i = 0; i < blocks.Count; i++)
			{
				Block block = blocks[i];
				if (!(block.Code == null) && block.BlockId != 0)
				{
					meshes[i] = TesselateBlockForInventory(block);
				}
			}
		}
		finally
		{
			game.EnqueueGameLaunchTask(delegate
			{
				FinishInventoryMeshes(meshes, 0);
			}, "blockInventoryTesselation");
			BlockTesselationHalfCompleted();
			ThreadDispose();
		}
	}

	private void FinishInventoryMeshes(MeshData[] meshes, int start)
	{
		Stopwatch sw = new Stopwatch();
		sw.Start();
		int cnt = 0;
		int i;
		for (i = start; i < meshes.Length; i++)
		{
			MeshData modeldataInv = meshes[i];
			if (modeldataInv == null)
			{
				continue;
			}
			if (modeldataInv == unknownBlockModelData)
			{
				blockModelRefsInventory[i] = unknownBlockModelRef;
			}
			else
			{
				blockModelRefsInventory[i]?.Dispose();
				blockModelRefsInventory[i] = game.api.renderapi.UploadMultiTextureMesh(modeldataInv);
			}
			if (cnt++ % 4 == 0 && sw.ElapsedMilliseconds >= 60)
			{
				game.EnqueueGameLaunchTask(delegate
				{
					FinishInventoryMeshes(meshes, i + 1);
				}, "blockInventoryTesselation");
				break;
			}
		}
	}

	public void TesselateBlocks_Async(IList<Block> blocks)
	{
		if (TLTesselator != null)
		{
			throw new Exception("A previous threadpool thread did not call ThreadDispose() when finished with the TesselatorManager");
		}
		try
		{
			for (int i = 0; i < blocks.Count; i++)
			{
				Block block = blocks[i];
				if (block != null && !(block.Code == null) && block.BlockId != 0)
				{
					TesselateBlock(block);
					CreateFastTextureAlternates(block);
				}
			}
		}
		finally
		{
			BlockTesselationHalfCompleted();
			ThreadDispose();
		}
	}

	private void BlockTesselationHalfCompleted()
	{
		if (Interlocked.Increment(ref finishedAsyncBlockTesselation) == 2)
		{
			game.Logger.Notification("Blocks tesselated");
			game.Logger.VerboseDebug("Server assets - done block tesselation");
		}
	}

	public static void CreateFastTextureAlternates(Block block)
	{
		BlockFacing[] aLLFACES;
		if (block.HasAlternates && block.DrawType != EnumDrawType.JSON)
		{
			BakedCompositeTexture[][] ftv2 = (block.FastTextureVariants = new BakedCompositeTexture[6][]);
			aLLFACES = BlockFacing.ALLFACES;
			foreach (BlockFacing facing2 in aLLFACES)
			{
				if (block.Textures.TryGetValue(facing2.Code, out var faceTexture2))
				{
					BakedCompositeTexture[] variants = faceTexture2.Baked.BakedVariants;
					if (variants != null && variants.Length != 0)
					{
						ftv2[facing2.Index] = variants;
					}
				}
			}
		}
		if (!block.HasTiles || block.DrawType == EnumDrawType.JSON)
		{
			return;
		}
		BakedCompositeTexture[][] ftv = (block.FastTextureVariants = new BakedCompositeTexture[6][]);
		aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing facing in aLLFACES)
		{
			if (block.Textures.TryGetValue(facing.Code, out var faceTexture))
			{
				BakedCompositeTexture[] tiles = faceTexture.Baked.BakedTiles;
				if (tiles != null && tiles.Length != 0)
				{
					ftv[facing.Index] = tiles;
				}
			}
		}
	}

	public void TesselateItems_Pre(IList<Item> itemtypes)
	{
		if (unknownItemModelRef == null)
		{
			CompositeTexture tex = new CompositeTexture(new AssetLocation("unknown"));
			tex.Bake(game.Platform.AssetManager);
			BakedBitmap bcBmp = TextureAtlasManager.LoadCompositeBitmap(game, new AssetLocationAndSource(tex.Baked.BakedName));
			unknownItemModelData = ShapeTesselator.VoxelizeTextureStatic(bcBmp.TexturePixels, bcBmp.Width, bcBmp.Height, game.BlockAtlasManager.UnknownTexturePos);
			unknownItemModelRef = game.api.renderapi.UploadMultiTextureMesh(unknownItemModelData);
		}
		if (itemModelRefsInventory == null)
		{
			itemModelRefsInventory = new MultiTextureMeshRef[itemtypes.Count];
		}
		if (altItemModelRefsInventory == null)
		{
			altItemModelRefsInventory = new MultiTextureMeshRef[itemtypes.Count][];
		}
	}

	public int TesselateItems(IList<Item> itemtypes, int offset, int maxCount)
	{
		Stopwatch sw = new Stopwatch();
		sw.Start();
		int cnt = 0;
		int i;
		for (i = offset; i < maxCount; i++)
		{
			Item item = itemtypes[i];
			if (item == null)
			{
				continue;
			}
			if (item.Code == null || ((item.FirstTexture == null || item.FirstTexture.Base.Path == "unknown") && item.Shape == null))
			{
				itemModelRefsInventory[item.ItemId] = unknownItemModelRef;
				continue;
			}
			Tesselator.TesselateItem(item, item.Shape, out var modeldata);
			if (itemModelRefsInventory[item.ItemId] != null)
			{
				itemModelRefsInventory[item.ItemId].Dispose();
			}
			itemModelRefsInventory[item.ItemId] = game.api.renderapi.UploadMultiTextureMesh(modeldata);
			if (item.Shape?.BakedAlternates != null)
			{
				if (altItemModelRefsInventory[item.ItemId] == null)
				{
					altItemModelRefsInventory[item.ItemId] = new MultiTextureMeshRef[item.Shape.BakedAlternates.Length];
				}
				for (int j = 0; item.Shape.BakedAlternates.Length > j; j++)
				{
					Tesselator.TesselateItem(item, item.Shape.BakedAlternates[j], out var modeldataalt);
					if (altItemModelRefsInventory[item.ItemId][j] != null)
					{
						altItemModelRefsInventory[item.ItemId][j].Dispose();
					}
					altItemModelRefsInventory[item.ItemId][j] = game.api.renderapi.UploadMultiTextureMesh(modeldataalt);
				}
			}
			if (cnt++ % 4 == 0 && sw.ElapsedMilliseconds >= 60)
			{
				i++;
				break;
			}
		}
		return i;
	}

	private void TesselateBlock(Block block)
	{
		if (block.IsMissing)
		{
			blockModelDatas[block.BlockId] = unknownBlockModelData;
			return;
		}
		TextureSource texSource = new TextureSource(game, game.BlockAtlasManager.Size, block);
		int altTextureCount = Tesselator.AltTexturesCount(block);
		int altShapeCount = ((block.Shape.BakedAlternates != null) ? block.Shape.BakedAlternates.Length : 0);
		int tilesCount = Tesselator.TileTexturesCount(block);
		block.HasAlternates = Math.Max(altTextureCount, altShapeCount) != 0;
		block.HasTiles = tilesCount > 0;
		if (block.Lod0Shape != null)
		{
			block.Lod0Mesh = Tesselate(texSource, block, block.Lod0Shape, altblockModelDatasLod0, altTextureCount, tilesCount);
			setLod0Flag(block.Lod0Mesh);
			MeshData[] alts = altblockModelDatasLod0[block.Id];
			int i = 0;
			while (alts != null && i < alts.Length)
			{
				setLod0Flag(alts[i]);
				i++;
			}
		}
		blockModelDatas[block.BlockId] = Tesselate(texSource, block, block.Shape, altblockModelDatasLod1, altTextureCount, tilesCount);
		if (block.Lod2Shape != null)
		{
			block.Lod2Mesh = Tesselate(texSource, block, block.Lod2Shape, altblockModelDatasLod2, altTextureCount, tilesCount);
		}
	}

	private MeshData TesselateBlockForInventory(Block block)
	{
		if (block.IsMissing)
		{
			return unknownBlockModelData;
		}
		TextureSource texSource = new TextureSource(game, game.BlockAtlasManager.Size, block, forInventory: true);
		texSource.blockShape = block.Shape;
		if (block.ShapeInventory != null)
		{
			texSource.blockShape = block.ShapeInventory;
		}
		MeshData modeldataInv;
		try
		{
			if (block.Shape.VoxelizeTexture)
			{
				BakedBitmap bcBmp = TextureAtlasManager.LoadCompositeBitmap(game, new AssetLocationAndSource(block.FirstTextureInventory.Baked.BakedName, "Block code ", block.Code));
				int textureSubId = block.FirstTextureInventory.Baked.TextureSubId;
				TextureAtlasPosition pos = game.BlockAtlasManager.TextureAtlasPositionsByTextureSubId[textureSubId];
				modeldataInv = ShapeTesselator.VoxelizeTextureStatic(bcBmp.TexturePixels, bcBmp.Width, bcBmp.Height, pos);
			}
			else
			{
				Tesselator.TesselateBlock(block, texSource.blockShape, out modeldataInv, texSource);
			}
		}
		catch (Exception e)
		{
			game.Platform.Logger.Error("Exception thrown when trying to tesselate block {0} with first texture {1}:", block, block.FirstTextureInventory?.Baked?.BakedName);
			game.Platform.Logger.Error(e);
			throw;
		}
		int count = modeldataInv.GetVerticesCount() / 4;
		for (int i = 0; i < count; i++)
		{
			byte[] climateColorMapIds = modeldataInv.ClimateColorMapIds;
			int curColorMapId = ((climateColorMapIds != null && climateColorMapIds.Length != 0) ? modeldataInv.ClimateColorMapIds[i] : 0);
			if (curColorMapId == 0)
			{
				continue;
			}
			JsonObject attributes = block.Attributes;
			if (attributes != null && attributes.IsTrue("ignoreTintInventory"))
			{
				continue;
			}
			string colorMap = game.ColorMaps.GetKeyAtIndex(curColorMapId - 1);
			byte[] tintBytes = ColorUtil.ToBGRABytes(game.WorldMap.ApplyColorMapOnRgba(colorMap, null, -1, 180, 138, flipRb: false));
			for (int j = 0; j < 4; j++)
			{
				int curVertex = i * 4 + j;
				for (int colind = 0; colind < 3; colind++)
				{
					int index = 4 * curVertex + colind;
					modeldataInv.Rgba[index] = (byte)(modeldataInv.Rgba[index] * tintBytes[colind] / 255);
				}
			}
		}
		modeldataInv.CompactBuffers();
		return modeldataInv;
	}

	private MeshData Tesselate(TextureSource texSource, Block block, CompositeShape shape, MeshData[][] altblockModelDatas, int altTextureCount, int tilesCount)
	{
		MeshData modeldata;
		try
		{
			Tesselator.TesselateBlock(block, shape, out modeldata, texSource);
		}
		catch (Exception e)
		{
			game.Platform.Logger.Error("Exception thrown when trying to tesselate block {0}:", block);
			game.Platform.Logger.Error(e);
			throw;
		}
		modeldata.CompactBuffers();
		int altShapeCount = ((shape.BakedAlternates != null) ? shape.BakedAlternates.Length : 0);
		int alternateCount = Math.Max(altTextureCount, altShapeCount);
		if (alternateCount != 0)
		{
			MeshData[] meshes = new MeshData[alternateCount];
			for (int i = 0; i < alternateCount; i++)
			{
				if (altTextureCount > 0)
				{
					texSource.UpdateVariant(block, i % altTextureCount);
				}
				CompositeShape altShape = ((altShapeCount == 0) ? shape : shape.BakedAlternates[i % altShapeCount]);
				Tesselator.TesselateBlock(block, altShape, out var altModeldata, texSource);
				altModeldata.CompactBuffers();
				meshes[i] = altModeldata;
			}
			altblockModelDatas[block.BlockId] = meshes;
		}
		else if (tilesCount != 0)
		{
			MeshData[] meshes2 = new MeshData[tilesCount];
			for (int j = 0; j < tilesCount; j++)
			{
				texSource.UpdateVariant(block, j % tilesCount);
				CompositeShape altShape2 = ((altShapeCount == 0) ? shape : shape.BakedAlternates[j % altShapeCount]);
				Tesselator.TesselateBlock(block, altShape2, out var altModeldata2, texSource);
				altModeldata2.CompactBuffers();
				meshes2[j] = altModeldata2;
			}
			altblockModelDatas[block.BlockId] = meshes2;
		}
		return modeldata;
	}

	private static void setLod0Flag(MeshData altModeldata)
	{
		for (int i = 0; i < altModeldata.FlagsCount; i++)
		{
			altModeldata.Flags[i] |= 4096;
		}
	}

	internal void Dispose()
	{
		IDisposable[] refs = blockModelRefsInventory;
		DisposeArray(refs);
		refs = itemModelRefsInventory;
		DisposeArray(refs);
		int i = 0;
		while (altItemModelRefsInventory != null && i < altItemModelRefsInventory.Length)
		{
			refs = altItemModelRefsInventory[i];
			DisposeArray(refs);
			i++;
		}
		unknownItemModelRef?.Dispose();
		unknownBlockModelRef?.Dispose();
		TLTesselator = null;
	}

	private void DisposeArray(IDisposable[] refs)
	{
		if (refs != null)
		{
			for (int i = 0; i < refs.Length; i++)
			{
				refs[i]?.Dispose();
			}
		}
	}

	public MultiTextureMeshRef GetDefaultBlockMeshRef(Block block)
	{
		return blockModelRefsInventory[block.Id];
	}

	public MultiTextureMeshRef GetDefaultItemMeshRef(Item item)
	{
		return itemModelRefsInventory[item.Id];
	}

	public Shape GetCachedShape(AssetLocation location)
	{
		shapes.TryGetValue(location, out var shape);
		if (shape != null && !shape.Loaded)
		{
			shape.Load(game, new AssetLocationAndSource(location));
		}
		return shape;
	}

	public void ThreadDispose()
	{
		TLTesselator = null;
	}
}
