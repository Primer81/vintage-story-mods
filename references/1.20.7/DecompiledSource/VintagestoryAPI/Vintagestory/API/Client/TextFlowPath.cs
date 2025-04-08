namespace Vintagestory.API.Client;

public class TextFlowPath
{
	public double X1;

	public double Y1;

	public double X2;

	public double Y2;

	public TextFlowPath()
	{
	}

	public TextFlowPath(double boxWidth)
	{
		X1 = 0.0;
		Y1 = 0.0;
		X2 = boxWidth;
		Y2 = 99999.0;
	}

	public TextFlowPath(double x1, double y1, double x2, double y2)
	{
		X1 = x1;
		Y1 = y1;
		X2 = x2;
		Y2 = y2;
	}
}
