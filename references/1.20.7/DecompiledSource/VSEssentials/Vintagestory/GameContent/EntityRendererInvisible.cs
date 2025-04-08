using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class EntityRendererInvisible : EntityRenderer
{
	public EntityRendererInvisible(Entity entity, ICoreClientAPI api)
		: base(entity, api)
	{
	}

	public override void Dispose()
	{
	}
}
