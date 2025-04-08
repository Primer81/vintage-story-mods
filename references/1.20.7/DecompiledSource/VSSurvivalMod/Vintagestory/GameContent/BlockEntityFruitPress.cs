using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityFruitPress : BlockEntityContainer
{
	private const int PacketIdAnimUpdate = 1001;

	private const int PacketIdScrewStart = 1002;

	private const int PacketIdUnscrew = 1003;

	private const int PacketIdScrewContinue = 1004;

	private static SimpleParticleProperties liquidParticles;

	private InventoryGeneric inv;

	private ICoreClientAPI capi;

	private BlockFruitPress ownBlock;

	private MeshData meshMovable;

	private MeshData bucketMesh;

	private MeshData bucketMeshTmp;

	private FruitpressContentsRenderer renderer;

	private AnimationMetaData compressAnimMeta = new AnimationMetaData
	{
		Animation = "compress",
		Code = "compress",
		AnimationSpeed = 0.5f,
		EaseOutSpeed = 0.5f,
		EaseInSpeed = 3f
	};

	private float? loadedFrame;

	private bool serverListenerActive;

	private long listenerId;

	private double juiceableLitresCapacity = 10.0;

	private double screwPercent;

	private double squeezedLitresLeft;

	private double pressSqueezeRel;

	private bool squeezeSoundPlayed;

	private int dryStackSize;

	private double lastLiquidTransferTotalHours;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "fruitpress";

	private BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>()?.animUtil;

	public ItemSlot MashSlot => inv[0];

	public ItemSlot BucketSlot => inv[1];

	private ItemStack mashStack => MashSlot.Itemstack;

	private double juiceableLitresLeft
	{
		get
		{
			return (mashStack?.Attributes?.GetDouble("juiceableLitresLeft")).GetValueOrDefault();
		}
		set
		{
			mashStack.Attributes.SetDouble("juiceableLitresLeft", value);
		}
	}

	private double juiceableLitresTransfered
	{
		get
		{
			return (mashStack?.Attributes?.GetDouble("juiceableLitresTransfered")).GetValueOrDefault();
		}
		set
		{
			mashStack.Attributes.SetDouble("juiceableLitresTransfered", value);
		}
	}

	public bool CompressAnimFinished
	{
		get
		{
			RunningAnimation anim = animUtil.animator.GetAnimationState("compress");
			return anim.CurrentFrame >= (float)(anim.Animation.QuantityFrames - 1);
		}
	}

	public bool CompressAnimActive
	{
		get
		{
			if (!animUtil.activeAnimationsByAnimCode.ContainsKey("compress"))
			{
				return animUtil.animator.GetAnimationState("compress")?.Active ?? false;
			}
			return true;
		}
	}

	public bool CanScrew => !CompressAnimFinished;

	public bool CanUnscrew
	{
		get
		{
			if (!CompressAnimFinished)
			{
				return CompressAnimActive;
			}
			return true;
		}
	}

	public bool CanFillRemoveItems => !CompressAnimActive;

	static BlockEntityFruitPress()
	{
		liquidParticles = new SimpleParticleProperties
		{
			MinVelocity = new Vec3f(-0.04f, 0f, -0.04f),
			AddVelocity = new Vec3f(0.08f, 0f, 0.08f),
			addLifeLength = 0.5f,
			LifeLength = 0.5f,
			MinQuantity = 0.25f,
			GravityEffect = 0.5f,
			SelfPropelled = true,
			MinSize = 0.1f,
			MaxSize = 0.2f
		};
	}

	public BlockEntityFruitPress()
	{
		inv = new InventoryGeneric(2, "fruitpress-0", null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		ownBlock = base.Block as BlockFruitPress;
		capi = api as ICoreClientAPI;
		if (ownBlock == null)
		{
			return;
		}
		Shape shape = Shape.TryGet(api, "shapes/block/wood/fruitpress/part-movable.json");
		if (api.Side == EnumAppSide.Client)
		{
			capi.Tesselator.TesselateShape(ownBlock, shape, out meshMovable, new Vec3f(0f, ownBlock.Shape.rotateY, 0f));
			animUtil.InitializeAnimator("fruitpress", shape, null, new Vec3f(0f, ownBlock.Shape.rotateY, 0f));
		}
		else
		{
			shape.InitForAnimations(api.Logger, "shapes/block/wood/fruitpress/part-movable.json");
			animUtil.InitializeAnimatorServer("fruitpress", shape);
		}
		if (api.Side == EnumAppSide.Client)
		{
			renderer = new FruitpressContentsRenderer(api as ICoreClientAPI, Pos, this);
			(api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "fruitpress");
			renderer.reloadMeshes(getJuiceableProps(mashStack), mustReload: true);
			genBucketMesh();
		}
		else if (serverListenerActive)
		{
			if (loadedFrame > 0f)
			{
				animUtil.StartAnimation(compressAnimMeta);
			}
			if (listenerId == 0L)
			{
				listenerId = RegisterGameTickListener(onTick100msServer, 25);
			}
		}
	}

	private void onTick25msClient(float dt)
	{
		double squeezeRel = mashStack?.Attributes.GetDouble("squeezeRel", 1.0) ?? 1.0;
		float selfHeight = (float)(juiceableLitresTransfered + juiceableLitresLeft) / 10f;
		if (MashSlot.Empty || renderer.juiceTexPos == null || squeezeRel >= 1.0 || pressSqueezeRel > squeezeRel || squeezedLitresLeft < 0.01)
		{
			return;
		}
		Random rand = Api.World.Rand;
		liquidParticles.MinQuantity = (float)juiceableLitresLeft / 10f;
		for (int j = 0; j < 4; j++)
		{
			BlockFacing face = BlockFacing.HORIZONTALS[j];
			liquidParticles.Color = capi.BlockTextureAtlas.GetRandomColor(renderer.juiceTexPos, rand.Next(30));
			Vec3d minPos = face.Plane.Startd.Add(-0.5, 0.0, -0.5);
			Vec3d maxPos = face.Plane.Endd.Add(-0.5, 0.0, -0.5);
			minPos.Mul(0.5);
			maxPos.Mul(0.5);
			maxPos.Y = 0.3125 - (1.0 - squeezeRel + (double)Math.Max(0f, 0.9f - selfHeight)) * 0.5;
			minPos.Add(face.Normalf.X * 1.2f / 16f, 0.0, face.Normalf.Z * 1.2f / 16f);
			maxPos.Add(face.Normalf.X * 1.2f / 16f, 0.0, face.Normalf.Z * 1.2f / 16f);
			liquidParticles.MinPos = minPos;
			liquidParticles.AddPos = maxPos.Sub(minPos);
			liquidParticles.MinPos.Add(Pos).Add(0.5, 1.0, 0.5);
			Api.World.SpawnParticles(liquidParticles);
		}
		if (squeezeRel < 0.8999999761581421)
		{
			liquidParticles.MinPos = Pos.ToVec3d().Add(0.375, 0.699999988079071, 0.375);
			liquidParticles.AddPos.Set(0.25, 0.0, 0.25);
			for (int i = 0; i < 3; i++)
			{
				liquidParticles.Color = capi.BlockTextureAtlas.GetRandomColor(renderer.juiceTexPos, rand.Next(30));
				Api.World.SpawnParticles(liquidParticles);
			}
		}
	}

	private void onTick100msServer(float dt)
	{
		RunningAnimation anim = animUtil.animator.GetAnimationState("compress");
		if (serverListenerActive)
		{
			anim.CurrentFrame = loadedFrame.GetValueOrDefault();
			updateSqueezeRel(animUtil.animator.GetAnimationState("compress"));
			serverListenerActive = false;
			loadedFrame = null;
			return;
		}
		if (CompressAnimActive)
		{
			(Api as ICoreServerAPI)?.Network.BroadcastBlockEntityPacket(Pos, 1001, new FruitPressAnimPacket
			{
				AnimationState = EnumFruitPressAnimState.ScrewContinue,
				AnimationSpeed = compressAnimMeta.AnimationSpeed,
				CurrentFrame = anim.CurrentFrame
			});
		}
		if (MashSlot.Empty)
		{
			return;
		}
		JuiceableProperties juiceProps = getJuiceableProps(mashStack);
		double totalHours = Api.World.Calendar.TotalHours;
		double squeezeRel = mashStack.Attributes.GetDouble("squeezeRel", 1.0);
		squeezedLitresLeft = Math.Max(Math.Max(0.0, squeezedLitresLeft), juiceableLitresLeft - (juiceableLitresLeft + juiceableLitresTransfered) * screwPercent);
		double litresToTransfer = Math.Min(squeezedLitresLeft, Math.Round((totalHours - lastLiquidTransferTotalHours) * (CompressAnimActive ? GameMath.Clamp(squeezedLitresLeft * (1.0 - squeezeRel) * 500.0, 25.0, 100.0) : 5.0), 2));
		if (Api.Side == EnumAppSide.Server && CompressAnimActive && squeezeRel < 1.0 && pressSqueezeRel <= squeezeRel && !squeezeSoundPlayed && juiceableLitresLeft > 0.0)
		{
			Api.World.PlaySoundAt(new AssetLocation("sounds/player/wetclothsqueeze.ogg"), Pos, 0.0, null, randomizePitch: false);
			squeezeSoundPlayed = true;
		}
		BlockLiquidContainerBase cntBlock = BucketSlot?.Itemstack?.Collectible as BlockLiquidContainerBase;
		if (Api.Side == EnumAppSide.Server && juiceProps != null && squeezedLitresLeft > 0.0)
		{
			ItemStack liquidStack = juiceProps.LiquidStack.ResolvedItemstack;
			liquidStack.StackSize = 999999;
			float actuallyTransfered;
			if (cntBlock != null && !cntBlock.IsFull(BucketSlot.Itemstack))
			{
				float beforelitres = cntBlock.GetCurrentLitres(BucketSlot.Itemstack);
				if (litresToTransfer > 0.0)
				{
					cntBlock.TryPutLiquid(BucketSlot.Itemstack, liquidStack, (float)litresToTransfer);
				}
				actuallyTransfered = cntBlock.GetCurrentLitres(BucketSlot.Itemstack) - beforelitres;
			}
			else
			{
				actuallyTransfered = (float)litresToTransfer;
			}
			juiceableLitresLeft -= actuallyTransfered;
			squeezedLitresLeft -= ((pressSqueezeRel <= squeezeRel) ? actuallyTransfered : (actuallyTransfered * 100f));
			juiceableLitresTransfered += actuallyTransfered;
			lastLiquidTransferTotalHours = totalHours;
			MarkDirty(redrawOnClient: true);
		}
		else if (Api.Side == EnumAppSide.Server && (!CompressAnimActive || juiceableLitresLeft <= 0.0))
		{
			UnregisterGameTickListener(listenerId);
			listenerId = 0L;
			MarkDirty(redrawOnClient: true);
		}
	}

	public bool OnBlockInteractStart(IPlayer byPlayer, BlockSelection blockSel, EnumFruitPressSection section, bool firstEvent)
	{
		firstEvent |= Api.Side == EnumAppSide.Server;
		if (section == EnumFruitPressSection.MashContainer && firstEvent)
		{
			return InteractMashContainer(byPlayer, blockSel);
		}
		if (section == EnumFruitPressSection.Ground && firstEvent)
		{
			return InteractGround(byPlayer, blockSel);
		}
		if (section == EnumFruitPressSection.Screw)
		{
			return InteractScrew(byPlayer, blockSel, firstEvent);
		}
		return false;
	}

	private bool InteractScrew(IPlayer byPlayer, BlockSelection blockSel, bool firstEvent)
	{
		if (Api.Side == EnumAppSide.Server)
		{
			return true;
		}
		if (!CompressAnimActive && !byPlayer.Entity.Controls.CtrlKey && firstEvent)
		{
			(Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos, 1002);
			return true;
		}
		if (CanUnscrew && (byPlayer.Entity.Controls.CtrlKey || (CompressAnimFinished && !byPlayer.Entity.Controls.CtrlKey)) && firstEvent)
		{
			(Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos, 1003);
			return true;
		}
		if (compressAnimMeta.AnimationSpeed == 0f && !byPlayer.Entity.Controls.CtrlKey)
		{
			(Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos, 1004);
			return true;
		}
		return false;
	}

	private bool InteractMashContainer(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot handslot = byPlayer.InventoryManager.ActiveHotbarSlot;
		ItemStack handStack = handslot.Itemstack;
		if (CompressAnimActive)
		{
			(Api as ICoreClientAPI)?.TriggerIngameError(this, "compressing", Lang.Get("Release the screw first to add/remove fruit"));
			return false;
		}
		if (!handslot.Empty)
		{
			JuiceableProperties hprops = getJuiceableProps(handStack);
			if (hprops == null)
			{
				return false;
			}
			if (!hprops.LitresPerItem.HasValue && !handStack.Attributes.HasAttribute("juiceableLitresLeft"))
			{
				return false;
			}
			ItemStack pressedStack = (hprops.LitresPerItem.HasValue ? hprops.PressedStack.ResolvedItemstack.Clone() : handStack.GetEmptyClone());
			if (MashSlot.Empty)
			{
				MashSlot.Itemstack = pressedStack;
				if (!hprops.LitresPerItem.HasValue)
				{
					mashStack.StackSize = 1;
					dryStackSize = GameMath.RoundRandom(Api.World.Rand, ((float)juiceableLitresLeft + (float)juiceableLitresTransfered) * getJuiceableProps(mashStack).PressedDryRatio);
					handslot.TakeOut(1);
					MarkDirty(redrawOnClient: true);
					renderer?.reloadMeshes(hprops, mustReload: true);
					(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
					return true;
				}
			}
			else if (juiceableLitresLeft + juiceableLitresTransfered >= juiceableLitresCapacity)
			{
				(Api as ICoreClientAPI)?.TriggerIngameError(this, "fullcontainer", Lang.Get("Container is full, press out juice and remove the mash before adding more"));
				return false;
			}
			if (!mashStack.Equals(Api.World, pressedStack, GlobalConstants.IgnoredStackAttributes.Append("juiceableLitresLeft", "juiceableLitresTransfered", "squeezeRel")))
			{
				(Api as ICoreClientAPI)?.TriggerIngameError(this, "fullcontainer", Lang.Get("Cannot mix fruit"));
				return false;
			}
			float transferableLitres = (float)handStack.Attributes.GetDecimal("juiceableLitresLeft");
			float usedLitres = (float)handStack.Attributes.GetDecimal("juiceableLitresTransfered");
			int removeItems;
			if (!hprops.LitresPerItem.HasValue)
			{
				if (juiceableLitresLeft + juiceableLitresTransfered + (double)transferableLitres + (double)usedLitres > juiceableLitresCapacity)
				{
					(Api as ICoreClientAPI)?.TriggerIngameError(this, "fullcontainer", Lang.Get("Container is full, press out juice and remove the mash before adding more"));
					return false;
				}
				TransitionState[] sourceTransitionStates = handStack.Collectible.UpdateAndGetTransitionStates(Api.World, handslot);
				TransitionState[] targetTransitionStates = mashStack.Collectible.UpdateAndGetTransitionStates(Api.World, MashSlot);
				if (sourceTransitionStates != null && targetTransitionStates != null)
				{
					Dictionary<EnumTransitionType, TransitionState> targetStatesByType = null;
					targetStatesByType = new Dictionary<EnumTransitionType, TransitionState>();
					TransitionState[] array = targetTransitionStates;
					foreach (TransitionState state in array)
					{
						targetStatesByType[state.Props.Type] = state;
					}
					float t = (transferableLitres + usedLitres) / (transferableLitres + usedLitres + (float)juiceableLitresLeft + (float)juiceableLitresTransfered);
					array = sourceTransitionStates;
					foreach (TransitionState sourceState in array)
					{
						TransitionState targetState = targetStatesByType[sourceState.Props.Type];
						mashStack.Collectible.SetTransitionState(mashStack, sourceState.Props.Type, sourceState.TransitionedHours * t + targetState.TransitionedHours * (1f - t));
					}
				}
				removeItems = 1;
			}
			else
			{
				int desiredTransferAmount = Math.Min(handStack.StackSize, byPlayer.Entity.Controls.ShiftKey ? 1 : (byPlayer.Entity.Controls.CtrlKey ? handStack.Item.MaxStackSize : 4));
				while ((double)((float)desiredTransferAmount * hprops.LitresPerItem.Value) + juiceableLitresLeft + juiceableLitresTransfered > juiceableLitresCapacity)
				{
					desiredTransferAmount--;
				}
				if (desiredTransferAmount <= 0)
				{
					(Api as ICoreClientAPI)?.TriggerIngameError(this, "fullcontainer", Lang.Get("Container is full, press out juice and remove the mash before adding more"));
					return false;
				}
				transferableLitres = (float)desiredTransferAmount * hprops.LitresPerItem.Value;
				removeItems = desiredTransferAmount;
			}
			if (removeItems > 0)
			{
				AssetLocation stackCode = handslot.Itemstack.Collectible.Code;
				handslot.TakeOut(removeItems);
				Api.World.Logger.Audit("{0} Put {1}x{2} into Fruitpress at {3}.", byPlayer.PlayerName, removeItems, stackCode, blockSel.Position);
				mashStack.Attributes.SetDouble("juiceableLitresLeft", juiceableLitresLeft += transferableLitres);
				mashStack.Attributes.SetDouble("juiceableLitresTransfered", juiceableLitresTransfered += usedLitres);
				mashStack.StackSize = 1;
				dryStackSize = GameMath.RoundRandom(Api.World.Rand, ((float)juiceableLitresLeft + (float)juiceableLitresTransfered) * getJuiceableProps(mashStack).PressedDryRatio);
				handslot.MarkDirty();
				MarkDirty(redrawOnClient: true);
				renderer?.reloadMeshes(hprops, mustReload: true);
				(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			}
			return true;
		}
		if (MashSlot.Empty)
		{
			return false;
		}
		convertDryMash();
		if (!byPlayer.InventoryManager.TryGiveItemstack(mashStack, slotNotifyEffect: true))
		{
			Api.World.SpawnItemEntity(mashStack, Pos);
		}
		Api.World.Logger.Audit("{0} Took 1x{1} from Fruitpress at {2}.", byPlayer.PlayerName, mashStack.Collectible.Code, blockSel.Position);
		MashSlot.Itemstack = null;
		renderer?.reloadMeshes(null, mustReload: true);
		if (Api.Side == EnumAppSide.Server)
		{
			MarkDirty(redrawOnClient: true);
		}
		return true;
	}

	private bool InteractGround(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot handslot = byPlayer.InventoryManager.ActiveHotbarSlot;
		ItemStack handStack = handslot.Itemstack;
		if (handslot.Empty && !BucketSlot.Empty)
		{
			if (!byPlayer.InventoryManager.TryGiveItemstack(BucketSlot.Itemstack, slotNotifyEffect: true))
			{
				Api.World.SpawnItemEntity(BucketSlot.Itemstack, Pos);
			}
			Api.World.Logger.Audit("{0} Took 1x{1} from Fruitpress at {2}.", byPlayer.PlayerName, BucketSlot.Itemstack.Collectible.Code, blockSel.Position);
			if (BucketSlot.Itemstack.Block != null)
			{
				Api.World.PlaySoundAt(BucketSlot.Itemstack.Block.Sounds.Place, Pos, -0.5, byPlayer);
			}
			BucketSlot.Itemstack = null;
			MarkDirty(redrawOnClient: true);
			bucketMesh?.Clear();
		}
		else if (handStack != null && handStack.Collectible is BlockLiquidContainerBase { AllowHeldLiquidTransfer: not false, IsTopOpened: not false, CapacityLitres: <20f } && BucketSlot.Empty && handslot.TryPutInto(Api.World, BucketSlot) > 0)
		{
			Api.World.Logger.Audit("{0} Put 1x{1} into Fruitpress at {2}.", byPlayer.PlayerName, BucketSlot.Itemstack.Collectible.Code, blockSel.Position);
			handslot.MarkDirty();
			MarkDirty(redrawOnClient: true);
			genBucketMesh();
			Api.World.PlaySoundAt(handStack.Block.Sounds.Place, Pos, -0.5, byPlayer);
		}
		return true;
	}

	public bool OnBlockInteractStep(float secondsUsed, IPlayer byPlayer, EnumFruitPressSection section)
	{
		if (section != EnumFruitPressSection.Screw)
		{
			return false;
		}
		if (mashStack != null)
		{
			updateSqueezeRel(animUtil.animator.GetAnimationState("compress"));
		}
		if (!CompressAnimActive)
		{
			return (base.Block as BlockFruitPress).RightMouseDown;
		}
		return true;
	}

	public void OnBlockInteractStop(float secondsUsed, IPlayer byPlayer)
	{
		updateSqueezeRel(animUtil.animator.GetAnimationState("compress"));
		if (CompressAnimActive)
		{
			compressAnimMeta.AnimationSpeed = 0f;
			(Api as ICoreServerAPI)?.Network.BroadcastBlockEntityPacket(Pos, 1001, new FruitPressAnimPacket
			{
				AnimationState = EnumFruitPressAnimState.ScrewContinue,
				AnimationSpeed = 0f
			});
		}
	}

	private void updateSqueezeRel(RunningAnimation anim)
	{
		if (anim != null && mashStack != null)
		{
			double squeezeRel = GameMath.Clamp(1f - anim.CurrentFrame / (float)anim.Animation.QuantityFrames / 2f, 0.1f, 1f);
			float selfHeight = (float)(juiceableLitresTransfered + juiceableLitresLeft) / 10f;
			squeezeRel += (double)Math.Max(0f, 0.9f - selfHeight);
			pressSqueezeRel = GameMath.Clamp(squeezeRel, 0.10000000149011612, 1.0);
			squeezeRel = GameMath.Clamp(Math.Min(mashStack.Attributes.GetDouble("squeezeRel", 1.0), squeezeRel), 0.10000000149011612, 1.0);
			mashStack.Attributes.SetDouble("squeezeRel", squeezeRel);
			screwPercent = GameMath.Clamp(1f - anim.CurrentFrame / (float)(anim.Animation.QuantityFrames - 1), 0f, 1f) / selfHeight;
		}
	}

	private void convertDryMash()
	{
		if (juiceableLitresLeft < 0.01)
		{
			mashStack?.Attributes?.RemoveAttribute("juiceableLitresTransfered");
			mashStack?.Attributes?.RemoveAttribute("juiceableLitresLeft");
			mashStack?.Attributes?.RemoveAttribute("squeezeRel");
			if (mashStack?.Collectible.Code.Path != "rot")
			{
				mashStack.StackSize = dryStackSize;
			}
			dryStackSize = 0;
		}
	}

	public bool OnBlockInteractCancel(float secondsUsed, IPlayer byPlayer)
	{
		updateSqueezeRel(animUtil.animator.GetAnimationState("compress"));
		if (CompressAnimActive)
		{
			compressAnimMeta.AnimationSpeed = 0f;
			(Api as ICoreServerAPI)?.Network.BroadcastBlockEntityPacket(Pos, 1001, new FruitPressAnimPacket
			{
				AnimationState = EnumFruitPressAnimState.ScrewContinue,
				AnimationSpeed = 0f
			});
		}
		return true;
	}

	public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
	{
		switch (packetid)
		{
		case 1002:
			compressAnimMeta.AnimationSpeed = 0.5f;
			animUtil.StartAnimation(compressAnimMeta);
			squeezeSoundPlayed = false;
			lastLiquidTransferTotalHours = Api.World.Calendar.TotalHours;
			if (listenerId == 0L)
			{
				listenerId = RegisterGameTickListener(onTick100msServer, 25);
			}
			(Api as ICoreServerAPI)?.Network.BroadcastBlockEntityPacket(Pos, 1001, new FruitPressAnimPacket
			{
				AnimationState = EnumFruitPressAnimState.ScrewStart,
				AnimationSpeed = 0.5f
			});
			break;
		case 1004:
			compressAnimMeta.AnimationSpeed = 0.5f;
			(Api as ICoreServerAPI)?.Network.BroadcastBlockEntityPacket(Pos, 1001, new FruitPressAnimPacket
			{
				AnimationState = EnumFruitPressAnimState.ScrewContinue,
				AnimationSpeed = 0.5f
			});
			break;
		case 1003:
			compressAnimMeta.AnimationSpeed = 1.5f;
			animUtil.StopAnimation("compress");
			(Api as ICoreServerAPI)?.Network.BroadcastBlockEntityPacket(Pos, 1001, new FruitPressAnimPacket
			{
				AnimationState = EnumFruitPressAnimState.Unscrew,
				AnimationSpeed = 1.5f
			});
			animUtil.animator.GetAnimationState("compress").Stop();
			if (MashSlot.Empty && listenerId != 0L)
			{
				UnregisterGameTickListener(listenerId);
				listenerId = 0L;
			}
			break;
		}
		base.OnReceivedClientPacket(fromPlayer, packetid, data);
	}

	public override void OnReceivedServerPacket(int packetid, byte[] data)
	{
		if (packetid == 1001)
		{
			FruitPressAnimPacket packet = SerializerUtil.Deserialize<FruitPressAnimPacket>(data);
			compressAnimMeta.AnimationSpeed = packet.AnimationSpeed;
			if (packet.AnimationState == EnumFruitPressAnimState.ScrewStart)
			{
				animUtil.StartAnimation(compressAnimMeta);
				squeezeSoundPlayed = false;
				lastLiquidTransferTotalHours = Api.World.Calendar.TotalHours;
				if (listenerId == 0L)
				{
					listenerId = RegisterGameTickListener(onTick25msClient, 25);
				}
			}
			else if (packet.AnimationState == EnumFruitPressAnimState.ScrewContinue)
			{
				RunningAnimation anim = animUtil.animator.GetAnimationState("compress");
				if (anim.CurrentFrame <= 0f && packet.CurrentFrame > 0f)
				{
					compressAnimMeta.AnimationSpeed = 0.0001f;
					animUtil.StartAnimation(compressAnimMeta);
				}
				if (anim.CurrentFrame > 0f && anim.CurrentFrame < packet.CurrentFrame)
				{
					compressAnimMeta.AnimationSpeed = 0.0001f;
					while (anim.CurrentFrame < packet.CurrentFrame && anim.CurrentFrame < (float)(anim.Animation.QuantityFrames - 1))
					{
						anim.Progress(1f, 1f);
					}
					compressAnimMeta.AnimationSpeed = packet.AnimationSpeed;
					anim.CurrentFrame = packet.CurrentFrame;
					MarkDirty(redrawOnClient: true);
					updateSqueezeRel(anim);
				}
				if (listenerId == 0L)
				{
					listenerId = RegisterGameTickListener(onTick25msClient, 25);
				}
			}
			else if (packet.AnimationState == EnumFruitPressAnimState.Unscrew)
			{
				animUtil.StopAnimation("compress");
				if (listenerId != 0L)
				{
					UnregisterGameTickListener(listenerId);
					listenerId = 0L;
				}
			}
		}
		base.OnReceivedServerPacket(packetid, data);
	}

	public JuiceableProperties getJuiceableProps(ItemStack stack)
	{
		JuiceableProperties obj = ((stack != null && (stack.ItemAttributes?["juiceableProperties"].Exists).GetValueOrDefault()) ? stack.ItemAttributes["juiceableProperties"].AsObject<JuiceableProperties>(null, stack.Collectible.Code.Domain) : null);
		obj?.LiquidStack?.Resolve(Api.World, "juiceable properties liquidstack", stack.Collectible.Code);
		if (obj != null)
		{
			JsonItemStack pressedStack = obj.PressedStack;
			if (pressedStack != null)
			{
				pressedStack.Resolve(Api.World, "juiceable properties pressedstack", stack.Collectible.Code);
				return obj;
			}
			return obj;
		}
		return obj;
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		if (!MashSlot.Empty)
		{
			convertDryMash();
		}
		base.OnBlockBroken(byPlayer);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		renderer?.Dispose();
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		renderer?.Dispose();
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		bool wasEmpty = Inventory.Empty;
		ItemStack beforeStack = mashStack;
		base.FromTreeAttributes(tree, worldForResolving);
		squeezedLitresLeft = tree.GetDouble("squeezedLitresLeft");
		squeezeSoundPlayed = tree.GetBool("squeezeSoundPlayed");
		dryStackSize = tree.GetInt("dryStackSize");
		lastLiquidTransferTotalHours = tree.GetDouble("lastLiquidTransferTotalHours");
		if (worldForResolving.Side == EnumAppSide.Client)
		{
			if (listenerId > 0 && juiceableLitresLeft <= 0.0)
			{
				UnregisterGameTickListener(listenerId);
				listenerId = 0L;
			}
			renderer?.reloadMeshes(getJuiceableProps(mashStack), wasEmpty != Inventory.Empty || (beforeStack != null && mashStack != null && !beforeStack.Equals(Api.World, mashStack, GlobalConstants.IgnoredStackAttributes)));
			genBucketMesh();
			return;
		}
		if (listenerId == 0L)
		{
			serverListenerActive = tree.GetBool("ServerListenerActive");
		}
		if (listenerId != 0L || serverListenerActive)
		{
			loadedFrame = tree.GetFloat("CurrentFrame");
			compressAnimMeta.AnimationSpeed = tree.GetFloat("AnimationSpeed", compressAnimMeta.AnimationSpeed);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetDouble("squeezedLitresLeft", squeezedLitresLeft);
		tree.SetBool("squeezeSoundPlayed", squeezeSoundPlayed);
		tree.SetInt("dryStackSize", dryStackSize);
		tree.SetDouble("lastLiquidTransferTotalHours", lastLiquidTransferTotalHours);
		if (Api.Side == EnumAppSide.Server)
		{
			if (listenerId != 0L)
			{
				tree.SetBool("ServerListenerActive", value: true);
			}
			if (CompressAnimActive)
			{
				tree.SetFloat("CurrentFrame", animUtil.animator.GetAnimationState("compress").CurrentFrame);
				tree.SetFloat("AnimationSpeed", compressAnimMeta.AnimationSpeed);
			}
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (!base.OnTesselation(mesher, tessThreadTesselator))
		{
			mesher.AddMeshData(meshMovable);
		}
		mesher.AddMeshData(bucketMesh);
		return false;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		if (!BucketSlot.Empty && BucketSlot.Itemstack.Collectible is BlockLiquidContainerBase block)
		{
			dsc.Append(Lang.Get("Container:") + " ");
			block.GetContentInfo(BucketSlot, dsc, Api.World);
			dsc.AppendLine();
		}
		if (!MashSlot.Empty)
		{
			int stacksize = ((mashStack.Collectible.Code.Path != "rot") ? dryStackSize : MashSlot.StackSize);
			if (juiceableLitresLeft > 0.0 && mashStack.Collectible.Code.Path != "rot")
			{
				string juicename = getJuiceableProps(mashStack).LiquidStack.ResolvedItemstack.GetName().ToLowerInvariant();
				dsc.AppendLine(Lang.GetWithFallback("fruitpress-litreswhensqueezed", "Mash produces {0:0.##} litres of juice when squeezed", juiceableLitresLeft, juicename));
			}
			else
			{
				dsc.AppendLine(Lang.Get("{0}x {1}", stacksize, MashSlot.GetStackName().ToLowerInvariant()));
			}
		}
	}

	private void genBucketMesh()
	{
		if (BucketSlot.Empty || capi == null)
		{
			bucketMesh?.Clear();
			return;
		}
		ItemStack stack = BucketSlot.Itemstack;
		IContainedMeshSource meshSource = stack.Collectible?.GetCollectibleInterface<IContainedMeshSource>();
		if (meshSource != null)
		{
			bucketMeshTmp = meshSource.GenMesh(stack, capi.BlockTextureAtlas, Pos);
			bucketMeshTmp.CustomInts = new CustomMeshDataPartInt(bucketMeshTmp.FlagsCount);
			bucketMeshTmp.CustomInts.Count = bucketMeshTmp.FlagsCount;
			bucketMeshTmp.CustomInts.Values.Fill(67108864);
			bucketMeshTmp.CustomFloats = new CustomMeshDataPartFloat(bucketMeshTmp.FlagsCount * 2);
			bucketMeshTmp.CustomFloats.Count = bucketMeshTmp.FlagsCount * 2;
			bucketMesh = bucketMeshTmp.Clone();
		}
	}
}
