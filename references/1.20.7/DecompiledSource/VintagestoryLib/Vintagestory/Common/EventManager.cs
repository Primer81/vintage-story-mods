using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public abstract class EventManager
{
	private long listenerId;

	private long callBackId;

	internal List<GameTickListener> GameTickListenersEntity = new List<GameTickListener>();

	internal ConcurrentDictionary<long, DelayedCallback> DelayedCallbacksEntity = new ConcurrentDictionary<long, DelayedCallback>();

	internal List<GameTickListenerBlock> GameTickListenersBlock = new List<GameTickListenerBlock>();

	internal List<DelayedCallbackBlock> DelayedCallbacksBlock = new List<DelayedCallbackBlock>();

	internal ConcurrentDictionary<long, int> GameTickListenersEntityIndices = new ConcurrentDictionary<long, int>();

	internal ConcurrentDictionary<long, int> GameTickListenersBlockIndices = new ConcurrentDictionary<long, int>();

	internal Dictionary<BlockPos, DelayedCallbackBlock> SingleDelayedCallbacksBlock = new Dictionary<BlockPos, DelayedCallbackBlock>();

	private List<DelayedCallback> deletable = new List<DelayedCallback>();

	protected Thread serverThread;

	public abstract ILogger Logger { get; }

	public abstract string CommandPrefix { get; }

	public abstract long InWorldEllapsedMs { get; }

	public event OnGetClimateDelegate OnGetClimate;

	public event OnGetWindSpeedDelegate OnGetWindSpeed;

	public abstract bool HasPrivilege(string playerUid, string privilegecode);

	public virtual void TriggerOnGetClimate(ref ClimateCondition climate, BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.WorldGenValues, double totalDays = 0.0)
	{
		if (this.OnGetClimate != null)
		{
			Delegate[] invocationList = this.OnGetClimate.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				((OnGetClimateDelegate)invocationList[i])(ref climate, pos, mode, totalDays);
			}
		}
	}

	public virtual void TriggerOnGetWindSpeed(Vec3d pos, ref Vec3d windSpeed)
	{
		if (this.OnGetWindSpeed != null)
		{
			Delegate[] invocationList = this.OnGetWindSpeed.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				((OnGetWindSpeedDelegate)invocationList[i])(pos, ref windSpeed);
			}
		}
	}

	public virtual void TriggerGameTick(long ellapsedMilliseconds, IWorldAccessor world)
	{
		FrameProfilerUtil FrameProfiler = world.FrameProfiler;
		FrameProfiler.Mark("tick-begin");
		List<GameTickListener> GameTickListenersEntity = this.GameTickListenersEntity;
		if (FrameProfiler.Enabled)
		{
			for (int k = 0; k < GameTickListenersEntity.Count; k++)
			{
				GameTickListener listener2 = GameTickListenersEntity[k];
				if (listener2 != null && ellapsedMilliseconds - listener2.LastUpdateMilliseconds > listener2.Millisecondinterval)
				{
					listener2.OnTriggered(ellapsedMilliseconds);
					FrameProfiler.Mark("gmle" + listener2.Origin().GetType());
				}
			}
		}
		else
		{
			for (int l = 0; l < GameTickListenersEntity.Count; l++)
			{
				GameTickListener listener3 = GameTickListenersEntity[l];
				if (listener3 != null && ellapsedMilliseconds - listener3.LastUpdateMilliseconds > listener3.Millisecondinterval)
				{
					listener3.OnTriggered(ellapsedMilliseconds);
				}
			}
		}
		FrameProfiler.Mark("tick-gtentity");
		List<GameTickListenerBlock> GameTickListenersBlock = this.GameTickListenersBlock;
		for (int j = 0; j < GameTickListenersBlock.Count; j++)
		{
			GameTickListenerBlock listener = GameTickListenersBlock[j];
			if (listener != null && ellapsedMilliseconds - listener.LastUpdateMilliseconds > listener.Millisecondinterval)
			{
				listener.Handler(world, listener.Pos, (float)(ellapsedMilliseconds - listener.LastUpdateMilliseconds) / 1000f);
				listener.LastUpdateMilliseconds = ellapsedMilliseconds;
			}
		}
		FrameProfiler.Mark("tick-gtblock");
		deletable.Clear();
		foreach (KeyValuePair<long, DelayedCallback> entry in DelayedCallbacksEntity)
		{
			if (ellapsedMilliseconds - entry.Value.CallAtEllapsedMilliseconds >= 0)
			{
				DelayedCallback callback4 = entry.Value;
				callback4.Handler((float)(ellapsedMilliseconds - callback4.CallAtEllapsedMilliseconds) / 1000f);
				deletable.Add(callback4);
			}
		}
		FrameProfiler.Mark("tick-dcentity");
		foreach (DelayedCallback callback3 in deletable)
		{
			DelayedCallbacksEntity.TryRemove(callback3.ListenerId, out var _);
		}
		List<DelayedCallbackBlock> DelayedCallbacksBlock = this.DelayedCallbacksBlock;
		for (int i = 0; i < DelayedCallbacksBlock.Count; i++)
		{
			DelayedCallbackBlock callback2 = DelayedCallbacksBlock[i];
			if (ellapsedMilliseconds - callback2.CallAtEllapsedMilliseconds >= 0)
			{
				DelayedCallbacksBlock.RemoveAt(i);
				i--;
				callback2.Handler(world, callback2.Pos, (float)(ellapsedMilliseconds - callback2.CallAtEllapsedMilliseconds) / 1000f);
			}
		}
		Dictionary<BlockPos, DelayedCallbackBlock> SingleDelayedCallbacksBlock = this.SingleDelayedCallbacksBlock;
		if (SingleDelayedCallbacksBlock.Count > 0)
		{
			foreach (BlockPos pos in new List<BlockPos>(SingleDelayedCallbacksBlock.Keys))
			{
				DelayedCallbackBlock callback = SingleDelayedCallbacksBlock[pos];
				if (ellapsedMilliseconds - callback.CallAtEllapsedMilliseconds >= 0)
				{
					SingleDelayedCallbacksBlock.Remove(pos);
					callback.Handler(world, callback.Pos, (float)(ellapsedMilliseconds - callback.CallAtEllapsedMilliseconds) / 1000f);
				}
			}
		}
		FrameProfiler.Mark("tick-dcblock");
	}

	public virtual void TriggerGameTickDebug(long ellapsedMilliseconds, IWorldAccessor world)
	{
		List<GameTickListener> GameTickListenersEntity = this.GameTickListenersEntity;
		for (int k = 0; k < GameTickListenersEntity.Count; k++)
		{
			GameTickListener listener2 = GameTickListenersEntity[k];
			if (listener2 != null && ellapsedMilliseconds - listener2.LastUpdateMilliseconds > listener2.Millisecondinterval)
			{
				listener2.OnTriggered(ellapsedMilliseconds);
				world.FrameProfiler.Mark("gmle" + listener2.Origin().GetType());
			}
		}
		List<GameTickListenerBlock> GameTickListenersBlock = this.GameTickListenersBlock;
		for (int j = 0; j < GameTickListenersBlock.Count; j++)
		{
			GameTickListenerBlock listener = GameTickListenersBlock[j];
			if (listener != null && ellapsedMilliseconds - listener.LastUpdateMilliseconds > listener.Millisecondinterval)
			{
				listener.Handler(world, listener.Pos, (float)(ellapsedMilliseconds - listener.LastUpdateMilliseconds) / 1000f);
				listener.LastUpdateMilliseconds = ellapsedMilliseconds;
				world.FrameProfiler.Mark("gmlb" + listener.Handler.Target.GetType());
			}
		}
		deletable.Clear();
		foreach (KeyValuePair<long, DelayedCallback> entry in DelayedCallbacksEntity)
		{
			if (ellapsedMilliseconds - entry.Value.CallAtEllapsedMilliseconds >= 0)
			{
				DelayedCallback callback4 = entry.Value;
				callback4.Handler((float)(ellapsedMilliseconds - callback4.CallAtEllapsedMilliseconds) / 1000f);
				deletable.Add(callback4);
				world.FrameProfiler.Mark("dce" + callback4.Handler.Target.GetType());
			}
		}
		foreach (DelayedCallback callback3 in deletable)
		{
			DelayedCallbacksEntity.TryRemove(callback3.ListenerId, out var _);
		}
		List<DelayedCallbackBlock> DelayedCallbacksBlock = this.DelayedCallbacksBlock;
		for (int i = 0; i < DelayedCallbacksBlock.Count; i++)
		{
			DelayedCallbackBlock callback2 = DelayedCallbacksBlock[i];
			if (ellapsedMilliseconds - callback2.CallAtEllapsedMilliseconds >= 0)
			{
				DelayedCallbacksBlock.RemoveAt(i);
				i--;
				callback2.Handler(world, callback2.Pos, (float)(ellapsedMilliseconds - callback2.CallAtEllapsedMilliseconds) / 1000f);
				world.FrameProfiler.Mark("dcb" + callback2.Handler.Target.GetType());
			}
		}
		Dictionary<BlockPos, DelayedCallbackBlock> SingleDelayedCallbacksBlock = this.SingleDelayedCallbacksBlock;
		if (SingleDelayedCallbacksBlock.Count <= 0)
		{
			return;
		}
		foreach (BlockPos pos in new List<BlockPos>(SingleDelayedCallbacksBlock.Keys))
		{
			DelayedCallbackBlock callback = SingleDelayedCallbacksBlock[pos];
			if (ellapsedMilliseconds - callback.CallAtEllapsedMilliseconds >= 0)
			{
				SingleDelayedCallbacksBlock.Remove(pos);
				callback.Handler(world, callback.Pos, (float)(ellapsedMilliseconds - callback.CallAtEllapsedMilliseconds) / 1000f);
				world.FrameProfiler.Mark("sdcb" + callback.Handler.Target.GetType());
			}
		}
	}

	public virtual long AddGameTickListener(Action<float> handler, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		return AddGameTickListener(handler, null, millisecondInterval, initialDelayOffsetMs);
	}

	public virtual long AddGameTickListener(Action<float> handler, Action<Exception> errorHandler, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		long newListenerId = ++listenerId;
		GameTickListener listener = new GameTickListener
		{
			Handler = handler,
			ErrorHandler = errorHandler,
			Millisecondinterval = millisecondInterval,
			ListenerId = newListenerId,
			LastUpdateMilliseconds = InWorldEllapsedMs + initialDelayOffsetMs
		};
		List<GameTickListener> GameTickListenersEntity = this.GameTickListenersEntity;
		for (int i = 0; i < GameTickListenersEntity.Count; i++)
		{
			if (GameTickListenersEntity[i] == null)
			{
				GameTickListenersEntity[i] = listener;
				GameTickListenersEntityIndices[newListenerId] = i;
				if (GameTickListenersEntity[GameTickListenersEntityIndices[newListenerId]] != listener)
				{
					throw new InvalidOperationException("Failed to add listener properly");
				}
				return newListenerId;
			}
		}
		GameTickListenersEntity.Add(listener);
		GameTickListenersEntityIndices[newListenerId] = GameTickListenersEntity.Count - 1;
		if (GameTickListenersEntity[GameTickListenersEntityIndices[newListenerId]] != listener)
		{
			throw new InvalidOperationException("Failed to add listener properly");
		}
		return newListenerId;
	}

	public long AddGameTickListener(Action<IWorldAccessor, BlockPos, float> handler, BlockPos pos, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		long newListenerId = ++listenerId;
		GameTickListenerBlock listener = new GameTickListenerBlock
		{
			Handler = handler,
			Millisecondinterval = millisecondInterval,
			ListenerId = newListenerId,
			LastUpdateMilliseconds = InWorldEllapsedMs + initialDelayOffsetMs,
			Pos = pos.Copy()
		};
		List<GameTickListenerBlock> GameTickListenersBlock = this.GameTickListenersBlock;
		for (int i = 0; i < GameTickListenersBlock.Count; i++)
		{
			if (GameTickListenersBlock[i] == null)
			{
				GameTickListenersBlock[i] = listener;
				GameTickListenersBlockIndices[newListenerId] = i;
				return newListenerId;
			}
		}
		GameTickListenersBlock.Add(listener);
		GameTickListenersBlockIndices[newListenerId] = GameTickListenersBlock.Count - 1;
		return newListenerId;
	}

	public virtual long AddDelayedCallback(Action<float> handler, long callAfterEllapsedMS)
	{
		long newCallbackId = Interlocked.Increment(ref callBackId);
		DelayedCallback newCallback = new DelayedCallback
		{
			CallAtEllapsedMilliseconds = InWorldEllapsedMs + callAfterEllapsedMS,
			Handler = handler,
			ListenerId = newCallbackId
		};
		DelayedCallbacksEntity[newCallbackId] = newCallback;
		return newCallbackId;
	}

	public virtual long AddDelayedCallback(Action<IWorldAccessor, BlockPos, float> handler, BlockPos pos, long callAfterEllapsedMS)
	{
		long newCallbackId = Interlocked.Increment(ref callBackId);
		DelayedCallbacksBlock.Add(new DelayedCallbackBlock
		{
			CallAtEllapsedMilliseconds = InWorldEllapsedMs + callAfterEllapsedMS,
			Handler = handler,
			ListenerId = newCallbackId,
			Pos = pos.Copy()
		});
		return newCallbackId;
	}

	internal virtual long AddSingleDelayedCallback(Action<IWorldAccessor, BlockPos, float> handler, BlockPos pos, long callAfterEllapsedMs)
	{
		BlockPos cpos = pos.Copy();
		long newCallbackId = Interlocked.Increment(ref callBackId);
		SingleDelayedCallbacksBlock[cpos] = new DelayedCallbackBlock
		{
			CallAtEllapsedMilliseconds = InWorldEllapsedMs + callAfterEllapsedMs,
			Handler = handler,
			ListenerId = newCallbackId,
			Pos = cpos
		};
		return newCallbackId;
	}

	public void RemoveDelayedCallback(long callbackId)
	{
		if (callbackId == 0L || DelayedCallbacksEntity.TryRemove(callbackId, out var _))
		{
			return;
		}
		foreach (DelayedCallbackBlock val2 in DelayedCallbacksBlock)
		{
			if (val2.ListenerId == callbackId)
			{
				DelayedCallbacksBlock.Remove(val2);
				return;
			}
		}
		foreach (KeyValuePair<BlockPos, DelayedCallbackBlock> val in SingleDelayedCallbacksBlock)
		{
			if (val.Value.ListenerId == callbackId)
			{
				SingleDelayedCallbacksBlock.Remove(val.Key);
				break;
			}
		}
	}

	public void RemoveGameTickListener(long listenerId)
	{
		if (listenerId == 0L)
		{
			return;
		}
		int indexB;
		if (GameTickListenersEntityIndices.TryRemove(listenerId, out var index))
		{
			GameTickListener listener3 = GameTickListenersEntity[index];
			if (listener3 != null && listener3.ListenerId == listenerId)
			{
				GameTickListenersEntity[index] = null;
				return;
			}
		}
		else if (GameTickListenersBlockIndices.TryRemove(listenerId, out indexB))
		{
			GameTickListenerBlock listener4 = GameTickListenersBlock[indexB];
			if (listener4 != null && listener4.ListenerId == listenerId)
			{
				GameTickListenersBlock[indexB] = null;
				return;
			}
		}
		List<GameTickListener> GameTickListenersEntityLocal = GameTickListenersEntity;
		for (int j = 0; j < GameTickListenersEntityLocal.Count; j++)
		{
			GameTickListener listener2 = GameTickListenersEntityLocal[j];
			if (listener2 != null && listener2.ListenerId == listenerId)
			{
				GameTickListenersEntityLocal[j] = null;
				return;
			}
		}
		List<GameTickListenerBlock> GameTickListenersBlockLocal = GameTickListenersBlock;
		for (int i = 0; i < GameTickListenersBlockLocal.Count; i++)
		{
			GameTickListenerBlock listener = GameTickListenersBlockLocal[i];
			if (listener != null && listener.ListenerId == listenerId)
			{
				GameTickListenersBlockLocal[i] = null;
				break;
			}
		}
	}
}
