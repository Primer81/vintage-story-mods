using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ClothManager : ModSystem, IRenderer, IDisposable
{
	private int nextClothId = 1;

	private ICoreClientAPI capi;

	private ICoreServerAPI sapi;

	private ICoreAPI api;

	private Dictionary<int, ClothSystem> clothSystems = new Dictionary<int, ClothSystem>();

	internal ParticlePhysics partPhysics;

	private MeshRef ropeMeshRef;

	private MeshData updateMesh;

	private IShaderProgram prog;

	private ILoadedSound stretchSound;

	public float accum3s;

	public float accum100ms;

	private IServerNetworkChannel clothSystemChannel;

	public double RenderOrder => 1.0;

	public int RenderRange => 12;

	public override double ExecuteOrder()
	{
		return 0.4;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		this.api = api;
		partPhysics = new ParticlePhysics(api.World.GetLockFreeBlockAccessor());
		partPhysics.PhysicsTickTime = 1f / 60f;
		partPhysics.MotionCap = 10f;
		api.Network.RegisterChannel("clothphysics").RegisterMessageType<UnregisterClothSystemPacket>().RegisterMessageType<ClothSystemPacket>()
			.RegisterMessageType<ClothPointPacket>()
			.RegisterMessageType<ClothLengthPacket>();
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.ChatCommands.GetOrCreate("debug").BeginSubCommand("clothtest").WithDescription("Commands to test the cloth system")
			.BeginSubCommand("clear")
			.WithDescription("clears")
			.HandleWith(onClothTestClear)
			.EndSubCommand()
			.EndSubCommand();
		api.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "clothsimu");
		api.Event.BlockTexturesLoaded += Event_BlockTexturesLoaded;
		api.Network.GetChannel("clothphysics").SetMessageHandler<UnregisterClothSystemPacket>(onUnregPacketClient).SetMessageHandler<ClothSystemPacket>(onRegPacketClient)
			.SetMessageHandler<ClothPointPacket>(onPointPacketClient)
			.SetMessageHandler<ClothLengthPacket>(onLengthPacketClient);
		api.Event.LeaveWorld += Event_LeaveWorld;
	}

	public ClothSystem GetClothSystem(int clothid)
	{
		clothSystems.TryGetValue(clothid, out var sys);
		return sys;
	}

	public ClothSystem GetClothSystemAttachedToBlock(BlockPos pos)
	{
		foreach (ClothSystem sys in clothSystems.Values)
		{
			if (sys.FirstPoint.PinnedToBlockPos == pos || sys.LastPoint.PinnedToBlockPos == pos)
			{
				return sys;
			}
		}
		return null;
	}

	public void OnRenderFrame(float dt, EnumRenderStage stage)
	{
		if (updateMesh == null)
		{
			return;
		}
		dt = Math.Min(dt, 0.5f);
		if (!capi.IsGamePaused)
		{
			tickPhysics(dt);
		}
		accum100ms += dt;
		if ((double)accum100ms > 0.1)
		{
			accum100ms = 0f;
			if (clothSystems.Count > 0)
			{
				float maxext = -1f;
				ClothSystem maxcs = null;
				float stretchWarn = 0.4f;
				foreach (KeyValuePair<int, ClothSystem> clothSystem in clothSystems)
				{
					ClothSystem cs = clothSystem.Value;
					if (cs.MaxExtension > (double)cs.StretchWarn)
					{
						cs.secondsOverStretched += dt;
					}
					else
					{
						cs.secondsOverStretched = 0f;
					}
					if (cs.MaxExtension > (double)maxext)
					{
						maxext = (float)cs.MaxExtension;
						maxcs = cs;
						stretchWarn = cs.StretchWarn;
					}
				}
				if (maxext > stretchWarn && (double)maxcs.secondsOverStretched > 0.2)
				{
					float intensity = 10f * (maxext - stretchWarn);
					if (!stretchSound.IsPlaying)
					{
						stretchSound.Start();
					}
					stretchSound.SetPosition((float)maxcs.CenterPosition.X, (float)maxcs.CenterPosition.Y, (float)maxcs.CenterPosition.Z);
					stretchSound.SetVolume(GameMath.Clamp(intensity, 0.5f, 1f));
					stretchSound.SetPitch(GameMath.Clamp(intensity + 0.7f, 0.7f, 1.2f));
				}
				else
				{
					stretchSound.Stop();
				}
			}
			else
			{
				stretchSound.Stop();
			}
		}
		int count = 0;
		updateMesh.CustomFloats.Count = 0;
		foreach (KeyValuePair<int, ClothSystem> val2 in clothSystems)
		{
			if (val2.Value.Active)
			{
				count += val2.Value.UpdateMesh(updateMesh, dt);
				updateMesh.CustomFloats.Count = count * 20;
			}
		}
		if (count > 0)
		{
			if (prog.Disposed)
			{
				prog = capi.Shader.GetProgramByName("instanced");
			}
			capi.Render.GlToggleBlend(blend: false);
			prog.Use();
			prog.BindTexture2D("tex", capi.ItemTextureAtlas.Positions[0].atlasTextureId, 0);
			prog.Uniform("rgbaFogIn", capi.Render.FogColor);
			prog.Uniform("rgbaAmbientIn", capi.Render.AmbientColor);
			prog.Uniform("fogMinIn", capi.Render.FogMin);
			prog.Uniform("fogDensityIn", capi.Render.FogDensity);
			prog.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
			prog.UniformMatrix("modelViewMatrix", capi.Render.CameraMatrixOriginf);
			updateMesh.CustomFloats.Count = count * 20;
			capi.Render.UpdateMesh(ropeMeshRef, updateMesh);
			capi.Render.RenderMeshInstanced(ropeMeshRef, count);
			prog.Stop();
		}
		foreach (KeyValuePair<int, ClothSystem> val in clothSystems)
		{
			if (val.Value.Active)
			{
				val.Value.CustomRender(dt);
			}
		}
	}

	private void tickPhysics(float dt)
	{
		foreach (KeyValuePair<int, ClothSystem> val3 in clothSystems)
		{
			if (val3.Value.Active)
			{
				val3.Value.updateFixedStep(dt);
			}
		}
		if (sapi == null)
		{
			return;
		}
		List<int> toRemove = new List<int>();
		accum100ms += dt;
		if (accum100ms > 0.1f)
		{
			accum100ms = 0f;
			List<ClothPointPacket> packets = new List<ClothPointPacket>();
			foreach (KeyValuePair<int, ClothSystem> val2 in clothSystems)
			{
				ClothSystem cs2 = val2.Value;
				cs2.CollectDirtyPoints(packets);
				if (cs2.MaxExtension > (double)cs2.StretchRip)
				{
					cs2.secondsOverStretched += 0.1f;
					if ((double)cs2.secondsOverStretched > 4.0 - cs2.MaxExtension * 2.0)
					{
						Vec3d soundPos = cs2.CenterPosition;
						if (cs2.FirstPoint.PinnedToEntity != null)
						{
							soundPos = cs2.FirstPoint.PinnedToEntity.Pos.XYZ;
						}
						sapi.World.PlaySoundAt(new AssetLocation("sounds/effect/roperip"), soundPos.X, soundPos.Y, soundPos.Z);
						ClothPoint fp = cs2.FirstPoint;
						Vec3d dir = cs2.LastPoint.Pos - fp.Pos;
						double len = dir.Length();
						for (float i = 0f; (double)i < len; i += 0.15f)
						{
							Vec3d pos = new Vec3d(fp.Pos.X + dir.X * (double)i / len, fp.Pos.Y + dir.Y * (double)i / len, fp.Pos.Z + dir.Z * (double)i / len);
							sapi.World.SpawnParticles(2f, ColorUtil.ColorFromRgba(60, 97, 115, 255), pos, pos, new Vec3f(-4f, -1f, -4f), new Vec3f(4f, 2f, 4f), 2f, 1f, 0.5f, EnumParticleModel.Cube);
						}
						toRemove.Add(val2.Key);
					}
				}
				else
				{
					cs2.secondsOverStretched = 0f;
				}
			}
			foreach (ClothPointPacket p in packets)
			{
				clothSystemChannel.BroadcastPacket(p);
			}
		}
		accum3s += dt;
		if (accum3s > 3f)
		{
			accum3s = 0f;
			foreach (KeyValuePair<int, ClothSystem> val in clothSystems)
			{
				if (!val.Value.PinnedAnywhere)
				{
					toRemove.Add(val.Key);
				}
				else
				{
					val.Value.slowTick3s();
				}
			}
		}
		foreach (int id in toRemove)
		{
			bool spawnitem = true;
			ClothSystem cs = clothSystems[id];
			spawnitem &= (cs.FirstPoint.PinnedToEntity as EntityItem)?.Itemstack?.Collectible.Code.Path != "rope" && (cs.LastPoint.PinnedToEntity as EntityItem)?.Itemstack?.Collectible.Code.Path != "rope";
			if (cs.FirstPoint.PinnedToEntity is EntityAgent eagentn)
			{
				eagentn.WalkInventory(delegate(ItemSlot slot)
				{
					if (slot.Empty)
					{
						return true;
					}
					if ((slot.Itemstack.Attributes?.GetInt("clothId") ?? 0) == id)
					{
						spawnitem = false;
						slot.Itemstack.Attributes.RemoveAttribute("clothId");
						slot.Itemstack.Attributes.RemoveAttribute("ropeHeldByEntityId");
						return false;
					}
					return true;
				});
			}
			if (cs.LastPoint.PinnedToEntity is EntityAgent eagentn2)
			{
				eagentn2.WalkInventory(delegate(ItemSlot slot)
				{
					if (slot.Empty)
					{
						return true;
					}
					if ((slot.Itemstack.Attributes?.GetInt("clothId") ?? 0) == id)
					{
						spawnitem = false;
						slot.Itemstack.Attributes.RemoveAttribute("clothId");
						slot.Itemstack.Attributes.RemoveAttribute("ropeHeldByEntityId");
						return false;
					}
					return true;
				});
			}
			if (spawnitem)
			{
				sapi.World.SpawnItemEntity(new ItemStack(sapi.World.GetItem(new AssetLocation("rope"))), clothSystems[id].CenterPosition);
			}
			else if (cs.FirstPoint.PinnedToEntity is EntityItem && cs.LastPoint.PinnedToEntity is EntityPlayer)
			{
				cs.FirstPoint.PinnedToEntity.Die(EnumDespawnReason.Removed);
			}
			UnregisterCloth(id);
		}
	}

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	private void Event_LeaveWorld()
	{
		ropeMeshRef?.Dispose();
	}

	private void onPointPacketClient(ClothPointPacket msg)
	{
		if (clothSystems.TryGetValue(msg.ClothId, out var sys))
		{
			sys.updatePoint(msg);
		}
	}

	private void onLengthPacketClient(ClothLengthPacket msg)
	{
		if (clothSystems.TryGetValue(msg.ClothId, out var sys))
		{
			sys.ChangeRopeLength(msg.LengthChange);
		}
	}

	private void onRegPacketClient(ClothSystemPacket msg)
	{
		ClothSystem[] array = msg.ClothSystems;
		foreach (ClothSystem system in array)
		{
			system.Init(capi, this);
			system.restoreReferences();
			clothSystems[system.ClothId] = system;
		}
	}

	private void onUnregPacketClient(UnregisterClothSystemPacket msg)
	{
		int[] clothIds = msg.ClothIds;
		foreach (int clothid in clothIds)
		{
			UnregisterCloth(clothid);
		}
	}

	private void Event_BlockTexturesLoaded()
	{
		if (stretchSound == null)
		{
			stretchSound = capi.World.LoadSound(new SoundParams
			{
				Location = new AssetLocation("sounds/effect/ropestretch"),
				DisposeOnFinish = false,
				ShouldLoop = true,
				SoundType = EnumSoundType.Sound,
				Volume = 0.5f,
				ReferenceDistance = 5f
			});
		}
		prog = capi.Shader.GetProgramByName("instanced");
		Item itemRope = capi.World.GetItem(new AssetLocation("rope"));
		Shape shape = Shape.TryGet(capi, "shapes/item/ropesection.json");
		if (itemRope != null && shape != null)
		{
			capi.Tesselator.TesselateShape(itemRope, shape, out var meshData);
			updateMesh = new MeshData(initialiseArrays: false);
			updateMesh.CustomFloats = new CustomMeshDataPartFloat(202000)
			{
				Instanced = true,
				InterleaveOffsets = new int[5] { 0, 16, 32, 48, 64 },
				InterleaveSizes = new int[5] { 4, 4, 4, 4, 4 },
				InterleaveStride = 80,
				StaticDraw = false
			};
			updateMesh.CustomFloats.SetAllocationSize(202000);
			meshData.CustomFloats = updateMesh.CustomFloats;
			ropeMeshRef = capi.Render.UploadMesh(meshData);
			updateMesh.CustomFloats.Count = 0;
		}
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		base.StartServerSide(api);
		api.Event.RegisterGameTickListener(tickPhysics, 30);
		api.Event.MapRegionLoaded += Event_MapRegionLoaded;
		api.Event.MapRegionUnloaded += Event_MapRegionUnloaded;
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, onNowRunGame);
		api.Event.PlayerJoin += Event_PlayerJoin;
		clothSystemChannel = api.Network.GetChannel("clothphysics");
		api.ChatCommands.GetOrCreate("debug").BeginSubCommand("clothtest").WithDescription("Commands to test the cloth system")
			.BeginSubCommand("cloth")
			.WithDescription("cloth")
			.HandleWith(onClothTestCloth)
			.EndSubCommand()
			.BeginSubCommand("rope")
			.WithDescription("rope")
			.HandleWith(onClothTestRope)
			.EndSubCommand()
			.BeginSubCommand("clear")
			.WithDescription("clears")
			.HandleWith(onClothTestClearServer)
			.EndSubCommand()
			.BeginSubCommand("deleteloaded")
			.WithDescription("deleteloaded")
			.HandleWith(onClothTestDeleteloaded)
			.EndSubCommand()
			.EndSubCommand();
	}

	private void onNowRunGame()
	{
		foreach (ClothSystem value in clothSystems.Values)
		{
			value.updateActiveState(EnumActiveStateChange.Default);
		}
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
		if (clothSystems.Values.Count > 0)
		{
			clothSystemChannel.BroadcastPacket(new ClothSystemPacket
			{
				ClothSystems = clothSystems.Values.ToArray()
			});
		}
	}

	private void Event_GameWorldSave()
	{
		byte[] data = sapi.WorldManager.SaveGame.GetData("nextClothId");
		if (data != null)
		{
			nextClothId = SerializerUtil.Deserialize<int>(data);
		}
	}

	private void Event_SaveGameLoaded()
	{
		sapi.WorldManager.SaveGame.StoreData("nextClothId", SerializerUtil.Serialize(nextClothId));
	}

	private void Event_MapRegionUnloaded(Vec2i mapCoord, IMapRegion region)
	{
		List<ClothSystem> systems = new List<ClothSystem>();
		int regionSize = sapi.WorldManager.RegionSize;
		foreach (ClothSystem cs in clothSystems.Values)
		{
			BlockPos asBlockPos = cs.FirstPoint.Pos.AsBlockPos;
			int regx = asBlockPos.X / regionSize;
			int regZ = asBlockPos.Z / regionSize;
			if (regx == mapCoord.X && regZ == mapCoord.Y)
			{
				systems.Add(cs);
			}
		}
		region.SetModdata("clothSystems", SerializerUtil.Serialize(systems));
		if (systems.Count == 0)
		{
			return;
		}
		int[] clothIds = new int[systems.Count];
		for (int i = 0; i < systems.Count; i++)
		{
			clothSystems.Remove(systems[i].ClothId);
			clothIds[i] = systems[i].ClothId;
		}
		foreach (ClothSystem value in clothSystems.Values)
		{
			value.updateActiveState(EnumActiveStateChange.RegionNowUnloaded);
		}
		if (!sapi.Server.IsShuttingDown)
		{
			clothSystemChannel.BroadcastPacket(new UnregisterClothSystemPacket
			{
				ClothIds = clothIds
			});
		}
	}

	private void Event_MapRegionLoaded(Vec2i mapCoord, IMapRegion region)
	{
		byte[] data = region.GetModdata("clothSystems");
		if (data != null && data.Length != 0)
		{
			List<ClothSystem> rsystems = SerializerUtil.Deserialize<List<ClothSystem>>(data);
			if (sapi.Server.CurrentRunPhase < EnumServerRunPhase.RunGame)
			{
				foreach (ClothSystem system in rsystems)
				{
					system.Active = false;
					system.Init(api, this);
					clothSystems[system.ClothId] = system;
				}
				return;
			}
			foreach (ClothSystem value in clothSystems.Values)
			{
				value.updateActiveState(EnumActiveStateChange.RegionNowLoaded);
			}
			foreach (ClothSystem system2 in rsystems)
			{
				system2.Init(api, this);
				system2.restoreReferences();
				clothSystems[system2.ClothId] = system2;
			}
			if (rsystems.Count > 0)
			{
				clothSystemChannel.BroadcastPacket(new ClothSystemPacket
				{
					ClothSystems = rsystems.ToArray()
				});
			}
		}
		else
		{
			if (sapi.Server.CurrentRunPhase < EnumServerRunPhase.RunGame)
			{
				return;
			}
			foreach (ClothSystem value2 in clothSystems.Values)
			{
				value2.updateActiveState(EnumActiveStateChange.RegionNowLoaded);
			}
		}
	}

	private TextCommandResult onClothTestClearServer(TextCommandCallingArgs args)
	{
		int cnt = clothSystems.Count;
		int[] clothids = clothSystems.Select((KeyValuePair<int, ClothSystem> s) => s.Value.ClothId).ToArray();
		if (clothids.Length != 0)
		{
			clothSystemChannel.BroadcastPacket(new UnregisterClothSystemPacket
			{
				ClothIds = clothids
			});
		}
		clothSystems.Clear();
		nextClothId = 1;
		return TextCommandResult.Success(cnt + " cloth sims removed");
	}

	private TextCommandResult onClothTestDeleteloaded(TextCommandCallingArgs args)
	{
		int cnt = 0;
		foreach (KeyValuePair<long, IMapRegion> allLoadedMapRegion in sapi.WorldManager.AllLoadedMapRegions)
		{
			allLoadedMapRegion.Value.RemoveModdata("clothSystems");
			cnt++;
		}
		clothSystems.Clear();
		nextClothId = 1;
		return TextCommandResult.Success($"Ok, deleted in {cnt} regions");
	}

	public void RegisterCloth(ClothSystem sys)
	{
		if (api.Side != EnumAppSide.Client)
		{
			sys.ClothId = nextClothId++;
			clothSystems[sys.ClothId] = sys;
			sys.updateActiveState(EnumActiveStateChange.Default);
			clothSystemChannel.BroadcastPacket(new ClothSystemPacket
			{
				ClothSystems = new ClothSystem[1] { sys }
			});
		}
	}

	public void UnregisterCloth(int clothId)
	{
		if (sapi != null)
		{
			clothSystemChannel.BroadcastPacket(new UnregisterClothSystemPacket
			{
				ClothIds = new int[1] { clothId }
			});
		}
		clothSystems.Remove(clothId);
	}

	private TextCommandResult onClothTestClear(TextCommandCallingArgs textCommandCallingArgs)
	{
		int cnt = clothSystems.Count;
		clothSystems.Clear();
		nextClothId = 1;
		return TextCommandResult.Success(cnt + " cloth sims removed");
	}

	private TextCommandResult onClothTestCloth(TextCommandCallingArgs args)
	{
		float xsize = 0.5f + (float)api.World.Rand.NextDouble() * 3f;
		float ysize = 0.5f + (float)api.World.Rand.NextDouble() * 3f;
		float zsize = 0.5f + (float)api.World.Rand.NextDouble() * 3f;
		Vec3d pos = args.Caller.Entity.Pos.AheadCopy(2.0).XYZ.Add(0.0, 1.0, 0.0);
		ClothSystem sys = ClothSystem.CreateCloth(api, this, pos, pos.AddCopy(xsize, ysize, zsize));
		RegisterCloth(sys);
		sys.FirstPoint.PinTo(args.Caller.Entity, new Vec3f(0f, 0.5f, 0f));
		return TextCommandResult.Success();
	}

	private TextCommandResult onClothTestRope(TextCommandCallingArgs args)
	{
		float xsize = 0.5f + (float)api.World.Rand.NextDouble() * 3f;
		float zsize = 0.5f + (float)api.World.Rand.NextDouble() * 3f;
		xsize = 5f;
		Vec3d rpos = args.Caller.Entity.Pos.AheadCopy(2.0).XYZ.Add(0.0, 1.0, 0.0);
		ClothSystem sys = ClothSystem.CreateRope(api, this, rpos, rpos.AddCopy(xsize, zsize, xsize), null);
		sys.FirstPoint.PinTo(args.Caller.Entity, new Vec3f(0f, 0.5f, 0f));
		RegisterCloth(sys);
		return TextCommandResult.Success();
	}
}
