using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class BlockEntityBaseReturnTeleporter : BlockEntity
{
	public ILoadedSound translocatingSound;

	private float spinupAccum;

	private bool activated;

	private float translocVolume;

	private float translocPitch;

	public bool Activated => activated;

	private BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>().animUtil;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		setupGameTickers();
		if (api.World.Side == EnumAppSide.Client)
		{
			float rotY = base.Block.Shape.rotateY;
			animUtil.InitializeAnimator("basereturnteleporter", null, null, new Vec3f(0f, rotY, 0f));
			translocatingSound = (api as ICoreClientAPI).World.LoadSound(new SoundParams
			{
				Location = new AssetLocation("sounds/effect/translocate-active.ogg"),
				ShouldLoop = true,
				Position = Pos.ToVec3f(),
				RelativePosition = false,
				DisposeOnFinish = false,
				Volume = 0.5f
			});
		}
	}

	public void setupGameTickers()
	{
		if (Api.Side == EnumAppSide.Server)
		{
			RegisterGameTickListener(OnServerGameTick, 250);
		}
		else
		{
			RegisterGameTickListener(OnClientGameTick, 50);
		}
	}

	private void OnServerGameTick(float dt)
	{
		if (!activated)
		{
			return;
		}
		spinupAccum += dt;
		if (spinupAccum > 5f)
		{
			activated = false;
			IServerPlayer plr = Api.World.NearestPlayer((double)Pos.X + 0.5, (double)Pos.InternalY + 0.5, (double)Pos.Z + 0.5) as IServerPlayer;
			if (plr.Entity.Pos.DistanceTo(Pos.ToVec3d().Add(0.5, 0.0, 0.5)) < 5.0)
			{
				FuzzyEntityPos pos = plr.GetSpawnPosition(consumeSpawnUse: false);
				plr.Entity.TeleportToDouble(pos.X, pos.Y, pos.Z);
				Api.World.PlaySoundAt(new AssetLocation("sounds/effect/translocate-breakdimension"), plr.Entity.Pos.X, plr.Entity.Pos.InternalY, plr.Entity.Pos.Z, null, randomizePitch: false, 16f);
			}
			Api.World.PlaySoundAt(new AssetLocation("sounds/effect/translocate-breakdimension"), (float)Pos.X + 0.5f, (float)Pos.InternalY + 0.5f, (float)Pos.Z + 0.5f, null, randomizePitch: false, 16f);
			int color = ColorUtil.ToRgba(100, 220, 220, 220);
			Api.World.SpawnParticles(120f, color, Pos.ToVec3d(), Pos.ToVec3d().Add(1.0, 1.0, 1.0), new Vec3f(-1f, -1f, -1f), new Vec3f(1f, 1f, 1f), 2f, 0f);
			color = ColorUtil.ToRgba(255, 53, 221, 172);
			Api.World.SpawnParticles(100f, color, Pos.ToVec3d().Add(0.0, 0.25, 0.0), Pos.ToVec3d().Add(1.0, 1.25, 1.0), new Vec3f(-4f, 0f, -4f), new Vec3f(4f, 4f, 4f), 2f, 0.6f, 0.8f, EnumParticleModel.Cube);
			Block block = Api.World.GetBlock(new AssetLocation("basereturnteleporter-fried"));
			Api.World.BlockAccessor.SetBlock(block.Id, Pos);
		}
	}

	private void OnClientGameTick(float dt)
	{
		HandleSoundClient(dt);
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		AnimationMetaData meta = new AnimationMetaData
		{
			Animation = "deploy",
			Code = "deploy",
			AnimationSpeed = 1f,
			EaseInSpeed = 3f,
			EaseOutSpeed = 2f,
			Weight = 1f,
			BlendMode = EnumAnimationBlendMode.Average
		};
		animUtil.StartAnimation(meta);
	}

	public void OnInteract(IPlayer byPlayer)
	{
		if (activated)
		{
			activated = false;
			animUtil.StopAnimation("active");
			MarkDirty(redrawOnClient: true);
			return;
		}
		activated = true;
		MarkDirty(redrawOnClient: true);
		AnimationMetaData meta = new AnimationMetaData
		{
			Animation = "active",
			Code = "active",
			AnimationSpeed = 1f,
			EaseInSpeed = 1f,
			EaseOutSpeed = 2f,
			Weight = 1f,
			BlendMode = EnumAnimationBlendMode.Add
		};
		animUtil.StartAnimation(meta);
	}

	protected void HandleSoundClient(float dt)
	{
		ICoreClientAPI capi = Api as ICoreClientAPI;
		if (activated)
		{
			translocVolume = Math.Min(0.5f, translocVolume + dt / 3f);
			translocPitch = Math.Min(translocPitch + dt / 3f, 2.5f);
			if (capi != null && capi.World.Player.Entity.Pos.DistanceTo(Pos.ToVec3d().Add(0.5, 0.0, 0.5)) < 5.0)
			{
				capi.World.AddCameraShake(0.0575f);
			}
		}
		else
		{
			translocVolume = Math.Max(0f, translocVolume - 2f * dt);
			translocPitch = Math.Max(translocPitch - dt, 0.5f);
		}
		if (translocatingSound.IsPlaying)
		{
			translocatingSound.SetVolume(translocVolume);
			translocatingSound.SetPitch(translocPitch);
			if (translocVolume <= 0f)
			{
				translocatingSound.Stop();
			}
		}
		else if (translocVolume > 0f)
		{
			translocatingSound.Start();
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("activated", activated);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		activated = tree.GetBool("activated");
		if (activated)
		{
			ICoreAPI api = Api;
			if (api != null && api.Side == EnumAppSide.Client)
			{
				AnimationMetaData meta = new AnimationMetaData
				{
					Animation = "active",
					Code = "active",
					AnimationSpeed = 1f,
					EaseInSpeed = 1f,
					EaseOutSpeed = 2f,
					Weight = 1f,
					BlendMode = EnumAnimationBlendMode.Add
				};
				animUtil.StartAnimation(meta);
			}
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		translocatingSound?.Dispose();
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		translocatingSound?.Dispose();
	}
}
