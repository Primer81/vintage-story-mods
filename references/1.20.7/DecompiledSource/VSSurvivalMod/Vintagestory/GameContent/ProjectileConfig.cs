using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ProjectileConfig
{
	public AssetLocation Code;

	public NatFloat Quantity;

	public float Damage;

	public EnumDamageType DamageType;

	public JsonItemStack CollectibleStack;

	public EntityProperties EntityType;

	public int LeftToFire;
}
