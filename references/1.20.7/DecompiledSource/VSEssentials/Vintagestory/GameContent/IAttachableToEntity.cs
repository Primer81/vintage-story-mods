using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public interface IAttachableToEntity
{
	bool IsAttachable(Entity toEntity, ItemStack itemStack);

	void CollectTextures(ItemStack stack, Shape shape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict);

	string GetCategoryCode(ItemStack stack);

	CompositeShape GetAttachedShape(ItemStack stack, string slotCode);

	string[] GetDisableElements(ItemStack stack);

	string[] GetKeepElements(ItemStack stack);

	string GetTexturePrefixCode(ItemStack stack);

	static IAttachableToEntity FromCollectible(CollectibleObject cobj)
	{
		IAttachableToEntity iate = cobj.GetCollectibleInterface<IAttachableToEntity>();
		if (iate != null)
		{
			return iate;
		}
		return FromAttributes(cobj);
	}

	static IAttachableToEntity FromAttributes(CollectibleObject cobj)
	{
		AttributeAttachableToEntity iattr = cobj.Attributes?["attachableToEntity"].AsObject<AttributeAttachableToEntity>(null, cobj.Code.Domain);
		if (iattr == null)
		{
			JsonObject attributes = cobj.Attributes;
			if (attributes != null && attributes["wearableAttachment"].Exists)
			{
				return new AttributeAttachableToEntity
				{
					CategoryCode = cobj.Attributes["clothescategory"].AsString(),
					KeepElements = cobj.Attributes["keepElements"].AsStringArray(),
					DisableElements = cobj.Attributes["disableElements"].AsStringArray()
				};
			}
		}
		return iattr;
	}

	static void CollectTexturesFromCollectible(ItemStack stack, string texturePrefixCode, Shape gearShape, Dictionary<string, CompositeTexture> intoDict)
	{
		if (gearShape.Textures == null)
		{
			gearShape.Textures = new Dictionary<string, AssetLocation>();
		}
		IDictionary<string, CompositeTexture> dictionary;
		if (stack.Class != 0)
		{
			IDictionary<string, CompositeTexture> textures = stack.Item.Textures;
			dictionary = textures;
		}
		else
		{
			dictionary = stack.Block.Textures;
		}
		IDictionary<string, CompositeTexture> collectibleDict = dictionary;
		if (collectibleDict == null)
		{
			return;
		}
		foreach (KeyValuePair<string, CompositeTexture> val in collectibleDict)
		{
			gearShape.Textures[val.Key] = val.Value.Base;
		}
	}
}
