using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public class JsonAndLiquidTesselator : IBlockTesselator
{
	private IBlockTesselator liquid;

	private IBlockTesselator json;

	public JsonAndLiquidTesselator(ChunkTesselator tct)
	{
		liquid = new LiquidTesselator(tct);
		json = new JsonTesselator();
	}

	public void Tesselate(TCTCache vars)
	{
		float saveRandomOffetX = vars.finalX;
		float saveRandomOffetZ = vars.finalZ;
		vars.finalX = vars.lx;
		vars.finalZ = vars.lz;
		vars.RenderPass = EnumChunkRenderPass.Liquid;
		byte waterMapIndex = (byte)(vars.tct.game.ColorMaps.IndexOfKey("climateWaterTint") + 1);
		ColorMapData prevColorMapData = vars.ColorMapData;
		int prevFlags = vars.VertexFlags;
		vars.ColorMapData = new ColorMapData((byte)0, waterMapIndex, prevColorMapData.Temperature, prevColorMapData.Rainfall, frostable: false);
		vars.VertexFlags = 0;
		vars.ColorMapData = prevColorMapData;
		vars.VertexFlags = prevFlags;
		vars.RenderPass = EnumChunkRenderPass.OpaqueNoCull;
		vars.finalX = saveRandomOffetX;
		vars.finalZ = saveRandomOffetZ;
		vars.drawFaceFlags = 255;
		json.Tesselate(vars);
	}
}
