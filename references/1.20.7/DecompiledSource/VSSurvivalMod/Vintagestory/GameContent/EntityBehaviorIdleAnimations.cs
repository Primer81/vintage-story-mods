using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class EntityBehaviorIdleAnimations : EntityBehavior
{
	private float secondsIdleAccum;

	private string[] randomIdleAnimations;

	private EntityAgent eagent;

	private EntityBehaviorTiredness bhtiredness;

	public EntityBehaviorIdleAnimations(Entity entity)
		: base(entity)
	{
		eagent = entity as EntityAgent;
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		randomIdleAnimations = properties.Attributes["randomIdleAnimations"].AsArray<string>();
		bhtiredness = eagent.GetBehavior<EntityBehaviorTiredness>();
	}

	public override void OnGameTick(float dt)
	{
		if (!eagent.ServerControls.TriesToMove && !eagent.Controls.IsFlying && !eagent.Controls.Gliding)
		{
			ItemSlot rightHandItemSlot = eagent.RightHandItemSlot;
			if (rightHandItemSlot != null && rightHandItemSlot.Empty && !eagent.Swimming)
			{
				EntityBehaviorTiredness entityBehaviorTiredness = bhtiredness;
				if (entityBehaviorTiredness == null || !entityBehaviorTiredness.IsSleeping)
				{
					secondsIdleAccum += dt;
					if (secondsIdleAccum > 20f && eagent.World.Rand.NextDouble() < 0.004)
					{
						eagent.StartAnimation(randomIdleAnimations[eagent.World.Rand.Next(randomIdleAnimations.Length)]);
						secondsIdleAccum = 0f;
					}
					return;
				}
			}
		}
		secondsIdleAccum = 0f;
	}

	public override string PropertyName()
	{
		return "idleanimations";
	}
}
