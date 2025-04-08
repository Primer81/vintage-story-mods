using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemSkillTimeswitch : Item, ISkillItemRenderer
{
	private LoadedTexture iconTex;

	private ICoreClientAPI capi;

	public static float timeSwitchCooldown;

	private ElementBounds renderBounds = new ElementBounds();

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (api is ICoreClientAPI capi && timeSwitchCooldown <= 0f)
		{
			capi.SendChatMessage("/timeswitch toggle");
			capi.World.AddCameraShake(0.25f);
			timeSwitchCooldown = 3f;
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		capi = api as ICoreClientAPI;
		JsonObject attributes = Attributes;
		if (attributes != null && attributes["iconPath"].Exists)
		{
			AssetLocation iconloc = AssetLocation.Create(Attributes["iconPath"].ToString(), Code.Domain).WithPathPrefix("textures/");
			iconTex = ObjectCacheUtil.GetOrCreate(api, "skillicon-" + Code, () => capi.Gui.LoadSvgWithPadding(iconloc, 64, 64, 5, -1));
		}
		base.OnLoaded(api);
	}

	public void Render(float dt, float x, float y, float z)
	{
		if (capi.World.Player.WorldData.CurrentGameMode != EnumGameMode.Spectator)
		{
			float shakex = ((float)capi.World.Rand.NextDouble() * 60f - 30f) * Math.Max(0f, timeSwitchCooldown - 2.4f);
			float shakey = ((float)capi.World.Rand.NextDouble() * 60f - 30f) * Math.Max(0f, timeSwitchCooldown - 2.4f);
			x += shakex;
			y += shakey;
			float guiscale = 0.61538464f * RuntimeEnv.GUIScale;
			capi.Render.Render2DTexture(iconTex.TextureId, x, y, (float)iconTex.Width * guiscale, (float)iconTex.Height * guiscale, z);
			double delta = (float)iconTex.Height * 8f / 13f * GameMath.Clamp(timeSwitchCooldown / 3f * 2.5f, 0f, 1f);
			renderBounds.ParentBounds = capi.Gui.WindowBounds;
			renderBounds.fixedX = x / RuntimeEnv.GUIScale;
			renderBounds.fixedY = (double)(y / RuntimeEnv.GUIScale) + delta;
			renderBounds.fixedWidth = (float)iconTex.Width * 8f / 13f;
			renderBounds.fixedHeight = (double)((float)iconTex.Height * 8f / 13f) - delta;
			renderBounds.CalcWorldBounds();
			capi.Render.PushScissor(renderBounds);
			Vec4f col = new Vec4f((float)GuiStyle.ColorTime1[0], (float)GuiStyle.ColorTime1[1], (float)GuiStyle.ColorTime1[2], (float)GuiStyle.ColorTime1[3]);
			capi.Render.Render2DTexture(iconTex.TextureId, x, y, (float)iconTex.Width * guiscale, (float)iconTex.Height * guiscale, z, col);
			timeSwitchCooldown = Math.Max(0f, timeSwitchCooldown - dt);
			capi.Render.PopScissor();
			capi.Render.CheckGlError();
		}
	}
}
