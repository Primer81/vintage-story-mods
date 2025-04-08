using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class SystemClientTickingBlocks : ClientSystem
{
	private Vec3i commitedPlayerPosDiv8 = new Vec3i();

	private Queue<TickingBlockData> blockChangedTickers = new Queue<TickingBlockData>();

	private Dictionary<int, TickerMetaData> committedTickers = new Dictionary<int, TickerMetaData>();

	private object committedTickersLock = new object();

	private List<TickingBlockData> currentTickers = new List<TickingBlockData>();

	private Dictionary<Vec3i, Dictionary<AssetLocation, AmbientSound>> currentAmbientSoundsBySection = new Dictionary<Vec3i, Dictionary<AssetLocation, AmbientSound>>();

	private Vec3i currentPlayerPosDiv8 = new Vec3i();

	private bool shouldStartScanning;

	private object shouldStartScanningLock = new object();

	private BlockScanState scanState;

	private int scanPosition;

	private int finalScanPosition;

	private static int scanRange = 37;

	private static int scanSize = 2 * scanRange;

	private static int scanSectionSize = scanSize / 8;

	private IBlockAccessor searchBlockAccessor;

	private bool freezeCtBlocks;

	private float offthreadAccum;

	private int currentLeavesCount;

	public override string Name => "ctb";

	public SystemClientTickingBlocks(ClientMain game)
		: base(game)
	{
		game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.PlayerPosDiv8, PlayerPosDiv8Changed);
		game.eventManager.OnBlockChanged.Add(OnBlockChanged);
		game.api.eventapi.RegisterAsyncParticleSpawner(onOffThreadParticleTick);
		game.api.ChatCommands.Create("ctblocks").WithDescription("Lets to toggle on/off the updating of client ticking blocks. This can be useful when recording water falls and such").WithArgs(game.api.ChatCommands.Parsers.OptionalBool("freezeCtBlocks"))
			.HandleWith(OnCmdCtBlocks);
		searchBlockAccessor = new BlockAccessorRelaxed(game.WorldMap, game, synchronize: false, relight: false);
		finalScanPosition = scanSize * scanSize * scanSize;
	}

	private TextCommandResult OnCmdCtBlocks(TextCommandCallingArgs args)
	{
		freezeCtBlocks = (bool)args[0];
		return TextCommandResult.Success("Ct block updating now " + (freezeCtBlocks ? "frozen" : "active"));
	}

	public override void OnBlockTexturesLoaded()
	{
		for (int i = 0; i < game.Blocks.Count; i++)
		{
			if (game.Blocks[i] != null)
			{
				game.Blocks[i].DetermineTopMiddlePos();
			}
		}
		game.RegisterCallback(delegate
		{
			lock (shouldStartScanningLock)
			{
				shouldStartScanning = true;
			}
		}, 1000);
		game.RegisterGameTickListener(onTick20Secs, 20000, 123);
	}

	private void onTick20Secs(float dt)
	{
		lock (shouldStartScanningLock)
		{
			shouldStartScanning = true;
		}
	}

	private void OnBlockChanged(BlockPos pos, Block oldBlock)
	{
		Block block = game.WorldMap.RelaxedBlockAccess.GetBlock(pos);
		if (block.ShouldReceiveClientParticleTicks(game, game.player, pos, out var isWindAffected))
		{
			int baseX = commitedPlayerPosDiv8.X * 8 - scanRange;
			int baseY = commitedPlayerPosDiv8.Y * 8 - scanRange;
			int baseZ = commitedPlayerPosDiv8.Z * 8 - scanRange;
			int num = pos.X - baseX;
			int dy = pos.Y - baseY;
			int dz = pos.Z - baseZ;
			int deltaIndex3d = num | (dy << 10) | (dz << 20);
			lock (committedTickersLock)
			{
				if (!committedTickers.ContainsKey(deltaIndex3d))
				{
					blockChangedTickers.Enqueue(new TickingBlockData
					{
						DeltaIndex3d = deltaIndex3d,
						IsWindAffected = isWindAffected,
						WindAffectedNess = (isWindAffected ? SearchWindAffectedNess(pos, game.BlockAccessor) : 0f)
					});
				}
			}
		}
		if (block.Sounds?.Ambient != oldBlock?.Sounds?.Ambient)
		{
			lock (shouldStartScanningLock)
			{
				shouldStartScanning = true;
			}
		}
	}

	private bool onOffThreadParticleTick(float dt, IAsyncParticleManager manager)
	{
		bool updateWindAffectedness = false;
		offthreadAccum += dt;
		if (offthreadAccum > 4f)
		{
			offthreadAccum = 0f;
			updateWindAffectedness = true;
		}
		Dictionary<int, TickerMetaData> tickers;
		lock (committedTickersLock)
		{
			tickers = committedTickers;
			while (blockChangedTickers.Count > 0)
			{
				TickingBlockData data = blockChangedTickers.Dequeue();
				tickers[data.DeltaIndex3d] = new TickerMetaData
				{
					TickingSinceMs = game.ElapsedMilliseconds,
					IsWindAffected = data.IsWindAffected,
					WindAffectedNess = data.WindAffectedNess
				};
			}
		}
		if (manager.BlockAccess is ICachingBlockAccessor icba)
		{
			icba.Begin();
		}
		int baseX = commitedPlayerPosDiv8.X * 8 - scanRange;
		int baseY = commitedPlayerPosDiv8.Y * 8 - scanRange;
		int baseZ = commitedPlayerPosDiv8.Z * 8 - scanRange;
		long ellapseMs = game.ElapsedMilliseconds;
		foreach (KeyValuePair<int, TickerMetaData> val in tickers)
		{
			BlockPos pos = new BlockPos(baseX + (val.Key & 0x3FF), baseY + ((val.Key >> 10) & 0x3FF), baseZ + ((val.Key >> 20) & 0x3FF));
			if (updateWindAffectedness && val.Value.IsWindAffected)
			{
				val.Value.WindAffectedNess = SearchWindAffectedNess(pos, manager.BlockAccess);
			}
			manager.BlockAccess.GetBlock(pos)?.OnAsyncClientParticleTick(manager, pos, val.Value.WindAffectedNess, (float)(ellapseMs - val.Value.TickingSinceMs) / 1000f);
		}
		return true;
	}

	private void PlayerPosDiv8Changed(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
	{
		lock (shouldStartScanningLock)
		{
			shouldStartScanning = true;
			currentPlayerPosDiv8 = newValues.PlayerPosDiv8.ToVec3i();
		}
	}

	public void CommitScan()
	{
		if (freezeCtBlocks)
		{
			currentTickers.Clear();
			return;
		}
		List<AmbientSound> sounds;
		lock (shouldStartScanningLock)
		{
			long elapsedMs = game.ElapsedMilliseconds;
			Dictionary<int, TickerMetaData> newCommittedTickers = new Dictionary<int, TickerMetaData>();
			int diffX = (currentPlayerPosDiv8.X - commitedPlayerPosDiv8.X) * 8;
			int diffY = (currentPlayerPosDiv8.Y - commitedPlayerPosDiv8.Y) * 8;
			int diffZ = (currentPlayerPosDiv8.Z - commitedPlayerPosDiv8.Z) * 8;
			foreach (TickingBlockData val in currentTickers)
			{
				int num = val.DeltaIndex3d & 0x3FF;
				int dy = (val.DeltaIndex3d >> 10) & 0x3FF;
				int dz = (val.DeltaIndex3d >> 20) & 0x3FF;
				int index = (num + diffX) | (dy + diffY << 10) | (dz + diffZ << 20);
				long thiselapsedms = elapsedMs;
				if (committedTickers.TryGetValue(index, out var prevData))
				{
					thiselapsedms = prevData.TickingSinceMs;
				}
				newCommittedTickers[val.DeltaIndex3d] = new TickerMetaData
				{
					TickingSinceMs = thiselapsedms,
					IsWindAffected = val.IsWindAffected,
					WindAffectedNess = val.WindAffectedNess
				};
			}
			commitedPlayerPosDiv8 = currentPlayerPosDiv8;
			currentTickers.Clear();
			lock (committedTickersLock)
			{
				committedTickers = newCommittedTickers;
			}
			sounds = MergeEqualAmbientSounds();
		}
		game.eventManager?.OnAmbientSoundsScanComplete(sounds);
	}

	private List<AmbientSound> MergeEqualAmbientSounds()
	{
		Dictionary<AssetLocation, List<AmbientSound>> mergeddict = new Dictionary<AssetLocation, List<AmbientSound>>();
		foreach (Dictionary<AssetLocation, AmbientSound> sectionsounds in currentAmbientSoundsBySection.Values)
		{
			foreach (AssetLocation assetloc in sectionsounds.Keys)
			{
				bool added = false;
				if (mergeddict.TryGetValue(assetloc, out var sounds))
				{
					for (int i = 0; i < sounds.Count; i++)
					{
						AmbientSound sound = sounds[i];
						if (sound.DistanceTo(sectionsounds[assetloc]) < sound.MaxDistanceMerge)
						{
							sound.BoundingBoxes.AddRange(sectionsounds[assetloc].BoundingBoxes);
							sound.QuantityNearbyBlocks += sectionsounds[assetloc].QuantityNearbyBlocks;
							added = true;
							break;
						}
					}
					if (!added)
					{
						sounds.Add(sectionsounds[assetloc]);
					}
				}
				else
				{
					mergeddict[assetloc] = new List<AmbientSound> { sectionsounds[assetloc] };
				}
			}
		}
		List<AmbientSound> merged = new List<AmbientSound>();
		foreach (KeyValuePair<AssetLocation, List<AmbientSound>> item in mergeddict)
		{
			merged.AddRange(item.Value);
		}
		return merged;
	}

	public override int SeperateThreadTickIntervalMs()
	{
		return 5;
	}

	public override void OnSeperateThreadGameTick(float dt)
	{
		if (shouldStartScanning && scanState != BlockScanState.Done)
		{
			scanState = BlockScanState.Scanning;
			scanPosition = 0;
			currentLeavesCount = 0;
			lock (shouldStartScanningLock)
			{
				shouldStartScanning = false;
			}
			currentTickers.Clear();
			currentAmbientSoundsBySection.Clear();
		}
		if (scanState != BlockScanState.Scanning)
		{
			return;
		}
		int baseX = currentPlayerPosDiv8.X * 8 - scanRange;
		int baseY = currentPlayerPosDiv8.Y * 8 - scanRange;
		int baseZ = currentPlayerPosDiv8.Z * 8 - scanRange;
		IWorldChunk chunk = null;
		int cxBefore = 0;
		int cyBefore = -1;
		int czBefore = -912312;
		BlockPos tmpPos = new BlockPos();
		IList<Block> blocks = game.Blocks;
		for (int i = 0; i < 11000; i++)
		{
			int dx = scanPosition % scanSize;
			int dy = scanPosition / (scanSize * scanSize);
			int dz = scanPosition / scanSize % scanSize;
			tmpPos.Set(baseX + dx, baseY + dy, baseZ + dz);
			if (!game.WorldMap.IsValidPos(tmpPos))
			{
				scanPosition++;
				continue;
			}
			int cx = tmpPos.X / 32;
			int cy = tmpPos.Y / 32;
			int cz = tmpPos.Z / 32;
			if (cx != cxBefore || cy != cyBefore || cz != czBefore)
			{
				cxBefore = cx;
				cyBefore = cy;
				czBefore = cz;
				chunk = game.WorldMap.GetChunk(cx, cy, cz);
				chunk?.Unpack();
			}
			if (chunk == null)
			{
				scanPosition++;
				continue;
			}
			int lx = tmpPos.X % 32;
			int ly = tmpPos.Y % 32;
			int lz = tmpPos.Z % 32;
			Block block = blocks[chunk.Data[(ly * 32 + lz) * 32 + lx]];
			float str;
			if (block?.Sounds?.Ambient != null && (str = block.GetAmbientSoundStrength(game, tmpPos)) > 0f)
			{
				Vec3i sectionPos = new Vec3i(tmpPos.X / scanSectionSize, tmpPos.Y / scanSectionSize, tmpPos.Z / scanSectionSize);
				AmbientSound ambSound = null;
				Dictionary<AssetLocation, AmbientSound> ambSoundsofSection = null;
				if (!currentAmbientSoundsBySection.TryGetValue(sectionPos, out ambSoundsofSection))
				{
					ambSoundsofSection = (currentAmbientSoundsBySection[sectionPos] = new Dictionary<AssetLocation, AmbientSound>());
				}
				ambSoundsofSection.TryGetValue(block.Sounds.Ambient, out ambSound);
				if (ambSound == null)
				{
					ambSound = new AmbientSound
					{
						AssetLoc = block.Sounds.Ambient,
						Ratio = block.Sounds.AmbientBlockCount,
						VolumeMul = str,
						SoundType = block.Sounds.AmbientSoundType,
						SectionPos = sectionPos,
						MaxDistanceMerge = block.Sounds.AmbientMaxDistanceMerge
					};
					ambSound.BoundingBoxes.Add(new Cuboidi(tmpPos.X, tmpPos.Y, tmpPos.Z, tmpPos.X + 1, tmpPos.Y + 1, tmpPos.Z + 1));
					ambSoundsofSection[block.Sounds.Ambient] = ambSound;
				}
				else
				{
					ambSound.VolumeMul = str;
				}
				ambSound.QuantityNearbyBlocks++;
				Cuboidi box = ambSound.BoundingBoxes[0];
				box.GrowToInclude(tmpPos);
				if (tmpPos.X == box.X2)
				{
					box.X2++;
				}
				if (tmpPos.Y == box.Y2)
				{
					box.Y2++;
				}
				if (tmpPos.Z == box.Z2)
				{
					box.Z2++;
				}
			}
			if (block.BlockMaterial == EnumBlockMaterial.Leaves)
			{
				currentLeavesCount++;
			}
			bool isWindAffected = false;
			if (block.ShouldReceiveClientParticleTicks(game, game.player, tmpPos, out isWindAffected))
			{
				currentTickers.Add(new TickingBlockData
				{
					DeltaIndex3d = (dx | (dy << 10) | (dz << 20)),
					IsWindAffected = isWindAffected,
					WindAffectedNess = (isWindAffected ? SearchWindAffectedNess(tmpPos, searchBlockAccessor) : 0f)
				});
			}
			scanPosition++;
			if (scanPosition < finalScanPosition)
			{
				continue;
			}
			if (scanState == BlockScanState.Scanning)
			{
				scanState = BlockScanState.Done;
				game.EnqueueMainThreadTask(delegate
				{
					CommitScan();
					GlobalConstants.CurrentNearbyRelLeavesCountClient = (float)currentLeavesCount / (float)finalScanPosition;
					scanState = BlockScanState.Idle;
				}, "commitscan");
			}
			break;
		}
	}

	private float SearchWindAffectedNess(BlockPos pos, IBlockAccessor blockAccess)
	{
		return Math.Max(0f, 1f - (float)blockAccess.GetDistanceToRainFall(pos) / 5f);
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
