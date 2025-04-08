using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityResonator : BlockEntityContainer
{
	internal InventoryGeneric inventory;

	private MeshData cylinderMesh;

	private ResonatorRenderer renderer;

	public bool IsPlaying;

	private MusicTrack track;

	private long startLoadingMs;

	private long handlerId;

	private bool wasStopped;

	public override InventoryBase Inventory => inventory;

	public override string InventoryClassName => "echochamber";

	private BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>()?.animUtil;

	public bool HasDisc => !inventory[0].Empty;

	public BlockEntityResonator()
	{
		inventory = new InventoryGeneric(1, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (api.Side == EnumAppSide.Client)
		{
			(api as ICoreClientAPI).Event.RegisterRenderer(renderer = new ResonatorRenderer(Pos, api as ICoreClientAPI, getRotation()), EnumRenderStage.Opaque, "resonator");
			updateMeshesAndRenderer(api as ICoreClientAPI);
			RegisterGameTickListener(OnClientTick, 50);
			animUtil?.InitializeAnimator("resonator", null, null, new Vec3f(0f, getRotation(), 0f));
		}
	}

	private void OnClientTick(float dt)
	{
		if (track?.Sound != null && track.Sound.IsPlaying)
		{
			ICoreClientAPI capi = Api as ICoreClientAPI;
			Vec3d plrpos = capi.World.Player.Entity?.Pos?.XYZ;
			if (!(plrpos == null))
			{
				float dist = GameMath.Sqrt(plrpos.SquareDistanceTo((double)Pos.X + 0.5, (double)Pos.Y + 0.5, (double)Pos.Z + 0.5));
				float volume = GameMath.Clamp(1f / (float)Math.Log10(Math.Max(1.0, (double)dist * 0.7)) - 0.8f, 0f, 1f);
				track.Sound.SetVolume(volume);
				track.Sound.SetPitch(GameMath.Clamp(1f - capi.Render.ShaderUniforms.GlitchStrength, 0.1f, 1f));
			}
		}
	}

	public void OnInteract(IWorldAccessor world, IPlayer byPlayer)
	{
		if (HasDisc)
		{
			ItemStack stack = inventory[0].Itemstack;
			inventory[0].Itemstack = null;
			inventory[0].MarkDirty();
			if (!byPlayer.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
			{
				world.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 1.0, 0.5));
			}
			Api.World.Logger.Audit("{0} Took 1x{1} from Resonator at {2}.", byPlayer.PlayerName, stack.Collectible.Code, Pos);
			StopMusic();
			IsPlaying = false;
			MarkDirty(redrawOnClient: true);
		}
		else
		{
			ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
			if (slot?.Itemstack?.ItemAttributes != null && slot.Itemstack.ItemAttributes["isPlayableDisc"].AsBool() && slot.Itemstack.ItemAttributes["musicTrack"].AsString() != null)
			{
				inventory[0].Itemstack = slot.TakeOut(1);
				Api.World.Logger.Audit("{0} Put 1x{1} into Resonator at {2}.", byPlayer.PlayerName, inventory[0].Itemstack.Collectible.Code, Pos);
				slot.MarkDirty();
				StartMusic();
				IsPlaying = true;
				MarkDirty(redrawOnClient: true);
			}
		}
	}

	private void StartMusic()
	{
		if (track == null && Api.Side == EnumAppSide.Client)
		{
			string trackstring = inventory[0].Itemstack.ItemAttributes["musicTrack"].AsString();
			if (trackstring != null)
			{
				startLoadingMs = Api.World.ElapsedMilliseconds;
				track = (Api as ICoreClientAPI)?.StartTrack(new AssetLocation(trackstring), 99f, EnumSoundType.MusicGlitchunaffected, onTrackLoaded);
				wasStopped = false;
				Api.World.PlaySoundAt(new AssetLocation("sounds/block/vinyl"), Pos, 0.0, null, randomizePitch: false);
				updateMeshesAndRenderer(Api as ICoreClientAPI);
				animUtil.StartAnimation(new AnimationMetaData
				{
					Animation = "running",
					Code = "running",
					AnimationSpeed = 1f,
					EaseOutSpeed = 8f,
					EaseInSpeed = 8f
				});
			}
		}
	}

	private void StopMusic()
	{
		if (Api.Side == EnumAppSide.Client)
		{
			track?.Stop();
			track = null;
			Api.Event.UnregisterCallback(handlerId);
			cylinderMesh = null;
			updateMeshesAndRenderer(Api as ICoreClientAPI);
			wasStopped = true;
			animUtil.StopAnimation("running");
		}
	}

	private void onTrackLoaded(ILoadedSound sound)
	{
		if (track == null)
		{
			sound?.Dispose();
		}
		else
		{
			if (sound == null)
			{
				return;
			}
			track.Sound = sound;
			Api.Event.EnqueueMainThreadTask(delegate
			{
				track.loading = true;
			}, "settrackloading");
			long longMsPassed = Api.World.ElapsedMilliseconds - startLoadingMs;
			handlerId = RegisterDelayedCallback(delegate
			{
				if (sound.IsDisposed)
				{
					Api.World.Logger.Notification("Resonator track is diposed? o.O");
				}
				if (!wasStopped)
				{
					sound.Start();
				}
				track.loading = false;
			}, (int)Math.Max(0L, 500 - longMsPassed));
		}
	}

	private void updateMeshesAndRenderer(ICoreClientAPI capi)
	{
		if (HasDisc)
		{
			if (cylinderMesh == null)
			{
				cylinderMesh = getOrCreateMesh(capi, "resonatorTuningCylinder" + inventory[0].Itemstack.Collectible.LastCodePart() + "Mesh", (ICoreClientAPI cp) => createCylinderMesh(cp));
			}
		}
		else
		{
			cylinderMesh = null;
		}
		renderer.UpdateMeshes(cylinderMesh);
	}

	private MeshData createCylinderMesh(ICoreClientAPI cp)
	{
		cp.Tesselator.TesselateItem(inventory[0].Itemstack.Item, out var discMesh);
		return discMesh;
	}

	private int getRotation()
	{
		Block block = Api.World.BlockAccessor.GetBlock(Pos);
		int rot = 0;
		switch (block.LastCodePart())
		{
		case "north":
			rot = 0;
			break;
		case "east":
			rot = 270;
			break;
		case "south":
			rot = 180;
			break;
		case "west":
			rot = 90;
			break;
		}
		return rot;
	}

	private MeshData getOrCreateMesh(ICoreClientAPI capi, string code, CreateMeshDelegate onCreate)
	{
		if (!Api.ObjectCache.TryGetValue(code, out var obj))
		{
			MeshData mesh = onCreate(capi);
			Api.ObjectCache[code] = mesh;
			return mesh;
		}
		return (MeshData)obj;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		IsPlaying = tree.GetBool("isplaying");
		if (worldForResolving.Side == EnumAppSide.Client && Api != null)
		{
			if (IsPlaying && inventory[0]?.Itemstack != null)
			{
				StartMusic();
			}
			else
			{
				StopMusic();
			}
			MarkDirty(redrawOnClient: true);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("isplaying", IsPlaying);
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		renderer?.Dispose();
		track?.Stop();
		track = null;
		Api?.Event.UnregisterCallback(handlerId);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		renderer?.Dispose();
		track?.Stop();
		track = null;
		Api.Event.UnregisterCallback(handlerId);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
	}
}
