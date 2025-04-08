using System;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class CloudRenderer : CloudRendererBase, IRenderer, IDisposable
{
	private CloudTilesState committedState = new CloudTilesState();

	private CloudTilesState mainThreadState = new CloudTilesState();

	private CloudTilesState offThreadState = new CloudTilesState();

	private CloudTile[] Tiles;

	private CloudTile[] tempTiles;

	private bool newStateRready;

	private object cloudStateLock = new object();

	internal float blendedCloudDensity;

	internal float blendedGlobalCloudBrightness;

	public int QuantityCloudTiles = 25;

	private MeshRef cloudTilesMeshRef;

	private long windChangeTimer;

	private float cloudSpeedX;

	private float cloudSpeedZ;

	private float targetCloudSpeedX;

	private float targetCloudSpeedZ;

	private Random rand;

	private bool renderClouds;

	private WeatherSystemClient weatherSys;

	private Thread cloudTileUpdThread;

	private bool isShuttingDown;

	private ICoreClientAPI capi;

	private IShaderProgram prog;

	private Matrixf mvMat = new Matrixf();

	private int cloudTileBlendSpeed = 32;

	private MeshData updateMesh = new MeshData
	{
		CustomShorts = new CustomMeshDataPartShort()
	};

	private WeatherDataReaderPreLoad wreaderpreload;

	private bool isFirstTick = true;

	private bool requireTileRebuild;

	public bool instantTileBlend;

	private int accum = 20;

	public double RenderOrder => 0.35;

	public int RenderRange => 9999;

	public CloudRenderer(ICoreClientAPI capi, WeatherSystemClient weatherSys)
	{
		this.capi = capi;
		this.weatherSys = weatherSys;
		wreaderpreload = weatherSys.getWeatherDataReaderPreLoad();
		rand = new Random(capi.World.Seed);
		capi.Event.RegisterRenderer(this, EnumRenderStage.OIT, "clouds");
		capi.Event.ReloadShader += LoadShader;
		LoadShader();
		double time = capi.World.Calendar.TotalHours * 60.0;
		windOffsetX += 2.0 * time;
		windOffsetZ += 0.10000000149011612 * time;
		mainThreadState.WindTileOffsetX += (int)(windOffsetX / (double)base.CloudTileSize);
		windOffsetX %= base.CloudTileSize;
		mainThreadState.WindTileOffsetZ += (int)(windOffsetZ / (double)base.CloudTileSize);
		windOffsetZ %= base.CloudTileSize;
		offThreadState.Set(mainThreadState);
		committedState.Set(mainThreadState);
		InitCloudTiles(8 * capi.World.Player.WorldData.DesiredViewDistance);
		LoadCloudModel();
		capi.Settings.AddWatcher<int>("viewDistance", OnViewDistanceChanged);
		capi.Settings.AddWatcher("renderClouds", delegate(bool val)
		{
			renderClouds = val;
		});
		renderClouds = capi.Settings.Bool["renderClouds"];
		InitCustomDataBuffers(updateMesh);
		capi.Event.LeaveWorld += delegate
		{
			isShuttingDown = true;
		};
		cloudTileUpdThread = new Thread((ThreadStart)delegate
		{
			while (!isShuttingDown)
			{
				if (!newStateRready)
				{
					int num = (int)windOffsetX / base.CloudTileSize;
					int num2 = (int)windOffsetZ / base.CloudTileSize;
					int x = offThreadState.CenterTilePos.X;
					int z = offThreadState.CenterTilePos.Z;
					offThreadState.Set(mainThreadState);
					offThreadState.WindTileOffsetX += num;
					offThreadState.WindTileOffsetZ += num2;
					int num3 = num + x - offThreadState.CenterTilePos.X;
					int num4 = num2 + z - offThreadState.CenterTilePos.Z;
					if (num3 != 0 || num4 != 0)
					{
						MoveCloudTilesOffThread(num3, num4);
					}
					UpdateCloudTilesOffThread(instantTileBlend ? 32767 : cloudTileBlendSpeed);
					instantTileBlend = false;
					newStateRready = true;
				}
				Thread.Sleep(40);
			}
		});
		cloudTileUpdThread.IsBackground = true;
	}

	public bool LoadShader()
	{
		prog = capi.Shader.NewShaderProgram();
		prog.VertexShader = capi.Shader.NewShader(EnumShaderType.VertexShader);
		prog.FragmentShader = capi.Shader.NewShader(EnumShaderType.FragmentShader);
		capi.Shader.RegisterFileShaderProgram("clouds", prog);
		return prog.Compile();
	}

	private void OnViewDistanceChanged(int newValue)
	{
		requireTileRebuild = true;
	}

	public void CloudTick(float deltaTime)
	{
		blendedCloudDensity = capi.Ambient.BlendedCloudDensity;
		blendedGlobalCloudBrightness = capi.Ambient.BlendedCloudBrightness;
		if (isFirstTick)
		{
			weatherSys.ProcessWeatherUpdates();
			UpdateCloudTilesOffThread(32767);
			cloudTileUpdThread.Start();
			isFirstTick = false;
		}
		deltaTime = Math.Min(deltaTime, 1f);
		deltaTime *= capi.World.Calendar.SpeedOfTime / 60f;
		if (deltaTime > 0f)
		{
			if (windChangeTimer - capi.ElapsedMilliseconds < 0)
			{
				windChangeTimer = capi.ElapsedMilliseconds + rand.Next(20000, 120000);
				targetCloudSpeedX = (float)rand.NextDouble() * 5f;
				targetCloudSpeedZ = (float)rand.NextDouble() * 0.5f;
			}
			float windspeedx = 3f * (float)weatherSys.WeatherDataAtPlayer.GetWindSpeed(capi.World.Player.Entity.Pos.Y);
			cloudSpeedX += (targetCloudSpeedX + windspeedx - cloudSpeedX) * deltaTime;
			cloudSpeedZ += (targetCloudSpeedZ - cloudSpeedZ) * deltaTime;
		}
		lock (cloudStateLock)
		{
			if (deltaTime > 0f)
			{
				windOffsetX += cloudSpeedX * deltaTime;
				windOffsetZ += cloudSpeedZ * deltaTime;
			}
			mainThreadState.CenterTilePos.X = (int)capi.World.Player.Entity.Pos.X / base.CloudTileSize;
			mainThreadState.CenterTilePos.Z = (int)capi.World.Player.Entity.Pos.Z / base.CloudTileSize;
		}
		if (newStateRready)
		{
			int dx = offThreadState.WindTileOffsetX - committedState.WindTileOffsetX;
			int dz = offThreadState.WindTileOffsetZ - committedState.WindTileOffsetZ;
			committedState.Set(offThreadState);
			mainThreadState.WindTileOffsetX = committedState.WindTileOffsetX;
			mainThreadState.WindTileOffsetZ = committedState.WindTileOffsetZ;
			windOffsetX -= dx * base.CloudTileSize;
			windOffsetZ -= dz * base.CloudTileSize;
			UpdateBufferContents(updateMesh);
			capi.Render.UpdateMesh(cloudTilesMeshRef, updateMesh);
			weatherSys.ProcessWeatherUpdates();
			if (requireTileRebuild)
			{
				InitCloudTiles(8 * capi.World.Player.WorldData.DesiredViewDistance);
				UpdateCloudTiles();
				LoadCloudModel();
				InitCustomDataBuffers(updateMesh);
				requireTileRebuild = false;
				instantTileBlend = true;
			}
			newStateRready = false;
		}
		capi.World.FrameProfiler.Mark("gt-clouds");
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (renderClouds)
		{
			if (!capi.IsGamePaused)
			{
				CloudTick(deltaTime);
			}
			if (capi.Render.FrameWidth != 0)
			{
				capi.Render.ShaderUniforms.PerceptionEffectIntensity *= 20f;
				prog.Use();
				capi.Render.ShaderUniforms.PerceptionEffectIntensity /= 20f;
				prog.Uniform("sunPosition", capi.World.Calendar.SunPositionNormalized);
				double plrPosX = capi.World.Player.Entity.Pos.X;
				double plrPosZ = capi.World.Player.Entity.Pos.Z;
				double offsetX = (double)(committedState.CenterTilePos.X * base.CloudTileSize) - plrPosX + windOffsetX;
				double offsetZ = (double)(committedState.CenterTilePos.Z * base.CloudTileSize) - plrPosZ + windOffsetZ;
				prog.Uniform("sunColor", capi.World.Calendar.SunColor);
				prog.Uniform("dayLight", Math.Max(0f, capi.World.Calendar.DayLightStrength - capi.World.Calendar.MoonLightStrength * 0.95f));
				prog.Uniform("windOffset", new Vec3f((float)offsetX, 0f, (float)offsetZ));
				prog.Uniform("alpha", GameMath.Clamp(1f - 1.5f * Math.Max(0f, capi.Render.ShaderUniforms.GlitchStrength - 0.1f), 0f, 1f));
				prog.Uniform("rgbaFogIn", capi.Ambient.BlendedFogColor);
				prog.Uniform("fogMinIn", capi.Ambient.BlendedFogMin);
				prog.Uniform("fogDensityIn", capi.Ambient.BlendedFogDensity);
				prog.Uniform("playerPos", capi.Render.ShaderUniforms.PlayerPos);
				prog.Uniform("tileOffset", new Vec2f((committedState.CenterTilePos.X - committedState.TileOffsetX) * base.CloudTileSize, (committedState.CenterTilePos.Z - committedState.TileOffsetZ) * base.CloudTileSize));
				prog.Uniform("cloudTileSize", base.CloudTileSize);
				prog.Uniform("cloudsLength", (float)base.CloudTileSize * (float)CloudTileLength);
				prog.Uniform("globalCloudBrightness", blendedGlobalCloudBrightness);
				float yTranslate = (float)((double)(weatherSys.CloudLevelRel * (float)capi.World.BlockAccessor.MapSizeY) + 0.5 - capi.World.Player.Entity.CameraPos.Y);
				prog.Uniform("cloudYTranslate", yTranslate);
				prog.Uniform("cloudCounter", (float)(capi.World.Calendar.TotalHours * 20.0 % 578.0));
				prog.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
				prog.Uniform("flatFogDensity", capi.Ambient.BlendedFlatFogDensity);
				prog.Uniform("flatFogStart", capi.Ambient.BlendedFlatFogYPosForShader - (float)capi.World.Player.Entity.CameraPos.Y);
				mvMat.Set(capi.Render.CameraMatrixOriginf).Translate(offsetX, yTranslate, offsetZ);
				prog.UniformMatrix("modelViewMatrix", mvMat.Values);
				capi.Render.RenderMeshInstanced(cloudTilesMeshRef, QuantityCloudTiles);
				prog.Stop();
			}
		}
	}

	public void InitCloudTiles(int viewDistance)
	{
		CloudTileLength = GameMath.Clamp(viewDistance / base.CloudTileSize, 20, 200);
		QuantityCloudTiles = CloudTileLength * CloudTileLength;
		Tiles = new CloudTile[QuantityCloudTiles];
		tempTiles = new CloudTile[QuantityCloudTiles];
		int seed = rand.Next();
		for (int x = 0; x < CloudTileLength; x++)
		{
			for (int z = 0; z < CloudTileLength; z++)
			{
				Tiles[x * CloudTileLength + z] = new CloudTile
				{
					GridXOffset = (short)(x - CloudTileLength / 2),
					GridZOffset = (short)(z - CloudTileLength / 2),
					brightnessRand = new LCGRandom(seed)
				};
			}
		}
	}

	public void UpdateCloudTilesOffThread(int changeSpeed)
	{
		bool reloadRainNoiseValues = false;
		accum++;
		if (accum > 10)
		{
			accum = 0;
			reloadRainNoiseValues = true;
		}
		int cnt = CloudTileLength * CloudTileLength;
		int prevTopLeftRegX = -9999;
		int prevTopLeftRegZ = -9999;
		Vec3i tileOffset = new Vec3i(offThreadState.TileOffsetX - offThreadState.WindTileOffsetX, 0, offThreadState.TileOffsetZ - offThreadState.WindTileOffsetZ);
		Vec3i tileCenterPos = offThreadState.CenterTilePos;
		for (int i = 0; i < cnt; i++)
		{
			CloudTile cloudTile = Tiles[i];
			int tileXPos = tileCenterPos.X + cloudTile.GridXOffset;
			int tileZPos = tileCenterPos.Z + cloudTile.GridZOffset;
			cloudTile.brightnessRand.InitPositionSeed(tileXPos - offThreadState.WindTileOffsetX, tileZPos - offThreadState.WindTileOffsetZ);
			Vec3d cloudTilePos = new Vec3d(tileXPos * base.CloudTileSize, capi.World.SeaLevel, tileZPos * base.CloudTileSize);
			int regSize = capi.World.BlockAccessor.RegionSize;
			int topLeftRegX = (int)Math.Round(cloudTilePos.X / (double)regSize) - 1;
			int topLeftRegZ = (int)Math.Round(cloudTilePos.Z / (double)regSize) - 1;
			if (topLeftRegX != prevTopLeftRegX || topLeftRegZ != prevTopLeftRegZ)
			{
				prevTopLeftRegX = topLeftRegX;
				prevTopLeftRegZ = topLeftRegZ;
				wreaderpreload.LoadAdjacentSims(cloudTilePos);
				wreaderpreload.EnsureCloudTileCacheIsFresh(tileOffset);
			}
			if (reloadRainNoiseValues || !cloudTile.rainValuesSet)
			{
				wreaderpreload.LoadLerp(cloudTilePos);
				cloudTile.lerpRainCloudOverlay = wreaderpreload.lerpRainCloudOverlay;
				cloudTile.lerpRainOverlay = wreaderpreload.lerpRainOverlay;
				cloudTile.rainValuesSet = true;
			}
			else
			{
				wreaderpreload.LoadLerp(cloudTilePos, useArgValues: true, cloudTile.lerpRainCloudOverlay, cloudTile.lerpRainOverlay);
			}
			int cloudTileX = (int)cloudTilePos.X / base.CloudTileSize;
			int cloudTileZ = (int)cloudTilePos.Z / base.CloudTileSize;
			double density = GameMath.Clamp(wreaderpreload.GetBlendedCloudThicknessAt(cloudTileX, cloudTileZ), 0.0, 1.0);
			double bright = wreaderpreload.GetBlendedCloudBrightness(1f) * (double)(0.85f + cloudTile.brightnessRand.NextFloat() * 0.15f);
			cloudTile.TargetBrightnes = (short)(GameMath.Clamp(bright, 0.0, 1.0) * 32767.0);
			cloudTile.TargetThickness = (short)GameMath.Clamp(density * 32767.0, 0.0, 32767.0);
			cloudTile.TargetThinCloudMode = (short)GameMath.Clamp(wreaderpreload.GetBlendedThinCloudModeness() * 32767.0, 0.0, 32767.0);
			cloudTile.TargetCloudOpaquenes = (short)GameMath.Clamp(wreaderpreload.GetBlendedCloudOpaqueness() * 32767.0, 0.0, 32767.0);
			cloudTile.TargetUndulatingCloudMode = (short)GameMath.Clamp(wreaderpreload.GetBlendedUndulatingCloudModeness() * 32767.0, 0.0, 32767.0);
			cloudTile.Brightness = LerpTileValue(cloudTile.TargetBrightnes, cloudTile.Brightness, changeSpeed);
			cloudTile.SelfThickness = LerpTileValue(cloudTile.TargetThickness, cloudTile.SelfThickness, changeSpeed);
			cloudTile.ThinCloudMode = LerpTileValue(cloudTile.TargetThinCloudMode, cloudTile.ThinCloudMode, changeSpeed);
			cloudTile.CloudOpaqueness = LerpTileValue(cloudTile.TargetCloudOpaquenes, cloudTile.CloudOpaqueness, changeSpeed);
			cloudTile.UndulatingCloudMode = LerpTileValue(cloudTile.TargetUndulatingCloudMode, cloudTile.UndulatingCloudMode, changeSpeed);
			if (i > 0)
			{
				Tiles[i - 1].NorthTileThickness = cloudTile.SelfThickness;
			}
			if (i < Tiles.Length - 1)
			{
				Tiles[i + 1].SouthTileThickness = cloudTile.SelfThickness;
			}
			if (i < CloudTileLength - 1)
			{
				Tiles[i + CloudTileLength].EastTileThickness = cloudTile.SelfThickness;
			}
			if (i > CloudTileLength - 1)
			{
				Tiles[i - CloudTileLength].WestTileThickness = cloudTile.SelfThickness;
			}
		}
	}

	private short LerpTileValue(int target, int current, int changeSpeed)
	{
		float changeVal = GameMath.Clamp(target - current, -changeSpeed, changeSpeed);
		return (short)GameMath.Clamp((float)current + changeVal, 0f, 32767f);
	}

	public void MoveCloudTilesOffThread(int dx, int dz)
	{
		for (int x = 0; x < CloudTileLength; x++)
		{
			for (int z = 0; z < CloudTileLength; z++)
			{
				int newX = GameMath.Mod(x + dx, CloudTileLength);
				int newZ = GameMath.Mod(z + dz, CloudTileLength);
				CloudTile tile = Tiles[x * CloudTileLength + z];
				tile.GridXOffset = (short)(newX - CloudTileLength / 2);
				tile.GridZOffset = (short)(newZ - CloudTileLength / 2);
				tempTiles[newX * CloudTileLength + newZ] = tile;
			}
		}
		CloudTile[] flip = Tiles;
		Tiles = tempTiles;
		tempTiles = flip;
	}

	public void LoadCloudModel()
	{
		MeshData modeldata = new MeshData(24, 36, withNormals: false, withUv: false);
		modeldata.Flags = new int[24];
		float[] CloudSideShadings = new float[4] { 1f, 0.9f, 0.9f, 0.7f };
		MeshData tile = CloudMeshUtil.GetCubeModelDataForClouds(base.CloudTileSize / 2, base.CloudTileSize / 4, new Vec3f(0f, 0f, 0f));
		byte[] rgba = CubeMeshUtil.GetShadedCubeRGBA(-1, CloudSideShadings, smoothShadedSides: false);
		tile.SetRgba(rgba);
		tile.Flags = new int[24]
		{
			0, 0, 0, 0, 1, 1, 1, 1, 2, 2,
			2, 2, 3, 3, 3, 3, 4, 4, 4, 4,
			5, 5, 5, 5
		};
		modeldata.AddMeshData(tile);
		InitCustomDataBuffers(modeldata);
		UpdateBufferContents(modeldata);
		cloudTilesMeshRef?.Dispose();
		cloudTilesMeshRef = capi.Render.UploadMesh(modeldata);
	}

	private void InitCustomDataBuffers(MeshData modeldata)
	{
		modeldata.CustomShorts = new CustomMeshDataPartShort
		{
			StaticDraw = false,
			Instanced = true,
			InterleaveSizes = new int[8] { 2, 4, 1, 1, 1, 1, 1, 1 },
			InterleaveOffsets = new int[8] { 0, 4, 12, 14, 16, 18, 20, 22 },
			InterleaveStride = 24,
			Conversion = DataConversion.NormalizedFloat,
			Values = new short[QuantityCloudTiles * 12],
			Count = QuantityCloudTiles * 12
		};
	}

	private void UpdateBufferContents(MeshData mesh)
	{
		int pos = 0;
		for (int i = 0; i < Tiles.Length; i++)
		{
			CloudTile tile = Tiles[i];
			mesh.CustomShorts.Values[pos++] = (short)(base.CloudTileSize * tile.GridXOffset);
			mesh.CustomShorts.Values[pos++] = (short)(base.CloudTileSize * tile.GridZOffset);
			mesh.CustomShorts.Values[pos++] = tile.NorthTileThickness;
			mesh.CustomShorts.Values[pos++] = tile.EastTileThickness;
			mesh.CustomShorts.Values[pos++] = tile.SouthTileThickness;
			mesh.CustomShorts.Values[pos++] = tile.WestTileThickness;
			mesh.CustomShorts.Values[pos++] = tile.SelfThickness;
			mesh.CustomShorts.Values[pos++] = tile.Brightness;
			mesh.CustomShorts.Values[pos++] = tile.ThinCloudMode;
			mesh.CustomShorts.Values[pos++] = tile.UndulatingCloudMode;
			mesh.CustomShorts.Values[pos++] = tile.CloudOpaqueness;
			mesh.CustomShorts.Values[pos++] = 0;
		}
	}

	public void Dispose()
	{
		capi.Render.DeleteMesh(cloudTilesMeshRef);
	}
}
