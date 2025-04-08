using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemTemporalGear : Item
{
	public SimpleParticleProperties particlesHeld;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		particlesHeld = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(50, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.1f, -0.1f, -0.1f), new Vec3f(0.1f, 0.1f, 0.1f), 1.5f, 0f, 0.5f, 0.75f);
		particlesHeld.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.6f);
		particlesHeld.AddPos.Set(0.10000000149011612, 0.10000000149011612, 0.10000000149011612);
		particlesHeld.addLifeLength = 0.5f;
		particlesHeld.RandomVelocityChange = true;
	}

	public override void InGuiIdle(IWorldAccessor world, ItemStack stack)
	{
		GuiTransform.Rotation.Y = GameMath.Mod((float)world.ElapsedMilliseconds / 50f, 360f);
	}

	public override void OnGroundIdle(EntityItem entityItem)
	{
		GroundTransform.Rotation.Y = 0f - GameMath.Mod((float)entityItem.World.ElapsedMilliseconds / 50f, 360f);
		if (entityItem.World is IClientWorldAccessor)
		{
			particlesHeld.MinQuantity = 1f;
			Vec3d pos = entityItem.SidedPos.XYZ;
			SpawnParticles(entityItem.World, pos, final: false);
		}
	}

	public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
	{
		if (byEntity.World is IClientWorldAccessor)
		{
			FpHandTransform.Rotation.Y = GameMath.Mod((float)(-byEntity.World.ElapsedMilliseconds) / 50f, 360f);
			TpHandTransform.Rotation.Y = GameMath.Mod((float)(-byEntity.World.ElapsedMilliseconds) / 50f, 360f);
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null || api.World.BlockAccessor.GetBlock(blockSel.Position) is BlockStaticTranslocator)
		{
			return;
		}
		if (byEntity.World is IClientWorldAccessor)
		{
			IClientWorldAccessor world = byEntity.World as IClientWorldAccessor;
			ILoadedSound sound;
			byEntity.World.Api.ObjectCache["temporalGearSound"] = (sound = world.LoadSound(new SoundParams
			{
				Location = new AssetLocation("sounds/effect/gears.ogg"),
				ShouldLoop = true,
				Position = blockSel.Position.ToVec3f().Add(0.5f, 0.25f, 0.5f),
				DisposeOnFinish = false,
				Volume = 1f,
				Pitch = 0.7f
			}));
			sound?.Start();
			byEntity.World.RegisterCallback(delegate
			{
				if (byEntity.Controls.HandUse == EnumHandInteract.None)
				{
					sound?.Stop();
					sound?.Dispose();
				}
			}, 3600);
			byEntity.World.RegisterCallback(delegate
			{
				if (byEntity.Controls.HandUse == EnumHandInteract.None)
				{
					sound?.Stop();
					sound?.Dispose();
				}
			}, 20);
		}
		handHandling = EnumHandHandling.PreventDefault;
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null || !(byEntity is EntityPlayer))
		{
			return false;
		}
		FpHandTransform.Rotation.Y = GameMath.Mod((float)byEntity.World.ElapsedMilliseconds / (50f - secondsUsed * 20f), 360f);
		TpHandTransform.Rotation.Y = GameMath.Mod((float)byEntity.World.ElapsedMilliseconds / (50f - secondsUsed * 20f), 360f);
		if (byEntity.World is IClientWorldAccessor)
		{
			particlesHeld.MinQuantity = 1f;
			Vec3d pos = blockSel.Position.ToVec3d().Add(blockSel.HitPosition);
			SpawnParticles(byEntity.World, pos, final: false);
			(byEntity.World as IClientWorldAccessor).AddCameraShake(0.035f);
			ObjectCacheUtil.TryGet<ILoadedSound>(api, "temporalGearSound")?.SetPitch(0.7f + secondsUsed / 4f);
		}
		return (double)secondsUsed < 3.5;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (byEntity.World.Side == EnumAppSide.Client)
		{
			ILoadedSound loadedSound = ObjectCacheUtil.TryGet<ILoadedSound>(api, "temporalGearSound");
			loadedSound?.Stop();
			loadedSound?.Dispose();
		}
		if (blockSel != null && !((double)secondsUsed < 3.45))
		{
			slot.TakeOut(1);
			slot.MarkDirty();
			if (byEntity.World.Side == EnumAppSide.Client)
			{
				byEntity.World.PlaySoundAt(new AssetLocation("sounds/effect/portal.ogg"), byEntity, null, randomizePitch: false);
				particlesHeld.MinSize = 0.25f;
				particlesHeld.MaxSize = 0.5f;
				particlesHeld.MinQuantity = 300f;
				Vec3d pos = blockSel.Position.ToVec3d().Add(blockSel.HitPosition);
				SpawnParticles(byEntity.World, pos, final: true);
			}
			if (byEntity.World.Side == EnumAppSide.Server && byEntity is EntityPlayer)
			{
				IServerPlayer obj = byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID) as IServerPlayer;
				int uses = api.World.Config.GetString("temporalGearRespawnUses", "-1").ToInt();
				obj.SetSpawnPosition(new PlayerSpawnPos
				{
					x = byEntity.ServerPos.XYZInt.X,
					y = byEntity.ServerPos.XYZInt.Y,
					z = byEntity.ServerPos.XYZInt.Z,
					yaw = byEntity.ServerPos.Yaw,
					pitch = byEntity.ServerPos.Pitch,
					RemainingUses = uses
				});
			}
		}
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		if (byEntity.World.Side == EnumAppSide.Client)
		{
			ILoadedSound loadedSound = ObjectCacheUtil.TryGet<ILoadedSound>(api, "temporalGearSound");
			loadedSound?.Stop();
			loadedSound?.Dispose();
		}
		return true;
	}

	private void SpawnParticles(IWorldAccessor world, Vec3d pos, bool final)
	{
		if (final || world.Rand.NextDouble() > 0.8)
		{
			int h = 110 + world.Rand.Next(15);
			int v = 100 + world.Rand.Next(50);
			particlesHeld.MinPos = pos;
			particlesHeld.Color = ColorUtil.ReverseColorBytes(ColorUtil.HsvToRgba(h, 180, v));
			particlesHeld.MinSize = 0.2f;
			particlesHeld.ParticleModel = EnumParticleModel.Quad;
			particlesHeld.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -150f);
			particlesHeld.Color = ColorUtil.ReverseColorBytes(ColorUtil.HsvToRgba(h, 180, v, 150));
			world.SpawnParticles(particlesHeld);
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-useonground",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
