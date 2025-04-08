using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class EntityBlockFalling : Entity
{
	private const int packetIdMagicNumber = 1234;

	private static HashSet<long> fallingNow = new HashSet<long>();

	private readonly List<int> fallDirections = new List<int> { 0, 1, 2, 3 };

	private int lastFallDirection;

	private int hopUpHeight = 1;

	private FallingBlockParticlesModSystem particleSys;

	private int ticksAlive;

	private int lingerTicks;

	private AssetLocation blockCode;

	private ItemStack[] drops;

	private float impactDamageMul;

	private bool fallHandled;

	private byte[] lightHsv;

	private AssetLocation fallSound;

	private ILoadedSound sound;

	private float soundStartDelay;

	private bool canFallSideways;

	private Vec3d fallMotion = new Vec3d();

	private float pushaccum;

	internal float dustIntensity;

	internal ItemStack stackForParticleColor;

	internal bool nowImpacted;

	public bool InitialBlockRemoved;

	public BlockPos initialPos;

	public TreeAttribute blockEntityAttributes;

	public string blockEntityClass;

	public BlockEntity removedBlockentity;

	public bool DoRemoveBlock = true;

	public float maxSpawnHeightForParticles = 1.4f;

	public override float MaterialDensity => 99999f;

	public override byte[] LightHsv => lightHsv;

	public Block Block => World.BlockAccessor.GetBlock(blockCode);

	public override bool IsInteractable => false;

	public EntityBlockFalling()
	{
	}

	public EntityBlockFalling(Block block, BlockEntity blockEntity, BlockPos initialPos, AssetLocation fallSound, float impactDamageMul, bool canFallSideways, float dustIntensity)
	{
		this.impactDamageMul = impactDamageMul;
		this.fallSound = fallSound;
		this.canFallSideways = canFallSideways;
		this.dustIntensity = dustIntensity;
		WatchedAttributes.SetBool("canFallSideways", canFallSideways);
		WatchedAttributes.SetFloat("dustIntensity", dustIntensity);
		if (fallSound != null)
		{
			WatchedAttributes.SetString("fallSound", fallSound.ToShortString());
		}
		Code = new AssetLocation("blockfalling");
		blockCode = block.Code;
		removedBlockentity = blockEntity;
		this.initialPos = initialPos.Copy();
		ServerPos.SetPos(initialPos);
		ServerPos.X += 0.5;
		ServerPos.Y -= 0.01;
		ServerPos.Z += 0.5;
		Pos.SetFrom(ServerPos);
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		if (removedBlockentity != null)
		{
			blockEntityAttributes = new TreeAttribute();
			removedBlockentity.ToTreeAttributes(blockEntityAttributes);
			blockEntityClass = api.World.ClassRegistry.GetBlockEntityClass(removedBlockentity.GetType());
		}
		SimulationRange = (int)(0.75f * (float)GlobalConstants.DefaultSimulationRange);
		base.Initialize(properties, api, InChunkIndex3d);
		try
		{
			drops = Block.GetDrops(api.World, initialPos, null);
		}
		catch (Exception)
		{
			drops = null;
			api.Logger.Warning("Falling block entity could not properly initialise its drops during chunk loading, as original block is no longer at " + initialPos);
		}
		lightHsv = Block.GetLightHsv(World.BlockAccessor, initialPos);
		if (drops != null && drops.Length != 0)
		{
			stackForParticleColor = drops[0];
		}
		else
		{
			stackForParticleColor = new ItemStack(Block);
		}
		if (api.Side == EnumAppSide.Client && fallSound != null && fallingNow.Count < 100)
		{
			fallingNow.Add(EntityId);
			ICoreClientAPI capi = api as ICoreClientAPI;
			sound = capi.World.LoadSound(new SoundParams
			{
				Location = fallSound.WithPathPrefixOnce("sounds/").WithPathAppendixOnce(".ogg"),
				Position = new Vec3f((float)Pos.X, (float)Pos.Y, (float)Pos.Z),
				Range = 32f,
				Pitch = 0.8f + (float)capi.World.Rand.NextDouble() * 0.3f,
				Volume = 1f,
				SoundType = EnumSoundType.Ambient
			});
			sound.Start();
			soundStartDelay = 0.05f + (float)capi.World.Rand.NextDouble() / 3f;
		}
		canFallSideways = WatchedAttributes.GetBool("canFallSideways");
		dustIntensity = WatchedAttributes.GetFloat("dustIntensity");
		if (WatchedAttributes.HasAttribute("fallSound"))
		{
			fallSound = new AssetLocation(WatchedAttributes.GetString("fallSound"));
		}
		if (api.World.Side == EnumAppSide.Client)
		{
			particleSys = api.ModLoader.GetModSystem<FallingBlockParticlesModSystem>();
			particleSys.Register(this);
		}
		RandomizeFallingDirectionsOrder();
		if (DoRemoveBlock)
		{
			World.BlockAccessor.SetBlock(0, initialPos);
		}
	}

	public override void OnGameTick(float dt)
	{
		World.FrameProfiler.Enter("entity-tick-unsstablefalling");
		if (soundStartDelay > 0f)
		{
			soundStartDelay -= dt;
			if (soundStartDelay <= 0f)
			{
				sound.Start();
			}
		}
		if (sound != null)
		{
			sound.SetPosition((float)Pos.X, (float)Pos.Y, (float)Pos.Z);
		}
		if (lingerTicks > 0)
		{
			lingerTicks--;
			if (lingerTicks != 0)
			{
				return;
			}
			if (Api.Side == EnumAppSide.Client && sound != null)
			{
				sound.FadeOut(3f, delegate(ILoadedSound s)
				{
					s.Dispose();
				});
			}
			Die();
			return;
		}
		World.FrameProfiler.Mark("entity-tick-unsstablefalling-sound(etc)");
		ticksAlive++;
		if (ticksAlive >= 2 || Api.World.Side == EnumAppSide.Client)
		{
			if (!InitialBlockRemoved)
			{
				InitialBlockRemoved = true;
				UpdateBlock(remove: true, initialPos);
			}
			foreach (EntityBehavior behavior in base.SidedProperties.Behaviors)
			{
				behavior.OnGameTick(dt);
			}
			World.FrameProfiler.Mark("entity-tick-unsstablefalling-physics(etc)");
		}
		pushaccum += dt;
		fallMotion.X *= 0.9900000095367432;
		fallMotion.Z *= 0.9900000095367432;
		if (pushaccum > 0.2f)
		{
			pushaccum = 0f;
			if (!base.Collided)
			{
				Entity[] entities;
				if (Api.Side == EnumAppSide.Server)
				{
					entities = World.GetEntitiesAround(base.SidedPos.XYZ, 1.1f, 1.1f, (Entity e) => !(e is EntityBlockFalling));
					bool didhit = false;
					Entity[] array = entities;
					foreach (Entity entity in array)
					{
						bool nowhit = entity.ReceiveDamage(new DamageSource
						{
							Source = EnumDamageSource.Block,
							Type = EnumDamageType.Crushing,
							SourceBlock = Block,
							SourcePos = base.SidedPos.XYZ
						}, 10f * (float)Math.Abs(ServerPos.Motion.Y) * impactDamageMul);
						if (nowhit && !didhit)
						{
							didhit = nowhit;
							Api.World.PlaySoundAt(Block.Sounds.Break, entity);
						}
					}
				}
				else
				{
					entities = World.GetEntitiesAround(base.SidedPos.XYZ, 1.1f, 1.1f, (Entity e) => e is EntityPlayer);
				}
				for (int i = 0; i < entities.Length; i++)
				{
					entities[i].SidedPos.Motion.Add(fallMotion.X / 10.0, 0.0, fallMotion.Z / 10.0);
				}
			}
		}
		World.FrameProfiler.Mark("entity-tick-unsstablefalling-finalizemotion");
		if (Api.Side == EnumAppSide.Server && !base.Collided && World.Rand.NextDouble() < 0.01)
		{
			World.BlockAccessor.TriggerNeighbourBlockUpdate(ServerPos.AsBlockPos);
			World.FrameProfiler.Mark("entity-tick-unsstablefalling-neighborstrigger");
		}
		if (CollidedVertically && Pos.Motion.Length() < 0.0010000000474974513)
		{
			OnFallToGround(0.0);
			World.FrameProfiler.Mark("entity-tick-unsstablefalling-falltoground");
		}
		World.FrameProfiler.Leave();
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		if (Api.World.Side == EnumAppSide.Client)
		{
			fallingNow.Remove(EntityId);
			particleSys.Unregister(this);
		}
	}

	private void UpdateBlock(bool remove, BlockPos pos)
	{
		if (remove)
		{
			if (DoRemoveBlock)
			{
				World.BlockAccessor.MarkBlockDirty(pos, delegate
				{
					OnChunkRetesselated(on: true);
				});
			}
			else
			{
				OnChunkRetesselated(on: true);
			}
		}
		else
		{
			if (World.BlockAccessor.GetBlock(pos, 2).Id == 0 || Block.BlockMaterial != EnumBlockMaterial.Snow)
			{
				World.BlockAccessor.SetBlock(Block.BlockId, pos);
				World.BlockAccessor.MarkBlockDirty(pos, delegate
				{
					OnChunkRetesselated(on: false);
				});
			}
			else
			{
				OnChunkRetesselated(on: true);
			}
			if (blockEntityAttributes != null)
			{
				BlockEntity be = World.BlockAccessor.GetBlockEntity(pos);
				blockEntityAttributes.SetInt("posx", pos.X);
				blockEntityAttributes.SetInt("posy", pos.Y);
				blockEntityAttributes.SetInt("posz", pos.Z);
				be?.FromTreeAttributes(blockEntityAttributes, World);
			}
		}
		World.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
	}

	private void OnChunkRetesselated(bool on)
	{
		if (base.Properties.Client.Renderer is EntityBlockFallingRenderer renderer)
		{
			renderer.DoRender = on;
		}
	}

	private void RandomizeFallingDirectionsOrder()
	{
		for (int i = fallDirections.Count - 1; i > 0; i--)
		{
			int swapIndex = GameMath.MurmurHash3Mod(EntityId.GetHashCode(), i, i, fallDirections.Count);
			int temp = fallDirections[i];
			fallDirections[i] = fallDirections[swapIndex];
			fallDirections[swapIndex] = temp;
		}
		lastFallDirection = fallDirections[3];
	}

	public override void OnFallToGround(double motionY)
	{
		if (fallHandled)
		{
			return;
		}
		BlockPos pos = base.SidedPos.AsBlockPos;
		BlockPos finalPos = ServerPos.AsBlockPos;
		Block block = null;
		if (Api.Side == EnumAppSide.Server)
		{
			block = World.BlockAccessor.GetMostSolidBlock(finalPos);
			if (block.CanAcceptFallOnto(World, finalPos, Block, blockEntityAttributes))
			{
				Api.Event.EnqueueMainThreadTask(delegate
				{
					block.OnFallOnto(World, finalPos, Block, blockEntityAttributes);
				}, "BlockFalling-OnFallOnto");
				lingerTicks = 3;
				fallHandled = true;
				return;
			}
		}
		if (canFallSideways)
		{
			foreach (int i in fallDirections)
			{
				BlockFacing facing = BlockFacing.ALLFACES[i];
				if ((facing != BlockFacing.NORTH || lastFallDirection != BlockFacing.SOUTH.Index) && (facing != BlockFacing.WEST || lastFallDirection != BlockFacing.EAST.Index) && (facing != BlockFacing.SOUTH || lastFallDirection != BlockFacing.NORTH.Index) && (facing != BlockFacing.EAST || lastFallDirection != BlockFacing.WEST.Index) && World.BlockAccessor.GetMostSolidBlock(pos.X + facing.Normali.X, pos.InternalY + facing.Normali.Y, pos.Z + facing.Normali.Z).Replaceable >= 6000 && World.BlockAccessor.GetMostSolidBlock(pos.X + facing.Normali.X, pos.InternalY + facing.Normali.Y - 1, pos.Z + facing.Normali.Z).Replaceable >= 6000)
				{
					if (Api.Side == EnumAppSide.Server)
					{
						base.SidedPos.X += facing.Normali.X;
						base.SidedPos.Y += facing.Normali.Y;
						base.SidedPos.Z += facing.Normali.Z;
					}
					fallMotion.Set(facing.Normalf.X, 0.0, facing.Normalf.Z);
					lastFallDirection = i;
					return;
				}
			}
		}
		nowImpacted = true;
		if (Api.Side == EnumAppSide.Server)
		{
			if ((block.Id != 0 && Block.BlockMaterial == EnumBlockMaterial.Snow) || block.IsReplacableBy(Block))
			{
				Api.Event.EnqueueMainThreadTask(delegate
				{
					if (block.Id != 0 && Block.BlockMaterial == EnumBlockMaterial.Snow)
					{
						UpdateSnowLayer(finalPos, block);
						(Api as ICoreServerAPI).Network.BroadcastEntityPacket(EntityId, 1234);
					}
					else if (block.IsReplacableBy(Block))
					{
						if (!InitialBlockRemoved)
						{
							InitialBlockRemoved = true;
							UpdateBlock(remove: true, initialPos);
						}
						UpdateBlock(remove: false, finalPos);
						(Api as ICoreServerAPI).Network.BroadcastEntityPacket(EntityId, 1234);
					}
				}, "BlockFalling-consequences");
			}
			else
			{
				if (block.Replaceable < 6000)
				{
					base.SidedPos.Y += hopUpHeight;
					hopUpHeight++;
					if (hopUpHeight > 3)
					{
						hopUpHeight = 1;
					}
					return;
				}
				DropItems(finalPos);
			}
			if (impactDamageMul > 0f)
			{
				Entity[] entitiesInsideCuboid = World.GetEntitiesInsideCuboid(finalPos, finalPos.AddCopy(1, 1, 1), (Entity e) => !(e is EntityBlockFalling));
				bool didhit = false;
				Entity[] array = entitiesInsideCuboid;
				foreach (Entity entity in array)
				{
					bool nowhit = entity.ReceiveDamage(new DamageSource
					{
						Source = EnumDamageSource.Block,
						Type = EnumDamageType.Crushing,
						SourceBlock = Block,
						SourcePos = finalPos.ToVec3d()
					}, 18f * (float)Math.Abs(motionY) * impactDamageMul);
					if (nowhit && !didhit)
					{
						didhit = nowhit;
						Api.World.PlaySoundAt(Block.Sounds.Break, entity);
					}
				}
			}
		}
		lingerTicks = 50;
		fallHandled = true;
		hopUpHeight = 1;
	}

	private void UpdateSnowLayer(BlockPos finalPos, Block block)
	{
		Block snowblock = block.GetSnowCoveredVariant(finalPos, block.snowLevel + 1f);
		if (snowblock != null && snowblock != block)
		{
			World.BlockAccessor.ExchangeBlock(snowblock.Id, finalPos);
		}
	}

	public override void OnReceivedServerPacket(int packetid, byte[] data)
	{
		base.OnReceivedServerPacket(packetid, data);
		if (packetid != 1234)
		{
			return;
		}
		if (base.Properties.Client.Renderer is EntityBlockFallingRenderer)
		{
			World.BlockAccessor.MarkBlockDirty(Pos.AsBlockPos, delegate
			{
				OnChunkRetesselated(on: false);
			});
		}
		lingerTicks = 50;
		fallHandled = true;
		nowImpacted = true;
		particleSys.Unregister(this);
	}

	private void DropItems(BlockPos pos)
	{
		Vec3d dpos = pos.ToVec3d().Add(0.5, 0.5, 0.5);
		if (drops != null)
		{
			for (int i = 0; i < drops.Length; i++)
			{
				World.SpawnItemEntity(drops[i], dpos);
			}
		}
		if (removedBlockentity is IBlockEntityContainer bec)
		{
			bec.DropContents(dpos);
		}
	}

	public override void ToBytes(BinaryWriter writer, bool forClient)
	{
		WatchedAttributes.SetFloat("maxSpawnHeightForParticles", maxSpawnHeightForParticles);
		base.ToBytes(writer, forClient);
		writer.Write(initialPos.X);
		writer.Write(initialPos.Y);
		writer.Write(initialPos.Z);
		writer.Write(blockCode.ToShortString());
		writer.Write(blockEntityAttributes == null);
		if (blockEntityAttributes != null)
		{
			blockEntityAttributes.ToBytes(writer);
			writer.Write(blockEntityClass);
		}
		writer.Write(DoRemoveBlock);
	}

	public override void FromBytes(BinaryReader reader, bool forClient)
	{
		base.FromBytes(reader, forClient);
		initialPos = new BlockPos();
		initialPos.X = reader.ReadInt32();
		initialPos.Y = reader.ReadInt32();
		initialPos.Z = reader.ReadInt32();
		blockCode = new AssetLocation(reader.ReadString());
		if (!reader.ReadBoolean())
		{
			blockEntityAttributes = new TreeAttribute();
			blockEntityAttributes.FromBytes(reader);
			blockEntityClass = reader.ReadString();
		}
		if (WatchedAttributes.HasAttribute("fallSound"))
		{
			fallSound = new AssetLocation(WatchedAttributes.GetString("fallSound"));
		}
		canFallSideways = WatchedAttributes.GetBool("canFallSideways");
		dustIntensity = WatchedAttributes.GetFloat("dustIntensity");
		maxSpawnHeightForParticles = WatchedAttributes.GetFloat("maxSpawnHeightForParticles");
		DoRemoveBlock = reader.ReadBoolean();
	}

	public override bool ShouldReceiveDamage(DamageSource damageSource, float damage)
	{
		return false;
	}
}
