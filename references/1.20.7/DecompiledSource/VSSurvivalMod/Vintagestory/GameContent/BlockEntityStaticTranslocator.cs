using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityStaticTranslocator : BlockEntityTeleporterBase
{
	public int MinTeleporterRangeInBlocks = 400;

	public int MaxTeleporterRangeInBlocks = 8000;

	public BlockPos tpLocation;

	private BlockStaticTranslocator ownBlock;

	private Vec3d posvec;

	private ICoreServerAPI sapi;

	public int repairState;

	private bool activated;

	private bool canTeleport;

	public bool findNextChunk = true;

	public ILoadedSound translocatingSound;

	private float particleAngle;

	private float translocVolume;

	private float translocPitch;

	private long somebodyIsTeleportingReceivedTotalMs;

	public int RepairInteractionsRequired = 4;

	public bool Activated => true;

	public BlockPos TargetLocation => tpLocation;

	public bool FullyRepaired => repairState >= RepairInteractionsRequired;

	private BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>().animUtil;

	public override Vec3d GetTarget(Entity forEntity)
	{
		return tpLocation.ToVec3d().Add(-0.3, 1.0, -0.3);
	}

	public BlockEntityStaticTranslocator()
	{
		TeleportWarmupSec = 4.4f;
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (FullyRepaired)
		{
			setupGameTickers();
		}
		ownBlock = base.Block as BlockStaticTranslocator;
		posvec = new Vec3d((double)Pos.X + 0.5, Pos.Y, (double)Pos.Z + 0.5);
		if (api.World.Side == EnumAppSide.Client)
		{
			float rotY = base.Block.Shape.rotateY;
			animUtil.InitializeAnimator("translocator", null, null, new Vec3f(0f, rotY, 0f));
			updateSoundState();
		}
	}

	public void updateSoundState()
	{
		if (translocVolume > 0f)
		{
			startSound();
		}
		else
		{
			stopSound();
		}
	}

	public void startSound()
	{
		if (translocatingSound == null)
		{
			ICoreAPI api = Api;
			if (api != null && api.Side == EnumAppSide.Client)
			{
				translocatingSound = (Api as ICoreClientAPI).World.LoadSound(new SoundParams
				{
					Location = new AssetLocation("sounds/effect/translocate-active.ogg"),
					ShouldLoop = true,
					Position = Pos.ToVec3f(),
					RelativePosition = false,
					DisposeOnFinish = false,
					Volume = 0.5f
				});
				translocatingSound.Start();
			}
		}
	}

	public void stopSound()
	{
		if (translocatingSound != null)
		{
			translocatingSound.Stop();
			translocatingSound.Dispose();
			translocatingSound = null;
		}
	}

	public void DoActivate()
	{
		activated = true;
		canTeleport = true;
		MarkDirty(redrawOnClient: true);
	}

	public void DoRepair(IPlayer byPlayer)
	{
		if (FullyRepaired)
		{
			return;
		}
		if (repairState == 1)
		{
			int tlgearCostTrait = GameMath.RoundRandom(Api.World.Rand, byPlayer.Entity.Stats.GetBlended("temporalGearTLRepairCost") - 1f);
			if (tlgearCostTrait < 0)
			{
				repairState += -tlgearCostTrait;
				RepairInteractionsRequired = 4;
			}
			RepairInteractionsRequired += Math.Max(0, tlgearCostTrait);
		}
		repairState++;
		MarkDirty(redrawOnClient: true);
		if (FullyRepaired)
		{
			activated = true;
			setupGameTickers();
		}
	}

	public void setupGameTickers()
	{
		if (Api.Side == EnumAppSide.Server)
		{
			sapi = Api as ICoreServerAPI;
			RegisterGameTickListener(OnServerGameTick, 250);
		}
		else
		{
			RegisterGameTickListener(OnClientGameTick, 50);
		}
	}

	public override void OnEntityCollide(Entity entity)
	{
		if (FullyRepaired && Activated && canTeleport)
		{
			base.OnEntityCollide(entity);
		}
	}

	private void OnClientGameTick(float dt)
	{
		if (ownBlock == null || Api?.World == null || !canTeleport || !Activated)
		{
			return;
		}
		if (Api.World.ElapsedMilliseconds - somebodyIsTeleportingReceivedTotalMs > 6000)
		{
			somebodyIsTeleporting = false;
		}
		HandleSoundClient(dt);
		int num;
		int num2;
		if (Api.World.ElapsedMilliseconds > 100)
		{
			num = ((Api.World.ElapsedMilliseconds - lastOwnPlayerCollideMs < 100) ? 1 : 0);
			if (num != 0)
			{
				num2 = 1;
				goto IL_009a;
			}
		}
		else
		{
			num = 0;
		}
		num2 = (somebodyIsTeleporting ? 1 : 0);
		goto IL_009a;
		IL_009a:
		bool playerInside = (byte)num2 != 0;
		bool active = animUtil.activeAnimationsByAnimCode.ContainsKey("teleport");
		if (num == 0 && playerInside)
		{
			manager.lastTranslocateCollideMsOtherPlayer = Api.World.ElapsedMilliseconds;
		}
		SimpleParticleProperties currentParticles = (active ? ownBlock.insideParticles : ownBlock.idleParticles);
		if (playerInside)
		{
			AnimationMetaData meta = new AnimationMetaData
			{
				Animation = "teleport",
				Code = "teleport",
				AnimationSpeed = 1f,
				EaseInSpeed = 1f,
				EaseOutSpeed = 2f,
				Weight = 1f,
				BlendMode = EnumAnimationBlendMode.Add
			};
			animUtil.StartAnimation(meta);
			animUtil.StartAnimation(new AnimationMetaData
			{
				Animation = "idle",
				Code = "idle",
				AnimationSpeed = 1f,
				EaseInSpeed = 1f,
				EaseOutSpeed = 1f,
				Weight = 1f,
				BlendMode = EnumAnimationBlendMode.Average
			});
		}
		else
		{
			animUtil.StopAnimation("teleport");
		}
		if (animUtil.activeAnimationsByAnimCode.Count > 0 && Api.World.ElapsedMilliseconds - lastOwnPlayerCollideMs > 10000 && Api.World.ElapsedMilliseconds - manager.lastTranslocateCollideMsOtherPlayer > 10000)
		{
			animUtil.StopAnimation("idle");
		}
		int r = 53;
		int g = 221;
		int b = 172;
		currentParticles.Color = (r << 16) | (g << 8) | b | 0x32000000;
		currentParticles.AddPos.Set(0.0, 0.0, 0.0);
		currentParticles.BlueEvolve = null;
		currentParticles.RedEvolve = null;
		currentParticles.GreenEvolve = null;
		currentParticles.MinSize = 0.1f;
		currentParticles.MaxSize = 0.2f;
		currentParticles.SizeEvolve = null;
		currentParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 100f);
		bool rot = base.Block.Variant["side"] == "east" || base.Block.Variant["side"] == "west";
		particleAngle = (active ? (particleAngle + 5f * dt) : 0f);
		double dx = GameMath.Cos(particleAngle + (rot ? ((float)Math.PI / 2f) : 0f)) * 0.35f;
		double dy = 1.9 + Api.World.Rand.NextDouble() * 0.2;
		double dz = GameMath.Sin(particleAngle + (rot ? ((float)Math.PI / 2f) : 0f)) * 0.35f;
		currentParticles.LifeLength = GameMath.Sqrt(dx * dx + dz * dz) / 10f;
		currentParticles.MinPos.Set(posvec.X + dx, posvec.Y + dy, posvec.Z + dz);
		currentParticles.MinVelocity.Set((0f - (float)dx) / 2f, -1f - (float)Api.World.Rand.NextDouble() / 2f, (0f - (float)dz) / 2f);
		currentParticles.MinQuantity = (active ? 3f : 0.25f);
		currentParticles.AddVelocity.Set(0f, 0f, 0f);
		currentParticles.AddQuantity = 0.5f;
		Api.World.SpawnParticles(currentParticles);
		currentParticles.MinPos.Set(posvec.X - dx, posvec.Y + dy, posvec.Z - dz);
		currentParticles.MinVelocity.Set((float)dx / 2f, -1f - (float)Api.World.Rand.NextDouble() / 2f, (float)dz / 2f);
		Api.World.SpawnParticles(currentParticles);
	}

	protected virtual void HandleSoundClient(float dt)
	{
		ICoreClientAPI capi = Api as ICoreClientAPI;
		bool ownTranslocate = capi.World.ElapsedMilliseconds - lastOwnPlayerCollideMs <= 200;
		bool otherTranslocate = capi.World.ElapsedMilliseconds - lastEntityCollideMs <= 200;
		if (ownTranslocate || otherTranslocate)
		{
			translocVolume = Math.Min(0.5f, translocVolume + dt / 3f);
			translocPitch = Math.Min(translocPitch + dt / 3f, 2.5f);
			if (ownTranslocate)
			{
				capi.World.AddCameraShake(0.0575f);
			}
		}
		else
		{
			translocVolume = Math.Max(0f, translocVolume - 2f * dt);
			translocPitch = Math.Max(translocPitch - dt, 0.5f);
		}
		updateSoundState();
		if (translocVolume > 0f)
		{
			translocatingSound.SetVolume(translocVolume);
			translocatingSound.SetPitch(translocPitch);
		}
	}

	private void OnServerGameTick(float dt)
	{
		if (findNextChunk)
		{
			findNextChunk = false;
			int addrange = MaxTeleporterRangeInBlocks - MinTeleporterRangeInBlocks;
			int dx = (int)((double)MinTeleporterRangeInBlocks + sapi.World.Rand.NextDouble() * (double)addrange) * (2 * sapi.World.Rand.Next(2) - 1);
			int dz = (int)((double)MinTeleporterRangeInBlocks + sapi.World.Rand.NextDouble() * (double)addrange) * (2 * sapi.World.Rand.Next(2) - 1);
			int chunkX = (Pos.X + dx) / 32;
			int chunkZ = (Pos.Z + dz) / 32;
			if (!sapi.World.BlockAccessor.IsValidPos(Pos.X + dx, 1, Pos.Z + dz))
			{
				findNextChunk = true;
				return;
			}
			ChunkPeekOptions opts = new ChunkPeekOptions
			{
				OnGenerated = delegate(Dictionary<Vec2i, IServerChunk[]> chunks)
				{
					TestForExitPoint(chunks, chunkX, chunkZ);
				},
				UntilPass = EnumWorldGenPass.TerrainFeatures,
				ChunkGenParams = chunkGenParams()
			};
			sapi.WorldManager.PeekChunkColumn(chunkX, chunkZ, opts);
		}
		if (canTeleport && Activated)
		{
			try
			{
				HandleTeleportingServer(dt);
			}
			catch (Exception e)
			{
				Api.Logger.Warning("Exception when ticking Static Translocator at {0}", Pos);
				Api.Logger.Error(e);
			}
		}
	}

	private ITreeAttribute chunkGenParams()
	{
		TreeAttribute treeAttribute = new TreeAttribute();
		TreeAttribute subtree = (TreeAttribute)(treeAttribute["structureChanceModifier"] = new TreeAttribute());
		subtree.SetFloat("gates", 10f);
		subtree = (TreeAttribute)(treeAttribute["structureMaxCount"] = new TreeAttribute());
		subtree.SetInt("gates", 1);
		return treeAttribute;
	}

	private void TestForExitPoint(Dictionary<Vec2i, IServerChunk[]> columnsByChunkCoordinate, int centerCx, int centerCz)
	{
		BlockPos pos = HasExitPoint(columnsByChunkCoordinate, centerCx, centerCz);
		if (pos == null)
		{
			findNextChunk = true;
			return;
		}
		sapi.WorldManager.LoadChunkColumnPriority(centerCx, centerCz, new ChunkLoadOptions
		{
			ChunkGenParams = chunkGenParams(),
			OnLoaded = delegate
			{
				exitChunkLoaded(pos);
			}
		});
	}

	private void exitChunkLoaded(BlockPos exitPos)
	{
		BlockStaticTranslocator exitBlock = Api.World.BlockAccessor.GetBlock(exitPos) as BlockStaticTranslocator;
		if (exitBlock == null)
		{
			exitPos = HasExitPoint(exitPos);
			if (exitPos != null)
			{
				exitBlock = Api.World.BlockAccessor.GetBlock(exitPos) as BlockStaticTranslocator;
			}
		}
		if (exitBlock != null && !exitBlock.Repaired)
		{
			Api.World.BlockAccessor.SetBlock(ownBlock.Id, exitPos);
			BlockEntityStaticTranslocator beExit = Api.World.BlockAccessor.GetBlockEntity(exitPos) as BlockEntityStaticTranslocator;
			beExit.tpLocation = Pos.Copy();
			beExit.canTeleport = true;
			beExit.findNextChunk = false;
			beExit.activated = true;
			if (!beExit.FullyRepaired)
			{
				beExit.repairState = 4;
				beExit.setupGameTickers();
			}
			Api.World.BlockAccessor.MarkBlockEntityDirty(exitPos);
			Api.World.BlockAccessor.MarkBlockDirty(exitPos);
			Api.World.Logger.Debug("Connected translocator at {0} (chunkpos: {2}) to my location: {1}", exitPos, Pos, exitPos / 32);
			MarkDirty(redrawOnClient: true);
			tpLocation = exitPos;
			canTeleport = true;
		}
		else
		{
			Api.World.Logger.Warning("Translocator: Regen chunk but broken translocator is gone. Structure generation perhaps seed not consistent? May also just be pre-v1.10 chunk, so probably nothing to worry about. Searching again...");
			findNextChunk = true;
		}
	}

	private BlockPos HasExitPoint(BlockPos nearpos)
	{
		List<GeneratedStructure> structures = (Api.World.BlockAccessor.GetChunkAtBlockPos(nearpos) as IServerChunk)?.MapChunk?.MapRegion?.GeneratedStructures;
		if (structures == null)
		{
			return null;
		}
		foreach (GeneratedStructure structure in structures)
		{
			if (!structure.Code.Contains("gates"))
			{
				continue;
			}
			Cuboidi loc = structure.Location;
			BlockPos foundPos = null;
			Api.World.BlockAccessor.SearchBlocks(loc.Start.AsBlockPos, loc.End.AsBlockPos, delegate(Block block, BlockPos pos)
			{
				if (block is BlockStaticTranslocator { Repaired: false })
				{
					foundPos = pos.Copy();
					return false;
				}
				return true;
			});
			if (foundPos != null)
			{
				return foundPos;
			}
		}
		return null;
	}

	private BlockPos HasExitPoint(Dictionary<Vec2i, IServerChunk[]> columnsByChunkCoordinate, int centerCx, int centerCz)
	{
		foreach (GeneratedStructure structure in columnsByChunkCoordinate[new Vec2i(centerCx, centerCz)][0].MapChunk.MapRegion.GeneratedStructures)
		{
			if (structure.Code.Contains("gates"))
			{
				BlockPos pos = FindTranslocator(structure.Location, columnsByChunkCoordinate, centerCx, centerCz);
				if (pos != null)
				{
					return pos;
				}
			}
		}
		return null;
	}

	private BlockPos FindTranslocator(Cuboidi location, Dictionary<Vec2i, IServerChunk[]> columnsByChunkCoordinate, int centerCx, int centerCz)
	{
		for (int x = location.X1; x < location.X2; x++)
		{
			for (int y = location.Y1; y < location.Y2; y++)
			{
				for (int z = location.Z1; z < location.Z2; z++)
				{
					int cx = x / 32;
					int cz = z / 32;
					if (columnsByChunkCoordinate.TryGetValue(new Vec2i(cx, cz), out var chunks))
					{
						IServerChunk chunk = chunks[y / 32];
						int lx = x % 32;
						int num = y % 32;
						int lz = z % 32;
						int index3d = (num * 32 + lz) * 32 + lx;
						if (Api.World.Blocks[chunk.Data[index3d]] is BlockStaticTranslocator { Repaired: false })
						{
							return new BlockPos(x, y, z);
						}
					}
				}
			}
		}
		return null;
	}

	public long MapRegionIndex2D(int regionX, int regionZ)
	{
		return (long)regionZ * (long)(sapi.WorldManager.MapSizeX / sapi.WorldManager.RegionSize) + regionX;
	}

	protected override void didTeleport(Entity entity)
	{
		if (entity is EntityPlayer)
		{
			manager.DidTranslocateServer((entity as EntityPlayer).Player as IServerPlayer);
		}
		activated = false;
		ownBlock.teleportParticles.MinPos.Set(Pos.X, Pos.Y, Pos.Z);
		ownBlock.teleportParticles.AddPos.Set(1.0, 1.8, 1.0);
		ownBlock.teleportParticles.MinVelocity.Set(-1f, -1f, -1f);
		ownBlock.teleportParticles.AddVelocity.Set(2f, 2f, 2f);
		ownBlock.teleportParticles.MinQuantity = 150f;
		ownBlock.teleportParticles.AddQuantity = 0.5f;
		int r = 53;
		int g = 221;
		int b = 172;
		ownBlock.teleportParticles.Color = (r << 16) | (g << 8) | b | 0x64000000;
		ownBlock.teleportParticles.BlueEvolve = null;
		ownBlock.teleportParticles.RedEvolve = null;
		ownBlock.teleportParticles.GreenEvolve = null;
		ownBlock.teleportParticles.MinSize = 0.1f;
		ownBlock.teleportParticles.MaxSize = 0.2f;
		ownBlock.teleportParticles.SizeEvolve = null;
		ownBlock.teleportParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -10f);
		Api.World.SpawnParticles(ownBlock.teleportParticles);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api.Side == EnumAppSide.Server)
		{
			(Api as ICoreServerAPI).ModLoader.GetModSystem<TeleporterManager>().DeleteLocation(Pos);
		}
		translocatingSound?.Dispose();
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		translocatingSound?.Dispose();
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		canTeleport = tree.GetBool("canTele");
		repairState = tree.GetInt("repairState");
		findNextChunk = tree.GetBool("findNextChunk", defaultValue: true);
		activated = tree.GetBool("activated");
		tpLocationIsOffset = tree.GetBool("tpLocationIsOffset");
		if (canTeleport)
		{
			tpLocation = new BlockPos(tree.GetInt("teleX"), tree.GetInt("teleY"), tree.GetInt("teleZ"));
			if (tpLocation.X == 0 && tpLocation.Z == 0)
			{
				tpLocation = null;
			}
		}
		if (worldAccessForResolve == null || worldAccessForResolve.Side != EnumAppSide.Client)
		{
			return;
		}
		somebodyIsTeleportingReceivedTotalMs = worldAccessForResolve.ElapsedMilliseconds;
		if (tree.GetBool("somebodyDidTeleport"))
		{
			worldAccessForResolve.Api.Event.EnqueueMainThreadTask(delegate
			{
				worldAccessForResolve.PlaySoundAt(new AssetLocation("sounds/effect/translocate-breakdimension"), Pos, 0.0, null, randomizePitch: false, 16f);
			}, "playtelesound");
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("canTele", canTeleport);
		tree.SetInt("repairState", repairState);
		tree.SetBool("findNextChunk", findNextChunk);
		tree.SetBool("activated", activated);
		tree.SetBool("tpLocationIsOffset", tpLocationIsOffset);
		if (tpLocation != null)
		{
			tree.SetInt("teleX", tpLocation.X);
			tree.SetInt("teleY", tpLocation.Y);
			tree.SetInt("teleZ", tpLocation.Z);
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (animUtil.activeAnimationsByAnimCode.Count > 0 || (animUtil.animator != null && animUtil.animator.ActiveAnimationCount > 0))
		{
			return true;
		}
		if (!FullyRepaired)
		{
			MeshData mesh = ObjectCacheUtil.GetOrCreate(Api, "statictranslocator-" + repairState + "-" + ownBlock.Shape.rotateY, delegate
			{
				float rotateY = ownBlock.Shape.rotateY;
				_ = Api;
				string text = "normal";
				switch (repairState)
				{
				case 0:
					text = "broken";
					break;
				case 1:
					text = "repairstate1";
					break;
				case 2:
					text = "repairstate2";
					break;
				case 3:
					text = "repairstate3";
					break;
				}
				Shape shape = Shape.TryGet(Api, "shapes/block/machine/statictranslocator/" + text + ".json");
				tessThreadTesselator.TesselateShape(ownBlock, shape, out var modeldata, new Vec3f(0f, rotateY, 0f));
				return modeldata;
			});
			mesher.AddMeshData(mesh);
			return true;
		}
		return false;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (!FullyRepaired)
		{
			dsc.AppendLine(Lang.Get("Seems to be missing a couple of gears. I think I've seen such gears before."));
		}
		else if (tpLocation == null)
		{
			string[] lines = new string[3]
			{
				Lang.Get("Warping spacetime."),
				Lang.Get("Warping spacetime.."),
				Lang.Get("Warping spacetime...")
			};
			dsc.AppendLine(lines[(int)((float)Api.World.ElapsedMilliseconds / 1000f) % 3]);
		}
		else if (forPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			BlockPos pos = Api.World.DefaultSpawnPosition.AsBlockPos;
			BlockPos targetpos = tpLocation.Copy().Sub(pos.X, 0, pos.Z);
			if (tpLocationIsOffset)
			{
				targetpos.Add(Pos.X, pos.Y, pos.Z);
			}
			dsc.AppendLine(Lang.Get("Teleports to {0}", targetpos));
		}
		else
		{
			dsc.AppendLine(Lang.Get("Spacetime subduction completed."));
		}
	}
}
