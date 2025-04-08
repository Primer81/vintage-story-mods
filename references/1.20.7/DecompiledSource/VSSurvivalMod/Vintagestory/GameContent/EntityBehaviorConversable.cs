using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorConversable : EntityBehavior
{
	public static int BeginConvoPacketId = 1213;

	public static int SelectAnswerPacketId = 1214;

	public static int CloseConvoPacketId = 1215;

	public Dictionary<string, DialogueController> ControllerByPlayer = new Dictionary<string, DialogueController>();

	public GuiDialogueDialog Dialog;

	private EntityTalkUtil talkUtilInst;

	private IWorldAccessor world;

	private EntityAgent eagent;

	private DialogueConfig dialogue;

	private AssetLocation dialogueLoc;

	private bool approachPlayer;

	public Action<DialogueController> OnControllerCreated;

	private EntityBehaviorActivityDriven bhActivityDriven;

	private AiTaskGotoEntity gototask;

	private float gotoaccum;

	public const float BeginTalkRangeSq = 9f;

	public const float ApproachRangeSq = 16f;

	public const float StopTalkRangeSq = 25f;

	public string[] remainStationaryAnimations = new string[9] { "sit-idle", "sit-write", "sit-tinker", "sitfloor", "sitedge", "sitchair", "sitchairtable", "eatsittable", "bowl-eatsittable" };

	public EntityTalkUtil TalkUtil
	{
		get
		{
			if (!(entity is ITalkUtil tu))
			{
				return talkUtilInst;
			}
			return tu.TalkUtil;
		}
	}

	public event CanConverseDelegate CanConverse;

	public DialogueController GetOrCreateController(EntityPlayer player)
	{
		if (player == null)
		{
			return null;
		}
		DialogueComponent[] components;
		if (ControllerByPlayer.TryGetValue(player.PlayerUID, out var controller))
		{
			components = dialogue.components;
			for (int i = 0; i < components.Length; i++)
			{
				components[i].SetReferences(controller, Dialog);
			}
			return controller;
		}
		dialogue = loadDialogue(dialogueLoc, player);
		if (dialogue == null)
		{
			return null;
		}
		DialogueController dialogueController2 = (ControllerByPlayer[player.PlayerUID] = new DialogueController(world.Api, player, entity as EntityAgent, dialogue));
		controller = dialogueController2;
		controller.DialogTriggers += Controller_DialogTriggers;
		OnControllerCreated?.Invoke(controller);
		components = dialogue.components;
		for (int i = 0; i < components.Length; i++)
		{
			components[i].SetReferences(controller, Dialog);
		}
		return controller;
	}

	private int Controller_DialogTriggers(EntityAgent triggeringEntity, string value, JsonObject data)
	{
		if (value == "closedialogue")
		{
			Dialog?.TryClose();
		}
		if (value == "playanimation")
		{
			entity.AnimManager.StartAnimation(data.AsObject<AnimationMetaData>());
		}
		if (value == "giveitemstack" && entity.World.Side == EnumAppSide.Server)
		{
			JsonItemStack jsonItemStack = data.AsObject<JsonItemStack>();
			jsonItemStack.Resolve(entity.World, "conversable giveitem trigger");
			ItemStack itemstack = jsonItemStack.ResolvedItemstack;
			if (!triggeringEntity.TryGiveItemStack(itemstack))
			{
				entity.World.SpawnItemEntity(itemstack, triggeringEntity.Pos.XYZ);
			}
		}
		if (value == "spawnentity" && entity.World.Side == EnumAppSide.Server)
		{
			DlgSpawnEntityConfig cfg = data.AsObject<DlgSpawnEntityConfig>();
			float weightsum = 0f;
			for (int j = 0; j < cfg.Codes.Length; j++)
			{
				weightsum += cfg.Codes[j].Weight;
			}
			double rnd = entity.World.Rand.NextDouble() * (double)weightsum;
			for (int i = 0; i < cfg.Codes.Length; i++)
			{
				if ((rnd -= (double)cfg.Codes[i].Weight) <= 0.0)
				{
					TrySpawnEntity((triggeringEntity as EntityPlayer)?.Player, cfg.Codes[i].Code, cfg.Range, cfg);
					break;
				}
			}
		}
		if (value == "takefrominventory" && entity.World.Side == EnumAppSide.Server)
		{
			JsonItemStack jstack = data.AsObject<JsonItemStack>();
			jstack.Resolve(entity.World, "conversable giveitem trigger");
			ItemStack wantStack = jstack.ResolvedItemstack;
			ItemSlot slot2 = DialogueComponent.FindDesiredItem(triggeringEntity, wantStack);
			if (slot2 != null)
			{
				slot2.TakeOut(jstack.Quantity);
				slot2.MarkDirty();
			}
		}
		if ((value == "repairheldtool" || value == "repairheldarmor") && entity.World.Side == EnumAppSide.Server)
		{
			ItemSlot slot = triggeringEntity.RightHandItemSlot;
			if (!slot.Empty)
			{
				ItemRepairConfig rpcfg = data.AsObject<ItemRepairConfig>();
				int d = slot.Itemstack.Collectible.GetRemainingDurability(slot.Itemstack);
				int max = slot.Itemstack.Collectible.GetMaxDurability(slot.Itemstack);
				if (((value == "repairheldtool") ? slot.Itemstack.Collectible.Tool.HasValue : (slot.Itemstack.Collectible.FirstCodePart() == "armor")) && d < max)
				{
					slot.Itemstack.Collectible.SetDurability(slot.Itemstack, Math.Min(max, d + rpcfg.Amount));
					slot.MarkDirty();
				}
			}
		}
		if (value == "attack")
		{
			EnumDamageType damagetype = (EnumDamageType)Enum.Parse(typeof(EnumDamageType), data["type"].AsString("BluntAttack"));
			triggeringEntity.ReceiveDamage(new DamageSource
			{
				Source = EnumDamageSource.Entity,
				SourceEntity = entity,
				Type = damagetype
			}, data["damage"].AsInt());
		}
		if (value == "revealname")
		{
			IPlayer plr = (triggeringEntity as EntityPlayer)?.Player;
			if (plr != null)
			{
				string arg = data["selector"].ToString();
				if (arg != null && arg.StartsWith("e["))
				{
					EntitiesArgParser test = new EntitiesArgParser("test", world.Api, isMandatoryArg: true);
					TextCommandCallingArgs textCommandCallingArgs = new TextCommandCallingArgs();
					textCommandCallingArgs.Caller = new Caller
					{
						Type = EnumCallerType.Console,
						CallerRole = "admin",
						CallerPrivileges = new string[1] { "*" },
						FromChatGroupId = GlobalConstants.ConsoleGroup,
						Pos = new Vec3d(0.5, 0.5, 0.5)
					};
					textCommandCallingArgs.RawArgs = new CmdArgs(arg);
					TextCommandCallingArgs packedArgs = textCommandCallingArgs;
					if (test.TryProcess(packedArgs) == EnumParseResult.Good)
					{
						Entity[] array = (Entity[])test.GetValue();
						for (int k = 0; k < array.Length; k++)
						{
							array[k].GetBehavior<EntityBehaviorNameTag>().SetNameRevealedFor(plr.PlayerUID);
						}
					}
					else
					{
						world.Logger.Warning("Conversable trigger: Unable to reveal name, invalid selector - " + arg);
					}
				}
				else
				{
					entity.GetBehavior<EntityBehaviorNameTag>().SetNameRevealedFor(plr.PlayerUID);
				}
			}
		}
		return -1;
	}

	private void TrySpawnEntity(IPlayer forplayer, string entityCode, float range, DlgSpawnEntityConfig cfg)
	{
		EntityProperties etype = entity.World.GetEntityType(AssetLocation.Create(entityCode, entity.Code.Domain));
		if (etype == null)
		{
			entity.World.Logger.Warning("Dialogue system, unable to spawn {0}, no such entity exists", entityCode);
			return;
		}
		EntityPos serverPos = entity.ServerPos;
		BlockPos minpos = serverPos.Copy().Add(0f - range, 0.0, 0f - range).AsBlockPos;
		BlockPos maxpos = serverPos.Copy().Add(range, 0.0, range).AsBlockPos;
		Vec3d spawnpos = findSpawnPos(forplayer, etype, minpos, maxpos, rainheightmap: false, 4);
		if (spawnpos == null)
		{
			spawnpos = findSpawnPos(forplayer, etype, minpos, maxpos, rainheightmap: true, 1);
		}
		if (spawnpos == null)
		{
			spawnpos = findSpawnPos(forplayer, etype, minpos, maxpos, rainheightmap: true, 1);
		}
		if (!(spawnpos != null))
		{
			return;
		}
		Entity spawnentity = entity.Api.ClassRegistry.CreateEntity(etype);
		spawnentity.ServerPos.SetPos(spawnpos);
		entity.World.SpawnEntity(spawnentity);
		if (cfg.GiveStacks == null)
		{
			return;
		}
		JsonItemStack[] giveStacks = cfg.GiveStacks;
		foreach (JsonItemStack stack in giveStacks)
		{
			if (stack.Resolve(entity.World, "spawn entity give stack"))
			{
				entity.Api.Event.EnqueueMainThreadTask(delegate
				{
					spawnentity.TryGiveItemStack(stack.ResolvedItemstack.Clone());
				}, "tradedlggivestack");
			}
		}
	}

	private Vec3d findSpawnPos(IPlayer forplayer, EntityProperties etype, BlockPos minpos, BlockPos maxpos, bool rainheightmap, int mindistance)
	{
		bool spawned = false;
		BlockPos tmp = new BlockPos();
		IBlockAccessor ba = entity.World.BlockAccessor;
		CollisionTester collisionTester = entity.World.CollisionTester;
		ICoreServerAPI sapi = entity.Api as ICoreServerAPI;
		Vec3d okspawnpos = null;
		Vec3d epos = entity.ServerPos.XYZ;
		ba.WalkBlocks(minpos, maxpos, delegate(Block block, int x, int y, int z)
		{
			if (!spawned && !(epos.DistanceTo(x, y, z) < (float)mindistance))
			{
				int num = z % 32;
				int num2 = x % 32;
				IMapChunk mapChunkAtBlockPos = ba.GetMapChunkAtBlockPos(tmp.Set(x, y, z));
				int num3 = (rainheightmap ? mapChunkAtBlockPos.RainHeightMap[num * 32 + num2] : (mapChunkAtBlockPos.WorldGenTerrainHeightMap[num * 32 + num2] + 1));
				Vec3d vec3d = new Vec3d((double)x + 0.5, (double)num3 + 0.1, (double)z + 0.5);
				Cuboidf entityBoxRel = etype.SpawnCollisionBox.OmniNotDownGrowBy(0.1f);
				if (!collisionTester.IsColliding(ba, entityBoxRel, vec3d, alsoCheckTouch: false) && sapi.World.Claims.TestAccess(forplayer, vec3d.AsBlockPos, EnumBlockAccessFlags.BuildOrBreak) == EnumWorldAccessResponse.Granted)
				{
					spawned = true;
					okspawnpos = vec3d;
				}
			}
		}, centerOrder: true);
		return okspawnpos;
	}

	public EntityBehaviorConversable(Entity entity)
		: base(entity)
	{
		world = entity.World;
		eagent = entity as EntityAgent;
		if (world.Side == EnumAppSide.Client && !(entity is ITalkUtil))
		{
			talkUtilInst = new EntityTalkUtil(world.Api as ICoreClientAPI, entity, isMultiSoundVoice: false);
		}
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		approachPlayer = attributes["approachPlayer"].AsBool(defaultValue: true);
		string dlgStr = attributes["dialogue"].AsString();
		dialogueLoc = AssetLocation.Create(dlgStr, entity.Code.Domain);
		if (entity.World.Side == EnumAppSide.Server)
		{
			JsonObject[] behaviorsAsJsonObj = properties.Client.BehaviorsAsJsonObj;
			foreach (JsonObject val in behaviorsAsJsonObj)
			{
				if (val["code"].ToString() == attributes["code"].ToString() && dlgStr != val["dialogue"].AsString())
				{
					throw new InvalidOperationException(string.Format("Conversable behavior for entity {0}: You must define the same dialogue path on the client as well as the server side, currently they are set to {1} and {2}.", entity.Code, dlgStr, val["dialogue"].AsString()));
				}
			}
		}
		if (dialogueLoc == null)
		{
			world.Logger.Error(string.Concat("entity behavior conversable for entity ", entity.Code, ", dialogue path not set. Won't load dialogue."));
		}
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		base.AfterInitialized(onFirstSpawn);
		bhActivityDriven = entity.GetBehavior<EntityBehaviorActivityDriven>();
	}

	public override void OnEntitySpawn()
	{
		setupTaskBlocker();
	}

	public override void OnEntityLoaded()
	{
		setupTaskBlocker();
	}

	private void setupTaskBlocker()
	{
		if (entity.Api.Side != EnumAppSide.Server)
		{
			return;
		}
		EntityBehaviorTaskAI bhtaskAi = entity.GetBehavior<EntityBehaviorTaskAI>();
		if (bhtaskAi != null)
		{
			bhtaskAi.TaskManager.OnShouldExecuteTask += (IAiTask task) => ControllerByPlayer.Count == 0 || task is AiTaskIdle || task is AiTaskSeekEntity || task is AiTaskGotoEntity;
		}
		EntityBehaviorActivityDriven bhActivityDriven = entity.GetBehavior<EntityBehaviorActivityDriven>();
		if (bhActivityDriven != null)
		{
			bhActivityDriven.OnShouldRunActivitySystem += () => ControllerByPlayer.Count == 0 && gototask == null;
		}
	}

	private DialogueConfig loadDialogue(AssetLocation loc, EntityPlayer forPlayer)
	{
		string charclass = forPlayer.WatchedAttributes.GetString("characterClass");
		string ownPersonality = entity.WatchedAttributes.GetString("personality");
		IAsset asset = world.AssetManager.TryGet(loc.Clone().WithPathAppendixOnce($"-{ownPersonality}-{charclass}.json"));
		if (asset == null)
		{
			asset = world.AssetManager.TryGet(loc.Clone().WithPathAppendixOnce("-" + ownPersonality + ".json"));
		}
		if (asset == null)
		{
			asset = world.AssetManager.TryGet(loc.WithPathAppendixOnce(".json"));
		}
		if (asset == null)
		{
			world.Logger.Error(string.Concat("Entitybehavior conversable for entity ", entity.Code, ", dialogue asset ", loc, " not found. Won't load dialogue."));
			return null;
		}
		try
		{
			DialogueConfig dialogueConfig = asset.ToObject<DialogueConfig>();
			dialogueConfig.Init();
			return dialogueConfig;
		}
		catch (Exception e)
		{
			world.Logger.Error("Entitybehavior conversable for entity {0}, dialogue asset is invalid:", entity.Code);
			world.Logger.Error(e);
			return null;
		}
	}

	public override string PropertyName()
	{
		return "conversable";
	}

	public override void OnGameTick(float deltaTime)
	{
		if (gototask != null)
		{
			gotoaccum += deltaTime;
			if (gototask.TargetReached())
			{
				IServerPlayer splr = (gototask.targetEntity as EntityPlayer)?.Player as IServerPlayer;
				ICoreServerAPI sapi2 = entity.World.Api as ICoreServerAPI;
				if (splr != null && splr.ConnectionState == EnumClientState.Playing)
				{
					AiTaskLookAtEntity tasklook = new AiTaskLookAtEntity(eagent);
					tasklook.manualExecute = true;
					tasklook.targetEntity = gototask.targetEntity;
					(entity.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager).ExecuteTask(tasklook, 1);
					sapi2.Network.SendEntityPacket(splr, entity.EntityId, BeginConvoPacketId);
					beginConvoServer(splr);
				}
				gototask = null;
			}
			AiTaskGotoEntity aiTaskGotoEntity = gototask;
			if ((aiTaskGotoEntity != null && aiTaskGotoEntity.Finished) || gotoaccum > 3f)
			{
				gototask = null;
			}
		}
		foreach (KeyValuePair<string, DialogueController> val in ControllerByPlayer)
		{
			IPlayer player = world.PlayerByUid(val.Key);
			EntityPlayer entityplayer = player.Entity;
			if (!entityplayer.Alive || entityplayer.Pos.SquareDistanceTo(entity.Pos) > 25f)
			{
				ControllerByPlayer.Remove(val.Key);
				if (world.Api is ICoreServerAPI sapi)
				{
					sapi.Network.SendEntityPacket(player as IServerPlayer, entity.EntityId, CloseConvoPacketId);
				}
				else
				{
					Dialog?.TryClose();
				}
				break;
			}
		}
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
	{
		if (mode != EnumInteractMode.Interact || !(byEntity is EntityPlayer))
		{
			handled = EnumHandling.PassThrough;
		}
		else
		{
			if (!entity.Alive)
			{
				return;
			}
			if (this.CanConverse != null)
			{
				Delegate[] invocationList = this.CanConverse.GetInvocationList();
				for (int i = 0; i < invocationList.Length; i++)
				{
					if (!((CanConverseDelegate)invocationList[i])(out var errorMsg))
					{
						((byEntity as EntityPlayer)?.Player as IServerPlayer)?.SendIngameError("cantconverse", Lang.Get(errorMsg));
						return;
					}
				}
			}
			GetOrCreateController(byEntity as EntityPlayer);
			handled = EnumHandling.PreventDefault;
			EntityPlayer entityplr = byEntity as EntityPlayer;
			world.PlayerByUid(entityplr.PlayerUID);
			if (world.Side == EnumAppSide.Client)
			{
				_ = (ICoreClientAPI)world.Api;
				if (entityplr.Pos.SquareDistanceTo(entity.Pos) <= 9f)
				{
					GuiDialogueDialog dialog = Dialog;
					if (dialog == null || !dialog.IsOpened())
					{
						beginConvoClient();
					}
				}
				TalkUtil.Talk(EnumTalkType.Meet);
			}
			if (world.Side != EnumAppSide.Server || gototask != null || !(byEntity.Pos.SquareDistanceTo(entity.Pos) <= 16f) || remainStationaryOnCall())
			{
				return;
			}
			AiTaskManager tmgr = entity.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager;
			if (tmgr != null)
			{
				tmgr.StopTask(typeof(AiTaskWander));
				gototask = new AiTaskGotoEntity(eagent, entityplr);
				gototask.allowedExtraDistance = 1f;
				if (gototask.TargetReached() || !approachPlayer)
				{
					gotoaccum = 0f;
					gototask = null;
					AiTaskLookAtEntity tasklook = new AiTaskLookAtEntity(eagent);
					tasklook.manualExecute = true;
					tasklook.targetEntity = entityplr;
					tmgr.ExecuteTask(tasklook, 1);
				}
				else
				{
					tmgr.ExecuteTask(gototask, 1);
					bhActivityDriven?.ActivitySystem.Pause();
				}
				entity.AnimManager.TryStartAnimation(new AnimationMetaData
				{
					Animation = "welcome",
					Code = "welcome",
					Weight = 10f,
					EaseOutSpeed = 10000f,
					EaseInSpeed = 10000f
				});
				entity.AnimManager.StopAnimation("idle");
			}
		}
	}

	private bool remainStationaryOnCall()
	{
		EntityAgent eagent = entity as EntityAgent;
		if (eagent == null || eagent.MountedOn == null || !(eagent.MountedOn is BlockEntityBed))
		{
			return eagent.AnimManager.IsAnimationActive(remainStationaryAnimations);
		}
		return true;
	}

	private bool beginConvoClient()
	{
		ICoreClientAPI capi = entity.World.Api as ICoreClientAPI;
		EntityPlayer entityplr = capi.World.Player.Entity;
		if (capi.Gui.OpenedGuis.FirstOrDefault((GuiDialog dlg) => dlg is GuiDialogueDialog && dlg.IsOpened()) == null)
		{
			Dialog = new GuiDialogueDialog(capi, eagent);
			Dialog.OnClosed += Dialog_OnClosed;
			DialogueController controller = GetOrCreateController(entityplr);
			if (controller == null)
			{
				capi.TriggerIngameError(this, "errord", Lang.Get("Error when loading dialogue. Check log files."));
				return false;
			}
			Dialog.InitAndOpen();
			controller.ContinueExecute();
			capi.Network.SendEntityPacket(entity.EntityId, BeginConvoPacketId);
			return true;
		}
		capi.TriggerIngameError(this, "onlyonedialog", Lang.Get("Can only trade with one trader at a time"));
		return false;
	}

	private void Dialog_OnClosed()
	{
		ControllerByPlayer.Clear();
		Dialog = null;
		(world.Api as ICoreClientAPI).Network.SendEntityPacket(entity.EntityId, CloseConvoPacketId);
	}

	public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled)
	{
		base.OnReceivedClientPacket(player, packetid, data, ref handled);
		if (packetid == BeginConvoPacketId)
		{
			beginConvoServer(player);
		}
		if (packetid == SelectAnswerPacketId)
		{
			int id = SerializerUtil.Deserialize<int>(data);
			GetOrCreateController(player.Entity).PlayerSelectAnswerById(id);
		}
		if (packetid == CloseConvoPacketId)
		{
			ControllerByPlayer.Remove(player.PlayerUID);
		}
	}

	private void beginConvoServer(IServerPlayer player)
	{
		GetOrCreateController(player.Entity).ContinueExecute();
	}

	public override void OnReceivedServerPacket(int packetid, byte[] data, ref EnumHandling handled)
	{
		base.OnReceivedServerPacket(packetid, data, ref handled);
		if (packetid == BeginConvoPacketId)
		{
			ICoreClientAPI capi = entity.World.Api as ICoreClientAPI;
			if (!(capi.World.Player.Entity.Pos.SquareDistanceTo(entity.Pos) > 25f))
			{
				GuiDialogueDialog dialog = Dialog;
				if ((dialog == null || !dialog.IsOpened()) && beginConvoClient())
				{
					goto IL_008b;
				}
			}
			capi.Network.SendEntityPacket(entity.EntityId, CloseConvoPacketId);
		}
		goto IL_008b;
		IL_008b:
		if (packetid == CloseConvoPacketId)
		{
			ControllerByPlayer.Clear();
			Dialog?.TryClose();
			Dialog = null;
		}
	}
}
