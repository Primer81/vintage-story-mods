public class Packet_TransitionableProperties
{
	public Packet_NatFloat FreshHours;

	public Packet_NatFloat TransitionHours;

	public byte[] TransitionedStack;

	public int TransitionRatio;

	public int Type;

	public const int FreshHoursFieldID = 1;

	public const int TransitionHoursFieldID = 2;

	public const int TransitionedStackFieldID = 3;

	public const int TransitionRatioFieldID = 4;

	public const int TypeFieldID = 5;

	public void SetFreshHours(Packet_NatFloat value)
	{
		FreshHours = value;
	}

	public void SetTransitionHours(Packet_NatFloat value)
	{
		TransitionHours = value;
	}

	public void SetTransitionedStack(byte[] value)
	{
		TransitionedStack = value;
	}

	public void SetTransitionRatio(int value)
	{
		TransitionRatio = value;
	}

	public void SetType(int value)
	{
		Type = value;
	}

	internal void InitializeValues()
	{
	}
}
