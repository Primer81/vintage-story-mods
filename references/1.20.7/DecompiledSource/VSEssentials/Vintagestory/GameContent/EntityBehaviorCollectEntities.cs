using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class EntityBehaviorCollectEntities : EntityBehavior
{
	private int waitTicks;

	private int lastCollectedEntityIndex;

	private Vec3d tmp = new Vec3d();

	private float itemsPerSecond = 23f;

	private float unconsumedDeltaTime;

	public EntityBehaviorCollectEntities(Entity entity)
		: base(entity)
	{
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity.State != 0 || !entity.Alive)
		{
			return;
		}
		IPlayer player = (entity as EntityPlayer)?.Player;
		IServerPlayer obj = player as IServerPlayer;
		if (obj != null && obj.ItemCollectMode == 1)
		{
			EntityAgent obj2 = entity as EntityAgent;
			if (obj2 != null && !obj2.Controls.Sneak)
			{
				return;
			}
		}
		if (entity.IsActivityRunning("invulnerable"))
		{
			waitTicks = 3;
		}
		else
		{
			if (waitTicks-- > 0 || (player != null && player.WorldData.CurrentGameMode == EnumGameMode.Spectator))
			{
				return;
			}
			tmp.Set(entity.ServerPos.X, entity.ServerPos.InternalY + (double)entity.SelectionBox.Y1 + (double)(entity.SelectionBox.Y2 / 2f), entity.ServerPos.Z);
			Entity[] entities = entity.World.GetEntitiesAround(tmp, 1.5f, 1.5f, entityMatcher);
			if (entities.Length == 0)
			{
				unconsumedDeltaTime = 0f;
				return;
			}
			deltaTime = Math.Min(1f, deltaTime + unconsumedDeltaTime);
			while (deltaTime - 1f / itemsPerSecond > 0f)
			{
				Entity targetItem = null;
				int targetIndex;
				for (targetIndex = 0; targetIndex < entities.Length; targetIndex++)
				{
					if (entities[targetIndex] != null && targetIndex >= lastCollectedEntityIndex)
					{
						targetItem = entities[targetIndex];
						break;
					}
				}
				if (targetItem == null)
				{
					targetItem = entities[0];
					targetIndex = 0;
				}
				if (targetItem == null)
				{
					return;
				}
				if (!OnFoundCollectible(targetItem))
				{
					lastCollectedEntityIndex = (lastCollectedEntityIndex + 1) % entities.Length;
				}
				else
				{
					entities[targetIndex] = null;
				}
				deltaTime -= 1f / itemsPerSecond;
			}
			unconsumedDeltaTime = deltaTime;
		}
	}

	public virtual bool OnFoundCollectible(Entity foundEntity)
	{
		ItemStack itemstack = foundEntity.OnCollected(entity);
		bool collected = false;
		ItemStack origStack = itemstack.Clone();
		if (itemstack != null && itemstack.StackSize > 0)
		{
			collected = entity.TryGiveItemStack(itemstack);
		}
		if (itemstack != null && itemstack.StackSize <= 0)
		{
			foundEntity.Die(EnumDespawnReason.PickedUp);
		}
		if (collected)
		{
			itemstack.Collectible.OnCollected(itemstack, entity);
			TreeAttribute tree = new TreeAttribute();
			tree["itemstack"] = new ItemstackAttribute(origStack.Clone());
			tree["byentityid"] = new LongAttribute(entity.EntityId);
			entity.World.Api.Event.PushEvent("onitemcollected", tree);
			entity.World.PlaySoundAt(new AssetLocation("sounds/player/collect"), entity);
			return true;
		}
		return false;
	}

	private bool entityMatcher(Entity foundEntity)
	{
		return foundEntity.CanCollect(entity);
	}

	public override string PropertyName()
	{
		return "collectitems";
	}
}
