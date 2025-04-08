using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class BlockAccessorRevertable : BlockAccessorRelaxedBulkUpdate, IBlockAccessorRevertable, IBulkBlockAccessor, IBlockAccessor
{
	private readonly List<HistoryState> _historyStates = new List<HistoryState>();

	private int _currentHistoryStateIndex;

	private int _maxQuantityStates = 35;

	private bool _multiedit;

	private List<BlockUpdate> _blockUpdates = new List<BlockUpdate>();

	public int CurrentHistoryState => _currentHistoryStateIndex;

	public bool Relight
	{
		get
		{
			return relight;
		}
		set
		{
			relight = value;
		}
	}

	public int QuantityHistoryStates
	{
		get
		{
			return _maxQuantityStates;
		}
		set
		{
			_maxQuantityStates = value;
			while (_historyStates.Count > _maxQuantityStates)
			{
				_historyStates.RemoveAt(_historyStates.Count - 1);
			}
		}
	}

	public int AvailableHistoryStates => _historyStates.Count;

	public event Action<HistoryState> OnStoreHistoryState;

	public event Action<HistoryState, int> OnRestoreHistoryState;

	public BlockAccessorRevertable(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool relight, bool debug)
		: base(worldmap, worldAccessor, synchronize, relight, debug)
	{
		storeOldBlockEntityData = true;
	}

	public void SetHistoryStateBlock(int posX, int posY, int posZ, int oldBlockId, int newBlockId)
	{
		BlockPos pos = new BlockPos(posX, posY, posZ);
		byte[] data = null;
		if (worldAccessor.Blocks[oldBlockId].EntityClass != null)
		{
			TreeAttribute tree = new TreeAttribute();
			GetBlockEntity(new BlockPos(posX, posY, posZ)).ToTreeAttributes(tree);
			data = tree.ToBytes();
		}
		ItemStack byStack = null;
		if (StagedBlocks.TryGetValue(pos, out var bu) && bu.NewSolidBlockId == newBlockId)
		{
			byStack = bu.ByStack;
		}
		StagedBlocks[pos] = new BlockUpdate
		{
			OldBlockId = oldBlockId,
			NewSolidBlockId = newBlockId,
			NewFluidBlockId = (bu?.NewFluidBlockId ?? (-1)),
			Pos = pos,
			ByStack = byStack,
			OldBlockEntityData = data
		};
	}

	public void BeginMultiEdit()
	{
		_multiedit = true;
		synchronize = false;
	}

	public override List<BlockUpdate> Commit()
	{
		if (_multiedit)
		{
			_blockUpdates.AddRange(base.Commit());
			return _blockUpdates;
		}
		List<BlockUpdate> blockUpdates = base.Commit();
		HistoryState hs = new HistoryState
		{
			BlockUpdates = blockUpdates.ToArray()
		};
		StoreHistoryState(hs);
		return blockUpdates;
	}

	public void StoreHistoryState(HistoryState historyState)
	{
		if (_historyStates.Count >= _maxQuantityStates)
		{
			_historyStates.RemoveAt(_historyStates.Count - 1);
		}
		while (_currentHistoryStateIndex > 0)
		{
			_currentHistoryStateIndex--;
			_historyStates.RemoveAt(0);
		}
		_historyStates.Insert(0, historyState);
		this.OnStoreHistoryState?.Invoke(historyState);
	}

	public void StoreEntitySpawnToHistory(Entity entity)
	{
		HistoryState historyState;
		HistoryState historyState2 = (historyState = _historyStates[_currentHistoryStateIndex]);
		if (historyState.EntityUpdates == null)
		{
			historyState.EntityUpdates = new List<EntityUpdate>();
		}
		historyState2.EntityUpdates.Add(new EntityUpdate
		{
			EntityId = entity.EntityId,
			EntityProperties = entity.Properties,
			NewPosition = entity.ServerPos.Copy()
		});
	}

	public void StoreEntityMoveToHistory(BlockPos start, BlockPos end, Vec3i offset)
	{
		HistoryState historyState = _historyStates[_currentHistoryStateIndex];
		HistoryState historyState2 = historyState;
		if (historyState2.EntityUpdates == null)
		{
			historyState2.EntityUpdates = new List<EntityUpdate>();
		}
		Entity[] entitiesInsideCuboid = worldAccessor.GetEntitiesInsideCuboid(start, end, (Entity e) => !(e is EntityPlayer));
		foreach (Entity entity in entitiesInsideCuboid)
		{
			EntityPos newPosition = entity.ServerPos.Copy().Add(offset.X, offset.Y, offset.Z);
			historyState.EntityUpdates.Add(new EntityUpdate
			{
				EntityId = entity.EntityId,
				OldPosition = entity.ServerPos.Copy(),
				NewPosition = newPosition
			});
			entity.TeleportTo(newPosition);
		}
	}

	public void EndMultiEdit()
	{
		_multiedit = false;
		if (_blockUpdates.Count > 0)
		{
			HistoryState hs = new HistoryState
			{
				BlockUpdates = _blockUpdates.ToArray()
			};
			worldmap.SendBlockUpdateBulk(_blockUpdates.Select((BlockUpdate bu) => bu.Pos), relight);
			worldmap.SendDecorUpdateBulk(from b in _blockUpdates
				where b.Decors != null && b.Pos != null
				select b into bu
				select bu.Pos);
			StoreHistoryState(hs);
		}
		CommitBlockEntityData();
		_blockUpdates.Clear();
		synchronize = true;
	}

	public void CommitBlockEntityData()
	{
		if (_multiedit)
		{
			return;
		}
		BlockUpdate[] blockUpdates = _historyStates[0].BlockUpdates;
		foreach (BlockUpdate update in blockUpdates)
		{
			if (update.NewSolidBlockId >= 0 && worldAccessor.Blocks[update.NewSolidBlockId].EntityClass != null)
			{
				TreeAttribute tree = new TreeAttribute();
				BlockEntity blockEntity = GetBlockEntity(update.Pos);
				blockEntity?.ToTreeAttributes(tree);
				blockEntity?.MarkDirty(redrawOnClient: true);
				update.NewBlockEntityData = tree.ToBytes();
			}
		}
	}

	public void ChangeHistoryState(int quantity = 1)
	{
		bool redo = quantity < 0;
		quantity = Math.Abs(quantity);
		while (quantity > 0)
		{
			_currentHistoryStateIndex += ((!redo) ? 1 : (-1));
			if (_currentHistoryStateIndex < 0)
			{
				_currentHistoryStateIndex = 0;
				break;
			}
			if (_currentHistoryStateIndex > AvailableHistoryStates)
			{
				_currentHistoryStateIndex = AvailableHistoryStates;
				break;
			}
			HistoryState hs;
			if (redo)
			{
				hs = _historyStates[_currentHistoryStateIndex];
				RedoUpdate(hs);
			}
			else
			{
				hs = _historyStates[_currentHistoryStateIndex - 1];
				UndoUpdate(hs);
			}
			quantity--;
			List<BlockUpdate> updatedBlocks = base.Commit();
			if (!redo)
			{
				PostCommitCleanup(updatedBlocks);
			}
			this.OnRestoreHistoryState?.Invoke(hs, redo ? 1 : (-1));
			for (int i = 0; i < hs.BlockUpdates.Length; i++)
			{
				BlockUpdate upd = hs.BlockUpdates[i];
				BlockEntity be = null;
				TreeAttribute tree = null;
				if (redo)
				{
					if (upd.NewSolidBlockId >= 0 && worldAccessor.Blocks[upd.NewSolidBlockId].EntityClass != null && upd.NewBlockEntityData != null)
					{
						tree = TreeAttribute.CreateFromBytes(upd.NewBlockEntityData);
						be = GetBlockEntity(upd.Pos);
					}
				}
				else if (upd.OldBlockId >= 0 && worldAccessor.Blocks[upd.OldBlockId].EntityClass != null)
				{
					tree = TreeAttribute.CreateFromBytes(upd.OldBlockEntityData);
					be = GetBlockEntity(upd.Pos);
				}
				be?.FromTreeAttributes(tree, worldAccessor);
				be?.HistoryStateRestore();
				be?.MarkDirty(redrawOnClient: true);
			}
		}
	}

	private void RedoUpdate(HistoryState state)
	{
		BlockUpdate[] blockUpdates = state.BlockUpdates;
		foreach (BlockUpdate upd in blockUpdates)
		{
			BlockPos copied = upd.Pos.Copy();
			if (StagedBlocks.TryGetValue(copied, out var bu))
			{
				bu.NewSolidBlockId = upd.NewSolidBlockId;
				bu.NewFluidBlockId = upd.NewFluidBlockId;
				bu.ByStack = upd.ByStack;
			}
			else
			{
				StagedBlocks[copied] = new BlockUpdate
				{
					NewSolidBlockId = upd.NewSolidBlockId,
					NewFluidBlockId = upd.NewFluidBlockId,
					ByStack = upd.ByStack,
					Pos = copied
				};
			}
			if (upd.Decors == null)
			{
				continue;
			}
			BlockUpdate blockUpdate = StagedBlocks[copied];
			List<DecorUpdate> copiedDecors = blockUpdate.Decors ?? (blockUpdate.Decors = new List<DecorUpdate>());
			foreach (DecorUpdate update in upd.Decors)
			{
				copiedDecors.Add(update);
			}
		}
		if (state.EntityUpdates == null)
		{
			return;
		}
		foreach (EntityUpdate entityUpdate in state.EntityUpdates.Where((EntityUpdate e) => e.NewPosition != null && e.OldPosition != null))
		{
			worldAccessor.GetEntityById(entityUpdate.EntityId)?.TeleportTo(entityUpdate.NewPosition);
		}
		foreach (EntityUpdate entity in state.EntityUpdates.Where((EntityUpdate e) => e.OldPosition == null))
		{
			Entity entityById = worldAccessor.GetEntityById(entity.EntityId);
			if (entityById != null)
			{
				entityById?.Die(EnumDespawnReason.Removed);
			}
			else if (entity.EntityProperties != null && entity.NewPosition != null)
			{
				Entity newEntity = worldAccessor.ClassRegistry.CreateEntity(entity.EntityProperties);
				newEntity.DidImportOrExport(entity.NewPosition.AsBlockPos);
				newEntity.ServerPos.SetFrom(entity.NewPosition);
				worldAccessor.SpawnEntity(newEntity);
				entity.EntityId = newEntity.EntityId;
			}
		}
	}

	private void UndoUpdate(HistoryState state)
	{
		BlockUpdate[] bUpdates = state.BlockUpdates;
		for (int i = bUpdates.Length - 1; i >= 0; i--)
		{
			BlockUpdate upd = bUpdates[i];
			BlockPos copied = upd.Pos.Copy();
			if (StagedBlocks.TryGetValue(copied, out var bu))
			{
				bu.NewSolidBlockId = upd.OldBlockId;
				bu.NewFluidBlockId = upd.OldFluidBlockId;
				bu.ByStack = upd.ByStack;
			}
			else
			{
				StagedBlocks[copied] = new BlockUpdate
				{
					NewSolidBlockId = upd.OldBlockId,
					NewFluidBlockId = upd.OldFluidBlockId,
					ByStack = upd.ByStack,
					Pos = copied
				};
			}
			if (upd.OldDecors != null)
			{
				BlockUpdate blockUpdate = StagedBlocks[copied];
				List<DecorUpdate> copiedDecors = blockUpdate.Decors ?? (blockUpdate.Decors = new List<DecorUpdate>());
				foreach (DecorUpdate update in upd.OldDecors)
				{
					copiedDecors.Add(update);
				}
			}
		}
		if (state.EntityUpdates == null)
		{
			return;
		}
		foreach (EntityUpdate entityUpdate in state.EntityUpdates.Where((EntityUpdate e) => e.NewPosition != null && e.OldPosition != null))
		{
			worldAccessor.GetEntityById(entityUpdate.EntityId)?.TeleportTo(entityUpdate.OldPosition);
		}
		foreach (EntityUpdate entity in state.EntityUpdates.Where((EntityUpdate e) => e.OldPosition == null))
		{
			Entity entityById = worldAccessor.GetEntityById(entity.EntityId);
			if (entityById != null)
			{
				entityById?.Die(EnumDespawnReason.Removed);
			}
			else if (entity.EntityProperties != null && entity.NewPosition != null)
			{
				Entity newEntity = worldAccessor.ClassRegistry.CreateEntity(entity.EntityProperties);
				newEntity.DidImportOrExport(entity.NewPosition.AsBlockPos);
				newEntity.ServerPos.SetFrom(entity.NewPosition);
				worldAccessor.SpawnEntity(newEntity);
				entity.EntityId = newEntity.EntityId;
			}
		}
	}
}
