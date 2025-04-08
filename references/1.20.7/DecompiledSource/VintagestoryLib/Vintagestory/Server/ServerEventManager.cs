using System;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class ServerEventManager : EventManager
{
	internal ServerMain server;

	public Dictionary<EnumServerRunPhase, List<Action>> serverRunPhaseDelegates;

	public List<WorldGenThreadDelegate> WorldgenBlockAccessor = new List<WorldGenThreadDelegate>();

	public Dictionary<string, WorldGenHandler> WorldgenHandlers = new Dictionary<string, WorldGenHandler>();

	public List<EventBusListener> EventBusListeners = new List<EventBusListener>();

	public override ILogger Logger => ServerMain.Logger;

	public override string CommandPrefix => "/";

	public override long InWorldEllapsedMs => server.ElapsedMilliseconds;

	public event Action OnSaveGameCreated;

	public event Action OnSaveGameLoaded;

	public event Action AssetsFirstLoaded;

	public event Action AssetsFinalizer;

	public event Action OnGameWorldBeingSaved;

	public event Action OnWorldgenStartup;

	public event Action OnStartPhysicsThread;

	public event UpnpCompleteDelegate OnUpnpComplete;

	public event PlayerDelegate OnPlayerRespawn;

	public event PlayerDelegate OnPlayerJoin;

	public event PlayerDelegate OnPlayerNowPlaying;

	public event PlayerDelegate OnPlayerLeave;

	public event PlayerDelegate OnPlayerDisconnect;

	public event PlayerChatDelegate OnPlayerChat;

	public event PlayerDeathDelegate OnPlayerDeath;

	public event PlayerDelegate OnPlayerChangeGamemode;

	public event Vintagestory.API.Common.Func<IServerPlayer, ActiveSlotChangeEventArgs, EnumHandling> BeforeActiveSlotChanged;

	public event Action<IServerPlayer, ActiveSlotChangeEventArgs> AfterActiveSlotChanged;

	public event PlayerDelegate OnPlayerCreate;

	public event CanUseDelegate CanUseBlock;

	public event CanPlaceOrBreakDelegate CanPlaceOrBreakBlock;

	public event BlockUsedDelegate DidUseBlock;

	public event BlockPlacedDelegate DidPlaceBlock;

	public event BlockBrokenDelegate DidBreakBlock;

	public event BlockBreakDelegate BreakBlock;

	public event OnInteractDelegate OnPlayerInteractEntity;

	public event EntityDelegate OnEntitySpawn;

	public event EntityDespawnDelegate OnEntityDespawn;

	public event EntityDelegate OnEntityLoaded;

	public event TrySpawnEntityDelegate OnTrySpawnEntity;

	public ServerEventManager(ServerMain server)
	{
		this.server = server;
		Init();
	}

	private void Init()
	{
		serverRunPhaseDelegates = new Dictionary<EnumServerRunPhase, List<Action>>();
		foreach (EnumServerRunPhase stage in Enum.GetValues(typeof(EnumServerRunPhase)))
		{
			serverRunPhaseDelegates[stage] = new List<Action>();
		}
	}

	public override bool HasPrivilege(string playerUid, string privilegecode)
	{
		if (!server.GetServerPlayerData(playerUid).HasPrivilege(privilegecode, server.Config.RolesByCode))
		{
			return playerUid == "console";
		}
		return true;
	}

	internal void RegisterOnServerRunPhase(EnumServerRunPhase runPhase, Action handler)
	{
		server.ModEventManager.serverRunPhaseDelegates[runPhase].Add(handler);
	}

	public virtual void TriggerUpnpComplete(bool success)
	{
		Trigger(this.OnUpnpComplete?.GetInvocationList(), "OnUpnpComplete", delegate(UpnpCompleteDelegate dele)
		{
			dele?.Invoke(success);
		});
	}

	public virtual void TriggerEntityLoaded(Entity entity)
	{
		Trigger(this.OnEntityLoaded?.GetInvocationList(), "OnEntityLoaded", delegate(EntityDelegate dele)
		{
			dele?.Invoke(entity);
		});
	}

	public virtual void TriggerEntitySpawned(Entity entity)
	{
		Trigger(this.OnEntitySpawn?.GetInvocationList(), "OnEntitySpawn", delegate(EntityDelegate dele)
		{
			dele?.Invoke(entity);
		});
	}

	public virtual bool TriggerTrySpawnEntity(IBlockAccessor blockaccessor, ref EntityProperties properties, Vec3d position, long herdId)
	{
		if (this.OnTrySpawnEntity == null)
		{
			return true;
		}
		bool allow = true;
		Delegate[] invocationList = this.OnTrySpawnEntity.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			TrySpawnEntityDelegate dele = (TrySpawnEntityDelegate)invocationList[i];
			try
			{
				allow &= dele(blockaccessor, ref properties, position, herdId);
			}
			catch (Exception e)
			{
				ServerMain.Logger.Error("Exception thrown during handling event OnTrySpawnEntity. Will skip over.");
				ServerMain.Logger.Error(e);
			}
		}
		return allow;
	}

	public virtual void TriggerEntityDespawned(Entity entity, EntityDespawnData reason)
	{
		Trigger(this.OnEntityDespawn?.GetInvocationList(), "OnEntityDespawned", delegate(EntityDespawnDelegate dele)
		{
			dele?.Invoke(entity, reason);
		});
	}

	public virtual void OnAssetsFirstLoaded()
	{
		Trigger(this.AssetsFirstLoaded?.GetInvocationList(), "AssetsFirstLoaded", delegate(Action dele)
		{
			dele?.Invoke();
		});
	}

	public virtual void TriggerFinalizeAssets()
	{
		Trigger(this.AssetsFinalizer?.GetInvocationList(), "FinalizeAssets", delegate(Action dele)
		{
			dele?.Invoke();
		});
	}

	public virtual void TriggerWorldgenStartup()
	{
		Trigger(this.OnWorldgenStartup?.GetInvocationList(), "OnWorldgenStartup", delegate(Action dele)
		{
			dele?.Invoke();
		});
	}

	public virtual void TriggerPhysicsThreadStart()
	{
		this.OnStartPhysicsThread?.Invoke();
	}

	public virtual void TriggerSaveGameLoaded()
	{
		Trigger(this.OnSaveGameLoaded?.GetInvocationList(), "OnSaveGameLoaded", delegate(Action dele)
		{
			dele?.Invoke();
		});
	}

	public virtual void TriggerSaveGameCreated()
	{
		Trigger(this.OnSaveGameCreated?.GetInvocationList(), "OnSaveGameCreated", delegate(Action dele)
		{
			dele?.Invoke();
		});
	}

	public virtual void TriggerGameWorldBeingSaved()
	{
		if (this.OnGameWorldBeingSaved == null)
		{
			return;
		}
		Delegate[] invocationList = this.OnGameWorldBeingSaved.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			Action val = (Action)invocationList[i];
			try
			{
				val();
				FrameProfilerUtil frameProfiler = ServerMain.FrameProfiler;
				if (frameProfiler != null && frameProfiler.Enabled)
				{
					ServerMain.FrameProfiler.Mark("beingsaved-" + val.Target.ToString());
				}
			}
			catch (Exception e)
			{
				ServerMain.Logger.Error("Exception thrown during handling event OnGameWorldBeingSaved. Will skip over.");
				ServerMain.Logger.Error(e);
			}
		}
	}

	public virtual void TriggerDidUseBlock(IServerPlayer player, BlockSelection blockSel)
	{
		Trigger(this.DidUseBlock?.GetInvocationList(), "DidUseBlock", delegate(BlockUsedDelegate dele)
		{
			dele?.Invoke(player, blockSel);
		});
	}

	public virtual void TriggerDidBreakBlock(IServerPlayer player, int oldBlockId, BlockSelection blockSel)
	{
		Trigger(this.DidBreakBlock?.GetInvocationList(), "DidBreakBlock", delegate(BlockBrokenDelegate dele)
		{
			dele?.Invoke(player, oldBlockId, blockSel);
		});
	}

	public virtual void TriggerBreakBlock(IServerPlayer player, BlockSelection blockSel, ref float dropQuantityMultiplier, ref EnumHandling handling)
	{
		if (this.BreakBlock == null)
		{
			return;
		}
		Delegate[] invocationList = this.BreakBlock.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			BlockBreakDelegate dele = (BlockBreakDelegate)invocationList[i];
			try
			{
				dele?.Invoke(player, blockSel, ref dropQuantityMultiplier, ref handling);
				if (handling == EnumHandling.PreventSubsequent)
				{
					break;
				}
			}
			catch (Exception ex)
			{
				ServerMain.Logger.Error("Mod exception during event BreakBlock. Will skip over");
				ServerMain.Logger.Error(ex);
			}
		}
	}

	public virtual void TriggerPlayerInteractEntity(Entity entity, IPlayer byPlayer, ItemSlot slot, Vec3d hitPosition, int mode, ref EnumHandling handling)
	{
		if (this.OnPlayerInteractEntity == null)
		{
			return;
		}
		Delegate[] invocationList = this.OnPlayerInteractEntity.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			OnInteractDelegate dele = (OnInteractDelegate)invocationList[i];
			try
			{
				dele?.Invoke(entity, byPlayer, slot, hitPosition, mode, ref handling);
				if (handling == EnumHandling.PreventSubsequent)
				{
					break;
				}
			}
			catch (Exception ex)
			{
				ServerMain.Logger.Error("Mod exception during event BreakBlock. Will skip over");
				ServerMain.Logger.Error(ex);
			}
		}
	}

	public virtual void TriggerDidPlaceBlock(IServerPlayer player, int oldBlockId, BlockSelection blockSel, ItemStack withItemStack)
	{
		Trigger(this.DidPlaceBlock?.GetInvocationList(), "DidPlaceBlock", delegate(BlockPlacedDelegate dele)
		{
			dele?.Invoke(player, oldBlockId, blockSel, withItemStack);
		});
	}

	public virtual void TriggerPlayerLeave(IServerPlayer player)
	{
		Trigger(this.OnPlayerLeave?.GetInvocationList(), "OnPlayerLeave", delegate(PlayerDelegate dele)
		{
			dele?.Invoke(player);
		});
	}

	public virtual void TriggerPlayerDisconnect(IServerPlayer player)
	{
		Trigger(this.OnPlayerDisconnect?.GetInvocationList(), "OnPlayerDisconnect", delegate(PlayerDelegate dele)
		{
			dele?.Invoke(player);
		});
	}

	public virtual void TriggerPlayerCreate(IServerPlayer player)
	{
		Trigger(this.OnPlayerCreate?.GetInvocationList(), "OnPlayerCreate", delegate(PlayerDelegate dele)
		{
			dele?.Invoke(player);
		});
	}

	public virtual void TriggerPlayerJoin(IServerPlayer player)
	{
		Trigger(this.OnPlayerJoin?.GetInvocationList(), "OnPlayerJoin", delegate(PlayerDelegate dele)
		{
			dele?.Invoke(player);
		});
	}

	public virtual void TriggerPlayerNowPlaying(IServerPlayer player)
	{
		Trigger(this.OnPlayerNowPlaying?.GetInvocationList(), "OnPlayerNowPlaying", delegate(PlayerDelegate dele)
		{
			dele?.Invoke(player);
		});
	}

	public virtual void TriggerPlayerRespawn(IServerPlayer player)
	{
		Trigger(this.OnPlayerRespawn?.GetInvocationList(), "OnPlayerRespawn", delegate(PlayerDelegate dele)
		{
			dele?.Invoke(player);
		});
	}

	public virtual bool TriggerBeforeActiveSlotChanged(IServerPlayer player, int fromSlot, int toSlot)
	{
		ActiveSlotChangeEventArgs args = new ActiveSlotChangeEventArgs(fromSlot, toSlot);
		return this.BeforeActiveSlotChanged?.InvokeSafeCancellable(Logger, "BeforeActiveSlotChanged", player, args) ?? true;
	}

	public virtual void TriggerAfterActiveSlotChanged(IServerPlayer player, int fromSlot, int toSlot)
	{
		ActiveSlotChangeEventArgs args = new ActiveSlotChangeEventArgs(fromSlot, toSlot);
		Trigger(this.AfterActiveSlotChanged?.GetInvocationList(), "AfterActiveSlotChanged", delegate(Action<IServerPlayer, ActiveSlotChangeEventArgs> dele)
		{
			dele?.Invoke(player, args);
		});
	}

	public virtual void TriggerOnplayerChat(IServerPlayer player, int channelId, ref string message, ref string data, BoolRef consumed)
	{
		if (this.OnPlayerChat == null)
		{
			return;
		}
		Delegate[] invocationList = this.OnPlayerChat.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			PlayerChatDelegate dele = (PlayerChatDelegate)invocationList[i];
			try
			{
				dele(player, channelId, ref message, ref data, consumed);
			}
			catch (Exception ex)
			{
				Logger.Error("Mod exception: OnPlayerChat");
				Logger.Error(ex);
			}
			if (consumed.value)
			{
				break;
			}
		}
	}

	public virtual void TriggerPlayerDeath(IServerPlayer player, DamageSource source)
	{
		Trigger(this.OnPlayerDeath?.GetInvocationList(), "OnPlayerDeath", delegate(PlayerDeathDelegate dele)
		{
			dele?.Invoke(player, source);
		});
	}

	public virtual void TriggerPlayerChangeGamemode(IServerPlayer player)
	{
		Trigger(this.OnPlayerChangeGamemode?.GetInvocationList(), "OnPlayerChangeGamemode", delegate(PlayerDelegate dele)
		{
			dele?.Invoke(player);
		});
	}

	public virtual bool TriggerCanPlaceOrBreak(IServerPlayer player, BlockSelection blockSel, out string claimant)
	{
		claimant = null;
		if (this.CanPlaceOrBreakBlock == null)
		{
			return true;
		}
		bool retval = true;
		Delegate[] invocationList = this.CanPlaceOrBreakBlock.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			CanPlaceOrBreakDelegate dele = (CanPlaceOrBreakDelegate)invocationList[i];
			try
			{
				retval = retval && dele(player, blockSel, out claimant);
			}
			catch (Exception ex)
			{
				Logger.Error("Mod exception during CanPlaceOrBreak");
				Logger.Error(ex);
				retval = false;
				break;
			}
		}
		return retval;
	}

	public virtual bool TriggerCanUse(IServerPlayer player, BlockSelection blockSel)
	{
		bool retval = true;
		Trigger(this.CanUseBlock?.GetInvocationList(), "CanUse", delegate(CanUseDelegate dele)
		{
			retval = retval && dele(player, blockSel);
		}, delegate
		{
			retval = false;
		});
		return retval;
	}

	public void Trigger<T>(Delegate[] delegates, string eventName, Action<T> onDele, Action onException = null) where T : Delegate
	{
		if (delegates == null)
		{
			return;
		}
		for (int i = 0; i < delegates.Length; i++)
		{
			T dele = (T)delegates[i];
			try
			{
				onDele(dele);
			}
			catch (Exception ex)
			{
				ServerMain.Logger.Error("Mod exception during event " + eventName + ". Will skip to next event");
				ServerMain.Logger.Error(ex);
				onException?.Invoke();
			}
		}
	}

	public void Trigger<T>(Delegate[] delegates, string eventName, ActionBoolReturn<T> onDele, Action onException = null) where T : Delegate
	{
		if (delegates == null)
		{
			return;
		}
		for (int i = 0; i < delegates.Length; i++)
		{
			T dele = (T)delegates[i];
			try
			{
				if (!onDele(dele))
				{
					break;
				}
			}
			catch (Exception ex)
			{
				ServerMain.Logger.Error("Mod exception during event " + eventName + ". Will skip to next event");
				ServerMain.Logger.Error(ex);
				onException?.Invoke();
			}
		}
	}

	public WorldGenHandler GetWorldGenHandler(string worldType)
	{
		WorldGenHandler handler = null;
		WorldgenHandlers.TryGetValue(worldType, out handler);
		return handler;
	}

	public WorldGenHandler GetOrCreateWorldGenHandler(string worldType)
	{
		WorldGenHandler handler = null;
		if (!WorldgenHandlers.TryGetValue(worldType, out handler))
		{
			handler = (WorldgenHandlers[worldType] = new WorldGenHandler());
		}
		return handler;
	}

	public void WipeAllDelegates()
	{
		serverRunPhaseDelegates.Clear();
		this.AssetsFirstLoaded = null;
		this.AssetsFinalizer = null;
		this.OnSaveGameLoaded = null;
		this.OnGameWorldBeingSaved = null;
		this.DidUseBlock = null;
		this.DidPlaceBlock = null;
		this.DidBreakBlock = null;
		this.OnPlayerRespawn = null;
		this.OnPlayerCreate = null;
		this.OnPlayerJoin = null;
		this.OnPlayerNowPlaying = null;
		this.OnPlayerLeave = null;
		this.OnPlayerDisconnect = null;
		this.OnPlayerChat = null;
		this.OnPlayerDeath = null;
		this.OnEntityLoaded = null;
		this.OnEntityDespawn = null;
		this.OnEntitySpawn = null;
		this.OnTrySpawnEntity = null;
		this.OnPlayerInteractEntity = null;
		this.CanUseBlock = null;
		this.CanPlaceOrBreakBlock = null;
		GameTickListenersEntity.Clear();
		DelayedCallbacksEntity.Clear();
		GameTickListenersBlock.Clear();
		DelayedCallbacksBlock.Clear();
		Logger.ClearWatchers();
		this.OnPlayerChangeGamemode = null;
		this.BeforeActiveSlotChanged = null;
		this.AfterActiveSlotChanged = null;
		foreach (KeyValuePair<string, WorldGenHandler> worldgenHandler in WorldgenHandlers)
		{
			worldgenHandler.Value.WipeAllHandlers();
		}
		Init();
	}

	public override long AddGameTickListener(Action<float> handler, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.ServerMainThreadId)
		{
			string StackTrace = Environment.StackTrace;
			server.EnqueueMainThreadTask(delegate
			{
				ServerMain.Logger.Error("Warning: Attempting to add an entity listener outside of the main thread. " + Thread.CurrentThread.Name + " This may produce a race condition!\r\n" + StackTrace);
			});
		}
		return AddGameTickListener(handler, null, millisecondInterval, initialDelayOffsetMs);
	}

	public override long AddGameTickListener(Action<float> handler, Action<Exception> errorHandler, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.ServerMainThreadId)
		{
			string StackTrace = Environment.StackTrace;
			server.EnqueueMainThreadTask(delegate
			{
				ServerMain.Logger.Error("Warning: Attempting to add a BlockEntity listener outside of the main thread. " + Thread.CurrentThread.Name + " This may produce a race condition!\r\n" + StackTrace);
			});
		}
		return base.AddGameTickListener(handler, errorHandler, millisecondInterval, initialDelayOffsetMs);
	}

	public override long AddDelayedCallback(Action<IWorldAccessor, BlockPos, float> handler, BlockPos pos, long callAfterEllapsedMS)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.ServerMainThreadId)
		{
			string StackTrace = Environment.StackTrace;
			server.EnqueueMainThreadTask(delegate
			{
				ServerMain.Logger.Error("Warning: Attempting to add a block callback outside of the main thread. " + Thread.CurrentThread.Name + " This may produce a race condition!\r\n" + StackTrace);
			});
		}
		return base.AddDelayedCallback(handler, pos, callAfterEllapsedMS);
	}

	internal override long AddSingleDelayedCallback(Action<IWorldAccessor, BlockPos, float> handler, BlockPos pos, long callAfterEllapsedMs)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.ServerMainThreadId)
		{
			string StackTrace = Environment.StackTrace;
			server.EnqueueMainThreadTask(delegate
			{
				ServerMain.Logger.Error("Warning: Attempting to add a single block callback outside of the main thread. " + Thread.CurrentThread.Name + " This may produce a race condition!\r\n" + StackTrace);
			});
		}
		return base.AddSingleDelayedCallback(handler, pos, callAfterEllapsedMs);
	}
}
