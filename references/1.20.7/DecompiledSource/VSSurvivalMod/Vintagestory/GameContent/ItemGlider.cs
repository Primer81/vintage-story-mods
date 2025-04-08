using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class ItemGlider : Item, IWearableShapeSupplier
{
	private Shape gliderShape_unfoldStep1;

	private Shape gliderShape_unfoldStep2;

	private Shape gliderShape_unfolded;

	private bool subclassed;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		gliderShape_unfoldStep1 = Vintagestory.API.Common.Shape.TryGet(api, Attributes["unfoldShapeStep1"].AsObject<CompositeShape>().Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/"));
		gliderShape_unfoldStep2 = Vintagestory.API.Common.Shape.TryGet(api, Attributes["unfoldShapeStep2"].AsObject<CompositeShape>().Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/"));
		gliderShape_unfolded = Vintagestory.API.Common.Shape.TryGet(api, Attributes["unfoldedShape"].AsObject<CompositeShape>().Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/"));
	}

	public Shape GetShape(ItemStack stack, Entity forEntity, string texturePrefixCode)
	{
		if (!subclassed)
		{
			gliderShape_unfolded.SubclassForStepParenting(texturePrefixCode);
			gliderShape_unfoldStep1.SubclassForStepParenting(texturePrefixCode);
			gliderShape_unfoldStep2.SubclassForStepParenting(texturePrefixCode);
			subclassed = true;
		}
		return forEntity.Attributes.GetInt("unfoldStep") switch
		{
			1 => gliderShape_unfoldStep1, 
			2 => gliderShape_unfoldStep2, 
			3 => gliderShape_unfolded, 
			_ => null, 
		};
	}
}
