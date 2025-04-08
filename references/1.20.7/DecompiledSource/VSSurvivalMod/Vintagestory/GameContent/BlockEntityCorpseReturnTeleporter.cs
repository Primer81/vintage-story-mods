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

public class BlockEntityCorpseReturnTeleporter : BlockEntityTeleporterBase
{
	public ILoadedSound translocatingSound;

	private bool HasFuel;

	private BlockCorpseReturnTeleporter ownBlock;

	private bool canTeleport;

	private long somebodyIsTeleportingReceivedTotalMs;

	private float translocVolume;

	private float translocPitch;

	private float teleportAccum;

	private BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>().animUtil;

	public BlockEntityCorpseReturnTeleporter()
	{
		TeleportWarmupSec = 4.4f;
		canTeleport = true;
	}

	public override Vec3d GetTarget(Entity forEntity)
	{
		if (forEntity is EntityPlayer eplr)
		{
			IServerPlayer plr = eplr.Player as IServerPlayer;
			if (Api.ModLoader.GetModSystem<ModSystemCorpseReturnTeleporter>().lastDeathLocations.TryGetValue(plr.PlayerUID, out var location))
			{
				return location;
			}
		}
		return null;
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		setupGameTickers();
		ownBlock = base.Block as BlockCorpseReturnTeleporter;
		if (api.World.Side == EnumAppSide.Client)
		{
			float rotY = base.Block.Shape.rotateY;
			animUtil.InitializeAnimator("corpsereturnteleporter", null, null, new Vec3f(0f, rotY, 0f));
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
		if (canTeleport)
		{
			HandleTeleportingServer(dt);
		}
	}

	protected override void didTeleport(Entity entity)
	{
		if (entity is EntityPlayer)
		{
			manager.DidTranslocateServer((entity as EntityPlayer).Player as IServerPlayer);
		}
		HasFuel = false;
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

	private void OnClientGameTick(float dt)
	{
		if (ownBlock == null || Api?.World == null || !canTeleport)
		{
			return;
		}
		if (Api.World.ElapsedMilliseconds - somebodyIsTeleportingReceivedTotalMs > 6000)
		{
			somebodyIsTeleporting = false;
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
				goto IL_0092;
			}
		}
		else
		{
			num = 0;
		}
		num2 = (somebodyIsTeleporting ? 1 : 0);
		goto IL_0092;
		IL_0092:
		bool playerInside = (byte)num2 != 0;
		if (num == 0 && playerInside)
		{
			manager.lastTranslocateCollideMsOtherPlayer = Api.World.ElapsedMilliseconds;
		}
		if (playerInside)
		{
			teleportAccum += dt;
			if (teleportAccum > 1f)
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
			}
			animUtil.StartAnimation(new AnimationMetaData
			{
				Animation = "deploy",
				Code = "deploy",
				AnimationSpeed = 1.25f,
				EaseInSpeed = 2f,
				EaseOutSpeed = 30f,
				Weight = 1f,
				BlendMode = EnumAnimationBlendMode.Add
			});
		}
		else
		{
			animUtil.StopAnimation("teleport");
			animUtil.StopAnimation("deploy");
			teleportAccum = 0f;
		}
		if (animUtil.activeAnimationsByAnimCode.Count > 0 && Api.World.ElapsedMilliseconds - lastOwnPlayerCollideMs > 10000 && Api.World.ElapsedMilliseconds - manager.lastTranslocateCollideMsOtherPlayer > 10000)
		{
			animUtil.StopAnimation("deploy");
			animUtil.StopAnimation("teleport");
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

	public override void OnEntityCollide(Entity entity)
	{
		if (HasFuel)
		{
			base.OnEntityCollide(entity);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		if (worldAccessForResolve != null && worldAccessForResolve.Side == EnumAppSide.Client)
		{
			somebodyIsTeleportingReceivedTotalMs = worldAccessForResolve.ElapsedMilliseconds;
			if (tree.GetBool("somebodyDidTeleport"))
			{
				worldAccessForResolve.Api.Event.EnqueueMainThreadTask(delegate
				{
					worldAccessForResolve.PlaySoundAt(new AssetLocation("sounds/effect/translocate-breakdimension"), Pos, 0.0, null, randomizePitch: false, 16f);
				}, "playtelesound");
			}
		}
		HasFuel = tree.GetBool("hasFuel");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("hasFuel", HasFuel);
	}

	public bool OnInteract(IPlayer byPlayer)
	{
		ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (!HasFuel && !slot.Empty)
		{
			JsonObject itemAttributes = slot.Itemstack.ItemAttributes;
			if (itemAttributes != null && itemAttributes.IsTrue("corpseReturnFuel"))
			{
				HasFuel = true;
				if (Api.Side == EnumAppSide.Server)
				{
					slot.TakeOut(1);
				}
				(Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				MarkDirty(redrawOnClient: true);
			}
			return true;
		}
		return false;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		if (!HasFuel)
		{
			dsc.AppendLine(Lang.Get("No fuel, add temporal gear"));
		}
	}
}
