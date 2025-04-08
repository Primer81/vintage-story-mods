using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemCreatureInventory : Item, ITexPositionSource
{
	private ICoreClientAPI capi;

	private EntityProperties nowTesselatingEntityType;

	private static Dictionary<EnumItemRenderTarget, string> map = new Dictionary<EnumItemRenderTarget, string>
	{
		{
			EnumItemRenderTarget.Ground,
			"groundTransform"
		},
		{
			EnumItemRenderTarget.HandTp,
			"tpHandTransform"
		},
		{
			EnumItemRenderTarget.Gui,
			"guiTransform"
		},
		{
			EnumItemRenderTarget.HandTpOff,
			"tpOffHandTransform"
		}
	};

	public Size2i AtlasSize => capi.ItemTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			nowTesselatingEntityType.Client.Textures.TryGetValue(textureCode, out var cTex);
			AssetLocation texPath;
			if (cTex == null)
			{
				nowTesselatingEntityType.Client.LoadedShape.Textures.TryGetValue(textureCode, out texPath);
			}
			else
			{
				texPath = cTex.Base;
			}
			if (texPath != null)
			{
				capi.ItemTextureAtlas.GetOrInsertTexture(texPath, out var _, out var texPos);
				return texPos;
			}
			return null;
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		capi = api as ICoreClientAPI;
		List<JsonItemStack> stacks = new List<JsonItemStack>();
		foreach (EntityProperties entitytype in api.World.EntityTypes)
		{
			JsonObject attributes = entitytype.Attributes;
			if (attributes == null || attributes["inCreativeInventory"].AsBool(defaultValue: true))
			{
				JsonItemStack jstack = new JsonItemStack
				{
					Code = Code,
					Type = EnumItemClass.Item,
					Attributes = new JsonObject(JToken.Parse(string.Concat("{ \"type\": \"", entitytype.Code, "\" }")))
				};
				jstack.Resolve(api.World, "creatureinventory");
				stacks.Add(jstack);
			}
		}
		CreativeInventoryStacks = new CreativeTabAndStackList[1]
		{
			new CreativeTabAndStackList
			{
				Stacks = stacks.ToArray(),
				Tabs = new string[3] { "general", "items", "creatures" }
			}
		};
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		Dictionary<string, MultiTextureMeshRef> meshrefs = ObjectCacheUtil.GetOrCreate(capi, "itemcreatureinventorymeshes", () => new Dictionary<string, MultiTextureMeshRef>());
		string code = itemstack.Attributes.GetString("type");
		if (!meshrefs.ContainsKey(code))
		{
			AssetLocation location = new AssetLocation(code);
			EntityProperties type = (nowTesselatingEntityType = api.World.GetEntityType(location));
			Shape shape = type.Client.LoadedShape;
			if (shape != null)
			{
				capi.Tesselator.TesselateShape("itemcreatureinventory", shape, out var meshdata, this, null, 0, 0, 0);
				ModelTransform tf = type.Attributes?[map[target]]?.AsObject<ModelTransform>();
				if (tf != null)
				{
					meshdata.ModelTransform(tf);
				}
				ItemRenderInfo obj = renderinfo;
				MultiTextureMeshRef modelRef = (meshrefs[code] = capi.Render.UploadMultiTextureMesh(meshdata));
				obj.ModelRef = modelRef;
			}
		}
		else
		{
			renderinfo.ModelRef = meshrefs[code];
		}
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}

	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
	{
		return null;
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null)
		{
			return;
		}
		IPlayer player = byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID);
		if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			return;
		}
		if (!(byEntity is EntityPlayer) || player.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			slot.TakeOut(1);
			slot.MarkDirty();
		}
		AssetLocation location = new AssetLocation(slot.Itemstack.Attributes.GetString("type"));
		EntityProperties type = byEntity.World.GetEntityType(location);
		if (type == null)
		{
			byEntity.World.Logger.Error("ItemCreature: No such entity - {0}", location);
			if (api.World.Side == EnumAppSide.Client)
			{
				(api as ICoreClientAPI).TriggerIngameError(this, "nosuchentity", $"No such entity loaded - '{location}'.");
			}
			return;
		}
		Entity entity = byEntity.World.ClassRegistry.CreateEntity(type);
		if (entity == null)
		{
			return;
		}
		entity.ServerPos.X = (float)(blockSel.Position.X + ((!blockSel.DidOffset) ? blockSel.Face.Normali.X : 0)) + 0.5f;
		entity.ServerPos.Y = blockSel.Position.Y + ((!blockSel.DidOffset) ? blockSel.Face.Normali.Y : 0);
		entity.ServerPos.Z = (float)(blockSel.Position.Z + ((!blockSel.DidOffset) ? blockSel.Face.Normali.Z : 0)) + 0.5f;
		entity.ServerPos.Yaw = (float)byEntity.World.Rand.NextDouble() * 2f * (float)Math.PI;
		entity.Pos.SetFrom(entity.ServerPos);
		entity.PositionBeforeFalling.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		entity.Attributes.SetString("origin", "playerplaced");
		JsonObject attributes = Attributes;
		if (attributes != null && attributes.IsTrue("setGuardedEntityAttribute"))
		{
			entity.WatchedAttributes.SetLong("guardedEntityId", byEntity.EntityId);
			if (byEntity is EntityPlayer eplr)
			{
				entity.WatchedAttributes.SetString("guardedPlayerUid", eplr.PlayerUID);
			}
		}
		byEntity.World.SpawnEntity(entity);
		handHandling = EnumHandHandling.PreventDefaultAction;
	}

	public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity byEntity, EnumHand hand)
	{
		EntityProperties type = byEntity.World.GetEntityType(new AssetLocation(Code.Domain, CodeEndWithoutParts(1)));
		if (type == null)
		{
			return base.GetHeldTpIdleAnimation(activeHotbarSlot, byEntity, hand);
		}
		if (Math.Max(type.CollisionBoxSize.X, type.CollisionBoxSize.Y) > 1f)
		{
			return "holdunderarm";
		}
		return "holdbothhands";
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-place",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
