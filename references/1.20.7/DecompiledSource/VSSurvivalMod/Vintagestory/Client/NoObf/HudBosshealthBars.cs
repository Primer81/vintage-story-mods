using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Client.NoObf;

public class HudBosshealthBars : HudElement
{
	private float lastHealth;

	private float lastMaxHealth;

	public int barIndex;

	public EntityAgent TargetEntity;

	private GuiElementStatbar healthbar;

	private long listenerId;

	public override double InputOrder => 1.0;

	public override string ToggleKeyCombinationCode => null;

	public override bool Focusable => false;

	public HudBosshealthBars(ICoreClientAPI capi, EntityAgent bossEntity, int barIndex)
		: base(capi)
	{
		TargetEntity = bossEntity;
		listenerId = capi.Event.RegisterGameTickListener(OnGameTick, 20);
		this.barIndex = barIndex;
		ComposeGuis();
	}

	private void OnGameTick(float dt)
	{
		UpdateHealth();
	}

	private void UpdateHealth()
	{
		ITreeAttribute healthTree = TargetEntity.WatchedAttributes.GetTreeAttribute("health");
		if (healthTree != null)
		{
			float? health = healthTree.TryGetFloat("currenthealth");
			float? maxHealth = healthTree.TryGetFloat("maxhealth");
			if (health.HasValue && maxHealth.HasValue && (lastHealth != health || lastMaxHealth != maxHealth) && healthbar != null)
			{
				healthbar.SetLineInterval(1f);
				healthbar.SetValues(health.Value, 0f, maxHealth.Value);
				lastHealth = health.Value;
				lastMaxHealth = maxHealth.Value;
			}
		}
	}

	public void ComposeGuis()
	{
		float width = 850f;
		ElementBounds dialogBounds = new ElementBounds
		{
			Alignment = EnumDialogArea.CenterFixed,
			BothSizing = ElementSizing.Fixed,
			fixedWidth = width,
			fixedHeight = 50.0,
			fixedY = 10 + barIndex * 25
		}.WithFixedAlignmentOffset(0.0, 5.0);
		ElementBounds healthBarBounds = ElementBounds.Fixed(0.0, 18.0, width, 14.0);
		ITreeAttribute healthTree = TargetEntity.WatchedAttributes.GetTreeAttribute("health");
		string key = "bosshealthbar-" + TargetEntity.EntityId;
		Composers["bosshealthbar"] = capi.Gui.CreateCompo(key, dialogBounds.FlatCopy().FixedGrow(0.0, 20.0)).BeginChildElements(dialogBounds).AddIf(healthTree != null)
			.AddStaticText(Lang.Get(TargetEntity.Code.Domain + ":item-creature-" + TargetEntity.Code.Path), CairoFont.WhiteSmallText(), ElementBounds.Fixed(0.0, 0.0, 200.0, 20.0))
			.AddStatbar(healthBarBounds, GuiStyle.HealthBarColor, "healthstatbar")
			.EndIf()
			.EndChildElements()
			.Compose();
		healthbar = Composers["bosshealthbar"].GetStatbar("healthstatbar");
		TryOpen();
	}

	public override bool TryClose()
	{
		return base.TryClose();
	}

	public override bool ShouldReceiveKeyboardEvents()
	{
		return false;
	}

	public override void OnRenderGUI(float deltaTime)
	{
		base.OnRenderGUI(deltaTime);
	}

	protected override void OnFocusChanged(bool on)
	{
	}

	public override void OnMouseDown(MouseEvent args)
	{
	}

	public override void Dispose()
	{
		base.Dispose();
		capi.Event.UnregisterGameTickListener(listenerId);
	}
}
