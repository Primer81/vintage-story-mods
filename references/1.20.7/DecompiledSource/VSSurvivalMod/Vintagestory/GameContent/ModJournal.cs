using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModJournal : ModSystem
{
	private ICoreServerAPI sapi;

	private Dictionary<string, Journal> journalsByPlayerUid = new Dictionary<string, Journal>();

	private Dictionary<string, Dictionary<string, LoreDiscovery>> loreDiscoveryiesByPlayerUid = new Dictionary<string, Dictionary<string, LoreDiscovery>>();

	private Dictionary<string, JournalAsset> journalAssetsByCode;

	private IServerNetworkChannel serverChannel;

	private ICoreClientAPI capi;

	private IClientNetworkChannel clientChannel;

	private Journal ownJournal = new Journal();

	private GuiDialogJournal dialog;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		capi.Input.RegisterHotKey("journal", Lang.Get("Journal"), GlKeys.J);
		capi.Input.SetHotKeyHandler("journal", OnHotkeyJournal);
		clientChannel = api.Network.RegisterChannel("journal").RegisterMessageType(typeof(JournalEntry)).RegisterMessageType(typeof(Journal))
			.RegisterMessageType(typeof(JournalChapter))
			.SetMessageHandler<Journal>(OnJournalItemsReceived)
			.SetMessageHandler<JournalEntry>(OnJournalItemReceived)
			.SetMessageHandler<JournalChapter>(OnJournalPieceReceived);
	}

	private bool OnHotkeyJournal(KeyCombination comb)
	{
		if (dialog != null)
		{
			dialog.TryClose();
			dialog = null;
			return true;
		}
		dialog = new GuiDialogJournal(ownJournal.Entries, capi);
		dialog.TryOpen();
		dialog.OnClosed += delegate
		{
			dialog = null;
		};
		return true;
	}

	private void OnJournalPieceReceived(JournalChapter entryPiece)
	{
		ownJournal.Entries[entryPiece.EntryId].Chapters.Add(entryPiece);
	}

	private void OnJournalItemReceived(JournalEntry entry)
	{
		if (entry.EntryId >= ownJournal.Entries.Count)
		{
			ownJournal.Entries.Add(entry);
		}
		else
		{
			ownJournal.Entries[entry.EntryId] = entry;
		}
	}

	private void OnJournalItemsReceived(Journal fullJournal)
	{
		ownJournal = fullJournal;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Event.PlayerJoin += OnPlayerJoin;
		api.Event.SaveGameLoaded += OnSaveGameLoaded;
		api.Event.GameWorldSave += OnGameGettingSaved;
		serverChannel = api.Network.RegisterChannel("journal").RegisterMessageType(typeof(JournalEntry)).RegisterMessageType(typeof(Journal))
			.RegisterMessageType(typeof(JournalChapter));
		api.Event.RegisterEventBusListener(OnLoreDiscovery, 0.5, "loreDiscovery");
	}

	private void OnGameGettingSaved()
	{
		sapi.WorldManager.SaveGame.StoreData("journalItemsByPlayerUid", SerializerUtil.Serialize(journalsByPlayerUid));
		sapi.WorldManager.SaveGame.StoreData("loreDiscoveriesByPlayerUid", SerializerUtil.Serialize(loreDiscoveryiesByPlayerUid));
	}

	private void OnSaveGameLoaded()
	{
		try
		{
			byte[] data2 = sapi.WorldManager.SaveGame.GetData("journalItemsByPlayerUid");
			if (GameVersion.IsLowerVersionThan(sapi.WorldManager.SaveGame.LastSavedGameVersion, "1.14-pre.1"))
			{
				if (data2 != null)
				{
					journalsByPlayerUid = upgrade(SerializerUtil.Deserialize<Dictionary<string, JournalOld>>(data2));
				}
				sapi.World.Logger.Notification("Upgraded journalItemsByPlayerUid from v1.13 format to v1.14 format");
			}
			else if (data2 != null)
			{
				journalsByPlayerUid = SerializerUtil.Deserialize<Dictionary<string, Journal>>(data2);
			}
		}
		catch (Exception e2)
		{
			sapi.World.Logger.Error("Failed loading journalItemsByPlayerUid. Resetting.");
			sapi.World.Logger.Error(e2);
		}
		if (journalsByPlayerUid == null)
		{
			journalsByPlayerUid = new Dictionary<string, Journal>();
		}
		try
		{
			byte[] data = sapi.WorldManager.SaveGame.GetData("loreDiscoveriesByPlayerUid");
			if (data != null)
			{
				loreDiscoveryiesByPlayerUid = SerializerUtil.Deserialize<Dictionary<string, Dictionary<string, LoreDiscovery>>>(data);
			}
		}
		catch (Exception e)
		{
			sapi.World.Logger.Error("Failed loading loreDiscoveryiesByPlayerUid. Resetting. Exception: {0}", e);
		}
		if (loreDiscoveryiesByPlayerUid == null)
		{
			loreDiscoveryiesByPlayerUid = new Dictionary<string, Dictionary<string, LoreDiscovery>>();
		}
	}

	private Dictionary<string, Journal> upgrade(Dictionary<string, JournalOld> dict)
	{
		Dictionary<string, Journal> newdict = new Dictionary<string, Journal>();
		foreach (KeyValuePair<string, JournalOld> val in dict)
		{
			List<JournalEntry> entries = new List<JournalEntry>();
			foreach (JournalEntryOld entry in val.Value.Entries)
			{
				List<JournalChapter> chapters = new List<JournalChapter>();
				foreach (JournalChapterOld chapter in entry.Chapters)
				{
					chapters.Add(new JournalChapter
					{
						Text = chapter.Text,
						EntryId = chapter.EntryId
					});
				}
				entries.Add(new JournalEntry
				{
					Chapters = chapters,
					Editable = entry.Editable,
					EntryId = entry.EntryId,
					LoreCode = entry.LoreCode,
					Title = entry.Title
				});
			}
			newdict[val.Key] = new Journal
			{
				Entries = entries
			};
		}
		return newdict;
	}

	private void OnPlayerJoin(IServerPlayer byPlayer)
	{
		if (journalsByPlayerUid.TryGetValue(byPlayer.PlayerUID, out var journal))
		{
			serverChannel.SendPacket(journal, byPlayer);
		}
	}

	public void AddOrUpdateJournalEntry(IServerPlayer forPlayer, JournalEntry entry)
	{
		if (!journalsByPlayerUid.TryGetValue(forPlayer.PlayerUID, out var journal))
		{
			journal = (journalsByPlayerUid[forPlayer.PlayerUID] = new Journal());
		}
		for (int i = 0; i < journal.Entries.Count; i++)
		{
			if (journal.Entries[i].LoreCode == entry.LoreCode)
			{
				journal.Entries[i] = entry;
				serverChannel.SendPacket(entry, forPlayer);
				return;
			}
		}
		journal.Entries.Add(entry);
		serverChannel.SendPacket(entry, forPlayer);
	}

	private void OnLoreDiscovery(string eventName, ref EnumHandling handling, IAttribute data)
	{
		TreeAttribute obj = data as TreeAttribute;
		string playerUid = obj.GetString("playeruid");
		string category = obj.GetString("category");
		IServerPlayer plr = sapi.World.PlayerByUid(playerUid) as IServerPlayer;
		ItemSlot itemslot = plr.InventoryManager.ActiveHotbarSlot;
		string journalEntryCode = itemslot.Itemstack.Attributes.GetString("discoveryCode");
		LoreDiscovery discovery;
		if (journalEntryCode != null)
		{
			int[] chapters = (itemslot.Itemstack.Attributes["chapterIds"] as IntArrayAttribute).value;
			discovery = new LoreDiscovery
			{
				Code = journalEntryCode,
				ChapterIds = new List<int>(chapters)
			};
		}
		else
		{
			discovery = getRandomLoreDiscovery(sapi.World, plr, category);
		}
		if (discovery == null)
		{
			if (journalEntryCode == null)
			{
				plr.SendIngameError("alreadydiscovered", Lang.Get("Nothing new in these pages"));
			}
		}
		else if (TryDiscoverLore(discovery, plr, itemslot))
		{
			itemslot.MarkDirty();
			plr.Entity.World.PlaySoundAt(new AssetLocation("sounds/effect/writing"), plr.Entity);
			handling = EnumHandling.PreventDefault;
		}
	}

	public bool TryDiscoverLore(LoreDiscovery newdiscovery, IServerPlayer plr, ItemSlot slot = null)
	{
		string playerUid = plr.PlayerUID;
		if (!journalsByPlayerUid.TryGetValue(playerUid, out var journal))
		{
			journal = (journalsByPlayerUid[playerUid] = new Journal());
		}
		JournalEntry entry = null;
		ensureJournalAssetsLoaded();
		JournalAsset asset = journalAssetsByCode[newdiscovery.Code];
		for (int j = 0; j < journal.Entries.Count; j++)
		{
			if (journal.Entries[j].LoreCode == newdiscovery.Code)
			{
				entry = journal.Entries[j];
				break;
			}
		}
		bool isNew = false;
		if (entry == null)
		{
			List<JournalEntry> entries = journal.Entries;
			JournalEntry obj = new JournalEntry
			{
				Editable = false,
				Title = asset.Title,
				LoreCode = newdiscovery.Code,
				EntryId = journal.Entries.Count
			};
			entry = obj;
			entries.Add(obj);
			isNew = true;
		}
		bool anyAdded = false;
		loreDiscoveryiesByPlayerUid.TryGetValue(plr.PlayerUID, out var discoveredLore);
		if (discoveredLore == null)
		{
			discoveredLore = (loreDiscoveryiesByPlayerUid[plr.PlayerUID] = new Dictionary<string, LoreDiscovery>());
		}
		if (discoveredLore.TryGetValue(asset.Code, out var olddisc))
		{
			foreach (int newChapterId in newdiscovery.ChapterIds)
			{
				if (!olddisc.ChapterIds.Contains(newChapterId))
				{
					olddisc.ChapterIds.Add(newChapterId);
					anyAdded = true;
				}
			}
		}
		else
		{
			discoveredLore[asset.Code] = newdiscovery;
			anyAdded = true;
		}
		if (!anyAdded)
		{
			return false;
		}
		int partnum = 0;
		int partcount = asset.Pieces.Length;
		for (int i = 0; i < newdiscovery.ChapterIds.Count; i++)
		{
			JournalChapter chapter = new JournalChapter
			{
				Text = asset.Pieces[newdiscovery.ChapterIds[i]],
				EntryId = entry.EntryId,
				ChapterId = newdiscovery.ChapterIds[i]
			};
			entry.Chapters.Add(chapter);
			if (!isNew)
			{
				serverChannel.SendPacket(chapter, plr);
			}
			partnum = newdiscovery.ChapterIds[i];
		}
		if (slot != null)
		{
			slot.Itemstack.Attributes.SetString("discoveryCode", newdiscovery.Code);
			slot.Itemstack.Attributes["chapterIds"] = new IntArrayAttribute(newdiscovery.ChapterIds.ToArray());
			slot.Itemstack.Attributes["textCodes"] = new StringArrayAttribute(newdiscovery.ChapterIds.Select((int id) => asset.Pieces[id]).ToArray());
			slot.Itemstack.Attributes.SetString("titleCode", entry.Title);
			slot.MarkDirty();
		}
		if (isNew)
		{
			serverChannel.SendPacket(entry, plr);
		}
		sapi.SendIngameDiscovery(plr, "lore-" + newdiscovery.Code, null, partnum + 1, partcount);
		sapi.World.PlaySoundAt(new AssetLocation("sounds/effect/deepbell"), plr.Entity, null, randomizePitch: false, 5f, 0.5f);
		return true;
	}

	protected TextCommandResult DiscoverEverything(TextCommandCallingArgs args)
	{
		IServerPlayer plr = args.Caller.Player as IServerPlayer;
		JournalAsset[] array = sapi.World.AssetManager.GetMany<JournalAsset>(sapi.World.Logger, "config/lore/").Values.ToArray();
		if (!journalsByPlayerUid.TryGetValue(plr.PlayerUID, out var journal))
		{
			journal = (journalsByPlayerUid[plr.PlayerUID] = new Journal());
		}
		journal.Entries.Clear();
		JournalAsset[] array2 = array;
		foreach (JournalAsset val in array2)
		{
			JournalEntry entry = null;
			journal.Entries.Add(entry = new JournalEntry
			{
				Editable = false,
				Title = val.Title,
				LoreCode = val.Code,
				EntryId = journal.Entries.Count
			});
			serverChannel.SendPacket(entry, plr);
			string[] pieces = val.Pieces;
			foreach (string part in pieces)
			{
				JournalChapter piece = new JournalChapter
				{
					Text = part,
					EntryId = entry.EntryId
				};
				entry.Chapters.Add(piece);
				serverChannel.SendPacket(piece, plr);
			}
		}
		return TextCommandResult.Success("All lore added");
	}

	private LoreDiscovery getRandomLoreDiscovery(IWorldAccessor world, IPlayer serverplayer, string category)
	{
		ensureJournalAssetsLoaded();
		JournalAsset[] journalAssets = journalAssetsByCode.Values.ToArray();
		journalAssets.Shuffle(world.Rand);
		foreach (JournalAsset journalAsset in journalAssets)
		{
			if (category == null || !(journalAsset.Category != category))
			{
				LoreDiscovery dis = getNextUndiscoveredChapter(serverplayer, journalAsset);
				if (dis != null)
				{
					return dis;
				}
			}
		}
		return null;
	}

	private LoreDiscovery getNextUndiscoveredChapter(IPlayer plr, JournalAsset asset)
	{
		loreDiscoveryiesByPlayerUid.TryGetValue(plr.PlayerUID, out var discoveredLore);
		if (discoveredLore == null)
		{
			discoveredLore = (loreDiscoveryiesByPlayerUid[plr.PlayerUID] = new Dictionary<string, LoreDiscovery>());
		}
		if (!discoveredLore.ContainsKey(asset.Code))
		{
			return new LoreDiscovery
			{
				Code = asset.Code,
				ChapterIds = new List<int> { 0 }
			};
		}
		LoreDiscovery ld = discoveredLore[asset.Code];
		for (int p = 0; p < asset.Pieces.Length; p++)
		{
			if (!ld.ChapterIds.Contains(p))
			{
				return new LoreDiscovery
				{
					ChapterIds = new List<int> { p },
					Code = ld.Code
				};
			}
		}
		return null;
	}

	private void ensureJournalAssetsLoaded()
	{
		if (journalAssetsByCode == null)
		{
			journalAssetsByCode = new Dictionary<string, JournalAsset>();
			JournalAsset[] array = sapi.World.AssetManager.GetMany<JournalAsset>(sapi.World.Logger, "config/lore/").Values.ToArray();
			foreach (JournalAsset asset in array)
			{
				journalAssetsByCode[asset.Code] = asset;
			}
		}
	}

	public bool DidDiscoverLore(string playerUid, string code, int chapterId)
	{
		if (!journalsByPlayerUid.TryGetValue(playerUid, out var journal))
		{
			return false;
		}
		for (int i = 0; i < journal.Entries.Count; i++)
		{
			if (!(journal.Entries[i].LoreCode == code))
			{
				continue;
			}
			JournalEntry entry = journal.Entries[i];
			for (int j = 0; j < entry.Chapters.Count; j++)
			{
				if (entry.Chapters[j].ChapterId == chapterId)
				{
					return true;
				}
			}
			break;
		}
		return false;
	}
}
