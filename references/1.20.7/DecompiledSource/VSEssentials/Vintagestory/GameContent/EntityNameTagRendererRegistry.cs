using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityNameTagRendererRegistry : ModSystem
{
	public class DefaultEntitlementTagRenderer
	{
		public double[] color;

		public TextBackground background;

		public LoadedTexture renderTag(ICoreClientAPI capi, Entity entity)
		{
			string name = entity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName;
			if (name != null && name.Length > 0)
			{
				return capi.Gui.TextTexture.GenUnscaledTextTexture(name, CairoFont.WhiteMediumText().WithColor(color), background);
			}
			return null;
		}
	}

	public static NameTagRendererDelegate DefaultNameTagRenderer = delegate(ICoreClientAPI capi, Entity entity)
	{
		string text = entity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName;
		return (text != null && text.Length > 0) ? capi.Gui.TextTexture.GenUnscaledTextTexture(Lang.GetIfExists("nametag-" + text.ToLowerInvariant()) ?? text, CairoFont.WhiteMediumText().WithColor(ColorUtil.WhiteArgbDouble), new TextBackground
		{
			FillColor = GuiStyle.DialogLightBgColor,
			Padding = 3,
			Radius = GuiStyle.ElementBGRadius,
			Shade = true,
			BorderColor = GuiStyle.DialogBorderColor,
			BorderWidth = 3.0
		}) : null;
	};

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
	}

	internal NameTagRendererDelegate GetNameTagRenderer(Entity entity)
	{
		List<Entitlement> entitlements = (entity as EntityPlayer)?.Player?.Entitlements;
		if (entitlements != null && entitlements.Count > 0)
		{
			Entitlement ent = entitlements[0];
			double[] color = null;
			if (GlobalConstants.playerColorByEntitlement.TryGetValue(ent.Code, out color))
			{
				GlobalConstants.playerTagBackgroundByEntitlement.TryGetValue(ent.Code, out var bg);
				return new DefaultEntitlementTagRenderer
				{
					color = color,
					background = bg
				}.renderTag;
			}
		}
		return DefaultNameTagRenderer;
	}
}
