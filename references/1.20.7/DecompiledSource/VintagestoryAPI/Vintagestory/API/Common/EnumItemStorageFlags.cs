using System;

namespace Vintagestory.API.Common;

/// <summary>
/// Determines the kinds of storage types the item can be put into
/// </summary>
[Flags]
public enum EnumItemStorageFlags
{
	/// <summary>
	/// Of no particular type
	/// </summary>
	General = 1,
	/// <summary>
	/// The item can be placed into a backpack slot
	/// </summary>
	Backpack = 2,
	/// <summary>
	/// The item can be placed in a slot related to mining or smithing
	/// </summary>
	Metallurgy = 4,
	/// <summary>
	/// The item can be placed in a slot related to jewelcrafting
	/// </summary>
	Jewellery = 8,
	/// <summary>
	/// The item can be placed in a slot related to alchemy
	/// </summary>
	Alchemy = 0x10,
	/// <summary>
	/// The item can be placed in a slot related to farming
	/// </summary>
	Agriculture = 0x20,
	/// <summary>
	/// Moneys
	/// </summary>
	Currency = 0x40,
	/// <summary>
	/// Clothes, Armor and Accessories
	/// </summary>
	Outfit = 0x80,
	/// <summary>
	/// Off hand slot
	/// </summary>
	Offhand = 0x100,
	/// <summary>
	/// Arrows
	/// </summary>
	Arrow = 0x200,
	/// <summary>
	/// Skill slot
	/// </summary>
	Skill = 0x400,
	/// <summary>
	/// Custom storage flag for mods
	/// </summary>
	Custom1 = 0x800,
	/// <summary>
	/// Custom storage flag for mods
	/// </summary>
	Custom2 = 0x1000,
	/// <summary>
	/// Custom storage flag for mods
	/// </summary>
	Custom3 = 0x2000,
	/// <summary>
	/// Custom storage flag for mods
	/// </summary>
	Custom4 = 0x4000,
	/// <summary>
	/// Custom storage flag for mods
	/// </summary>
	Custom5 = 0x8000,
	/// <summary>
	/// Custom storage flag for mods
	/// </summary>
	Custom6 = 0x10000,
	/// <summary>
	/// Custom storage flag for mods
	/// </summary>
	Custom7 = 0x20000,
	/// <summary>
	/// Custom storage flag for mods
	/// </summary>
	Custom8 = 0x40000,
	/// <summary>
	/// Custom storage flag for mods
	/// </summary>
	Custom9 = 0x80000,
	/// <summary>
	/// Custom storage flag for mods
	/// </summary>
	Custom10 = 0x100000
}
