using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Vintagestory.API.Util;

public class IgnoreFile
{
	public readonly string filename;

	public readonly string fullpath;

	private List<string> ignored = new List<string>();

	private List<string> ignoredFiles = new List<string>();

	public IgnoreFile(string filename, string fullpath)
	{
		this.filename = filename;
		this.fullpath = fullpath;
		string[] array = File.ReadAllLines(filename);
		foreach (string line in array)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}
			if (line.StartsWithOrdinal("!"))
			{
				ignoredFiles.Add(WildCardToRegular(line.Substring(1)));
				continue;
			}
			bool num = line.EndsWith('/');
			string path = cleanUpPath(line.Replace('/', Path.DirectorySeparatorChar));
			if (num)
			{
				ReadOnlySpan<char> readOnlySpan = path;
				char reference = Path.DirectorySeparatorChar;
				ReadOnlySpan<char> readOnlySpan2 = new ReadOnlySpan<char>(in reference);
				char reference2 = '*';
				path = string.Concat(readOnlySpan, readOnlySpan2, new ReadOnlySpan<char>(in reference2));
			}
			ignored.Add(WildCardToRegular(path));
		}
	}

	private string cleanUpPath(string path)
	{
		return Path.Combine(path.Split('/', '\\'));
	}

	private static string WildCardToRegular(string value)
	{
		return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
	}

	public bool Available(string path)
	{
		if (ignoredFiles.Count > 0 && File.Exists(path))
		{
			string name = Path.GetFileName(path);
			foreach (string ignore2 in ignoredFiles)
			{
				if (Regex.IsMatch(name, ignore2))
				{
					return false;
				}
			}
		}
		path = cleanUpPath(path.Replace(fullpath, ""));
		foreach (string ignore in ignored)
		{
			if (Regex.IsMatch(path, ignore))
			{
				return false;
			}
		}
		return true;
	}

	private bool IsPathDirectory(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		path = path.Trim();
		if (Directory.Exists(path))
		{
			return true;
		}
		if (File.Exists(path))
		{
			return false;
		}
		if (new char[2] { '\\', '/' }.Any((char x) => path.EndsWith(x)))
		{
			return true;
		}
		return string.IsNullOrWhiteSpace(Path.GetExtension(path));
	}
}
