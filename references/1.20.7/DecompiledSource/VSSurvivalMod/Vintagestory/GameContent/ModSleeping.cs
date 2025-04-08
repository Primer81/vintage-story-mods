using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSleeping : ModSystem
{
	private ICoreAPI api;

	public bool AllSleeping;

	public float GameSpeedBoost;

	private ICoreServerAPI sapi;

	private IServerNetworkChannel serverChannel;

	private ICoreClientAPI capi;

	private IClientNetworkChannel clientChannel;

	private EyesOverlayRenderer renderer;

	private IShaderProgram eyeShaderProg;

	private float sleepLevel;

	public string VertexShaderCode = "\r\n#version 330 core\r\n#extension GL_ARB_explicit_attrib_location: enable\r\n\r\nlayout(location = 0) in vec3 vertex;\r\n\r\nout vec2 uv;\r\n\r\nvoid main(void)\r\n{\r\n    gl_Position = vec4(vertex.xy, 0, 1);\r\n    uv = (vertex.xy + 1.0) / 2.0;\r\n}\r\n";

	public string FragmentShaderCode = "\r\n#version 330 core\r\n\r\nin vec2 uv;\r\n\r\nout vec4 outColor;\r\n\r\nuniform float level;\r\n\r\nvoid main () {\r\n    vec2 uvOffseted = vec2(uv.x - 0.5, 2 * (1 + 2*level) * (uv.y - 0.5));\r\n\tfloat strength = 1 - smoothstep(1.1 - level, 0, length(uvOffseted));\r\n\toutColor = vec4(0, 0, 0, min(1, (4 * level - 0.8) + level * strength));\r\n}\r\n";

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		this.api = api;
		sapi = api;
		api.Event.RegisterGameTickListener(ServerSlowTick, 200);
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.RegisterGameTickListener(FastTick, 20);
		serverChannel = api.Network.RegisterChannel("sleeping").RegisterMessageType(typeof(NetworksMessageAllSleepMode));
	}

	private void Event_SaveGameLoaded()
	{
		api.World.Calendar?.RemoveTimeSpeedModifier("sleeping");
		GameSpeedBoost = 0f;
	}

	private void FastTick(float dt)
	{
		if (api.Side == EnumAppSide.Client)
		{
			renderer.Level = sleepLevel;
			bool sleeping = capi.World?.Player?.Entity?.MountedOn is BlockEntityBed;
			sleepLevel = GameMath.Clamp(sleepLevel + dt * ((sleeping && AllSleeping) ? 0.1f : (-0.35f)), 0f, 0.99f);
		}
		if (AllSleeping && api.World.Config.GetString("temporalStormSleeping", "0").ToInt() == 0 && api.ModLoader.GetModSystem<SystemTemporalStability>().StormStrength > 0f)
		{
			WakeAllPlayers();
		}
		else if (!(GameSpeedBoost <= 0f) || AllSleeping)
		{
			GameSpeedBoost = GameMath.Clamp(GameSpeedBoost + dt * (float)(AllSleeping ? 400 : (-2000)), 0f, 17000f);
			api.World.Calendar.SetTimeSpeedModifier("sleeping", (int)GameSpeedBoost);
		}
	}

	public void WakeAllPlayers()
	{
		GameSpeedBoost = 0f;
		api.World.Calendar.SetTimeSpeedModifier("sleeping", (int)GameSpeedBoost);
		if (api.Side == EnumAppSide.Client)
		{
			EntityBehaviorTiredness behavior = capi.World.Player.Entity.GetBehavior<EntityBehaviorTiredness>();
			if (behavior != null && behavior.IsSleeping)
			{
				capi.World.Player?.Entity.TryUnmount();
			}
			AllSleeping = false;
			return;
		}
		IPlayer[] allOnlinePlayers = sapi.World.AllOnlinePlayers;
		foreach (IPlayer player in allOnlinePlayers)
		{
			IServerPlayer splr = player as IServerPlayer;
			if (splr.ConnectionState == EnumClientState.Playing && splr.WorldData.CurrentGameMode != EnumGameMode.Spectator)
			{
				EntityBehaviorTiredness behavior2 = splr.Entity.GetBehavior<EntityBehaviorTiredness>();
				IMountableSeat mount = player.Entity?.MountedOn;
				if (behavior2 != null && behavior2.IsSleeping && mount != null)
				{
					player.Entity.TryUnmount();
				}
			}
		}
		AllSleeping = false;
	}

	private void ServerSlowTick(float dt)
	{
		bool nowAllSleeping = AreAllPlayersSleeping();
		if (nowAllSleeping != AllSleeping)
		{
			if (nowAllSleeping)
			{
				serverChannel.BroadcastPacket(new NetworksMessageAllSleepMode
				{
					On = true
				});
			}
			else
			{
				serverChannel.BroadcastPacket(new NetworksMessageAllSleepMode
				{
					On = false
				});
			}
			AllSleeping = nowAllSleeping;
		}
	}

	public bool AreAllPlayersSleeping()
	{
		int quantitySleeping = 0;
		int quantityAwake = 0;
		IPlayer[] allOnlinePlayers = sapi.World.AllOnlinePlayers;
		for (int i = 0; i < allOnlinePlayers.Length; i++)
		{
			IServerPlayer splr = allOnlinePlayers[i] as IServerPlayer;
			if (splr.ConnectionState == EnumClientState.Playing && splr.WorldData.CurrentGameMode != EnumGameMode.Spectator)
			{
				EntityBehaviorTiredness behavior = splr.Entity.GetBehavior<EntityBehaviorTiredness>();
				if (behavior != null && behavior.IsSleeping)
				{
					quantitySleeping++;
				}
				else
				{
					quantityAwake++;
				}
			}
		}
		if (quantitySleeping > 0)
		{
			return quantityAwake == 0;
		}
		return false;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		this.api = api;
		capi = api;
		api.Event.RegisterGameTickListener(FastTick, 20);
		api.Event.ReloadShader += LoadShader;
		LoadShader();
		renderer = new EyesOverlayRenderer(api, eyeShaderProg);
		api.Event.RegisterRenderer(renderer, EnumRenderStage.Ortho, "sleeping");
		api.Event.LeaveWorld += delegate
		{
			renderer?.Dispose();
		};
		clientChannel = api.Network.RegisterChannel("sleeping").RegisterMessageType(typeof(NetworksMessageAllSleepMode)).SetMessageHandler<NetworksMessageAllSleepMode>(OnAllSleepingStateChanged);
	}

	public bool LoadShader()
	{
		eyeShaderProg = capi.Shader.NewShaderProgram();
		eyeShaderProg.VertexShader = capi.Shader.NewShader(EnumShaderType.VertexShader);
		eyeShaderProg.FragmentShader = capi.Shader.NewShader(EnumShaderType.FragmentShader);
		eyeShaderProg.VertexShader.Code = VertexShaderCode;
		eyeShaderProg.FragmentShader.Code = FragmentShaderCode;
		capi.Shader.RegisterMemoryShaderProgram("sleepoverlay", eyeShaderProg);
		if (renderer != null)
		{
			renderer.eyeShaderProg = eyeShaderProg;
		}
		return eyeShaderProg.Compile();
	}

	private void OnAllSleepingStateChanged(NetworksMessageAllSleepMode networkMessage)
	{
		AllSleeping = networkMessage.On;
		if (!AllSleeping && GameSpeedBoost <= 0f && api.World.Calendar != null)
		{
			api.World.Calendar.SetTimeSpeedModifier("sleeping", 0f);
		}
	}
}
