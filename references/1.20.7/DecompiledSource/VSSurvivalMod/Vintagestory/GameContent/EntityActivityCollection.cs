using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class EntityActivityCollection
{
	[JsonProperty]
	public string Name;

	[JsonProperty]
	public List<EntityActivity> Activities = new List<EntityActivity>();

	private EntityActivitySystem vas;

	public EntityActivityCollection()
	{
	}

	public EntityActivityCollection(EntityActivitySystem vas)
	{
		this.vas = vas;
	}

	public EntityActivityCollection Clone()
	{
		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.All
		};
		return JsonUtil.ToObject<EntityActivityCollection>(JsonConvert.SerializeObject(this, Formatting.Indented, settings), "", settings);
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
		foreach (EntityActivity activity in Activities)
		{
			activity.OnLoaded(vas);
		}
	}
}
