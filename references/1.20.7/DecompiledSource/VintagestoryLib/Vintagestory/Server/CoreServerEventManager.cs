using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class CoreServerEventManager : ServerEventManager
{
	private ServerEventManager modEventManager;

	internal void defragLists()
	{
		prune(server.EventManager.GameTickListenersBlock);
		prune(server.EventManager.GameTickListenersEntity);
		defrag(server.EventManager.DelayedCallbacksBlock);
		ServerMain.Logger.Notification("Defragmented listener lists");
	}

	internal static void defrag<T>(List<T> list)
	{
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] == null)
			{
				list.RemoveAt(i);
				i--;
			}
		}
	}

	internal static void prune<T>(List<T> list) where T : GameTickListenerBase
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.ServerMainThreadId)
		{
			throw new InvalidOperationException("Attempting to defrag listeners outside of the main thread. This may produce a race condition!");
		}
		int last = list.Count - 1;
		while (last >= 0 && list[last] == null)
		{
			list.RemoveAt(last);
			last--;
		}
	}

	public CoreServerEventManager(ServerMain server, ServerEventManager modEventManager)
		: base(server)
	{
		this.modEventManager = modEventManager;
	}

	public override void TriggerPlayerInteractEntity(Entity entity, IPlayer byPlayer, ItemSlot slot, Vec3d hitPosition, int mode, ref EnumHandling handling)
	{
		base.TriggerPlayerInteractEntity(entity, byPlayer, slot, hitPosition, mode, ref handling);
		modEventManager.TriggerPlayerInteractEntity(entity, byPlayer, slot, hitPosition, mode, ref handling);
	}

	public override void TriggerDidBreakBlock(IServerPlayer player, int oldBlockId, BlockSelection blockSel)
	{
		base.TriggerDidBreakBlock(player, oldBlockId, blockSel);
		modEventManager.TriggerDidBreakBlock(player, oldBlockId, blockSel);
	}

	public override void TriggerBreakBlock(IServerPlayer player, BlockSelection blockSel, ref float dropQuantityMultiplier, ref EnumHandling handling)
	{
		base.TriggerBreakBlock(player, blockSel, ref dropQuantityMultiplier, ref handling);
		modEventManager.TriggerBreakBlock(player, blockSel, ref dropQuantityMultiplier, ref handling);
	}

	public override void TriggerDidPlaceBlock(IServerPlayer player, int oldBlockId, BlockSelection blockSel, ItemStack withItemStack)
	{
		base.TriggerDidPlaceBlock(player, oldBlockId, blockSel, withItemStack);
		modEventManager.TriggerDidPlaceBlock(player, oldBlockId, blockSel, withItemStack);
	}

	public override void TriggerDidUseBlock(IServerPlayer player, BlockSelection blockSel)
	{
		base.TriggerDidUseBlock(player, blockSel);
		modEventManager.TriggerDidUseBlock(player, blockSel);
	}

	public override void TriggerGameTick(long ellapsedMilliseconds, IWorldAccessor world)
	{
		base.TriggerGameTick(ellapsedMilliseconds, server);
		modEventManager.TriggerGameTick(ellapsedMilliseconds, server);
	}

	public override void TriggerGameTickDebug(long ellapsedMilliseconds, IWorldAccessor world)
	{
		base.TriggerGameTickDebug(ellapsedMilliseconds, server);
		modEventManager.TriggerGameTickDebug(ellapsedMilliseconds, server);
	}

	public override bool TriggerCanPlaceOrBreak(IServerPlayer player, BlockSelection blockSel, out string claimant)
	{
		if (base.TriggerCanPlaceOrBreak(player, blockSel, out claimant))
		{
			return modEventManager.TriggerCanPlaceOrBreak(player, blockSel, out claimant);
		}
		return false;
	}

	public override bool TriggerCanUse(IServerPlayer player, BlockSelection blockSel)
	{
		if (base.TriggerCanUse(player, blockSel))
		{
			return modEventManager.TriggerCanUse(player, blockSel);
		}
		return false;
	}

	public override void TriggerOnplayerChat(IServerPlayer player, int channelId, ref string message, ref string data, BoolRef consumed)
	{
		base.TriggerOnplayerChat(player, channelId, ref message, ref data, consumed);
		if (!consumed.value)
		{
			modEventManager.TriggerOnplayerChat(player, channelId, ref message, ref data, consumed);
		}
	}

	public override void TriggerPlayerDisconnect(IServerPlayer player)
	{
		base.TriggerPlayerDisconnect(player);
		modEventManager.TriggerPlayerDisconnect(player);
	}

	public override void TriggerPlayerJoin(IServerPlayer player)
	{
		base.TriggerPlayerJoin(player);
		modEventManager.TriggerPlayerJoin(player);
	}

	public override void TriggerPlayerNowPlaying(IServerPlayer player)
	{
		base.TriggerPlayerNowPlaying(player);
		modEventManager.TriggerPlayerNowPlaying(player);
	}

	public override void TriggerPlayerLeave(IServerPlayer player)
	{
		base.TriggerPlayerLeave(player);
		modEventManager.TriggerPlayerLeave(player);
	}

	public override bool TriggerBeforeActiveSlotChanged(IServerPlayer player, int fromSlot, int toSlot)
	{
		if (modEventManager.TriggerBeforeActiveSlotChanged(player, fromSlot, toSlot))
		{
			return base.TriggerBeforeActiveSlotChanged(player, fromSlot, toSlot);
		}
		return false;
	}

	public override void TriggerAfterActiveSlotChanged(IServerPlayer player, int fromSlot, int toSlot)
	{
		modEventManager.TriggerAfterActiveSlotChanged(player, fromSlot, toSlot);
		base.TriggerAfterActiveSlotChanged(player, fromSlot, toSlot);
	}

	public override void TriggerPlayerRespawn(IServerPlayer player)
	{
		base.TriggerPlayerRespawn(player);
		modEventManager.TriggerPlayerRespawn(player);
	}

	public override void TriggerPlayerCreate(IServerPlayer player)
	{
		base.TriggerPlayerCreate(player);
		modEventManager.TriggerPlayerCreate(player);
	}

	public override bool TriggerTrySpawnEntity(IBlockAccessor blockaccessor, ref EntityProperties properties, Vec3d position, long herdId)
	{
		if (base.TriggerTrySpawnEntity(blockaccessor, ref properties, position, herdId))
		{
			return modEventManager.TriggerTrySpawnEntity(blockaccessor, ref properties, position, herdId);
		}
		return false;
	}

	public override void TriggerGameWorldBeingSaved()
	{
		modEventManager.TriggerGameWorldBeingSaved();
		base.TriggerGameWorldBeingSaved();
	}

	public override void TriggerSaveGameLoaded()
	{
		base.TriggerSaveGameLoaded();
		modEventManager.TriggerSaveGameLoaded();
	}

	public override void TriggerEntitySpawned(Entity entity)
	{
		base.TriggerEntitySpawned(entity);
		modEventManager.TriggerEntitySpawned(entity);
	}

	public override void TriggerEntityDespawned(Entity entity, EntityDespawnData reason)
	{
		base.TriggerEntityDespawned(entity, reason);
		modEventManager.TriggerEntityDespawned(entity, reason);
	}

	public override void TriggerPlayerChangeGamemode(IServerPlayer player)
	{
		base.TriggerPlayerChangeGamemode(player);
		modEventManager.TriggerPlayerChangeGamemode(player);
	}

	public override void TriggerEntityLoaded(Entity entity)
	{
		base.TriggerEntityLoaded(entity);
		modEventManager.TriggerEntityLoaded(entity);
	}

	public override void TriggerPlayerDeath(IServerPlayer player, DamageSource source)
	{
		base.TriggerPlayerDeath(player, source);
		modEventManager.TriggerPlayerDeath(player, source);
	}

	public override void TriggerOnGetClimate(ref ClimateCondition climate, BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.WorldGenValues, double totalDays = 0.0)
	{
		base.TriggerOnGetClimate(ref climate, pos, mode, totalDays);
		modEventManager.TriggerOnGetClimate(ref climate, pos, mode, totalDays);
	}

	public override void TriggerOnGetWindSpeed(Vec3d pos, ref Vec3d windSpeed)
	{
		base.TriggerOnGetWindSpeed(pos, ref windSpeed);
		modEventManager.TriggerOnGetWindSpeed(pos, ref windSpeed);
	}
}
