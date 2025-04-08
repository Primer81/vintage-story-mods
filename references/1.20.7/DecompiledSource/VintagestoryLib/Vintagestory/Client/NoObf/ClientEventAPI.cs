using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ClientEventAPI : IClientEventAPI, IEventAPI
{
	private static string strThreadException = "Cannot call this method outside the main thread. Not thread safe to do so. You can use the thread safe method .EnqueueMainThreadTask() to queue up the register on the main thread instead.";

	private ClientMain game;

	internal Dictionary<int, ItemRenderDelegate>[][] itemStackRenderersByTarget;

	public event MouseEventDelegate MouseDown;

	public event MouseEventDelegate MouseUp;

	public event MouseEventDelegate MouseMove;

	public event KeyEventDelegate KeyDown;

	public event KeyEventDelegate KeyUp;

	public event FileDropDelegate FileDrop;

	public event PlayerEventDelegate PlayerJoin;

	public event PlayerEventDelegate PlayerLeave;

	public event PlayerEventDelegate PlayerEntitySpawn;

	public event PlayerEventDelegate PlayerEntityDespawn;

	public event Action BlockTexturesLoaded;

	public event IsPlayerReadyDelegate IsPlayerReady;

	public event OnGamePauseResume PauseResume;

	public event Action LeaveWorld;

	public event Action LeftWorld;

	public event ChatLineDelegate ChatMessage;

	public event BlockChangedDelegate BlockChanged;

	public event Vintagestory.API.Common.Func<ActiveSlotChangeEventArgs, EnumHandling> BeforeActiveSlotChanged;

	public event Action<ActiveSlotChangeEventArgs> AfterActiveSlotChanged;

	public event ChunkDirtyDelegate ChunkDirty;

	public event Action LevelFinalize;

	public event Action HotkeysChanged;

	public event ClientChatLineDelegate OnSendChatMessage;

	public event EntityDeathDelegate OnEntityDeath;

	public event MatchGridRecipeDelegate MatchesGridRecipe;

	public event PlayerEventDelegate PlayerDeath;

	public event TestBlockAccessDelegate OnTestBlockAccess;

	public event MapRegionLoadedDelegate MapRegionLoaded;

	public event MapRegionUnloadDelegate MapRegionUnloaded;

	public event PlayerCommonDelegate PlayerDimensionChanged;

	public event OnGetClimateDelegate OnGetClimate
	{
		add
		{
			ClientEventManager em = game.eventManager;
			if (em != null)
			{
				em.OnGetClimate += value;
			}
		}
		remove
		{
			ClientEventManager em = game.eventManager;
			if (em != null)
			{
				em.OnGetClimate -= value;
			}
		}
	}

	public event TestBlockAccessDelegate TestBlockAccess
	{
		add
		{
			OnTestBlockAccess += value;
		}
		remove
		{
			OnTestBlockAccess -= value;
		}
	}

	public event OnGetWindSpeedDelegate OnGetWindSpeed
	{
		add
		{
			ClientEventManager em = game.eventManager;
			if (em != null)
			{
				em.OnGetWindSpeed += value;
			}
		}
		remove
		{
			ClientEventManager em = game.eventManager;
			if (em != null)
			{
				em.OnGetWindSpeed -= value;
			}
		}
	}

	public event IngameErrorDelegate InGameError
	{
		add
		{
			ClientEventManager em = game.eventManager;
			if (em != null)
			{
				em.InGameError += value;
			}
		}
		remove
		{
			ClientEventManager em = game.eventManager;
			if (em != null)
			{
				em.InGameError -= value;
			}
		}
	}

	public event IngameDiscoveryDelegate InGameDiscovery
	{
		add
		{
			ClientEventManager em = game.eventManager;
			if (em != null)
			{
				em.InGameDiscovery += value;
			}
		}
		remove
		{
			ClientEventManager em = game.eventManager;
			if (em != null)
			{
				em.InGameDiscovery -= value;
			}
		}
	}

	public event Action ColorsPresetChanged
	{
		add
		{
			ClientEventManager em = game.eventManager;
			if (em != null)
			{
				em.ColorPresetChanged += value;
			}
		}
		remove
		{
			ClientEventManager em = game.eventManager;
			if (em != null)
			{
				em.ColorPresetChanged -= value;
			}
		}
	}

	public event EntityDelegate OnEntitySpawn
	{
		add
		{
			game.eventManager?.OnEntitySpawn.Add(value);
		}
		remove
		{
			game.eventManager?.OnEntitySpawn.Remove(value);
		}
	}

	public event EntityDelegate OnEntityLoaded
	{
		add
		{
			game.eventManager?.OnEntityLoaded.Add(value);
		}
		remove
		{
			game.eventManager?.OnEntityLoaded.Remove(value);
		}
	}

	public event EntityDespawnDelegate OnEntityDespawn
	{
		add
		{
			game.eventManager?.OnEntityDespawn.Add(value);
		}
		remove
		{
			game.eventManager?.OnEntityDespawn.Remove(value);
		}
	}

	public event ActionBoolReturn ReloadShader
	{
		add
		{
			game.eventManager?.OnReloadShaders.Add(value);
		}
		remove
		{
			game.eventManager?.OnReloadShaders.Remove(value);
		}
	}

	public event Action ReloadTextures
	{
		add
		{
			ClientEventManager em = game.eventManager;
			if (em != null)
			{
				em.OnReloadTextures += value;
			}
		}
		remove
		{
			ClientEventManager em = game.eventManager;
			if (em != null)
			{
				em.OnReloadTextures -= value;
			}
		}
	}

	public event Action ReloadShapes
	{
		add
		{
			ClientEventManager em = game.eventManager;
			if (em != null)
			{
				em.OnReloadShapes += value;
			}
		}
		remove
		{
			ClientEventManager em = game.eventManager;
			if (em != null)
			{
				em.OnReloadShapes -= value;
			}
		}
	}

	public ClientEventAPI(ClientMain game)
	{
		this.game = game;
		game.eventManager?.OnNewServerToClientChatLine.Add(onChatLine);
		game.eventManager?.OnPlayerDeath.Add(playerDeath);
		int len = Enum.GetNames(typeof(EnumItemRenderTarget)).Length;
		itemStackRenderersByTarget = new Dictionary<int, ItemRenderDelegate>[2][];
		itemStackRenderersByTarget[0] = new Dictionary<int, ItemRenderDelegate>[len];
		itemStackRenderersByTarget[1] = new Dictionary<int, ItemRenderDelegate>[len];
		for (int i = 0; i < len; i++)
		{
			itemStackRenderersByTarget[0][i] = new Dictionary<int, ItemRenderDelegate>();
			itemStackRenderersByTarget[1][i] = new Dictionary<int, ItemRenderDelegate>();
		}
	}

	private void playerDeath(int clientid, int livesLeft)
	{
		this.PlayerDeath?.Invoke(game.AllPlayers.FirstOrDefault((IPlayer plr) => plr.ClientId == clientid) as IClientPlayer);
	}

	public EnumWorldAccessResponse TriggerTestBlockAccess(IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, ref string claimant, EnumWorldAccessResponse response)
	{
		if (this.OnTestBlockAccess == null)
		{
			return response;
		}
		Delegate[] invocationList = this.OnTestBlockAccess.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			response = ((TestBlockAccessDelegate)invocationList[i])(player, blockSel, accessType, ref claimant, response);
		}
		return response;
	}

	public void TriggerMapregionLoaded(Vec2i mapCoord, IMapRegion mapregion)
	{
		this.MapRegionLoaded?.Invoke(mapCoord, mapregion);
	}

	public void TriggerMapregionUnloaded(Vec2i mapCoord, IMapRegion mapregion)
	{
		this.MapRegionUnloaded?.Invoke(mapCoord, mapregion);
	}

	public void TriggerChunkDirty(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason)
	{
		this.ChunkDirty?.Invoke(chunkCoord, chunk, reason);
	}

	private void onChatLine(int groupId, string message, EnumChatType chattype, string data)
	{
		this.ChatMessage?.Invoke(groupId, message, chattype, data);
	}

	public void TriggerBlockChanged(BlockPos pos, Block oldBlock)
	{
		this.BlockChanged?.Invoke(pos, oldBlock);
	}

	public void TriggerPlayerJoin(IClientPlayer plr)
	{
		this.PlayerJoin?.Invoke(plr);
	}

	public void TriggerPlayerLeave(IClientPlayer plr)
	{
		this.PlayerLeave?.Invoke(plr);
	}

	public void TriggerPlayerEntitySpawn(IClientPlayer plr)
	{
		this.PlayerEntitySpawn?.Invoke(plr);
	}

	public void TriggerPlayerEntityDespawn(IClientPlayer plr)
	{
		this.PlayerEntityDespawn?.Invoke(plr);
	}

	public bool TriggerFileDrop(string filename)
	{
		FileDropEvent ev = new FileDropEvent
		{
			Filename = filename
		};
		Delegate[] invocationList = this.FileDrop.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			((FileDropDelegate)invocationList[i])(ev);
			if (ev.Handled)
			{
				break;
			}
		}
		return ev.Handled;
	}

	public void TriggerPauseResume(bool pause)
	{
		this.PauseResume?.Invoke(pause);
	}

	public void TriggerMouseDown(MouseEvent ev)
	{
		if (this.MouseDown == null)
		{
			return;
		}
		Delegate[] invocationList = this.MouseDown.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			((MouseEventDelegate)invocationList[i])(ev);
			if (ev.Handled)
			{
				break;
			}
		}
	}

	public void TriggerMouseUp(MouseEvent ev)
	{
		if (this.MouseUp == null)
		{
			return;
		}
		Delegate[] invocationList = this.MouseUp.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			((MouseEventDelegate)invocationList[i])(ev);
			if (ev.Handled)
			{
				break;
			}
		}
	}

	public void TriggerMouseMove(MouseEvent ev)
	{
		if (this.MouseMove == null)
		{
			return;
		}
		Delegate[] invocationList = this.MouseMove.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			((MouseEventDelegate)invocationList[i])(ev);
			if (ev.Handled)
			{
				break;
			}
		}
	}

	public void TriggerKeyDown(KeyEvent ev)
	{
		if (this.KeyDown == null)
		{
			return;
		}
		Delegate[] invocationList = this.KeyDown.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			((KeyEventDelegate)invocationList[i])(ev);
			if (ev.Handled)
			{
				break;
			}
		}
	}

	public void TriggerKeyUp(KeyEvent ev)
	{
		if (this.KeyUp == null)
		{
			return;
		}
		Delegate[] invocationList = this.KeyUp.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			((KeyEventDelegate)invocationList[i])(ev);
			if (ev.Handled)
			{
				break;
			}
		}
	}

	internal bool TriggerBeforeActiveSlotChanged(ILogger logger, int fromSlot, int toSlot)
	{
		ActiveSlotChangeEventArgs args = new ActiveSlotChangeEventArgs(fromSlot, toSlot);
		return this.BeforeActiveSlotChanged?.InvokeSafeCancellable(logger, "BeforeActiveSlotChanged", args) ?? true;
	}

	internal void TriggerAfterActiveSlotChanged(ILogger logger, int fromSlot, int toSlot)
	{
		ActiveSlotChangeEventArgs args = new ActiveSlotChangeEventArgs(fromSlot, toSlot);
		this.AfterActiveSlotChanged?.InvokeSafe(logger, "AfterActiveSlotChanged", args);
	}

	public void TriggerLevelFinalize()
	{
		this.LevelFinalize?.Invoke();
	}

	public void TriggerLeaveWorld()
	{
		this.LeaveWorld?.Invoke();
	}

	public void TriggerLeftWorld()
	{
		this.LeftWorld?.Invoke();
	}

	public void TriggerHotkeysChanged()
	{
		this.HotkeysChanged?.Invoke();
	}

	public void TriggerBlockTexturesLoaded()
	{
		this.BlockTexturesLoaded?.Invoke();
	}

	public bool TriggerIsPlayerReady()
	{
		if (this.IsPlayerReady == null)
		{
			return true;
		}
		EnumHandling handling = EnumHandling.PassThrough;
		bool ok = true;
		Delegate[] invocationList = this.IsPlayerReady.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			bool hereOk = ((IsPlayerReadyDelegate)invocationList[i])(ref handling);
			if (handling != 0)
			{
				ok = ok && hereOk;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		return ok;
	}

	public void RegisterRenderer(IRenderer renderer, EnumRenderStage stage, string profilingName = null)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Cannot call this method outside the main thread. Not thread safe to do so.");
		}
		game.eventManager?.RegisterRenderer(renderer, stage, profilingName);
	}

	public void RegisterItemstackRenderer(CollectibleObject forObj, ItemRenderDelegate rendererDelegate, EnumItemRenderTarget target)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Cannot call this method outside the main thread. Not thread safe to do so.");
		}
		itemStackRenderersByTarget[(int)forObj.ItemClass][(int)target][forObj.Id] = rendererDelegate;
	}

	public void UnregisterItemstackRenderer(CollectibleObject forObj, EnumItemRenderTarget target)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Cannot call this method outside the main thread. Not thread safe to do so.");
		}
		itemStackRenderersByTarget[(int)forObj.ItemClass][(int)target].Remove(forObj.Id);
	}

	public void UnregisterRenderer(IRenderer renderer, EnumRenderStage stage)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Cannot call this method outside the main thread. Not thread safe to do so.");
		}
		game.eventManager?.UnregisterRenderer(renderer, stage);
	}

	public void RegisterReloadShapes(Action handler)
	{
		ClientEventManager em = game.eventManager;
		if (em != null)
		{
			em.OnReloadShapes += handler;
		}
	}

	public void UnregisterReloadShapes(Action handler)
	{
		ClientEventManager em = game.eventManager;
		if (em != null)
		{
			em.OnReloadShapes -= handler;
		}
	}

	public void RegisterOnLeaveWorld(Action handler)
	{
		LeaveWorld += handler;
	}

	public void PushEvent(string eventName, IAttribute data = null)
	{
		EnumHandling handling = EnumHandling.PassThrough;
		for (int i = 0; i < game.eventManager?.EventBusListeners.Count; i++)
		{
			EventBusListener listener = game.eventManager?.EventBusListeners[i];
			if (listener.filterByName == null || listener.filterByName.Equals(eventName))
			{
				listener.handler(eventName, ref handling, data);
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	public void RegisterEventBusListener(EventBusListenerDelegate OnEvent, double priority = 0.5, string filterByEventName = null)
	{
		if (game.eventManager == null)
		{
			return;
		}
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException(strThreadException);
		}
		for (int i = 0; i < game.eventManager.EventBusListeners.Count; i++)
		{
			if (!(game.eventManager.EventBusListeners[i].priority >= priority))
			{
				game.eventManager?.EventBusListeners.Insert(i, new EventBusListener
				{
					handler = OnEvent,
					priority = priority,
					filterByName = filterByEventName
				});
				return;
			}
		}
		game.eventManager?.EventBusListeners.Add(new EventBusListener
		{
			handler = OnEvent,
			priority = priority,
			filterByName = filterByEventName
		});
	}

	public long RegisterGameTickListener(Action<float> OnGameTick, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException(strThreadException);
		}
		return game.RegisterGameTickListener(OnGameTick, millisecondInterval, initialDelayOffsetMs);
	}

	public long RegisterGameTickListener(Action<float> OnGameTick, Action<Exception> errorHandler, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException(strThreadException);
		}
		return game.RegisterGameTickListener(OnGameTick, errorHandler, millisecondInterval, initialDelayOffsetMs);
	}

	public long RegisterGameTickListener(Action<IWorldAccessor, BlockPos, float> OnGameTick, BlockPos pos, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException(strThreadException);
		}
		return game.RegisterGameTickListener(OnGameTick, pos, millisecondInterval, initialDelayOffsetMs);
	}

	public long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException(strThreadException);
		}
		return game.RegisterCallback(OnTimePassed, millisecondDelay);
	}

	public long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay, bool permittedWhilePaused)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException(strThreadException);
		}
		return game.RegisterCallback(OnTimePassed, millisecondDelay, permittedWhilePaused);
	}

	public long RegisterCallback(Action<IWorldAccessor, BlockPos, float> OnTimePassed, BlockPos pos, int millisecondDelay)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException(strThreadException);
		}
		return game.RegisterCallback(OnTimePassed, pos, millisecondDelay);
	}

	public void UnregisterCallback(long listenerId)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException(strThreadException);
		}
		game.UnregisterCallback(listenerId);
	}

	public void UnregisterGameTickListener(long listenerId)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException(strThreadException);
		}
		game.UnregisterGameTickListener(listenerId);
	}

	public void EnqueueMainThreadTask(Action action, string code)
	{
		game.EnqueueMainThreadTask(action, code);
	}

	internal void TriggerSendChatMessage(int groupId, ref string message, ref EnumHandling handling)
	{
		if (this.OnSendChatMessage == null)
		{
			return;
		}
		Delegate[] invocationList = this.OnSendChatMessage.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			((ClientChatLineDelegate)invocationList[i])(groupId, ref message, ref handling);
			if (handling == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	public void RegisterAsyncParticleSpawner(ContinousParticleSpawnTaskDelegate handler)
	{
		lock (game.asyncParticleSpawnersLock)
		{
			game.asyncParticleSpawners.Add(handler);
		}
	}

	public void TriggerEntityDeath(Entity entity, DamageSource damageSourceForDeath)
	{
		this.OnEntityDeath?.Invoke(entity, damageSourceForDeath);
	}

	public bool TriggerMatchesRecipe(IPlayer forPlayer, GridRecipe gridRecipe, ItemSlot[] ingredients, int gridWidth)
	{
		if (this.MatchesGridRecipe == null)
		{
			return true;
		}
		return this.MatchesGridRecipe(forPlayer, gridRecipe, ingredients, gridWidth);
	}

	public void TriggerPlayerDimensionChanged(IPlayer player)
	{
		this.PlayerDimensionChanged?.Invoke(player);
	}
}
