using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.Gui;

public class MainMenuEventApi : IClientEventAPI, IEventAPI
{
	public event ChatLineDelegate ChatMessage;

	public event ClientChatLineDelegate OnSendChatMessage;

	public event PlayerEventDelegate PlayerJoin;

	public event PlayerEventDelegate PlayerLeave;

	public event PlayerEventDelegate PlayerEntitySpawn;

	public event PlayerEventDelegate PlayerEntityDespawn;

	public event OnGamePauseResume PauseResume;

	public event Action LeaveWorld;

	public event BlockChangedDelegate BlockChanged;

	public event Vintagestory.API.Common.Func<ActiveSlotChangeEventArgs, EnumHandling> BeforeActiveSlotChanged;

	public event Action<ActiveSlotChangeEventArgs> AfterActiveSlotChanged;

	public event IngameErrorDelegate InGameError;

	public event IngameDiscoveryDelegate InGameDiscovery;

	public event Action BlockTexturesLoaded;

	public event ActionBoolReturn ReloadShader;

	public event Action ReloadTextures;

	public event Action LevelFinalize;

	public event Action ReloadShapes;

	public event Action HotkeysChanged;

	public event MouseEventDelegate MouseDown;

	public event MouseEventDelegate MouseUp;

	public event MouseEventDelegate MouseMove;

	public event KeyEventDelegate KeyDown;

	public event KeyEventDelegate KeyUp;

	public event FileDropDelegate FileDrop;

	public event EntityDelegate OnEntitySpawn;

	public event EntityDelegate OnEntityLoaded;

	public event EntityDeathDelegate OnEntityDeath;

	public event EntityDespawnDelegate OnEntityDespawn;

	public event ChunkDirtyDelegate ChunkDirty;

	public event OnGetClimateDelegate OnGetClimate;

	public event OnGetWindSpeedDelegate OnGetWindSpeed;

	public event Action LeftWorld;

	public event IsPlayerReadyDelegate IsPlayerReady;

	public event MatchGridRecipeDelegate MatchesGridRecipe;

	public event PlayerEventDelegate PlayerDeath;

	public event TestBlockAccessDelegate OnTestBlockAccess;

	public event MapRegionLoadedDelegate MapRegionLoaded;

	public event MapRegionUnloadDelegate MapRegionUnloaded;

	public event Action ColorsPresetChanged;

	public event TestBlockAccessDelegate TestBlockAccess;

	public event PlayerCommonDelegate PlayerDimensionChanged;

	public void EnqueueMainThreadTask(Action action, string code)
	{
		ScreenManager.MainThreadTasks.Enqueue(action);
	}

	public void PushEvent(string eventName, IAttribute data = null)
	{
		throw new NotImplementedException();
	}

	public void RegisterAsyncParticleSpawner(ContinousParticleSpawnTaskDelegate handler)
	{
		throw new NotImplementedException();
	}

	public long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay)
	{
		throw new NotImplementedException();
	}

	public long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay, bool permittedWhilePaused)
	{
		throw new NotImplementedException();
	}

	public long RegisterCallback(Action<IWorldAccessor, BlockPos, float> OnTimePassed, BlockPos pos, int millisecondDelay)
	{
		throw new NotImplementedException();
	}

	public void RegisterEventBusListener(EventBusListenerDelegate OnEvent, double priority = 0.5, string filterByEventName = null)
	{
		throw new NotImplementedException();
	}

	public long RegisterGameTickListener(Action<float> OnGameTick, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		throw new NotImplementedException();
	}

	public long RegisterGameTickListener(Action<float> OnGameTick, Action<Exception> errorHandler, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		throw new NotImplementedException();
	}

	public long RegisterGameTickListener(Action<IWorldAccessor, BlockPos, float> OnGameTick, BlockPos pos, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		throw new NotImplementedException();
	}

	public void RegisterItemstackRenderer(CollectibleObject forObj, ItemRenderDelegate rendererDelegate, EnumItemRenderTarget target)
	{
		throw new NotImplementedException();
	}

	public void RegisterRenderer(IRenderer renderer, EnumRenderStage renderStage, string profilingName = null)
	{
		throw new NotImplementedException();
	}

	public void TriggerEntityDeath(Entity entity, DamageSource damageSourceForDeath)
	{
		throw new NotImplementedException();
	}

	public bool TriggerMatchesRecipe(IPlayer forPlayer, GridRecipe gridRecipe, ItemSlot[] ingredients, int gridWidth)
	{
		throw new NotImplementedException();
	}

	public void TriggerPlayerDimensionChanged(IPlayer player)
	{
		throw new NotImplementedException();
	}

	public void UnregisterCallback(long listenerId)
	{
		throw new NotImplementedException();
	}

	public void UnregisterGameTickListener(long listenerId)
	{
		throw new NotImplementedException();
	}

	public void UnregisterItemstackRenderer(CollectibleObject forObj, EnumItemRenderTarget target)
	{
		throw new NotImplementedException();
	}

	public void UnregisterRenderer(IRenderer renderer, EnumRenderStage renderStage)
	{
		throw new NotImplementedException();
	}
}
