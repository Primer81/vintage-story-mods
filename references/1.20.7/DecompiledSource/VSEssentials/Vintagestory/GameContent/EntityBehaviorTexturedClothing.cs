using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public abstract class EntityBehaviorTexturedClothing : EntityBehaviorContainer
{
	protected int skinTextureSubId;

	private ICoreClientAPI capi;

	private bool textureSpaceAllocated;

	public bool doReloadShapeAndSkin = true;

	protected TextureAtlasPosition skinTexPos
	{
		get
		{
			return (entity.Properties.Client.Renderer as EntityShapeRenderer).skinTexPos;
		}
		set
		{
			(entity.Properties.Client.Renderer as EntityShapeRenderer).skinTexPos = value;
		}
	}

	public Size2i AtlasSize => capi.EntityTextureAtlas.Size;

	public event Action<LoadedTexture, TextureAtlasPosition, int> OnReloadSkin;

	public EntityBehaviorTexturedClothing(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		capi = Api as ICoreClientAPI;
	}

	public override void OnTesselation(ref Shape entityShape, string shapePathForLogging, ref bool shapeIsCloned, ref string[] willDeleteElements)
	{
		base.OnTesselation(ref entityShape, shapePathForLogging, ref shapeIsCloned, ref willDeleteElements);
		reloadSkin();
	}

	public override ITexPositionSource GetTextureSource(ref EnumHandling handling)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Potentially attempting to insert a texture into the atlas outside of the main thread (if this allocation causes a new atlas to be created).");
		}
		handling = EnumHandling.PassThrough;
		if (!textureSpaceAllocated)
		{
			TextureAtlasPosition origTexPos = capi.EntityTextureAtlas.Positions[entity.Properties.Client.FirstTexture.Baked.TextureSubId];
			string skinBaseTextureKey = entity.Properties.Attributes?["skinBaseTextureKey"].AsString();
			if (skinBaseTextureKey != null)
			{
				origTexPos = capi.EntityTextureAtlas.Positions[entity.Properties.Client.Textures[skinBaseTextureKey].Baked.TextureSubId];
			}
			int width = (int)((origTexPos.x2 - origTexPos.x1) * (float)AtlasSize.Width);
			int height = (int)((origTexPos.y2 - origTexPos.y1) * (float)AtlasSize.Height);
			capi.EntityTextureAtlas.AllocateTextureSpace(width, height, out skinTextureSubId, out var skinTexPos);
			this.skinTexPos = skinTexPos;
			textureSpaceAllocated = true;
		}
		return null;
	}

	public void reloadSkin()
	{
		if (capi == null || !doReloadShapeAndSkin || skinTexPos == null)
		{
			return;
		}
		TextureAtlasPosition origTexPos = capi.EntityTextureAtlas.Positions[entity.Properties.Client.FirstTexture.Baked.TextureSubId];
		string skinBaseTextureKey = entity.Properties.Attributes?["skinBaseTextureKey"].AsString();
		if (skinBaseTextureKey != null)
		{
			origTexPos = capi.EntityTextureAtlas.Positions[entity.Properties.Client.Textures[skinBaseTextureKey].Baked.TextureSubId];
		}
		LoadedTexture entityAtlas = new LoadedTexture(null)
		{
			TextureId = origTexPos.atlasTextureId,
			Width = capi.EntityTextureAtlas.Size.Width,
			Height = capi.EntityTextureAtlas.Size.Height
		};
		capi.Render.GlToggleBlend(blend: false);
		capi.EntityTextureAtlas.RenderTextureIntoAtlas(skinTexPos.atlasTextureId, entityAtlas, (int)(origTexPos.x1 * (float)AtlasSize.Width), (int)(origTexPos.y1 * (float)AtlasSize.Height), (int)((origTexPos.x2 - origTexPos.x1) * (float)AtlasSize.Width), (int)((origTexPos.y2 - origTexPos.y1) * (float)AtlasSize.Height), skinTexPos.x1 * (float)capi.EntityTextureAtlas.Size.Width, skinTexPos.y1 * (float)capi.EntityTextureAtlas.Size.Height, -1f);
		capi.Render.GlToggleBlend(blend: true, EnumBlendMode.Overlay);
		this.OnReloadSkin?.Invoke(entityAtlas, skinTexPos, skinTextureSubId);
		int[] renderOrder = new int[12]
		{
			3, 4, 2, 11, 9, 1, 7, 6, 0, 5,
			10, 8
		};
		foreach (int slotid in renderOrder)
		{
			ItemStack stack = Inventory[slotid]?.Itemstack;
			if (stack != null && !hideClothing && stack.Item.FirstTexture != null)
			{
				int itemTextureSubId = stack.Item.FirstTexture.Baked.TextureSubId;
				TextureAtlasPosition itemTexPos = capi.ItemTextureAtlas.Positions[itemTextureSubId];
				LoadedTexture itemAtlas = new LoadedTexture(null)
				{
					TextureId = itemTexPos.atlasTextureId,
					Width = capi.ItemTextureAtlas.Size.Width,
					Height = capi.ItemTextureAtlas.Size.Height
				};
				capi.EntityTextureAtlas.RenderTextureIntoAtlas(skinTexPos.atlasTextureId, itemAtlas, itemTexPos.x1 * (float)capi.ItemTextureAtlas.Size.Width, itemTexPos.y1 * (float)capi.ItemTextureAtlas.Size.Height, (itemTexPos.x2 - itemTexPos.x1) * (float)capi.ItemTextureAtlas.Size.Width, (itemTexPos.y2 - itemTexPos.y1) * (float)capi.ItemTextureAtlas.Size.Height, skinTexPos.x1 * (float)capi.EntityTextureAtlas.Size.Width, skinTexPos.y1 * (float)capi.EntityTextureAtlas.Size.Height);
			}
		}
		capi.Render.GlToggleBlend(blend: true);
		capi.Render.BindTexture2d(skinTexPos.atlasTextureId);
		capi.Render.GlGenerateTex2DMipmaps();
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		capi?.EntityTextureAtlas.FreeTextureSpace(skinTextureSubId);
	}

	public override string PropertyName()
	{
		return "clothing";
	}
}
