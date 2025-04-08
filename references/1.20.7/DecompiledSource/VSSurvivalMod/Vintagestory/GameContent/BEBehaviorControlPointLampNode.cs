using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class BEBehaviorControlPointLampNode : BEBehaviorShapeFromAttributes, INetworkedLight
{
	private ModSystemControlPoints modSys;

	protected string networkCode;

	public BEBehaviorControlPointLampNode(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public void setNetwork(string networkCode)
	{
		this.networkCode = networkCode;
		registerToControlPoint(networkCode);
		Blockentity.MarkDirty(redrawOnClient: true);
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		registerToControlPoint(null);
	}

	private void registerToControlPoint(string previousNetwork)
	{
		if (Api.Side == EnumAppSide.Server)
		{
			modSys = Api.ModLoader.GetModSystem<ModSystemControlPoints>();
			if (previousNetwork != null)
			{
				modSys[AssetLocation.Create(networkCode, base.Block.Code.Domain)].Activate -= BEBehaviorControlPointLampNode_Activate;
			}
			if (networkCode != null)
			{
				AssetLocation controlpointcode = AssetLocation.Create(networkCode, base.Block.Code.Domain);
				modSys[controlpointcode].Activate += BEBehaviorControlPointLampNode_Activate;
				BEBehaviorControlPointLampNode_Activate(modSys[controlpointcode]);
			}
		}
	}

	private void BEBehaviorControlPointLampNode_Activate(ControlPoint cpoint)
	{
		if (cpoint.ControlData != null)
		{
			string newState = (((bool)cpoint.ControlData) ? "on/" : "off/");
			string newType = Type.Replace("off/", newState).Replace("on/", newState);
			if (Type != newType)
			{
				string oldType = Type;
				Type = newType;
				Blockentity.MarkDirty(redrawOnClient: true);
				loadMesh();
				relight(oldType);
			}
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		networkCode = tree.GetString("networkCode");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetString("networkCode", networkCode);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		if (Api is ICoreClientAPI capi && capi.Settings.Bool["extendedDebugInfo"])
		{
			dsc.AppendLine("network code: " + networkCode);
		}
	}

	public override void OnPlacementBySchematic(ICoreServerAPI api, IBlockAccessor blockAccessor, BlockPos pos, Dictionary<int, Dictionary<int, int>> replaceBlocks, int centerrockblockid, Block layerBlock, bool resolveImports)
	{
		IShapeTypeProps obj = (base.Block as BlockShapeFromAttributes)?.GetTypeProps(Type, null, this);
		byte? obj2;
		if (obj == null)
		{
			obj2 = null;
		}
		else
		{
			byte[] lightHsv = obj.LightHsv;
			obj2 = ((lightHsv != null) ? new byte?(lightHsv[2]) : null);
		}
		if (obj2 > 0 && blockAccessor is IWorldGenBlockAccessor wgen)
		{
			wgen.ScheduleBlockLightUpdate(pos, 0, base.Block.BlockId);
		}
	}
}
