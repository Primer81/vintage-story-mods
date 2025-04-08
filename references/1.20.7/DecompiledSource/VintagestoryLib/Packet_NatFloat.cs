public class Packet_NatFloat
{
	public int Avg;

	public int Var;

	public int Dist;

	public const int AvgFieldID = 1;

	public const int VarFieldID = 2;

	public const int DistFieldID = 3;

	public void SetAvg(int value)
	{
		Avg = value;
	}

	public void SetVar(int value)
	{
		Var = value;
	}

	public void SetDist(int value)
	{
		Dist = value;
	}

	internal void InitializeValues()
	{
	}
}
