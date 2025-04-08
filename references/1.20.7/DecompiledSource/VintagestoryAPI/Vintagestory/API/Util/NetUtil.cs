using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Vintagestory.API.Config;

namespace Vintagestory.API.Util;

public static class NetUtil
{
	public static void OpenUrlInBrowser(string url)
	{
		try
		{
			Process.Start("start \"" + url + "\"");
		}
		catch
		{
			if (RuntimeEnv.OS == OS.Windows)
			{
				url = url.Replace("&", "^&");
				if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
				{
					Process.Start(new ProcessStartInfo(url)
					{
						UseShellExecute = true
					});
				}
				else
				{
					Process.Start("explorer.exe", url);
				}
			}
			else if (RuntimeEnv.OS == OS.Linux)
			{
				url = "\"" + url + "\"";
				Process.Start("xdg-open", url);
			}
			else
			{
				if (RuntimeEnv.OS != OS.Mac)
				{
					throw;
				}
				url = "\"" + url + "\"";
				Process.Start("open", url);
			}
		}
	}

	public static bool IsPrivateIp(string ip)
	{
		string[] parts = ip.Split('.');
		if (parts.Length < 2)
		{
			return false;
		}
		int secondnum = 0;
		int.TryParse(parts[1], out secondnum);
		if (!(parts[0] == "10") && (!(parts[0] == "172") || secondnum < 16 || secondnum > 31))
		{
			if (parts[0] == "192")
			{
				return parts[1] == "168";
			}
			return false;
		}
		return true;
	}

	/// <summary>
	/// Extracts hostname, port and password from given uri. Error will be non null if the uri is incorrect in some ways
	/// </summary>
	/// <param name="uri"></param>
	/// <param name="error"></param>
	/// <returns></returns>
	public static UriInfo getUriInfo(string uri, out string error)
	{
		bool isipv6 = false;
		string password = null;
		if (uri.Contains("@"))
		{
			string[] array = uri.Split('@');
			password = array[0];
			uri = array[1];
		}
		IPAddress addr = null;
		if (IPAddress.TryParse(uri, out addr))
		{
			_ = addr.AddressFamily;
			isipv6 = addr.AddressFamily == AddressFamily.InterNetworkV6;
		}
		string hostname = uri;
		int port = 0;
		int? outport = null;
		if (!isipv6 && uri.Contains(":"))
		{
			string[] array2 = uri.Split(':');
			hostname = array2[0];
			if (int.TryParse(array2[1], out port))
			{
				outport = port;
			}
			else
			{
				error = Lang.Get("Invalid ipv6 address or invalid port number");
			}
		}
		if (isipv6 && uri.Contains("]:"))
		{
			string[] array3 = uri.Split(new string[1] { "]:" }, StringSplitOptions.None);
			hostname = addr.ToString();
			if (int.TryParse(array3[1], out port))
			{
				outport = port;
			}
			else
			{
				error = Lang.Get("Invalid port number");
			}
		}
		error = null;
		UriInfo result = default(UriInfo);
		result.Hostname = hostname;
		result.Password = password;
		result.Port = outport;
		return result;
	}
}
