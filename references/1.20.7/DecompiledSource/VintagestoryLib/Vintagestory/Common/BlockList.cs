using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.Common;

public class BlockList : IList<Block>, ICollection<Block>, IEnumerable<Block>, IEnumerable
{
	[CompilerGenerated]
	private sealed class _003CGetEnumerator_003Ed__21 : IEnumerator<Block>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private Block _003C_003E2__current;

		public BlockList _003C_003E4__this;

		private int _003Ci_003E5__2;

		Block IEnumerator<Block>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CGetEnumerator_003Ed__21(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			int num = _003C_003E1__state;
			BlockList blockList = _003C_003E4__this;
			if (num != 0)
			{
				if (num != 1)
				{
					return false;
				}
				_003C_003E1__state = -1;
				goto IL_006e;
			}
			_003C_003E1__state = -1;
			_003Ci_003E5__2 = 0;
			goto IL_007e;
			IL_006e:
			_003Ci_003E5__2++;
			goto IL_007e;
			IL_007e:
			if (_003Ci_003E5__2 < blockList.blocks.Length && _003Ci_003E5__2 < blockList.count)
			{
				Block block = blockList.blocks[_003Ci_003E5__2];
				if (!(block?.Code == null))
				{
					_003C_003E2__current = block;
					_003C_003E1__state = 1;
					return true;
				}
				goto IL_006e;
			}
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	[CompilerGenerated]
	private sealed class _003CSystem_002DCollections_002DIEnumerable_002DGetEnumerator_003Ed__22 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public BlockList _003C_003E4__this;

		private int _003Ci_003E5__2;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CSystem_002DCollections_002DIEnumerable_002DGetEnumerator_003Ed__22(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			int num = _003C_003E1__state;
			BlockList blockList = _003C_003E4__this;
			if (num != 0)
			{
				if (num != 1)
				{
					return false;
				}
				_003C_003E1__state = -1;
				goto IL_006e;
			}
			_003C_003E1__state = -1;
			_003Ci_003E5__2 = 0;
			goto IL_007e;
			IL_006e:
			_003Ci_003E5__2++;
			goto IL_007e;
			IL_007e:
			if (_003Ci_003E5__2 < blockList.blocks.Length && _003Ci_003E5__2 < blockList.count)
			{
				Block block = blockList.blocks[_003Ci_003E5__2];
				if (!(block?.Code == null))
				{
					_003C_003E2__current = block;
					_003C_003E1__state = 1;
					return true;
				}
				goto IL_006e;
			}
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	private Block[] blocks;

	private int count;

	private Dictionary<int, Block> noBlocks = new Dictionary<int, Block>();

	private GameMain game;

	public static ModelTransform guitf = ModelTransform.BlockDefaultGui();

	public static ModelTransform fptf = ModelTransform.BlockDefaultFp();

	public static ModelTransform gndtf = ModelTransform.BlockDefaultGround();

	public static ModelTransform tptf = ModelTransform.BlockDefaultTp();

	public Block[] BlocksFast => blocks;

	public Block this[int index]
	{
		get
		{
			if (index >= count)
			{
				return getOrCreateNoBlock(index);
			}
			Block block = blocks[index];
			if (block == null || block.Id != index)
			{
				return blocks[index] = getNoBlock(index, game.World.Api);
			}
			return block;
		}
		set
		{
			if (index != value.Id)
			{
				throw new InvalidOperationException("Trying to add a block at index != id");
			}
			while (index >= count)
			{
				Add(null);
			}
			blocks[index] = value;
		}
	}

	public int Count => count;

	public bool IsReadOnly => false;

	public BlockList(GameMain game, int initialSize = 10000)
	{
		this.game = game;
		blocks = new Block[initialSize];
	}

	public BlockList(GameMain game, Block[] fromBlocks)
	{
		this.game = game;
		blocks = fromBlocks;
		count = fromBlocks.Length;
		for (int index = 0; index < fromBlocks.Length; index++)
		{
			Block block = fromBlocks[index];
			if (block == null || block.Id != index)
			{
				blocks[index] = getNoBlock(index, game.World.Api);
			}
		}
	}

	public void PreAlloc(int atLeastSize)
	{
		if (atLeastSize > blocks.Length)
		{
			Array.Resize(ref blocks, atLeastSize + 10);
		}
	}

	public void Add(Block block)
	{
		if (blocks.Length <= count)
		{
			Array.Resize(ref blocks, blocks.Length + 250);
		}
		blocks[count++] = block;
	}

	public void Clear()
	{
		count = 0;
	}

	public bool Contains(Block item)
	{
		return blocks.Contains(item);
	}

	public Block[] Search(AssetLocation wildcard)
	{
		if (wildcard.Path.Length == 0)
		{
			return new Block[0];
		}
		string wildcardPathAsRegex = WildcardUtil.Prepare(wildcard.Path);
		if (wildcardPathAsRegex == null)
		{
			for (int i = 0; i < blocks.Length; i++)
			{
				if (i > count)
				{
					return new Block[0];
				}
				Block block = blocks[i];
				if (block != null && !block.IsMissing && wildcard.Equals(block.Code) && block.Id == i)
				{
					return new Block[1] { block };
				}
			}
			return new Block[0];
		}
		List<Block> foundBlocks = new List<Block>();
		for (int j = 0; j < blocks.Length; j++)
		{
			if (j > count)
			{
				return foundBlocks.ToArray();
			}
			Block block2 = blocks[j];
			if (block2?.Code != null && !block2.IsMissing && wildcard.WildCardMatch(block2.Code, wildcardPathAsRegex) && block2.Id == j)
			{
				foundBlocks.Add(block2);
			}
		}
		return foundBlocks.ToArray();
	}

	public void CopyTo(Block[] array, int arrayIndex)
	{
		for (int i = arrayIndex; i < count; i++)
		{
			array[i] = this[i];
		}
	}

	[IteratorStateMachine(typeof(_003CGetEnumerator_003Ed__21))]
	public IEnumerator<Block> GetEnumerator()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetEnumerator_003Ed__21(0)
		{
			_003C_003E4__this = this
		};
	}

	[IteratorStateMachine(typeof(_003CSystem_002DCollections_002DIEnumerable_002DGetEnumerator_003Ed__22))]
	IEnumerator IEnumerable.GetEnumerator()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CSystem_002DCollections_002DIEnumerable_002DGetEnumerator_003Ed__22(0)
		{
			_003C_003E4__this = this
		};
	}

	public int IndexOf(Block item)
	{
		return blocks.IndexOf(item);
	}

	public void Insert(int index, Block item)
	{
		throw new NotImplementedException("This method should not be used on block lists, it changes block ids in unexpected ways");
	}

	public bool Remove(Block item)
	{
		throw new NotImplementedException("This method should not be used on block lists, it changes block ids in unexpected ways");
	}

	public void RemoveAt(int index)
	{
		throw new NotImplementedException("This method should not be used on block lists, it changes block ids in unexpected ways");
	}

	private Block getOrCreateNoBlock(int id)
	{
		if (!noBlocks.TryGetValue(id, out var block))
		{
			block = (noBlocks[id] = getNoBlock(id, game.World.Api));
		}
		return block;
	}

	public static Block getNoBlock(int id, ICoreAPI Api)
	{
		Block block = new Block();
		block.Code = null;
		block.BlockId = id;
		block.IsMissing = true;
		block.Textures = new FastSmallDictionary<string, CompositeTexture>(0);
		block.GuiTransform = guitf;
		block.FpHandTransform = fptf;
		block.GroundTransform = gndtf;
		block.TpHandTransform = tptf;
		block.DrawType = EnumDrawType.Empty;
		block.MatterState = EnumMatterState.Gas;
		block.Sounds = new BlockSounds();
		block.Replaceable = 999;
		block.CollisionBoxes = null;
		block.SelectionBoxes = null;
		block.RainPermeable = true;
		block.AllSidesOpaque = false;
		block.SideSolid = new SmallBoolArray(0);
		block.VertexFlags = new VertexFlags();
		block.OnLoadedNative(Api);
		return block;
	}
}
