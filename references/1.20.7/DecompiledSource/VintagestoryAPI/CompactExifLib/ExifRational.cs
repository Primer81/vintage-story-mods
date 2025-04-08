using System;

namespace CompactExifLib;

public struct ExifRational
{
	public uint Numer;

	public uint Denom;

	public bool Sign;

	public ExifRational(int _Numer, int _Denom)
	{
		if (_Numer < 0)
		{
			Numer = (uint)(-_Numer);
			Sign = true;
		}
		else
		{
			Numer = (uint)_Numer;
			Sign = false;
		}
		if (_Denom < 0)
		{
			Denom = (uint)(-_Denom);
			Sign = !Sign;
		}
		else
		{
			Denom = (uint)_Denom;
		}
	}

	public ExifRational(uint _Numer, uint _Denom, bool _Sign = false)
	{
		Numer = _Numer;
		Denom = _Denom;
		Sign = _Sign;
	}

	public bool IsNegative()
	{
		if (Sign)
		{
			return Numer != 0;
		}
		return false;
	}

	public bool IsPositive()
	{
		if (!Sign)
		{
			return Numer != 0;
		}
		return false;
	}

	public bool IsZero()
	{
		return Numer == 0;
	}

	public bool IsValid()
	{
		return Denom != 0;
	}

	public new string ToString()
	{
		string Sign = "";
		if (IsNegative())
		{
			Sign = "-";
		}
		return Sign + Numer + "/" + Denom;
	}

	public static decimal ToDecimal(ExifRational Value)
	{
		decimal ret = (decimal)Value.Numer / (decimal)Value.Denom;
		if (Value.Sign)
		{
			ret = -ret;
		}
		return ret;
	}

	public static ExifRational FromDecimal(decimal Value)
	{
		uint denom = 1u;
		ExifRational ret = default(ExifRational);
		decimal numer;
		if (Value >= 0m)
		{
			numer = Value;
			ret.Sign = false;
		}
		else
		{
			numer = -Value;
			ret.Sign = true;
		}
		if (numer >= 1000000000m)
		{
			numer = Math.Truncate(numer + 0.5m);
			if (!(numer <= 4294967295m))
			{
				throw new OverflowException();
			}
			ret.Numer = (uint)numer;
		}
		else
		{
			while (numer != decimal.Truncate(numer))
			{
				decimal tempNumer = numer * 10m;
				if (denom > 100000000 || !(decimal.Truncate(tempNumer + 0.5m) < 1000000000m))
				{
					break;
				}
				numer = tempNumer;
				denom *= 10;
			}
			ret.Numer = (uint)decimal.Truncate(numer + 0.5m);
		}
		ret.Denom = denom;
		while (ret.Denom >= 10 && ret.Numer % 10 == 0)
		{
			ret.Numer /= 10u;
			ret.Denom /= 10u;
		}
		return ret;
	}
}
