using System;
using System.ComponentModel;
using System.Globalization;

namespace Vintagestory.API.Common;

internal class StringAssetLocationConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
	{
		if (sourceType == typeof(string))
		{
			return true;
		}
		return base.CanConvertFrom(context, sourceType);
	}

	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if (value is string && value as string != "")
		{
			return new AssetLocation(value as string);
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if (destinationType == typeof(string))
		{
			return (value as AssetLocation).ToString();
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}
}
