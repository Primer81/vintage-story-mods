using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class ModSystemGliding : ModSystem
{
	private ICoreClientAPI capi;

	protected ILoadedSound glideSound;

	private bool HasGlider
	{
		get
		{
			foreach (ItemSlot slot in capi.World.Player.InventoryManager.GetOwnInventory("backpack"))
			{
				if (slot is ItemSlotBackpack && slot.Itemstack?.Collectible is ItemGlider)
				{
					return true;
				}
			}
			return false;
		}
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Input.InWorldAction += Input_InWorldAction;
		api.Event.RegisterGameTickListener(onClientTick, 20, 1);
	}

	private void onClientTick(float dt)
	{
		ToggleglideSounds(capi.World.Player.Entity.Controls.Gliding);
		IPlayer[] allOnlinePlayers = capi.World.AllOnlinePlayers;
		foreach (IPlayer plr in allOnlinePlayers)
		{
			if (plr.Entity == null)
			{
				continue;
			}
			float speed = 15f;
			float glidingAccum = plr.Entity.Attributes.GetFloat("glidingAccum");
			int unfoldStep = plr.Entity.Attributes.GetInt("unfoldStep");
			if (plr.Entity.Controls.Gliding)
			{
				glidingAccum = Math.Min(3.01f / speed, glidingAccum + dt);
				if (!HasGlider)
				{
					plr.Entity.Controls.Gliding = false;
					plr.Entity.WalkPitch = 0f;
				}
			}
			else
			{
				glidingAccum = Math.Max(0f, glidingAccum - dt);
			}
			int nowUnfoldStep = (int)(glidingAccum * speed);
			if (unfoldStep != nowUnfoldStep)
			{
				unfoldStep = nowUnfoldStep;
				plr.Entity.MarkShapeModified();
				plr.Entity.Attributes.SetInt("unfoldStep", unfoldStep);
			}
			plr.Entity.Attributes.SetFloat("glidingAccum", glidingAccum);
		}
	}

	public void ToggleglideSounds(bool on)
	{
		if (on)
		{
			if (glideSound == null || !glideSound.IsPlaying)
			{
				glideSound = capi.World.LoadSound(new SoundParams
				{
					Location = new AssetLocation("sounds/effect/gliding.ogg"),
					ShouldLoop = true,
					Position = null,
					RelativePosition = true,
					DisposeOnFinish = false,
					Volume = 0f
				});
				if (glideSound != null)
				{
					glideSound.Start();
					glideSound.PlaybackPosition = glideSound.SoundLengthSeconds * (float)capi.World.Rand.NextDouble();
					glideSound.FadeIn(1f, delegate
					{
					});
				}
			}
		}
		else
		{
			glideSound?.Stop();
			glideSound?.Dispose();
			glideSound = null;
		}
	}

	private void Input_InWorldAction(EnumEntityAction action, bool on, ref EnumHandling handled)
	{
		EntityPlayer eplr = capi.World.Player.Entity;
		if (action == EnumEntityAction.Jump && on && !eplr.OnGround && HasGlider && !eplr.Controls.IsFlying)
		{
			eplr.Controls.Gliding = true;
			eplr.Controls.IsFlying = true;
			eplr.MarkShapeModified();
		}
		if (action == EnumEntityAction.Glide && !on)
		{
			eplr.MarkShapeModified();
		}
	}
}
