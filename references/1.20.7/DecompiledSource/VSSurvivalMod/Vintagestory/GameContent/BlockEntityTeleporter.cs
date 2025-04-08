using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class BlockEntityTeleporter : BlockEntityTeleporterBase
{
	private BlockTeleporter ownBlock;

	private Vec3d posvec;

	private TeleporterLocation tpLocation;

	public ILoadedSound teleportingSound;

	private float teleSoundVolume;

	private float teleSoundPitch = 0.7f;

	public override Vec3d GetTarget(Entity forEntity)
	{
		return tpLocation?.TargetPos?.ToVec3d().Add(-0.3, 1.0, -0.3);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (api.Side == EnumAppSide.Server)
		{
			tpLocation = manager.GetOrCreateLocation(Pos);
			RegisterGameTickListener(OnServerGameTick, 50);
		}
		else
		{
			RegisterGameTickListener(OnClientGameTick, 50);
			teleportingSound = (api as ICoreClientAPI).World.LoadSound(new SoundParams
			{
				Location = new AssetLocation("sounds/block/teleporter.ogg"),
				ShouldLoop = true,
				Position = Pos.ToVec3f(),
				RelativePosition = false,
				DisposeOnFinish = false,
				Volume = 0.5f
			});
		}
		ownBlock = base.Block as BlockTeleporter;
		posvec = new Vec3d(Pos.X, Pos.Y + 1, Pos.Z);
	}

	private void OnClientGameTick(float dt)
	{
		if (ownBlock != null && Api?.World != null)
		{
			HandleSoundClient(dt);
			SimpleParticleProperties currentParticles = ((Api.World.ElapsedMilliseconds > 100 && Api.World.ElapsedMilliseconds - lastOwnPlayerCollideMs < 100) ? ownBlock.insideParticles : ownBlock.idleParticles);
			currentParticles.MinPos = posvec;
			Api.World.SpawnParticles(currentParticles);
		}
	}

	protected virtual void HandleSoundClient(float dt)
	{
		if ((Api as ICoreClientAPI).World.ElapsedMilliseconds - lastEntityCollideMs > 100)
		{
			teleSoundVolume = Math.Max(0f, teleSoundVolume - 2f * dt);
			teleSoundPitch = Math.Max(0.7f, teleSoundPitch - 2f * dt);
		}
		else
		{
			teleSoundVolume = Math.Min(0.5f, teleSoundVolume + dt / 3f);
			teleSoundPitch = Math.Min(6f, teleSoundPitch + dt);
		}
		if (teleportingSound == null)
		{
			return;
		}
		teleportingSound.SetVolume(teleSoundVolume);
		teleportingSound.SetPitch(teleSoundPitch);
		if (teleportingSound.IsPlaying)
		{
			if (teleSoundVolume <= 0f)
			{
				teleportingSound.Stop();
			}
		}
		else if (teleSoundVolume > 0f)
		{
			teleportingSound.Start();
		}
	}

	protected override void didTeleport(Entity entity)
	{
		if (entity is EntityPlayer)
		{
			manager.DidTranslocateServer((entity as EntityPlayer).Player as IServerPlayer);
		}
	}

	private void OnServerGameTick(float dt)
	{
		try
		{
			HandleTeleportingServer(dt);
		}
		catch (Exception e)
		{
			Api.Logger.Warning("Exception when ticking Teleporter at {0}", Pos);
			Api.Logger.Error(e);
		}
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api.Side == EnumAppSide.Server)
		{
			(Api as ICoreServerAPI).ModLoader.GetModSystem<TeleporterManager>().DeleteLocation(Pos);
		}
		teleportingSound?.Dispose();
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		teleportingSound?.Dispose();
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		if (tpLocation != null)
		{
			ICoreAPI api = Api;
			if (api == null || api.Side != EnumAppSide.Client)
			{
				return;
			}
		}
		tpLocation = new TeleporterLocation
		{
			SourceName = tree.GetString("sourceName"),
			TargetName = tree.GetString("targetName")
		};
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		if (tpLocation != null)
		{
			tree.SetString("sourceName", (tpLocation.SourceName == null) ? "" : tpLocation.SourceName);
			tree.SetString("targetName", (tpLocation.TargetName == null) ? "" : tpLocation.TargetName);
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (tpLocation != null)
		{
			dsc.AppendLine(Lang.Get("teleporter-info", tpLocation.SourceName, tpLocation.TargetName));
		}
	}
}
