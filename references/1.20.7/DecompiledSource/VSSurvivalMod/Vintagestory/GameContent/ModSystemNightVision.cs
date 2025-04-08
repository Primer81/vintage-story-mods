using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModSystemNightVision : ModSystem, IRenderer, IDisposable
{
	private ICoreClientAPI capi;

	private ICoreServerAPI sapi;

	private EntityBehaviorPlayerInventory bh;

	private double lastCheckTotalHours;

	public double RenderOrder => 0.0;

	public int RenderRange => 1;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Event.RegisterRenderer(this, EnumRenderStage.Before, "nightvision");
		api.Event.LevelFinalize += Event_LevelFinalize;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		sapi = api;
		api.Event.RegisterGameTickListener(onTickServer1s, 1000, 200);
	}

	private void onTickServer1s(float dt)
	{
		double totalHours = sapi.World.Calendar.TotalHours;
		double hoursPassed = totalHours - lastCheckTotalHours;
		if (!(hoursPassed > 0.05))
		{
			return;
		}
		IPlayer[] allOnlinePlayers = sapi.World.AllOnlinePlayers;
		for (int i = 0; i < allOnlinePlayers.Length; i++)
		{
			IInventory inv = allOnlinePlayers[i].InventoryManager.GetOwnInventory("character");
			if (inv != null)
			{
				ItemSlot headArmorSlot = inv[12];
				if (headArmorSlot.Itemstack?.Collectible is ItemNightvisiondevice invd)
				{
					invd.AddFuelHours(headArmorSlot.Itemstack, 0.0 - hoursPassed);
					headArmorSlot.MarkDirty();
				}
			}
		}
		lastCheckTotalHours = totalHours;
	}

	private void Event_LevelFinalize()
	{
		bh = capi.World.Player.Entity.GetBehavior<EntityBehaviorPlayerInventory>();
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (bh?.Inventory != null)
		{
			ItemStack stack = bh.Inventory[12]?.Itemstack;
			ItemNightvisiondevice itemnvd = stack?.Collectible as ItemNightvisiondevice;
			double fuelLeft = itemnvd?.GetFuelHours(stack) ?? 0.0;
			if (itemnvd != null)
			{
				capi.Render.ShaderUniforms.NightVisionStrength = (float)GameMath.Clamp(fuelLeft * 20.0, 0.0, 0.8);
			}
			else
			{
				capi.Render.ShaderUniforms.NightVisionStrength = 0f;
			}
		}
	}
}
