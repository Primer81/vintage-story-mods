using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class FruitingSystem : ModSystem
{
	private ICoreClientAPI capi;

	private ICoreServerAPI sapi;

	public ICoreAPI Api;

	public FruitRendererSystem Renderer;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		Api = api;
		_ = api.World is IClientWorldAccessor;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		capi = api;
		api.Event.BlockTexturesLoaded += onLoaded;
		api.Event.LeaveWorld += delegate
		{
			Renderer?.Dispose();
		};
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		base.StartServerSide(api);
	}

	private void onLoaded()
	{
		Renderer = new FruitRendererSystem(capi);
	}

	public override void Dispose()
	{
		base.Dispose();
		Renderer?.Dispose();
	}

	public void AddFruit(AssetLocation code, Vec3d position, FruitData data)
	{
		if (Api.Side == EnumAppSide.Client)
		{
			Item fruit = Api.World.GetItem(code);
			if (fruit != null)
			{
				Renderer.AddFruit(fruit, position, data);
			}
		}
	}

	public void RemoveFruit(string fruitCode, Vec3d position)
	{
		if (Api.Side == EnumAppSide.Client)
		{
			Item fruit = Api.World.GetItem(new AssetLocation(fruitCode));
			if (fruit != null)
			{
				Renderer.RemoveFruit(fruit, position);
			}
		}
	}
}
