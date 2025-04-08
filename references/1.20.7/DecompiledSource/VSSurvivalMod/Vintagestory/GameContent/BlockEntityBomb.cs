using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class BlockEntityBomb : BlockEntity
{
	public float RemainingSeconds;

	private bool lit;

	private string ignitedByPlayerUid;

	private float blastRadius;

	private float injureRadius;

	private EnumBlastType blastType;

	private ILoadedSound fuseSound;

	public static SimpleParticleProperties smallSparks;

	private bool combusted;

	public bool CascadeLit { get; set; }

	public virtual float FuseTimeSeconds => 4f;

	public virtual EnumBlastType BlastType => blastType;

	public virtual float BlastRadius => blastRadius;

	public virtual float InjureRadius => injureRadius;

	public bool IsLit => lit;

	static BlockEntityBomb()
	{
		smallSparks = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(255, 255, 233, 0), new Vec3d(), new Vec3d(), new Vec3f(-3f, 5f, -3f), new Vec3f(3f, 8f, 3f), 0.03f, 1f, 0.05f, 0.15f, EnumParticleModel.Quad);
		smallSparks.VertexFlags = 255;
		smallSparks.AddPos.Set(0.0625, 0.0, 0.0625);
		smallSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.05f);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		RegisterGameTickListener(OnTick, 50);
		if (fuseSound == null && api.Side == EnumAppSide.Client)
		{
			fuseSound = ((IClientWorldAccessor)api.World).LoadSound(new SoundParams
			{
				Location = new AssetLocation("sounds/effect/fuse"),
				ShouldLoop = true,
				Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
				DisposeOnFinish = false,
				Volume = 0.1f,
				Range = 16f
			});
		}
		blastRadius = base.Block.Attributes["blastRadius"].AsInt(4);
		injureRadius = base.Block.Attributes["injureRadius"].AsInt(8);
		blastType = (EnumBlastType)base.Block.Attributes["blastType"].AsInt();
	}

	private void OnTick(float dt)
	{
		if (lit)
		{
			RemainingSeconds -= dt;
			if (Api.Side == EnumAppSide.Server && RemainingSeconds <= 0f)
			{
				Combust(dt);
			}
			if (Api.Side == EnumAppSide.Client)
			{
				smallSparks.MinPos.Set((double)Pos.X + 0.45, (double)Pos.Y + 0.53, (double)Pos.Z + 0.45);
				Api.World.SpawnParticles(smallSparks);
			}
		}
	}

	public void Combust(float dt)
	{
		if (!combusted)
		{
			if (nearToClaimedLand())
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, -0.5, null, randomizePitch: false, 16f);
				lit = false;
				MarkDirty(redrawOnClient: true);
			}
			else
			{
				combusted = true;
				Api.World.BlockAccessor.SetBlock(0, Pos);
				((IServerWorldAccessor)Api.World).CreateExplosion(Pos, BlastType, BlastRadius, InjureRadius);
			}
		}
	}

	public bool nearToClaimedLand()
	{
		int rad = (int)Math.Ceiling(BlastRadius);
		Cuboidi exploArea = new Cuboidi(Pos.AddCopy(-rad, -rad, -rad), Pos.AddCopy(rad, rad, rad));
		List<LandClaim> claims = (Api as ICoreServerAPI).WorldManager.SaveGame.LandClaims;
		for (int i = 0; i < claims.Count; i++)
		{
			if (claims[i].Intersects(exploArea))
			{
				return true;
			}
		}
		return false;
	}

	internal void OnBlockExploded(BlockPos pos)
	{
		if (Api.Side == EnumAppSide.Server && (!lit || (double)RemainingSeconds > 0.3) && !nearToClaimedLand())
		{
			Api.World.RegisterCallback(Combust, 250);
			CascadeLit = true;
		}
	}

	internal void OnIgnite(IPlayer byPlayer)
	{
		if (!lit)
		{
			if (Api.Side == EnumAppSide.Client)
			{
				fuseSound.Start();
			}
			lit = true;
			RemainingSeconds = FuseTimeSeconds;
			ignitedByPlayerUid = byPlayer?.PlayerUID;
			MarkDirty();
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		RemainingSeconds = tree.GetFloat("remainingSeconds");
		lit = tree.GetInt("lit") > 0;
		ignitedByPlayerUid = tree.GetString("ignitedByPlayerUid");
		if (!lit)
		{
			ICoreAPI api = Api;
			if (api != null && api.Side == EnumAppSide.Client)
			{
				fuseSound.Stop();
			}
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetFloat("remainingSeconds", RemainingSeconds);
		tree.SetInt("lit", lit ? 1 : 0);
		tree.SetString("ignitedByPlayerUid", ignitedByPlayerUid);
	}

	~BlockEntityBomb()
	{
		if (fuseSound != null)
		{
			fuseSound.Dispose();
		}
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (fuseSound != null)
		{
			fuseSound.Stop();
		}
	}
}
