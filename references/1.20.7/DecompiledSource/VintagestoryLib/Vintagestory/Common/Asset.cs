using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.Common;

public class Asset : IAsset
{
	public byte[] Data;

	public AssetLocation Location;

	public string FilePath;

	private IAssetOrigin origin;

	public IAssetOrigin Origin
	{
		get
		{
			return origin;
		}
		set
		{
			origin = value;
		}
	}

	public string Name => Location.GetName();

	string IAsset.Name => Name;

	byte[] IAsset.Data
	{
		get
		{
			return Data;
		}
		set
		{
			Data = value;
		}
	}

	AssetLocation IAsset.Location => Location;

	public bool IsPatched { get; set; }

	public Asset(byte[] bytes, AssetLocation Location, IAssetOrigin origin)
	{
		Data = bytes;
		this.origin = origin;
		this.Location = Location;
	}

	public Asset(AssetLocation Location)
	{
		this.Location = Location;
	}

	public T ToObject<T>(JsonSerializerSettings settings = null)
	{
		try
		{
			return JsonUtil.ToObject<T>(BytesToString(Data), Location.Domain, settings);
		}
		catch (Exception e)
		{
			throw new JsonReaderException("Failed deserializing " + Name + ": " + e.Message);
		}
	}

	public string ToText()
	{
		return BytesToString(Data);
	}

	public BitmapRef ToBitmap(ICoreClientAPI api)
	{
		return api.Render.BitmapCreateFromPng(Data);
	}

	public bool IsLoaded()
	{
		return Data != null;
	}

	public override string ToString()
	{
		return Location.ToString();
	}

	public override int GetHashCode()
	{
		return Location.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		return Location.Equals(obj);
	}

	public static string BytesToString(byte[] data)
	{
		if (data == null || data.Length == 0)
		{
			return "";
		}
		if (data[0] == 123)
		{
			return Encoding.UTF8.GetString(data);
		}
		using MemoryStream stream = new MemoryStream(data);
		using StreamReader sr = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
		return sr.ReadToEnd();
	}
}
