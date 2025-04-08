using System;
using System.Text;
using System.Text.RegularExpressions;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class CharacterExtraDialogs : ModSystem
{
	private ICoreClientAPI capi;

	private GuiDialogCharacterBase dlg;

	private GuiDialog.DlgComposers Composers => dlg.Composers;

	public event Action<StringBuilder> OnEnvText;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	private bool IsOpened()
	{
		return dlg.IsOpened();
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		base.StartClientSide(api);
		dlg = api.Gui.LoadedGuis.Find((GuiDialog dlg) => dlg is GuiDialogCharacterBase) as GuiDialogCharacterBase;
		dlg.OnOpened += Dlg_OnOpened;
		dlg.OnClosed += Dlg_OnClosed;
		dlg.TabClicked += Dlg_TabClicked;
		dlg.ComposeExtraGuis += Dlg_ComposeExtraGuis;
		api.Event.RegisterGameTickListener(On2sTick, 2000);
	}

	private void Dlg_TabClicked(int tabIndex)
	{
		if (tabIndex != 0)
		{
			Dlg_OnClosed();
		}
		if (tabIndex == 0)
		{
			Dlg_OnOpened();
		}
	}

	private void Dlg_ComposeExtraGuis()
	{
		ComposeEnvGui();
		ComposeStatsGui();
	}

	private void On2sTick(float dt)
	{
		if (IsOpened())
		{
			updateEnvText();
		}
	}

	private void Dlg_OnClosed()
	{
		capi.World.Player.Entity.WatchedAttributes.UnregisterListener(UpdateStatBars);
		capi.World.Player.Entity.WatchedAttributes.UnregisterListener(UpdateStats);
	}

	private void Dlg_OnOpened()
	{
		capi.World.Player.Entity.WatchedAttributes.RegisterModifiedListener("hunger", UpdateStatBars);
		capi.World.Player.Entity.WatchedAttributes.RegisterModifiedListener("stats", UpdateStats);
		capi.World.Player.Entity.WatchedAttributes.RegisterModifiedListener("bodyTemp", UpdateStats);
	}

	public virtual void ComposeEnvGui()
	{
		ElementBounds leftDlgBounds = Composers["playercharacter"].Bounds;
		CairoFont font = CairoFont.WhiteSmallText().WithLineHeightMultiplier(1.2);
		string envText = getEnvText();
		int cntlines = 1 + Regex.Matches(envText, "\n").Count;
		double height = font.GetFontExtents().Height * font.LineHeightMultiplier * (double)cntlines / (double)RuntimeEnv.GUIScale;
		ElementBounds textBounds = ElementBounds.Fixed(0.0, 25.0, (int)(leftDlgBounds.InnerWidth / (double)RuntimeEnv.GUIScale - 40.0), height);
		textBounds.Name = "textbounds";
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.Name = "bgbounds";
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		bgBounds.WithChildren(textBounds);
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.None).WithFixedPosition(leftDlgBounds.renderX / (double)RuntimeEnv.GUIScale, leftDlgBounds.renderY / (double)RuntimeEnv.GUIScale + leftDlgBounds.OuterHeight / (double)RuntimeEnv.GUIScale + 10.0);
		dialogBounds.Name = "dialogbounds";
		Composers["environment"] = capi.Gui.CreateCompo("environment", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(Lang.Get("Environment"), delegate
		{
			dlg.OnTitleBarClose();
		})
			.BeginChildElements(bgBounds)
			.AddDynamicText(envText, font, textBounds, "dyntext")
			.EndChildElements()
			.Compose();
	}

	private void updateEnvText()
	{
		if (IsOpened() && Composers?["environment"] != null)
		{
			Composers["environment"].GetDynamicText("dyntext").SetNewTextAsync(getEnvText());
		}
	}

	private string getEnvText()
	{
		string date = capi.World.Calendar.PrettyDate();
		ClimateCondition conds = capi.World.BlockAccessor.GetClimateAt(capi.World.Player.Entity.Pos.AsBlockPos);
		string temp = "?";
		string rainfallfreq = "?";
		if (conds != null)
		{
			temp = (int)conds.Temperature + "°C";
			rainfallfreq = Lang.Get("freq-veryrare");
			if ((double)conds.WorldgenRainfall > 0.9)
			{
				rainfallfreq = Lang.Get("freq-allthetime");
			}
			else if ((double)conds.WorldgenRainfall > 0.7)
			{
				rainfallfreq = Lang.Get("freq-verycommon");
			}
			else if ((double)conds.WorldgenRainfall > 0.45)
			{
				rainfallfreq = Lang.Get("freq-common");
			}
			else if ((double)conds.WorldgenRainfall > 0.3)
			{
				rainfallfreq = Lang.Get("freq-uncommon");
			}
			else if ((double)conds.WorldgenRainfall > 0.15)
			{
				rainfallfreq = Lang.Get("freq-rarely");
			}
		}
		StringBuilder sb = new StringBuilder();
		sb.Append(Lang.Get("character-envtext", date, temp, rainfallfreq));
		this.OnEnvText?.Invoke(sb);
		return sb.ToString();
	}

	public virtual void ComposeStatsGui()
	{
		ElementBounds leftDlgBounds = Composers["playercharacter"].Bounds;
		ElementBounds bounds = Composers["environment"].Bounds;
		ElementBounds leftColumnBounds = ElementBounds.Fixed(0.0, 25.0, 90.0, 20.0);
		ElementBounds rightColumnBounds = ElementBounds.Fixed(120.0, 30.0, 120.0, 8.0);
		ElementBounds leftColumnBoundsW = ElementBounds.Fixed(0.0, 0.0, 140.0, 20.0);
		ElementBounds rightColumnBoundsW = ElementBounds.Fixed(165.0, 0.0, 120.0, 20.0);
		EntityPlayer entity = capi.World.Player.Entity;
		double b = bounds.InnerHeight / (double)RuntimeEnv.GUIScale + 10.0;
		ElementBounds bgBounds = ElementBounds.Fixed(0.0, 0.0, 235.0, leftDlgBounds.InnerHeight / (double)RuntimeEnv.GUIScale - GuiStyle.ElementToDialogPadding - 20.0 + b).WithFixedPadding(GuiStyle.ElementToDialogPadding);
		ElementBounds dialogBounds = bgBounds.ForkBoundingParent().WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset((leftDlgBounds.renderX + leftDlgBounds.OuterWidth + 10.0) / (double)RuntimeEnv.GUIScale, b / 2.0);
		float? health = null;
		float? maxhealth = null;
		float? saturation = null;
		float? maxsaturation = null;
		getHealthSat(out health, out maxhealth, out saturation, out maxsaturation);
		float walkspeed = entity.Stats.GetBlended("walkspeed");
		float healingEffectivness = entity.Stats.GetBlended("healingeffectivness");
		float hungerRate = entity.Stats.GetBlended("hungerrate");
		float rangedWeaponAcc = entity.Stats.GetBlended("rangedWeaponsAcc");
		float rangedWeaponSpeed = entity.Stats.GetBlended("rangedWeaponsSpeed");
		ITreeAttribute tempTree = entity.WatchedAttributes.GetTreeAttribute("bodyTemp");
		float wetness = entity.WatchedAttributes.GetFloat("wetness");
		string wetnessString = "";
		if ((double)wetness > 0.7)
		{
			wetnessString = Lang.Get("wetness_soakingwet");
		}
		else if ((double)wetness > 0.4)
		{
			wetnessString = Lang.Get("wetness_wet");
		}
		else if ((double)wetness > 0.1)
		{
			wetnessString = Lang.Get("wetness_slightlywet");
		}
		Composers["playerstats"] = capi.Gui.CreateCompo("playerstats", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(Lang.Get("Stats"), delegate
		{
			dlg.OnTitleBarClose();
		})
			.BeginChildElements(bgBounds);
		if (saturation.HasValue)
		{
			Composers["playerstats"].AddStaticText(Lang.Get("playerinfo-nutrition"), CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold), leftColumnBounds.WithFixedWidth(200.0)).AddStaticText(Lang.Get("playerinfo-nutrition-Freeza"), CairoFont.WhiteDetailText(), leftColumnBounds = leftColumnBounds.BelowCopy().WithFixedWidth(90.0)).AddStaticText(Lang.Get("playerinfo-nutrition-Vegita"), CairoFont.WhiteDetailText(), leftColumnBounds = leftColumnBounds.BelowCopy())
				.AddStaticText(Lang.Get("playerinfo-nutrition-Krillin"), CairoFont.WhiteDetailText(), leftColumnBounds = leftColumnBounds.BelowCopy())
				.AddStaticText(Lang.Get("playerinfo-nutrition-Cell"), CairoFont.WhiteDetailText(), leftColumnBounds = leftColumnBounds.BelowCopy())
				.AddStaticText(Lang.Get("playerinfo-nutrition-Dairy"), CairoFont.WhiteDetailText(), leftColumnBounds = leftColumnBounds.BelowCopy())
				.AddStatbar(rightColumnBounds = rightColumnBounds.BelowCopy(0.0, 16.0), GuiStyle.FoodBarColor, "fruitBar")
				.AddStatbar(rightColumnBounds = rightColumnBounds.BelowCopy(0.0, 12.0), GuiStyle.FoodBarColor, "vegetableBar")
				.AddStatbar(rightColumnBounds = rightColumnBounds.BelowCopy(0.0, 12.0), GuiStyle.FoodBarColor, "grainBar")
				.AddStatbar(rightColumnBounds = rightColumnBounds.BelowCopy(0.0, 12.0), GuiStyle.FoodBarColor, "proteinBar")
				.AddStatbar(rightColumnBounds = rightColumnBounds.BelowCopy(0.0, 12.0), GuiStyle.FoodBarColor, "dairyBar");
			leftColumnBoundsW = leftColumnBoundsW.FixedUnder(leftColumnBounds, -5.0);
		}
		Composers["playerstats"].AddStaticText(Lang.Get("Physical"), CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold), leftColumnBoundsW.WithFixedWidth(200.0).WithFixedOffset(0.0, 23.0)).Execute(delegate
		{
			leftColumnBoundsW = leftColumnBoundsW.FlatCopy();
			leftColumnBoundsW.fixedY += 5.0;
		});
		if (health.HasValue)
		{
			GuiComposer composer = Composers["playerstats"].AddStaticText(Lang.Get("Health Points"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy());
			float? num = health;
			string? text = num.ToString();
			num = maxhealth;
			composer.AddDynamicText(text + " / " + num, CairoFont.WhiteDetailText(), rightColumnBoundsW = rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, leftColumnBoundsW.fixedY).WithFixedHeight(30.0), "health");
		}
		if (saturation.HasValue)
		{
			Composers["playerstats"].AddStaticText(Lang.Get("Satiety"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy()).AddDynamicText((int)saturation.Value + " / " + (int)maxsaturation.Value, CairoFont.WhiteDetailText(), rightColumnBoundsW = rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, leftColumnBoundsW.fixedY), "satiety");
		}
		if (tempTree != null)
		{
			Composers["playerstats"].AddStaticText(Lang.Get("Body Temperature"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy()).AddRichtext((tempTree == null) ? "-" : getBodyTempText(tempTree), CairoFont.WhiteDetailText(), rightColumnBoundsW = rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, leftColumnBoundsW.fixedY), "bodytemp");
		}
		if (wetnessString.Length > 0)
		{
			Composers["playerstats"].AddRichtext(wetnessString, CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy());
		}
		Composers["playerstats"].AddStaticText(Lang.Get("Walk speed"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy()).AddDynamicText((int)Math.Round(100f * walkspeed) + "%", CairoFont.WhiteDetailText(), rightColumnBoundsW = rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, leftColumnBoundsW.fixedY), "walkspeed").AddStaticText(Lang.Get("Healing effectivness"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy())
			.AddDynamicText((int)Math.Round(100f * healingEffectivness) + "%", CairoFont.WhiteDetailText(), rightColumnBoundsW = rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, leftColumnBoundsW.fixedY), "healeffectiveness");
		if (saturation.HasValue)
		{
			Composers["playerstats"].AddStaticText(Lang.Get("Hunger rate"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy()).AddDynamicText((int)Math.Round(100f * hungerRate) + "%", CairoFont.WhiteDetailText(), rightColumnBoundsW = rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, leftColumnBoundsW.fixedY), "hungerrate");
		}
		Composers["playerstats"].AddStaticText(Lang.Get("Ranged Accuracy"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy()).AddDynamicText((int)Math.Round(100f * rangedWeaponAcc) + "%", CairoFont.WhiteDetailText(), rightColumnBoundsW = rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, leftColumnBoundsW.fixedY), "rangedweaponacc").AddStaticText(Lang.Get("Ranged Charge Speed"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy())
			.AddDynamicText((int)Math.Round(100f * rangedWeaponSpeed) + "%", CairoFont.WhiteDetailText(), rightColumnBoundsW = rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, leftColumnBoundsW.fixedY), "rangedweaponchargespeed")
			.EndChildElements()
			.Compose();
		UpdateStatBars();
	}

	private string getBodyTempText(ITreeAttribute tempTree)
	{
		float baseTemp = tempTree.GetFloat("bodytemp");
		if (baseTemp > 37f)
		{
			baseTemp = 37f + (baseTemp - 37f) / 10f;
		}
		return $"{baseTemp:0.#}°C";
	}

	private void getHealthSat(out float? health, out float? maxHealth, out float? saturation, out float? maxSaturation)
	{
		health = null;
		maxHealth = null;
		saturation = null;
		maxSaturation = null;
		ITreeAttribute healthTree = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("health");
		if (healthTree != null)
		{
			health = healthTree.TryGetFloat("currenthealth");
			maxHealth = healthTree.TryGetFloat("maxhealth");
		}
		if (health.HasValue)
		{
			health = (float)Math.Round(health.Value, 1);
		}
		if (maxHealth.HasValue)
		{
			maxHealth = (float)Math.Round(maxHealth.Value, 1);
		}
		ITreeAttribute hungerTree = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("hunger");
		if (hungerTree != null)
		{
			saturation = hungerTree.TryGetFloat("currentsaturation");
			maxSaturation = hungerTree.TryGetFloat("maxsaturation");
		}
		if (saturation.HasValue)
		{
			saturation = (int)saturation.Value;
		}
	}

	private void UpdateStats()
	{
		EntityPlayer entity = capi.World.Player.Entity;
		GuiComposer compo = Composers["playerstats"];
		if (compo != null && IsOpened())
		{
			getHealthSat(out var health, out var maxhealth, out var saturation, out var maxsaturation);
			float walkspeed = entity.Stats.GetBlended("walkspeed");
			float healingEffectivness = entity.Stats.GetBlended("healingeffectivness");
			float hungerRate = entity.Stats.GetBlended("hungerrate");
			float rangedWeaponAcc = entity.Stats.GetBlended("rangedWeaponsAcc");
			float rangedWeaponSpeed = entity.Stats.GetBlended("rangedWeaponsSpeed");
			if (health.HasValue)
			{
				GuiElementDynamicText dynamicText = compo.GetDynamicText("health");
				float? num = health;
				string? text = num.ToString();
				num = maxhealth;
				dynamicText.SetNewText(text + " / " + num);
			}
			if (saturation.HasValue)
			{
				compo.GetDynamicText("satiety").SetNewText((int)saturation.Value + " / " + (int)maxsaturation.Value);
			}
			compo.GetDynamicText("walkspeed").SetNewText((int)Math.Round(100f * walkspeed) + "%");
			compo.GetDynamicText("healeffectiveness").SetNewText((int)Math.Round(100f * healingEffectivness) + "%");
			compo.GetDynamicText("hungerrate")?.SetNewText((int)Math.Round(100f * hungerRate) + "%");
			compo.GetDynamicText("rangedweaponacc").SetNewText((int)Math.Round(100f * rangedWeaponAcc) + "%");
			compo.GetDynamicText("rangedweaponchargespeed").SetNewText((int)Math.Round(100f * rangedWeaponSpeed) + "%");
			ITreeAttribute tempTree = entity.WatchedAttributes.GetTreeAttribute("bodyTemp");
			compo.GetRichtext("bodytemp").SetNewText(getBodyTempText(tempTree), CairoFont.WhiteDetailText());
		}
	}

	private void UpdateStatBars()
	{
		GuiComposer compo = Composers["playerstats"];
		if (compo != null && IsOpened())
		{
			ITreeAttribute hungerTree = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("hunger");
			if (hungerTree != null)
			{
				float saturation = hungerTree.GetFloat("currentsaturation");
				float maxSaturation = hungerTree.GetFloat("maxsaturation");
				float fruitLevel = hungerTree.GetFloat("fruitLevel");
				float vegetableLevel = hungerTree.GetFloat("vegetableLevel");
				float grainLevel = hungerTree.GetFloat("grainLevel");
				float proteinLevel = hungerTree.GetFloat("proteinLevel");
				float dairyLevel = hungerTree.GetFloat("dairyLevel");
				compo.GetDynamicText("satiety").SetNewText((int)saturation + " / " + maxSaturation);
				Composers["playerstats"].GetStatbar("fruitBar").SetLineInterval(maxSaturation / 10f);
				Composers["playerstats"].GetStatbar("vegetableBar").SetLineInterval(maxSaturation / 10f);
				Composers["playerstats"].GetStatbar("grainBar").SetLineInterval(maxSaturation / 10f);
				Composers["playerstats"].GetStatbar("proteinBar").SetLineInterval(maxSaturation / 10f);
				Composers["playerstats"].GetStatbar("dairyBar").SetLineInterval(maxSaturation / 10f);
				Composers["playerstats"].GetStatbar("fruitBar").SetValues(fruitLevel, 0f, maxSaturation);
				Composers["playerstats"].GetStatbar("vegetableBar").SetValues(vegetableLevel, 0f, maxSaturation);
				Composers["playerstats"].GetStatbar("grainBar").SetValues(grainLevel, 0f, maxSaturation);
				Composers["playerstats"].GetStatbar("proteinBar").SetValues(proteinLevel, 0f, maxSaturation);
				Composers["playerstats"].GetStatbar("dairyBar").SetValues(dairyLevel, 0f, maxSaturation);
			}
		}
	}
}
