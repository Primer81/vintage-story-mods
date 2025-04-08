public class KeyValue
{
	private Key Key_;

	private byte[] Value;

	public static KeyValue Create(Key key, byte[] value)
	{
		return new KeyValue
		{
			Key_ = key,
			Value = value
		};
	}
}
