using System;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.ServerMods.NoObf;

public class JsonPatch
{
	public EnumJsonPatchOp Op;

	public AssetLocation File;

	public string FromPath;

	public string Path;

	public PatchModDependence[] DependsOn;

	public bool Enabled = true;

	public EnumAppSide? Side = EnumAppSide.Universal;

	public PatchCondition Condition;

	[JsonProperty]
	[JsonConverter(typeof(JsonAttributesConverter))]
	public JsonObject Value;

	[Obsolete("Use Side instead")]
	public EnumAppSide? SideType
	{
		get
		{
			return Side;
		}
		set
		{
			Side = value;
		}
	}
}
