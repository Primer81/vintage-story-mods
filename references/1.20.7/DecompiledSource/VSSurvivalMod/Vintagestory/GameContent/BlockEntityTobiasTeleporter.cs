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

public class BlockEntityTobiasTeleporter : BlockEntityTeleporterBase
{
	private ICoreServerAPI sapi;

	public ILoadedSound translocatingSound;

	private float translocVolume;

	private float translocPitch;

	public string OwnerPlayerUid { get; set; }

	public string OwnerName { get; set; }

	public bool IsAtTobiasCave { get; set; }

	public TobiasTeleporter SystemTobiasTeleporter { get; set; }

	private BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>().animUtil;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		SystemTobiasTeleporter = api.ModLoader.GetModSystem<TobiasTeleporter>();
		if (Api is ICoreServerAPI serverApi)
		{
			sapi = serverApi;
			RegisterGameTickListener(OnServerGameTick, 250);
			if (IsAtTobiasCave)
			{
				string side = sapi.World.BlockAccessor.GetBlock(Pos).Variant["side"];
				Vec3d tpPos = Pos.ToVec3d() + BlockTobiasTeleporter.GetTeleportOffset(side);
				SystemTobiasTeleporter.TeleporterData.TobiasTeleporterLocation = tpPos;
			}
			else
			{
				if (string.IsNullOrEmpty(OwnerPlayerUid))
				{
					return;
				}
				sapi.ModLoader.GetModSystem<GenStoryStructures>().storyStructureInstances.TryGetValue("tobiascave", out var location);
				if (location != null && Pos.DistanceTo(location.CenterPos) < 20f)
				{
					IsAtTobiasCave = true;
					OwnerPlayerUid = null;
					OwnerName = null;
					MarkDirty();
					return;
				}
				string ownerName = sapi.PlayerData.GetPlayerDataByUid(OwnerPlayerUid)?.LastKnownPlayername;
				if (OwnerName != ownerName && !string.IsNullOrEmpty(ownerName))
				{
					OwnerName = ownerName;
					MarkDirty();
				}
			}
		}
		else
		{
			RegisterGameTickListener(OnClientGameTick, 50);
			float rotY = base.Block.Shape.rotateY;
			animUtil.InitializeAnimator("tobiasteleporter", null, null, new Vec3f(0f, rotY, 0f));
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

	public override void OnEntityCollide(Entity entity)
	{
		if (Api.Side == EnumAppSide.Server)
		{
			if (entity is EntityPlayer player && ((IsAtTobiasCave && SystemTobiasTeleporter.TryGetPlayerLocation(player.PlayerUID, out var location)) || (SystemTobiasTeleporter.IsAllowedToTeleport(player.PlayerUID, out location) && player.PlayerUID.Equals(OwnerPlayerUid))))
			{
				base.OnEntityCollide(entity);
			}
		}
		else if (entity is EntityPlayer player2 && somebodyIsTeleporting && ((IsAtTobiasCave && SystemTobiasTeleporter.OwnLastUsage != 0.0) || player2.PlayerUID.Equals(OwnerPlayerUid)))
		{
			base.OnEntityCollide(entity);
		}
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		base.OnBlockBroken(byPlayer);
		if (Api is ICoreServerAPI && !string.IsNullOrWhiteSpace(OwnerPlayerUid))
		{
			SystemTobiasTeleporter.RemovePlayerTeleporter(OwnerPlayerUid);
		}
	}

	public override Vec3d GetTarget(Entity forEntity)
	{
		if (IsAtTobiasCave)
		{
			if (forEntity is EntityPlayer player && SystemTobiasTeleporter.TryGetPlayerLocation(player.PlayerUID, out var location))
			{
				return location;
			}
		}
		else if (forEntity is EntityPlayer player2 && player2.PlayerUID.Equals(OwnerPlayerUid))
		{
			return SystemTobiasTeleporter.TeleporterData.TobiasTeleporterLocation;
		}
		return null;
	}

	protected override void didTeleport(Entity entity)
	{
		if (entity is EntityPlayer)
		{
			manager.DidTranslocateServer((entity as EntityPlayer).Player as IServerPlayer);
		}
		if (entity.Pos.DistanceTo(SystemTobiasTeleporter.TeleporterData.TobiasTeleporterLocation) < 2.0)
		{
			SystemTobiasTeleporter.UpdatePlayerLastTeleport(entity);
			MarkDirty();
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
			Api.Logger.Warning("Exception when ticking Tobias Teleporter at {0}", Pos);
			Api.Logger.Error(e);
		}
	}

	private void OnClientGameTick(float dt)
	{
		if (Api?.World == null)
		{
			return;
		}
		HandleSoundClient(dt);
		int num;
		int num2;
		if (Api.World.ElapsedMilliseconds > 100)
		{
			num = ((Api.World.ElapsedMilliseconds - lastOwnPlayerCollideMs < 100) ? 1 : 0);
			if (num != 0)
			{
				num2 = 1;
				goto IL_005c;
			}
		}
		else
		{
			num = 0;
		}
		num2 = (somebodyIsTeleporting ? 1 : 0);
		goto IL_005c;
		IL_005c:
		bool playerInside = (byte)num2 != 0;
		if (num == 0 && playerInside)
		{
			manager.lastTranslocateCollideMsOtherPlayer = Api.World.ElapsedMilliseconds;
		}
		if (playerInside)
		{
			AnimationMetaData meta = new AnimationMetaData
			{
				Animation = "teleport",
				Code = "teleport",
				AnimationSpeed = 1f,
				EaseInSpeed = 1f,
				EaseOutSpeed = 2f,
				Weight = 1f,
				BlendMode = EnumAnimationBlendMode.Add
			};
			animUtil.StartAnimation(meta);
			animUtil.StartAnimation(new AnimationMetaData
			{
				Animation = "idle",
				Code = "idle",
				AnimationSpeed = 1f,
				EaseInSpeed = 1f,
				EaseOutSpeed = 1f,
				Weight = 1f,
				BlendMode = EnumAnimationBlendMode.Average
			});
		}
		else
		{
			animUtil.StopAnimation("teleport");
		}
		if (animUtil.activeAnimationsByAnimCode.Count > 0 && Api.World.ElapsedMilliseconds - lastOwnPlayerCollideMs > 10000 && Api.World.ElapsedMilliseconds - manager.lastTranslocateCollideMsOtherPlayer > 10000)
		{
			animUtil.StopAnimation("idle");
		}
	}

	protected virtual void HandleSoundClient(float dt)
	{
		ICoreClientAPI capi = Api as ICoreClientAPI;
		bool ownTranslocate = capi.World.ElapsedMilliseconds - lastOwnPlayerCollideMs <= 200;
		bool otherTranslocate = capi.World.ElapsedMilliseconds - lastEntityCollideMs <= 200;
		if (ownTranslocate || otherTranslocate)
		{
			translocVolume = Math.Min(0.5f, translocVolume + dt / 3f);
			translocPitch = Math.Min(translocPitch + dt / 3f, 2.5f);
			if (ownTranslocate)
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

	public override void OnBlockRemoved()
	{
		translocatingSound?.Stop();
		translocatingSound?.Dispose();
		base.OnBlockRemoved();
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		OwnerPlayerUid = tree.GetString("ownerPlayerUid");
		OwnerName = tree.GetString("ownerName");
		IsAtTobiasCave = tree.GetBool("IsAtTobiasCave");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetString("ownerPlayerUid", OwnerPlayerUid);
		tree.SetString("ownerName", OwnerName);
		tree.SetBool("IsAtTobiasCave", IsAtTobiasCave);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		double nextUsage = Api.ModLoader.GetModSystem<TobiasTeleporter>().GetNextUsage();
		if (!IsAtTobiasCave && nextUsage > 0.0 && forPlayer.PlayerUID.Equals(OwnerPlayerUid))
		{
			dsc.AppendLine(Lang.Get("Can use again in: " + Lang.Get("count-days", Math.Round(nextUsage, 1))));
		}
		if (!IsAtTobiasCave)
		{
			dsc.AppendLine(Lang.Get("Owned by {0}", OwnerName));
		}
	}
}
