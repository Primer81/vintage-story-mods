using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityDrifter : EntityHumanoid
{
	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		AnimationTrigger animationTrigger = new AnimationTrigger();
		animationTrigger.OnControls = new EnumEntityActivity[1] { EnumEntityActivity.Dead };
		AnimationTrigger trigger = animationTrigger;
		if (EntityId % 5 == 0L)
		{
			properties.Client.Animations.FirstOrDefault((AnimationMetaData a) => a.Code == "die").TriggeredBy = null;
			properties.Client.Animations.FirstOrDefault((AnimationMetaData a) => a.Code == "crawldie").TriggeredBy = trigger;
			properties.CollisionBoxSize = new Vec2f(0.9f, 0.6f);
			properties.SelectionBoxSize = new Vec2f(0.9f, 0.6f);
			properties.Client.Animations[2].TriggeredBy = null;
			properties.Client.Animations[5].TriggeredBy = new AnimationTrigger
			{
				DefaultAnim = true
			};
			properties.CanClimb = false;
			AnimManager = new RemapAnimationManager(new Dictionary<string, string>
			{
				{ "idle", "crawlidle" },
				{ "standwalk", "crawlwalk" },
				{ "standlowwalk", "crawlwalk" },
				{ "standrun", "crawlrun" },
				{ "standidle", "crawlidle" },
				{ "standdespair", "crawlemote" },
				{ "standcry", "crawlemote" },
				{ "standhurt", "crawlhurt" },
				{ "standdie", "crawldie" }
			});
		}
		else
		{
			properties.Client.Animations.FirstOrDefault((AnimationMetaData a) => a.Code == "die").TriggeredBy = trigger;
			properties.Client.Animations.FirstOrDefault((AnimationMetaData a) => a.Code == "crawldie").TriggeredBy = null;
		}
		base.Initialize(properties, api, InChunkIndex3d);
	}
}
