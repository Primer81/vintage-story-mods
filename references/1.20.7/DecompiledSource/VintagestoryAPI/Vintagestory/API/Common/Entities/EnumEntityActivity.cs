using System;

namespace Vintagestory.API.Common.Entities;

/// <summary>
/// A list of activities that an entity can perform.
/// </summary>
[DocumentAsJson]
[Flags]
public enum EnumEntityActivity
{
	None = 0,
	Idle = 1,
	Move = 2,
	SprintMode = 4,
	SneakMode = 8,
	Fly = 0x10,
	Swim = 0x20,
	Jump = 0x40,
	Fall = 0x80,
	Climb = 0x100,
	FloorSitting = 0x200,
	Dead = 0x400,
	Break = 0x800,
	Place = 0x1000,
	Glide = 0x2000,
	Mounted = 0x4000
}
