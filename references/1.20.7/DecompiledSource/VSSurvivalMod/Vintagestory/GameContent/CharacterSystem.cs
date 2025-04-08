using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class CharacterSystem : ModSystem
{
	private ICoreAPI api;

	private ICoreClientAPI capi;

	private ICoreServerAPI sapi;

	private GuiDialogCreateCharacter createCharDlg;

	private GuiDialogCharacterBase charDlg;

	private bool didSelect;

	public List<CharacterClass> characterClasses = new List<CharacterClass>();

	public List<Trait> traits = new List<Trait>();

	public Dictionary<string, CharacterClass> characterClassesByCode = new Dictionary<string, CharacterClass>();

	public Dictionary<string, Trait> TraitsByCode = new Dictionary<string, Trait>();

	private SeraphRandomizerConstraints randomizerConstraints;

	public override void Start(ICoreAPI api)
	{
		this.api = api;
		api.Network.RegisterChannel("charselection").RegisterMessageType<CharacterSelectionPacket>().RegisterMessageType<CharacterSelectedState>();
		api.Event.MatchesGridRecipe += Event_MatchesGridRecipe;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Event.BlockTexturesLoaded += onLoadedUniversal;
		api.Network.GetChannel("charselection").SetMessageHandler<CharacterSelectedState>(onSelectedState);
		api.Event.IsPlayerReady += Event_IsPlayerReady;
		api.Event.PlayerJoin += Event_PlayerJoin;
		this.api.ChatCommands.Create("charsel").WithDescription("Open the character selection menu").HandleWith(onCharSelCmd);
		api.Event.BlockTexturesLoaded += loadCharacterClasses;
		charDlg = api.Gui.LoadedGuis.Find((GuiDialog dlg) => dlg is GuiDialogCharacterBase) as GuiDialogCharacterBase;
		charDlg.Tabs.Add(new GuiTab
		{
			Name = Lang.Get("charactertab-traits"),
			DataInt = 1
		});
		charDlg.RenderTabHandlers.Add(composeTraitsTab);
	}

	private void onLoadedUniversal()
	{
		randomizerConstraints = api.Assets.Get("config/seraphrandomizer.json").ToObject<SeraphRandomizerConstraints>();
	}

	private void composeTraitsTab(GuiComposer compo)
	{
		compo.AddRichtext(getClassTraitText(), CairoFont.WhiteDetailText().WithLineHeightMultiplier(1.15), ElementBounds.Fixed(0.0, 25.0, 385.0, 200.0));
	}

	private string getClassTraitText()
	{
		string charClass = capi.World.Player.Entity.WatchedAttributes.GetString("characterClass");
		CharacterClass chclass = characterClasses.FirstOrDefault((CharacterClass c) => c.Code == charClass);
		StringBuilder fulldesc = new StringBuilder();
		StringBuilder attributes = new StringBuilder();
		foreach (Trait trait2 in from code in chclass.Traits
			select TraitsByCode[code] into trait
			orderby (int)trait.Type
			select trait)
		{
			attributes.Clear();
			foreach (KeyValuePair<string, double> val in trait2.Attributes)
			{
				if (attributes.Length > 0)
				{
					attributes.Append(", ");
				}
				attributes.Append(Lang.Get(string.Format(GlobalConstants.DefaultCultureInfo, "charattribute-{0}-{1}", val.Key, val.Value)));
			}
			if (attributes.Length > 0)
			{
				fulldesc.AppendLine(Lang.Get("traitwithattributes", Lang.Get("trait-" + trait2.Code), attributes));
				continue;
			}
			string desc = Lang.GetIfExists("traitdesc-" + trait2.Code);
			if (desc != null)
			{
				fulldesc.AppendLine(Lang.Get("traitwithattributes", Lang.Get("trait-" + trait2.Code), desc));
			}
			else
			{
				fulldesc.AppendLine(Lang.Get("trait-" + trait2.Code));
			}
		}
		if (chclass.Traits.Length == 0)
		{
			fulldesc.AppendLine(Lang.Get("No positive or negative traits"));
		}
		return fulldesc.ToString();
	}

	private void loadCharacterClasses()
	{
		onLoadedUniversal();
		traits = api.Assets.Get("config/traits.json").ToObject<List<Trait>>();
		characterClasses = api.Assets.Get("config/characterclasses.json").ToObject<List<CharacterClass>>();
		foreach (Trait trait in traits)
		{
			TraitsByCode[trait.Code] = trait;
		}
		foreach (CharacterClass charclass in characterClasses)
		{
			characterClassesByCode[charclass.Code] = charclass;
			JsonItemStack[] gear = charclass.Gear;
			foreach (JsonItemStack jstack in gear)
			{
				if (!jstack.Resolve(api.World, "character class gear", printWarningOnError: false))
				{
					api.World.Logger.Warning(string.Concat("Unable to resolve character class gear ", jstack.Type.ToString(), " with code ", jstack.Code, " item/block does not seem to exist. Will ignore."));
				}
			}
		}
	}

	public void setCharacterClass(EntityPlayer eplayer, string classCode, bool initializeGear = true)
	{
		CharacterClass charclass = characterClasses.FirstOrDefault((CharacterClass c) => c.Code == classCode);
		if (charclass == null)
		{
			throw new ArgumentException("Not a valid character class code!");
		}
		eplayer.WatchedAttributes.SetString("characterClass", charclass.Code);
		if (initializeGear)
		{
			EntityBehaviorPlayerInventory bh = eplayer.GetBehavior<EntityBehaviorPlayerInventory>();
			EntityShapeRenderer essr = capi?.World.Player.Entity.Properties.Client.Renderer as EntityShapeRenderer;
			bh.doReloadShapeAndSkin = false;
			IInventory inv = bh.Inventory;
			if (inv != null)
			{
				for (int i = 0; i < inv.Count && i < 12; i++)
				{
					inv[i].Itemstack = null;
				}
				JsonItemStack[] gear = charclass.Gear;
				foreach (JsonItemStack jstack in gear)
				{
					if (!jstack.Resolve(api.World, "character class gear", printWarningOnError: false))
					{
						api.World.Logger.Warning(string.Concat("Unable to resolve character class gear ", jstack.Type.ToString(), " with code ", jstack.Code, " item/block does not seem to exist. Will ignore."));
						continue;
					}
					ItemStack stack = jstack.ResolvedItemstack?.Clone();
					if (stack != null)
					{
						if (!Enum.TryParse<EnumCharacterDressType>(stack.ItemAttributes["clothescategory"].AsString(), ignoreCase: true, out var dresstype))
						{
							eplayer.TryGiveItemStack(stack);
							continue;
						}
						inv[(int)dresstype].Itemstack = stack;
						inv[(int)dresstype].MarkDirty();
					}
				}
				if (essr != null)
				{
					bh.doReloadShapeAndSkin = true;
					essr.TesselateShape();
				}
			}
		}
		applyTraitAttributes(eplayer);
	}

	private void applyTraitAttributes(EntityPlayer eplr)
	{
		string classcode = eplr.WatchedAttributes.GetString("characterClass");
		CharacterClass charclass = characterClasses.FirstOrDefault((CharacterClass c) => c.Code == classcode);
		if (charclass == null)
		{
			throw new ArgumentException("Not a valid character class code!");
		}
		foreach (KeyValuePair<string, EntityFloatStats> stats in eplr.Stats)
		{
			foreach (KeyValuePair<string, EntityStat<float>> statmod in stats.Value.ValuesByKey)
			{
				if (statmod.Key == "trait")
				{
					stats.Value.Remove(statmod.Key);
					break;
				}
			}
		}
		string[] extraTraits = eplr.WatchedAttributes.GetStringArray("extraTraits");
		IEnumerable<string> enumerable;
		if (extraTraits != null)
		{
			enumerable = charclass.Traits.Concat(extraTraits);
		}
		else
		{
			IEnumerable<string> enumerable2 = charclass.Traits;
			enumerable = enumerable2;
		}
		foreach (string traitcode in enumerable)
		{
			if (!TraitsByCode.TryGetValue(traitcode, out var trait))
			{
				continue;
			}
			foreach (KeyValuePair<string, double> val in trait.Attributes)
			{
				string attrcode = val.Key;
				double attrvalue = val.Value;
				eplr.Stats.Set(attrcode, "trait", (float)attrvalue, persistent: true);
			}
		}
		eplr.GetBehavior<EntityBehaviorHealth>()?.MarkDirty();
	}

	private TextCommandResult onCharSelCmd(TextCommandCallingArgs textCommandCallingArgs)
	{
		bool allowcharselonce = capi.World.Player.Entity.WatchedAttributes.GetBool("allowcharselonce") || capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative;
		if (createCharDlg == null && allowcharselonce)
		{
			createCharDlg = new GuiDialogCreateCharacter(capi, this);
			createCharDlg.PrepAndOpen();
		}
		else if (createCharDlg == null)
		{
			return TextCommandResult.Success(Lang.Get("You don't have permission to change you character and class. An admin needs to grant you allowcharselonce permission"));
		}
		if (!createCharDlg.IsOpened())
		{
			createCharDlg.TryOpen();
		}
		return TextCommandResult.Success();
	}

	private void onSelectedState(CharacterSelectedState p)
	{
		didSelect = p.DidSelect;
	}

	private void Event_PlayerJoin(IClientPlayer byPlayer)
	{
		if (!(byPlayer.PlayerUID == capi.World.Player.PlayerUID))
		{
			return;
		}
		if (!didSelect)
		{
			createCharDlg = new GuiDialogCreateCharacter(capi, this);
			createCharDlg.PrepAndOpen();
			createCharDlg.OnClosed += delegate
			{
				capi.PauseGame(paused: false);
			};
			capi.Event.EnqueueMainThreadTask(delegate
			{
				capi.PauseGame(paused: true);
			}, "pausegame");
			capi.Event.PushEvent("begincharacterselection");
		}
		else
		{
			capi.Event.PushEvent("skipcharacterselection");
		}
	}

	private bool Event_IsPlayerReady(ref EnumHandling handling)
	{
		if (didSelect)
		{
			return true;
		}
		handling = EnumHandling.PreventDefault;
		return false;
	}

	private bool Event_MatchesGridRecipe(IPlayer player, GridRecipe recipe, ItemSlot[] ingredients, int gridWidth)
	{
		if (recipe.RequiresTrait == null)
		{
			return true;
		}
		string classcode = player.Entity.WatchedAttributes.GetString("characterClass");
		if (classcode == null)
		{
			return true;
		}
		if (characterClassesByCode.TryGetValue(classcode, out var charclass))
		{
			if (charclass.Traits.Contains(recipe.RequiresTrait))
			{
				return true;
			}
			string[] extraTraits = player.Entity.WatchedAttributes.GetStringArray("extraTraits");
			if (extraTraits != null && extraTraits.Contains(recipe.RequiresTrait))
			{
				return true;
			}
		}
		return false;
	}

	public bool HasTrait(IPlayer player, string trait)
	{
		string classcode = player.Entity.WatchedAttributes.GetString("characterClass");
		if (classcode == null)
		{
			return true;
		}
		if (characterClassesByCode.TryGetValue(classcode, out var charclass))
		{
			if (charclass.Traits.Contains(trait))
			{
				return true;
			}
			string[] extraTraits = player.Entity.WatchedAttributes.GetStringArray("extraTraits");
			if (extraTraits != null && extraTraits.Contains(trait))
			{
				return true;
			}
		}
		return false;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Network.GetChannel("charselection").SetMessageHandler<CharacterSelectionPacket>(onCharacterSelection);
		api.Event.PlayerJoin += Event_PlayerJoinServer;
		api.Event.ServerRunPhase(EnumServerRunPhase.LoadGamePre, loadCharacterClasses);
	}

	private void Event_PlayerJoinServer(IServerPlayer byPlayer)
	{
		didSelect = SerializerUtil.Deserialize(byPlayer.GetModdata("createCharacter"), defaultValue: false);
		if (!didSelect)
		{
			setCharacterClass(byPlayer.Entity, characterClasses[0].Code, initializeGear: false);
		}
		double classChangeMonths = sapi.World.Config.GetDecimal("allowClassChangeAfterMonths", -1.0);
		if (sapi.World.Config.GetBool("allowOneFreeClassChange") && byPlayer.ServerData.LastCharacterSelectionDate == null)
		{
			byPlayer.Entity.WatchedAttributes.SetBool("allowcharselonce", value: true);
		}
		else if (classChangeMonths >= 0.0)
		{
			DateTime date = DateTime.UtcNow;
			string lastDateChange = byPlayer.ServerData.LastCharacterSelectionDate ?? byPlayer.ServerData.FirstJoinDate ?? "1/1/1970 00:00 AM";
			double monthsPassed = date.Subtract(DateTimeOffset.Parse(lastDateChange).UtcDateTime).TotalDays / 30.0;
			if (classChangeMonths < monthsPassed)
			{
				byPlayer.Entity.WatchedAttributes.SetBool("allowcharselonce", value: true);
			}
		}
		sapi.Network.GetChannel("charselection").SendPacket(new CharacterSelectedState
		{
			DidSelect = didSelect
		}, byPlayer);
	}

	public bool randomizeSkin(Entity entity, Dictionary<string, string> preSelection, bool playVoice = true)
	{
		if (preSelection == null)
		{
			preSelection = new Dictionary<string, string>();
		}
		EntityBehaviorExtraSkinnable skinMod = entity.GetBehavior<EntityBehaviorExtraSkinnable>();
		bool mustached = api.World.Rand.NextDouble() < 0.3;
		Dictionary<string, RandomizerConstraint> currentConstraints = new Dictionary<string, RandomizerConstraint>();
		SkinnablePart[] availableSkinParts = skinMod.AvailableSkinParts;
		foreach (SkinnablePart skinpart in availableSkinParts)
		{
			int index = api.World.Rand.Next(skinpart.Variants.Length);
			string variantCode = null;
			if (preSelection.TryGetValue(skinpart.Code, out variantCode))
			{
				index = skinpart.Variants.IndexOf((SkinnablePartVariant val) => val.Code == variantCode);
			}
			else
			{
				if (currentConstraints.TryGetValue(skinpart.Code, out var partConstraints))
				{
					variantCode = partConstraints.SelectRandom(api.World.Rand, skinpart.Variants);
					index = skinpart.Variants.IndexOf((SkinnablePartVariant val) => val.Code == variantCode);
				}
				if ((skinpart.Code == "mustache" || skinpart.Code == "beard") && !mustached)
				{
					index = 0;
					variantCode = "none";
				}
			}
			if (variantCode == null)
			{
				variantCode = skinpart.Variants[index].Code;
			}
			skinMod.selectSkinPart(skinpart.Code, variantCode, retesselateShape: true, playVoice);
			if (randomizerConstraints.Constraints.TryGetValue(skinpart.Code, out var partConstraintsGroup) && partConstraintsGroup.TryGetValue(variantCode, out var constraints))
			{
				foreach (KeyValuePair<string, RandomizerConstraint> val2 in constraints)
				{
					currentConstraints[val2.Key] = val2.Value;
				}
			}
			if (skinpart.Code == "voicetype" && variantCode == "high")
			{
				mustached = false;
			}
		}
		return true;
	}

	private void onCharacterSelection(IServerPlayer fromPlayer, CharacterSelectionPacket p)
	{
		bool didSelectBefore = fromPlayer.GetModData("createCharacter", defaultValue: false);
		if (didSelectBefore && !fromPlayer.Entity.WatchedAttributes.GetBool("allowcharselonce") && fromPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			fromPlayer.Entity.WatchedAttributes.MarkPathDirty("skinConfig");
			fromPlayer.BroadcastPlayerData(sendInventory: true);
			return;
		}
		if (p.DidSelect)
		{
			fromPlayer.SetModData("createCharacter", data: true);
			setCharacterClass(fromPlayer.Entity, p.CharacterClass, !didSelectBefore || fromPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative);
			EntityBehaviorExtraSkinnable bh = fromPlayer.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
			bh.ApplyVoice(p.VoiceType, p.VoicePitch, testTalk: false);
			foreach (KeyValuePair<string, string> skinpart in p.SkinParts)
			{
				bh.selectSkinPart(skinpart.Key, skinpart.Value, retesselateShape: false);
			}
			DateTime date = DateTime.UtcNow;
			fromPlayer.ServerData.LastCharacterSelectionDate = date.ToShortDateString() + " " + date.ToShortTimeString();
			bool allowOneFreeClassChange = sapi.World.Config.GetBool("allowOneFreeClassChange");
			if (!didSelectBefore && allowOneFreeClassChange)
			{
				fromPlayer.ServerData.LastCharacterSelectionDate = null;
			}
			else
			{
				fromPlayer.Entity.WatchedAttributes.RemoveAttribute("allowcharselonce");
			}
		}
		fromPlayer.Entity.WatchedAttributes.MarkPathDirty("skinConfig");
		fromPlayer.BroadcastPlayerData(sendInventory: true);
	}

	internal void ClientSelectionDone(IInventory characterInv, string characterClass, bool didSelect)
	{
		List<ClothStack> clothesPacket = new List<ClothStack>();
		for (int i = 0; i < characterInv.Count; i++)
		{
			ItemSlot slot = characterInv[i];
			if (slot.Itemstack != null)
			{
				clothesPacket.Add(new ClothStack
				{
					Code = slot.Itemstack.Collectible.Code.ToShortString(),
					SlotNum = i,
					Class = slot.Itemstack.Class
				});
			}
		}
		Dictionary<string, string> skinParts = new Dictionary<string, string>();
		EntityBehaviorExtraSkinnable bh = capi.World.Player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
		foreach (AppliedSkinnablePartVariant val in bh.AppliedSkinParts)
		{
			skinParts[val.PartCode] = val.Code;
		}
		if (didSelect)
		{
			storePreviousSelection(skinParts);
		}
		capi.Network.GetChannel("charselection").SendPacket(new CharacterSelectionPacket
		{
			Clothes = clothesPacket.ToArray(),
			DidSelect = didSelect,
			SkinParts = skinParts,
			CharacterClass = characterClass,
			VoicePitch = bh.VoicePitch,
			VoiceType = bh.VoiceType
		});
		capi.Network.SendPlayerNowReady();
		createCharDlg = null;
		capi.Event.PushEvent("finishcharacterselection");
	}

	public Dictionary<string, string> getPreviousSelection()
	{
		Dictionary<string, string> lastSelection = new Dictionary<string, string>();
		if (capi == null || !capi.Settings.String.Exists("lastSkinSelection"))
		{
			return lastSelection;
		}
		string[] array = capi.Settings.String["lastSkinSelection"].Split(",");
		for (int i = 0; i < array.Length; i++)
		{
			string[] keyval = array[i].Split(":");
			lastSelection[keyval[0]] = keyval[1];
		}
		return lastSelection;
	}

	public void storePreviousSelection(Dictionary<string, string> selection)
	{
		List<string> parts = new List<string>();
		foreach (KeyValuePair<string, string> val in selection)
		{
			parts.Add(val.Key + ":" + val.Value);
		}
		capi.Settings.String["lastSkinSelection"] = string.Join(",", parts);
	}
}
