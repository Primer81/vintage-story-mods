public class Packet_PlayerDeath
{
	public int ClientId;

	public int LivesLeft;

	public const int ClientIdFieldID = 1;

	public const int LivesLeftFieldID = 2;

	public void SetClientId(int value)
	{
		ClientId = value;
	}

	public void SetLivesLeft(int value)
	{
		LivesLeft = value;
	}

	internal void InitializeValues()
	{
	}
}
