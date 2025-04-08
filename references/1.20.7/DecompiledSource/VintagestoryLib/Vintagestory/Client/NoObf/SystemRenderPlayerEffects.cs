using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemRenderPlayerEffects : ClientSystem
{
	private Vec4d inval = new Vec4d();

	private Vec4d outval = new Vec4d();

	private Vec3f outval3 = new Vec3f();

	private int maxDynLights;

	public override string Name => "reple";

	public SystemRenderPlayerEffects(ClientMain game)
		: base(game)
	{
		game.eventManager.RegisterRenderer(onBeforeRender, EnumRenderStage.Before, Name, 0.1);
		maxDynLights = ClientSettings.MaxDynamicLights;
		ClientSettings.Inst.AddWatcher("maxDynamicLights", delegate(int value)
		{
			maxDynLights = value;
		});
	}

	public override void OnOwnPlayerDataReceived()
	{
		game.api.Render.PerceptionEffects.OnOwnPlayerDataReceived(game.player.Entity);
	}

	private void onBeforeRender(float dt)
	{
		game.shUniforms.PointLightsCount = 0;
		Vec3d plrPos = game.EntityPlayer.Pos.XYZ;
		Entity[] ents = game.GetEntitiesAround(plrPos, 60f, 60f, (Entity e) => e.LightHsv != null && e.LightHsv[2] > 0);
		if (ents.Length > maxDynLights)
		{
			ents = ents.OrderBy((Entity e) => e.Pos.SquareDistanceTo(plrPos)).ToArray();
		}
		foreach (Entity entity in ents)
		{
			byte[] lightHsv = entity.LightHsv;
			AddPointLight(lightHsv, entity.Pos);
		}
		for (int i = 0; i < game.pointlights.Count; i++)
		{
			IPointLight pl = game.pointlights[i];
			AddPointLight(pl.Color, pl.Pos);
		}
		game.api.Render.PerceptionEffects.OnBeforeGameRender(dt);
	}

	private void AddPointLight(byte[] lighthsv, EntityPos pos)
	{
		int cnt = game.shUniforms.PointLightsCount;
		if (cnt < maxDynLights)
		{
			inval.Set(pos.X, pos.Y, pos.Z, 1.0);
			Mat4d.MulWithVec4(game.CurrentModelViewMatrixd, inval, outval);
			outval.W = (int)lighthsv[2];
			game.shUniforms.PointLights3[3 * cnt] = (float)outval.X;
			game.shUniforms.PointLights3[3 * cnt + 1] = (float)outval.Y;
			game.shUniforms.PointLights3[3 * cnt + 2] = (float)outval.Z;
			byte h = game.WorldMap.hueLevels[lighthsv[0]];
			int blocks = game.WorldMap.satLevels[lighthsv[1]];
			int blockv = (int)(game.WorldMap.BlockLightLevels[lighthsv[2]] * 255f);
			ColorUtil.ToRGBVec3f(ColorUtil.HsvToRgba(h, blocks, blockv), ref outval3);
			game.shUniforms.PointLightColors3[3 * cnt] = outval3.Z;
			game.shUniforms.PointLightColors3[3 * cnt + 1] = outval3.Y;
			game.shUniforms.PointLightColors3[3 * cnt + 2] = outval3.X;
			game.shUniforms.PointLightsCount++;
		}
	}

	private void AddPointLight(Vec3f color, Vec3d pos)
	{
		int cnt = game.shUniforms.PointLightsCount;
		if (cnt < maxDynLights)
		{
			inval.Set(pos.X, pos.Y, pos.Z, 1.0);
			Mat4d.MulWithVec4(game.CurrentModelViewMatrixd, inval, outval);
			game.shUniforms.PointLights3[3 * cnt] = (float)outval.X;
			game.shUniforms.PointLights3[3 * cnt + 1] = (float)outval.Y;
			game.shUniforms.PointLights3[3 * cnt + 2] = (float)outval.Z;
			game.shUniforms.PointLightColors3[3 * cnt] = color.Z;
			game.shUniforms.PointLightColors3[3 * cnt + 1] = color.Y;
			game.shUniforms.PointLightColors3[3 * cnt + 2] = color.X;
			game.shUniforms.PointLightsCount++;
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}
}
