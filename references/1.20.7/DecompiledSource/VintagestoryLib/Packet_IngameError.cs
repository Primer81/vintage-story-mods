public class Packet_IngameError
{
	public string Code;

	public string Message;

	public string[] LangParams;

	public int LangParamsCount;

	public int LangParamsLength;

	public const int CodeFieldID = 1;

	public const int MessageFieldID = 2;

	public const int LangParamsFieldID = 3;

	public void SetCode(string value)
	{
		Code = value;
	}

	public void SetMessage(string value)
	{
		Message = value;
	}

	public string[] GetLangParams()
	{
		return LangParams;
	}

	public void SetLangParams(string[] value, int count, int length)
	{
		LangParams = value;
		LangParamsCount = count;
		LangParamsLength = length;
	}

	public void SetLangParams(string[] value)
	{
		LangParams = value;
		LangParamsCount = value.Length;
		LangParamsLength = value.Length;
	}

	public int GetLangParamsCount()
	{
		return LangParamsCount;
	}

	public void LangParamsAdd(string value)
	{
		if (LangParamsCount >= LangParamsLength)
		{
			if ((LangParamsLength *= 2) == 0)
			{
				LangParamsLength = 1;
			}
			string[] newArray = new string[LangParamsLength];
			for (int i = 0; i < LangParamsCount; i++)
			{
				newArray[i] = LangParams[i];
			}
			LangParams = newArray;
		}
		LangParams[LangParamsCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
