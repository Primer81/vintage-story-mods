using System.Collections.Generic;

namespace Vintagestory.API.Common;

public class AssetCategory
{
	public static Dictionary<string, AssetCategory> categories = new Dictionary<string, AssetCategory>(14);

	public static AssetCategory blocktypes = new AssetCategory("blocktypes", AffectsGameplay: true, EnumAppSide.Server);

	public static AssetCategory itemtypes = new AssetCategory("itemtypes", AffectsGameplay: true, EnumAppSide.Server);

	public static AssetCategory lang = new AssetCategory("lang", AffectsGameplay: false, EnumAppSide.Universal);

	public static AssetCategory patches = new AssetCategory("patches", AffectsGameplay: false, EnumAppSide.Universal);

	public static AssetCategory config = new AssetCategory("config", AffectsGameplay: false, EnumAppSide.Universal);

	public static AssetCategory worldproperties = new AssetCategory("worldproperties", AffectsGameplay: true, EnumAppSide.Universal);

	public static AssetCategory sounds = new AssetCategory("sounds", AffectsGameplay: false, EnumAppSide.Universal);

	public static AssetCategory shapes = new AssetCategory("shapes", AffectsGameplay: false, EnumAppSide.Universal);

	public static AssetCategory shaders = new AssetCategory("shaders", AffectsGameplay: false, EnumAppSide.Client);

	public static AssetCategory shaderincludes = new AssetCategory("shaderincludes", AffectsGameplay: false, EnumAppSide.Client);

	public static AssetCategory textures = new AssetCategory("textures", AffectsGameplay: false, EnumAppSide.Universal);

	public static AssetCategory music = new AssetCategory("music", AffectsGameplay: false, EnumAppSide.Client);

	public static AssetCategory dialog = new AssetCategory("dialog", AffectsGameplay: false, EnumAppSide.Client);

	public static AssetCategory recipes = new AssetCategory("recipes", AffectsGameplay: true, EnumAppSide.Server);

	public static AssetCategory worldgen = new AssetCategory("worldgen", AffectsGameplay: true, EnumAppSide.Server);

	public static AssetCategory entities = new AssetCategory("entities", AffectsGameplay: true, EnumAppSide.Server);

	/// <summary>
	/// Path and name
	/// </summary>
	public string Code { get; private set; }

	/// <summary>
	/// Determines wether it will be used on server, client or both.
	/// </summary>
	public EnumAppSide SideType { get; private set; }

	/// <summary>
	/// Temporary solution to not change block types. Will be changed
	/// </summary>
	public bool AffectsGameplay { get; private set; }

	public AssetCategory(string code, bool AffectsGameplay, EnumAppSide SideType)
	{
		categories[code] = this;
		Code = code;
		this.AffectsGameplay = AffectsGameplay;
		this.SideType = SideType;
	}

	public override string ToString()
	{
		return Code;
	}

	/// <summary>
	/// Gets the asset category by code name
	/// </summary>
	/// <param name="code">The code name for the asset category.</param>
	/// <returns>An asset category.</returns>
	public static AssetCategory FromCode(string code)
	{
		if (!categories.ContainsKey(code))
		{
			return null;
		}
		return categories[code];
	}
}
