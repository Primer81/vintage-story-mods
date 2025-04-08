namespace CompactExifLib;

public struct GeoCoordinate
{
	public decimal Degree;

	public decimal Minute;

	public decimal Second;

	public char CardinalPoint;

	public static decimal ToDecimal(GeoCoordinate Value)
	{
		decimal DecimalDegree = Value.Degree + Value.Minute / 60m + Value.Second / 3600m;
		if (Value.CardinalPoint == 'S' || Value.CardinalPoint == 'W')
		{
			DecimalDegree = -DecimalDegree;
		}
		return DecimalDegree;
	}

	public static GeoCoordinate FromDecimal(decimal Value, bool IsLatitude)
	{
		GeoCoordinate ret = default(GeoCoordinate);
		decimal AbsValue;
		if (Value >= 0m)
		{
			ret.CardinalPoint = (IsLatitude ? 'N' : 'E');
			AbsValue = Value;
		}
		else
		{
			ret.CardinalPoint = (IsLatitude ? 'S' : 'W');
			AbsValue = -Value;
		}
		ret.Degree = decimal.Truncate(AbsValue);
		decimal frac = (AbsValue - ret.Degree) * 60m;
		ret.Minute = decimal.Truncate(frac);
		ret.Second = (frac - ret.Minute) * 60m;
		return ret;
	}
}
