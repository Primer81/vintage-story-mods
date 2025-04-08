using System;
using System.Collections.Generic;
using SkiaSharp;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorExtraSkinnable : EntityBehavior
{
	public Dictionary<string, SkinnablePart> AvailableSkinPartsByCode = new Dictionary<string, SkinnablePart>();

	public SkinnablePart[] AvailableSkinParts;

	public string VoiceType = "altoflute";

	public string VoicePitch = "medium";

	public string mainTextureCode;

	public List<AppliedSkinnablePartVariant> appliedTemp = new List<AppliedSkinnablePartVariant>();

	protected ITreeAttribute skintree;

	private bool didInit;

	public IReadOnlyList<AppliedSkinnablePartVariant> AppliedSkinParts
	{
		get
		{
			appliedTemp.Clear();
			ITreeAttribute appliedTree = skintree.GetTreeAttribute("appliedParts");
			if (appliedTree == null)
			{
				return appliedTemp;
			}
			SkinnablePart[] availableSkinParts = AvailableSkinParts;
			foreach (SkinnablePart part in availableSkinParts)
			{
				string code = appliedTree.GetString(part.Code);
				if (code != null && part.VariantsByCode.TryGetValue(code, out var variant))
				{
					appliedTemp.Add(variant.AppliedCopy(part.Code));
				}
			}
			return appliedTemp;
		}
	}

	public EntityBehaviorExtraSkinnable(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		skintree = entity.WatchedAttributes.GetTreeAttribute("skinConfig");
		if (skintree == null)
		{
			entity.WatchedAttributes["skinConfig"] = (skintree = new TreeAttribute());
		}
		mainTextureCode = properties.Attributes["mainTextureCode"].AsString("seraph");
		entity.WatchedAttributes.RegisterModifiedListener("skinConfig", onSkinConfigChanged);
		entity.WatchedAttributes.RegisterModifiedListener("voicetype", onVoiceConfigChanged);
		entity.WatchedAttributes.RegisterModifiedListener("voicepitch", onVoiceConfigChanged);
		AvailableSkinParts = properties.Attributes["skinnableParts"].AsObject<SkinnablePart[]>();
		SkinnablePart[] availableSkinParts = AvailableSkinParts;
		foreach (SkinnablePart val in availableSkinParts)
		{
			_ = val.Code;
			val.VariantsByCode = new Dictionary<string, SkinnablePartVariant>();
			AvailableSkinPartsByCode[val.Code] = val;
			if (val.Type == EnumSkinnableType.Texture && entity.Api.Side == EnumAppSide.Client)
			{
				ICoreClientAPI capi = entity.Api as ICoreClientAPI;
				new LoadedTexture(capi);
				SkinnablePartVariant[] variants = val.Variants;
				foreach (SkinnablePartVariant variant in variants)
				{
					AssetLocation textureLoc;
					if (val.TextureTemplate != null)
					{
						textureLoc = val.TextureTemplate.Clone();
						textureLoc.Path = textureLoc.Path.Replace("{code}", variant.Code);
					}
					else
					{
						textureLoc = variant.Texture;
					}
					IAsset asset = capi.Assets.TryGet(textureLoc.Clone().WithPathAppendixOnce(".png").WithPathPrefixOnce("textures/"));
					int r = 0;
					int g = 0;
					int b = 0;
					float c = 0f;
					BitmapRef bmp = asset.ToBitmap(capi);
					for (int i = 0; i < 8; i++)
					{
						Vec2d vec = GameMath.R2Sequence2D(i);
						SKColor col2 = bmp.GetPixelRel((float)vec.X, (float)vec.Y);
						if ((double)(int)col2.Alpha > 0.5)
						{
							r += col2.Red;
							g += col2.Green;
							b += col2.Blue;
							c += 1f;
						}
					}
					bmp.Dispose();
					c = Math.Max(1f, c);
					variant.Color = ColorUtil.ColorFromRgba((int)((float)b / c), (int)((float)g / c), (int)((float)r / c), 255);
					val.VariantsByCode[variant.Code] = variant;
				}
			}
			else
			{
				SkinnablePartVariant[] variants = val.Variants;
				foreach (SkinnablePartVariant variant2 in variants)
				{
					val.VariantsByCode[variant2.Code] = variant2;
				}
			}
		}
		if (entity.Api.Side == EnumAppSide.Server && AppliedSkinParts.Count == 0)
		{
			entity.Api.ModLoader.GetModSystem<CharacterSystem>().randomizeSkin(entity, null, playVoice: false);
		}
		onVoiceConfigChanged();
	}

	private void onSkinConfigChanged()
	{
		skintree = entity.WatchedAttributes["skinConfig"] as ITreeAttribute;
		entity.MarkShapeModified();
	}

	private void onVoiceConfigChanged()
	{
		VoiceType = entity.WatchedAttributes.GetString("voicetype");
		VoicePitch = entity.WatchedAttributes.GetString("voicepitch");
		ApplyVoice(VoiceType, VoicePitch, testTalk: false);
	}

	public override void OnEntityLoaded()
	{
		base.OnEntityLoaded();
		init();
	}

	public override void OnEntitySpawn()
	{
		base.OnEntitySpawn();
		init();
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		EntityBehaviorTexturedClothing ebhtc = entity.GetBehavior<EntityBehaviorTexturedClothing>();
		if (ebhtc != null)
		{
			ebhtc.OnReloadSkin -= Essr_OnReloadSkin;
		}
	}

	private void init()
	{
		if (entity.World.Side == EnumAppSide.Client && !didInit)
		{
			if (!(entity.Properties.Client.Renderer is EntityShapeRenderer))
			{
				throw new InvalidOperationException("The extra skinnable entity behavior requires the entity to use the Shape renderer.");
			}
			(entity.GetBehavior<EntityBehaviorTexturedClothing>() ?? throw new InvalidOperationException("The extra skinnable entity behavior requires the entity to have the TextureClothing entitybehavior.")).OnReloadSkin += Essr_OnReloadSkin;
			didInit = true;
		}
	}

	public override void OnTesselation(ref Shape entityShape, string shapePathForLogging, ref bool shapeIsCloned, ref string[] willDeleteElements)
	{
		if (!shapeIsCloned)
		{
			Shape newShape = entityShape.Clone();
			entityShape = newShape;
			shapeIsCloned = true;
		}
		foreach (AppliedSkinnablePartVariant skinpart in AppliedSkinParts)
		{
			AvailableSkinPartsByCode.TryGetValue(skinpart.PartCode, out var part2);
			if (part2 != null && part2.Type == EnumSkinnableType.Shape)
			{
				entityShape = addSkinPart(skinpart, entityShape, part2.DisableElements, shapePathForLogging);
			}
		}
		foreach (AppliedSkinnablePartVariant val2 in AppliedSkinParts)
		{
			AvailableSkinPartsByCode.TryGetValue(val2.PartCode, out var part);
			if (part != null && part.Type == EnumSkinnableType.Texture && part.TextureTarget != null && part.TextureTarget != mainTextureCode)
			{
				AssetLocation textureLoc;
				if (part.TextureTemplate != null)
				{
					textureLoc = part.TextureTemplate.Clone();
					textureLoc.Path = textureLoc.Path.Replace("{code}", val2.Code);
				}
				else
				{
					textureLoc = val2.Texture;
				}
				string code = "skinpart-" + part.TextureTarget;
				entityShape.TextureSizes.TryGetValue(code, out var sizes);
				if (sizes != null)
				{
					loadTexture(entityShape, code, textureLoc, sizes[0], sizes[1], shapePathForLogging);
				}
				else
				{
					entity.Api.Logger.Error("Skinpart has no textureSize: " + code + " in: " + shapePathForLogging);
				}
			}
		}
		EntityBehaviorTexturedClothing ebhtc = entity.GetBehavior<EntityBehaviorTexturedClothing>();
		InventoryBase inv = ebhtc.Inventory;
		if (inv == null)
		{
			return;
		}
		foreach (ItemSlot slot in inv)
		{
			if (slot.Empty || ebhtc.hideClothing)
			{
				continue;
			}
			JsonObject attrObj = slot.Itemstack.Collectible.Attributes;
			entityShape.RemoveElements(attrObj?["disableElements"]?.AsArray<string>());
			string[] keepEles = attrObj?["keepElements"]?.AsArray<string>();
			if (keepEles != null && willDeleteElements != null)
			{
				string[] array = keepEles;
				foreach (string val in array)
				{
					willDeleteElements = willDeleteElements.Remove(val);
				}
			}
		}
	}

	private void Essr_OnReloadSkin(LoadedTexture atlas, TextureAtlasPosition skinTexPos, int textureSubId)
	{
		ICoreClientAPI capi = entity.World.Api as ICoreClientAPI;
		foreach (AppliedSkinnablePartVariant val in AppliedSkinParts)
		{
			SkinnablePart part = AvailableSkinPartsByCode[val.PartCode];
			if (part.Type == EnumSkinnableType.Texture && (part.TextureTarget == null || !(part.TextureTarget != mainTextureCode)))
			{
				LoadedTexture texture = new LoadedTexture(capi);
				capi.Render.GetOrLoadTexture(val.Texture.Clone().WithPathAppendixOnce(".png"), ref texture);
				int posx = part.TextureRenderTo?.X ?? 0;
				int posy = part.TextureRenderTo?.Y ?? 0;
				capi.EntityTextureAtlas.RenderTextureIntoAtlas(skinTexPos.atlasTextureId, texture, 0f, 0f, texture.Width, texture.Height, skinTexPos.x1 * (float)capi.EntityTextureAtlas.Size.Width + (float)posx, skinTexPos.y1 * (float)capi.EntityTextureAtlas.Size.Height + (float)posy, (part.Code == "baseskin") ? (-1f) : 0.005f);
			}
		}
		IDictionary<string, CompositeTexture> textures = entity.Properties.Client.Textures;
		textures[mainTextureCode].Baked.TextureSubId = textureSubId;
		textures["skinpart-" + mainTextureCode] = textures[mainTextureCode];
	}

	public void selectSkinPart(string partCode, string variantCode, bool retesselateShape = true, bool playVoice = true)
	{
		AvailableSkinPartsByCode.TryGetValue(partCode, out var part);
		ITreeAttribute appliedTree = skintree.GetTreeAttribute("appliedParts");
		if (appliedTree == null)
		{
			appliedTree = (ITreeAttribute)(skintree["appliedParts"] = new TreeAttribute());
		}
		appliedTree[partCode] = new StringAttribute(variantCode);
		if (part != null && part.Type == EnumSkinnableType.Voice)
		{
			entity.WatchedAttributes.SetString(partCode, variantCode);
			if (partCode == "voicetype")
			{
				VoiceType = variantCode;
			}
			if (partCode == "voicepitch")
			{
				VoicePitch = variantCode;
			}
			ApplyVoice(VoiceType, VoicePitch, playVoice);
		}
		else
		{
			EntityShapeRenderer essr = entity.Properties.Client.Renderer as EntityShapeRenderer;
			if (retesselateShape)
			{
				essr?.TesselateShape();
			}
		}
	}

	public void ApplyVoice(string voiceType, string voicePitch, bool testTalk)
	{
		if (!AvailableSkinPartsByCode.TryGetValue("voicetype", out var availVoices) || !AvailableSkinPartsByCode.TryGetValue("voicepitch", out var _))
		{
			return;
		}
		VoiceType = voiceType;
		VoicePitch = voicePitch;
		if (entity is EntityPlayer { talkUtil: not null } plr && voiceType != null)
		{
			if (!availVoices.VariantsByCode.ContainsKey(voiceType))
			{
				voiceType = availVoices.Variants[0].Code;
			}
			plr.talkUtil.soundName = availVoices.VariantsByCode[voiceType].Sound;
			float pitchMod = 1f;
			switch (VoicePitch)
			{
			case "verylow":
				pitchMod = 0.6f;
				break;
			case "low":
				pitchMod = 0.8f;
				break;
			case "medium":
				pitchMod = 1f;
				break;
			case "high":
				pitchMod = 1.2f;
				break;
			case "veryhigh":
				pitchMod = 1.4f;
				break;
			}
			plr.talkUtil.pitchModifier = pitchMod;
			plr.talkUtil.chordDelayMul = 1.1f;
			if (testTalk)
			{
				plr.talkUtil.Talk(EnumTalkType.Idle);
			}
		}
	}

	protected Shape addSkinPart(AppliedSkinnablePartVariant part, Shape entityShape, string[] disableElements, string shapePathForLogging)
	{
		SkinnablePart skinpart = AvailableSkinPartsByCode[part.PartCode];
		if (skinpart.Type == EnumSkinnableType.Voice)
		{
			entity.WatchedAttributes.SetString("voicetype", part.Code);
			return entityShape;
		}
		entityShape.RemoveElements(disableElements);
		ICoreAPI api = entity.World.Api;
		ICoreClientAPI capi = entity.World.Api as ICoreClientAPI;
		CompositeShape tmpl = skinpart.ShapeTemplate;
		AssetLocation shapePath;
		if (part.Shape == null && tmpl != null)
		{
			shapePath = tmpl.Base.CopyWithPath("shapes/" + tmpl.Base.Path + ".json");
			shapePath.Path = shapePath.Path.Replace("{code}", part.Code);
		}
		else
		{
			shapePath = part.Shape.Base.CopyWithPath("shapes/" + part.Shape.Base.Path + ".json");
		}
		Shape partShape = Shape.TryGet(api, shapePath);
		if (partShape == null)
		{
			api.World.Logger.Warning("Entity skin shape {0} defined in entity config {1} not found or errored, was supposed to be at {2}. Skin part will be invisible.", shapePath, entity.Properties.Code, shapePath);
			return null;
		}
		string prefixcode = "skinpart";
		partShape.SubclassForStepParenting(prefixcode + "-");
		IDictionary<string, CompositeTexture> textures = entity.Properties.Client.Textures;
		entityShape.StepParentShape(partShape, shapePath.ToShortString(), shapePathForLogging, api.Logger, delegate(string texcode, AssetLocation loc)
		{
			if (capi != null && !textures.ContainsKey("skinpart-" + texcode) && skinpart.TextureRenderTo == null)
			{
				CompositeTexture compositeTexture2 = (textures[prefixcode + "-" + texcode] = new CompositeTexture(loc));
				CompositeTexture compositeTexture3 = compositeTexture2;
				compositeTexture3.Bake(api.Assets);
				capi.EntityTextureAtlas.GetOrInsertTexture(compositeTexture3.Baked.TextureFilenames[0], out var textureSubId, out var _);
				compositeTexture3.Baked.TextureSubId = textureSubId;
			}
		});
		return entityShape;
	}

	private void loadTexture(Shape entityShape, string code, AssetLocation location, int textureWidth, int textureHeight, string shapePathForLogging)
	{
		if (entity.World.Side != EnumAppSide.Server)
		{
			IDictionary<string, CompositeTexture> textures = entity.Properties.Client.Textures;
			ICoreClientAPI capi = entity.World.Api as ICoreClientAPI;
			CompositeTexture compositeTexture2 = (textures[code] = new CompositeTexture(location));
			CompositeTexture cmpt = compositeTexture2;
			cmpt.Bake(capi.Assets);
			if (!capi.EntityTextureAtlas.GetOrInsertTexture(cmpt.Baked.TextureFilenames[0], out var textureSubid, out var _, null, -1f))
			{
				capi.Logger.Warning("Skin part shape {0} defined texture {1}, no such texture found.", shapePathForLogging, location);
			}
			cmpt.Baked.TextureSubId = textureSubid;
			entityShape.TextureSizes[code] = new int[2] { textureWidth, textureHeight };
			textures[code] = cmpt;
		}
	}

	public override string PropertyName()
	{
		return "skinnableplayer";
	}
}
