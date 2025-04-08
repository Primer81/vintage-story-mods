using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorNameTag : EntityBehavior, IRenderer, IDisposable
{
	protected LoadedTexture nameTagTexture;

	protected bool showNameTagOnlyWhenTargeted;

	protected NameTagRendererDelegate nameTagRenderHandler;

	private ICoreClientAPI capi;

	protected int renderRange = 999;

	private IPlayer player;

	public string DisplayName
	{
		get
		{
			if (capi != null && TriggeredNameReveal && !IsNameRevealedFor(capi.World.Player.PlayerUID))
			{
				return UnrevealedDisplayName;
			}
			return entity.WatchedAttributes.GetTreeAttribute("nametag")?.GetString("name");
		}
	}

	public string UnrevealedDisplayName { get; set; }

	public bool ShowOnlyWhenTargeted
	{
		get
		{
			return entity.WatchedAttributes.GetTreeAttribute("nametag")?.GetBool("showtagonlywhentargeted") ?? false;
		}
		set
		{
			entity.WatchedAttributes.GetTreeAttribute("nametag")?.SetBool("showtagonlywhentargeted", value);
		}
	}

	public bool TriggeredNameReveal { get; set; }

	public int RenderRange
	{
		get
		{
			return entity.WatchedAttributes.GetTreeAttribute("nametag").GetInt("renderRange");
		}
		set
		{
			entity.WatchedAttributes.GetTreeAttribute("nametag")?.SetInt("renderRange", value);
		}
	}

	public double RenderOrder => 1.0;

	protected bool IsSelf => entity.EntityId == capi.World.Player.Entity.EntityId;

	public bool IsNameRevealedFor(string playeruid)
	{
		ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("nametag");
		if (treeAttribute == null)
		{
			return false;
		}
		return (treeAttribute.GetTreeAttribute("nameRevealedFor")?.HasAttribute(playeruid)).GetValueOrDefault();
	}

	public void SetNameRevealedFor(string playeruid)
	{
		ITreeAttribute ntree = entity.WatchedAttributes.GetTreeAttribute("nametag");
		if (ntree == null)
		{
			ntree = (ITreeAttribute)(entity.WatchedAttributes["nametag"] = new TreeAttribute());
		}
		ITreeAttribute tree = ntree?.GetTreeAttribute("nameRevealedFor");
		if (tree == null)
		{
			tree = (ITreeAttribute)(ntree["nameRevealedFor"] = new TreeAttribute());
		}
		tree.SetBool(playeruid, value: true);
		OnNameChanged();
	}

	public EntityBehaviorNameTag(Entity entity)
		: base(entity)
	{
		ITreeAttribute nametagTree = entity.WatchedAttributes.GetTreeAttribute("nametag");
		if (nametagTree == null)
		{
			entity.WatchedAttributes.SetAttribute("nametag", nametagTree = new TreeAttribute());
			nametagTree.SetString("name", "");
			nametagTree.SetInt("showtagonlywhentargeted", 0);
			nametagTree.SetInt("renderRange", 999);
			entity.WatchedAttributes.MarkPathDirty("nametag");
		}
	}

	public override void Initialize(EntityProperties entityType, JsonObject attributes)
	{
		base.Initialize(entityType, attributes);
		if ((DisplayName == null || DisplayName.Length == 0) && attributes["selectFromRandomName"].Exists)
		{
			string[] randomName = attributes["selectFromRandomName"].AsArray<string>();
			SetName(randomName[entity.World.Rand.Next(randomName.Length)]);
		}
		TriggeredNameReveal = attributes["triggeredNameReveal"].AsBool();
		RenderRange = attributes["renderRange"].AsInt(999);
		ShowOnlyWhenTargeted = attributes["showtagonlywhentargeted"].AsBool();
		UnrevealedDisplayName = attributes["unrevealedDisplayName"].AsString("Stranger");
		entity.WatchedAttributes.OnModified.Add(new TreeModifiedListener
		{
			path = "nametag",
			listener = OnNameChanged
		});
		OnNameChanged();
		capi = entity.World.Api as ICoreClientAPI;
		if (capi != null)
		{
			capi.Event.RegisterRenderer(this, EnumRenderStage.Ortho, "nametag");
		}
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (IsSelf && capi.Render.CameraType == EnumCameraMode.FirstPerson)
		{
			return;
		}
		if (nameTagRenderHandler == null || (entity is EntityPlayer && player == null))
		{
			player = (entity as EntityPlayer)?.Player;
			if (player != null || !(entity is EntityPlayer))
			{
				nameTagRenderHandler = capi.ModLoader.GetModSystem<EntityNameTagRendererRegistry>().GetNameTagRenderer(entity);
				OnNameChanged();
			}
		}
		if ((player != null && player.WorldData.CurrentGameMode == EnumGameMode.Spectator) || nameTagTexture == null)
		{
			return;
		}
		IPlayer obj = player;
		if (obj != null && (obj.Entity?.ServerControls.Sneak).GetValueOrDefault())
		{
			return;
		}
		IRenderAPI rapi = capi.Render;
		EntityPlayer entityPlayer = capi.World.Player.Entity;
		if (!(entity.Properties.Client.Renderer is EntityShapeRenderer esr))
		{
			return;
		}
		Vec3d pos = MatrixToolsd.Project(esr.getAboveHeadPosition(entityPlayer), rapi.PerspectiveProjectionMat, rapi.PerspectiveViewMat, rapi.FrameWidth, rapi.FrameHeight);
		if (!(pos.Z < 0.0))
		{
			float scale = 4f / Math.Max(1f, (float)pos.Z);
			float cappedScale = Math.Min(1f, scale);
			if (cappedScale > 0.75f)
			{
				cappedScale = 0.75f + (cappedScale - 0.75f) / 2f;
			}
			float offY = 0f;
			double dist = entityPlayer.Pos.SquareDistanceTo(entity.Pos);
			if (nameTagTexture != null && (!ShowOnlyWhenTargeted || capi.World.Player.CurrentEntitySelection?.Entity == entity) && (double)(renderRange * renderRange) > dist)
			{
				float posx = (float)pos.X - cappedScale * (float)nameTagTexture.Width / 2f;
				float posy = (float)rapi.FrameHeight - (float)pos.Y - (float)nameTagTexture.Height * Math.Max(0f, cappedScale);
				rapi.Render2DTexture(nameTagTexture.TextureId, posx, posy, cappedScale * (float)nameTagTexture.Width, cappedScale * (float)nameTagTexture.Height, 20f);
				offY += (float)nameTagTexture.Height;
			}
		}
	}

	public void Dispose()
	{
		if (nameTagTexture != null)
		{
			nameTagTexture.Dispose();
			nameTagTexture = null;
		}
		if (capi != null)
		{
			capi.Event.UnregisterRenderer(this, EnumRenderStage.Ortho);
		}
	}

	public override string GetName(ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		return DisplayName;
	}

	public void SetName(string playername)
	{
		ITreeAttribute nametagTree = entity.WatchedAttributes.GetTreeAttribute("nametag");
		if (nametagTree == null)
		{
			entity.WatchedAttributes.SetAttribute("nametag", nametagTree = new TreeAttribute());
		}
		nametagTree.SetString("name", playername);
		entity.WatchedAttributes.MarkPathDirty("nametag");
	}

	protected void OnNameChanged()
	{
		if (nameTagRenderHandler != null)
		{
			if (nameTagTexture != null)
			{
				nameTagTexture.Dispose();
				nameTagTexture = null;
			}
			nameTagTexture = nameTagRenderHandler(capi, entity);
		}
	}

	public override void OnEntityDeath(DamageSource damageSourceForDeath)
	{
		Dispose();
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		Dispose();
	}

	public override string PropertyName()
	{
		return "displayname";
	}
}
