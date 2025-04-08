using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Vintagestory.Client.NoObf;

public class ModSystemBossHealthBars : ModSystem
{
	private ICoreClientAPI capi;

	private EntityPartitioning partUtil;

	private List<HudBosshealthBars> trackedBosses = new List<HudBosshealthBars>();

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		partUtil = api.ModLoader.GetModSystem<EntityPartitioning>();
		api.Event.RegisterGameTickListener(onTick, 200, 12);
	}

	private void onTick(float dt)
	{
		List<EntityAgent> foundBosses = new List<EntityAgent>();
		Vec3d plrpos = capi.World.Player.Entity.Pos.XYZ;
		partUtil.WalkEntities(plrpos, 60.0, delegate(Entity e)
		{
			EntityBehaviorBoss behavior;
			if (e.Alive && e.IsInteractable && (behavior = e.GetBehavior<EntityBehaviorBoss>()) != null)
			{
				double num = e.Pos.DistanceTo(plrpos);
				if (behavior.ShowHealthBar && num <= (double)behavior.BossHpbarRange)
				{
					foundBosses.Add(e as EntityAgent);
				}
			}
			return true;
		}, EnumEntitySearchType.Creatures);
		int reorganizePositionsAt = -1;
		for (int j = 0; j < trackedBosses.Count; j++)
		{
			HudBosshealthBars hud = trackedBosses[j];
			if (foundBosses.Contains(hud.TargetEntity))
			{
				foundBosses.Remove(hud.TargetEntity);
				continue;
			}
			trackedBosses[j].TryClose();
			trackedBosses[j].Dispose();
			trackedBosses.RemoveAt(j);
			reorganizePositionsAt = j;
			j--;
		}
		foreach (EntityAgent eagent in foundBosses)
		{
			trackedBosses.Add(new HudBosshealthBars(capi, eagent, trackedBosses.Count));
		}
		if (reorganizePositionsAt >= 0)
		{
			for (int i = reorganizePositionsAt; i < trackedBosses.Count; i++)
			{
				trackedBosses[i].barIndex = i;
				trackedBosses[i].ComposeGuis();
			}
		}
	}
}
