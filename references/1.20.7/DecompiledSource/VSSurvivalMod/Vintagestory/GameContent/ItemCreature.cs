using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemCreature : Item
{
	private CompositeShape[] stepParentShapes;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		stepParentShapes = Attributes?["stepParentShapes"].AsObject<CompositeShape[]>();
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		if (stepParentShapes != null && stepParentShapes.Length != 0)
		{
			Dictionary<AssetLocation, MultiTextureMeshRef> dict = ObjectCacheUtil.GetOrCreate(capi, "itemcreaturemeshrefs", () => new Dictionary<AssetLocation, MultiTextureMeshRef>());
			if (dict.TryGetValue(Code, out var mmeshref))
			{
				renderinfo.ModelRef = mmeshref;
			}
			else
			{
				dict[Code] = (renderinfo.ModelRef = CreateOverlaidMeshRef(capi, Shape, stepParentShapes));
			}
		}
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}

	private MultiTextureMeshRef CreateOverlaidMeshRef(ICoreClientAPI capi, CompositeShape cshape, CompositeShape[] stepParentShapes)
	{
		Dictionary<string, CompositeTexture> textures = Textures;
		Shape shape = capi.Assets.TryGet(cshape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"))?.ToObject<Shape>();
		if (shape == null)
		{
			capi.Logger.Error("Entity {0} defines a shape {1}, but no such file found. Will use default shape.", Code, cshape.Base);
			return capi.TesselatorManager.GetDefaultItemMeshRef(this);
		}
		foreach (CompositeShape stepparentshape in stepParentShapes)
		{
			Shape overlayshape = capi.Assets.TryGet(stepparentshape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"))?.ToObject<Shape>();
			if (overlayshape == null)
			{
				capi.Logger.Error("Entity {0} defines a shape overlay {1}, but no such file found. Will ignore.", Code, stepparentshape.Base);
				continue;
			}
			string texturePrefixCode = null;
			JsonObject attributes = Attributes;
			if (attributes != null && attributes["wearableTexturePrefixCode"].Exists)
			{
				texturePrefixCode = Attributes["wearableTexturePrefixCode"].AsString();
			}
			overlayshape.SubclassForStepParenting(texturePrefixCode);
			shape.StepParentShape(overlayshape, stepparentshape.Base.ToShortString(), cshape.Base.ToShortString(), capi.Logger, delegate(string texcode, AssetLocation tloc)
			{
				if (texturePrefixCode != null || !textures.ContainsKey(texcode))
				{
					CompositeTexture compositeTexture2 = (textures[texturePrefixCode + texcode] = new CompositeTexture(tloc));
					CompositeTexture compositeTexture3 = compositeTexture2;
					compositeTexture3.Bake(capi.Assets);
					capi.ItemTextureAtlas.GetOrInsertTexture(compositeTexture3.Baked.TextureFilenames[0], out var textureSubId, out var _);
					compositeTexture3.Baked.TextureSubId = textureSubId;
				}
			});
		}
		TesselationMetaData meta = new TesselationMetaData
		{
			QuantityElements = cshape.QuantityElements,
			SelectiveElements = cshape.SelectiveElements,
			IgnoreElements = cshape.IgnoreElements,
			TexSource = capi.Tesselator.GetTextureSource(this),
			WithJointIds = false,
			WithDamageEffect = false,
			TypeForLogging = "item",
			Rotation = new Vec3f(cshape.rotateX, cshape.rotateY, cshape.rotateZ)
		};
		capi.Tesselator.TesselateShape(meta, shape, out var meshdata);
		return capi.Render.UploadMultiTextureMesh(meshdata);
	}

	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
	{
		return null;
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		Dictionary<AssetLocation, MultiTextureMeshRef> dict = ObjectCacheUtil.TryGet<Dictionary<AssetLocation, MultiTextureMeshRef>>(api, "itemcreaturemeshrefs");
		if (dict != null)
		{
			foreach (MultiTextureMeshRef value in dict.Values)
			{
				value.Dispose();
			}
		}
		base.OnUnloaded(api);
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
		AssetLocation location = new AssetLocation(Code.Domain, CodeEndWithoutParts(1));
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
		entity.ServerPos.Yaw = byEntity.Pos.Yaw + (float)Math.PI;
		entity.ServerPos.Dimension = blockSel.Position.dimension;
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

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		if (FirstCodePart(1) == "butterfly")
		{
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
			dsc.Insert(0, "<font color=\"#ccc\"><i>");
			dsc.Append("</i></font>");
			dsc.AppendLine(Lang.Get("itemdesc-creature-butterfly-all"));
		}
		else
		{
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		}
	}
}
