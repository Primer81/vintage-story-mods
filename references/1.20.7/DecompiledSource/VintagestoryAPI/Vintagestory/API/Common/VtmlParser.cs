using System;
using System.Collections.Generic;

namespace Vintagestory.API.Common;

public class VtmlParser
{
	public enum ParseState
	{
		SeekKey,
		ParseTagName,
		ParseKey,
		SeekValue,
		ParseQuotedValue,
		ParseValue
	}

	public static VtmlToken[] Tokenize(ILogger errorLogger, string vtml)
	{
		if (vtml == null)
		{
			return new VtmlToken[0];
		}
		List<VtmlToken> tokenized = new List<VtmlToken>();
		Stack<VtmlTagToken> tokenStack = new Stack<VtmlTagToken>();
		string text = "";
		string tag = "";
		bool insideTag = false;
		for (int pos = 0; pos < vtml.Length; pos++)
		{
			if (vtml[pos] == '<')
			{
				insideTag = true;
				if (text.Length > 0)
				{
					text = text.Replace("&gt;", ">").Replace("&lt;", "<").Replace("&nbsp;", " ");
					if (tokenStack.Count > 0)
					{
						tokenStack.Peek().ChildElements.Add(new VtmlTextToken
						{
							Text = text
						});
					}
					else
					{
						tokenized.Add(new VtmlTextToken
						{
							Text = text
						});
					}
				}
				text = "";
			}
			else if (vtml[pos] == '>')
			{
				if (!insideTag)
				{
					errorLogger.Error("Found closing tag char > but no tag was opened at " + pos + ". Use &gt;/&lt; if you want to display them as plain characters. See debug log for full text.");
					errorLogger.VerboseDebug(vtml);
				}
				insideTag = false;
				if (tag.Length > 0 && tag[0] == '/')
				{
					if (tokenStack.Count == 0 || tokenStack.Peek().Name != tag.Substring(1))
					{
						if (tokenStack.Count == 0)
						{
							errorLogger.Error("Found closing tag <" + tag.Substring(1) + "> at position " + pos + " but it was never opened. See debug log for full text.");
						}
						else
						{
							errorLogger.Error("Found closing tag <" + tag.Substring(1) + "> at position " + pos + " but <" + tokenStack.Peek().Name + "> should be closed first. See debug log for full text.");
						}
						errorLogger.VerboseDebug(vtml);
					}
					if (tokenStack.Count > 0)
					{
						tokenStack.Pop();
					}
					tag = "";
				}
				else if (tag == "br")
				{
					VtmlTagToken tagToken = new VtmlTagToken
					{
						Name = "br"
					};
					if (tokenStack.Count > 0)
					{
						tokenStack.Peek().ChildElements.Add(tagToken);
					}
					else
					{
						tokenized.Add(tagToken);
					}
					tag = "";
				}
				else if (pos > 0 && vtml[pos - 1] == '/')
				{
					VtmlTagToken tagToken = parseTagAttributes(tag.Substring(0, tag.Length - 1));
					if (tokenStack.Count > 0)
					{
						tokenStack.Peek().ChildElements.Add(tagToken);
					}
					else
					{
						tokenized.Add(tagToken);
					}
					tag = "";
				}
				else
				{
					VtmlTagToken tagToken = parseTagAttributes(tag);
					if (tokenStack.Count > 0)
					{
						tokenStack.Peek().ChildElements.Add(tagToken);
					}
					else
					{
						tokenized.Add(tagToken);
					}
					tokenStack.Push(tagToken);
					tag = "";
				}
			}
			else if (insideTag)
			{
				ReadOnlySpan<char> readOnlySpan = tag;
				char reference = vtml[pos];
				tag = string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference));
			}
			else
			{
				ReadOnlySpan<char> readOnlySpan2 = text;
				char reference = vtml[pos];
				text = string.Concat(readOnlySpan2, new ReadOnlySpan<char>(in reference));
			}
		}
		if (text.Length > 0)
		{
			text = text.Replace("&gt;", ">").Replace("&lt;", "<").Replace("&nbsp;", " ");
			tokenized.Add(new VtmlTextToken
			{
				Text = text
			});
		}
		return tokenized.ToArray();
	}

	private static VtmlTagToken parseTagAttributes(string tag)
	{
		Dictionary<string, string> attributes = new Dictionary<string, string>();
		string tagName = null;
		ParseState state = ParseState.ParseTagName;
		char insideQuotedValueChar = '\0';
		string key = "";
		string value = "";
		for (int i = 0; i < tag.Length; i++)
		{
			bool isWhiteSpace = tag[i] == ' ' || tag[i] == '\t' || tag[i] == '\r' || tag[i] == '\n';
			bool isQuote = tag[i] == '\'' || tag[i] == '"';
			switch (state)
			{
			case ParseState.ParseTagName:
			{
				if (isWhiteSpace)
				{
					state = ParseState.SeekKey;
					break;
				}
				ReadOnlySpan<char> readOnlySpan2 = tagName;
				char reference = tag[i];
				tagName = string.Concat(readOnlySpan2, new ReadOnlySpan<char>(in reference));
				break;
			}
			case ParseState.SeekKey:
				if (!isWhiteSpace)
				{
					key = tag[i].ToString() ?? "";
					state = ParseState.ParseKey;
				}
				break;
			case ParseState.ParseKey:
				if (tag[i] == '=')
				{
					state = ParseState.SeekValue;
					value = "";
				}
				else if (isWhiteSpace)
				{
					attributes[key] = null;
					state = ParseState.SeekKey;
				}
				else
				{
					ReadOnlySpan<char> readOnlySpan3 = key;
					char reference = tag[i];
					key = string.Concat(readOnlySpan3, new ReadOnlySpan<char>(in reference));
				}
				break;
			case ParseState.SeekValue:
				if (!isWhiteSpace)
				{
					if (isQuote)
					{
						state = ParseState.ParseQuotedValue;
						insideQuotedValueChar = tag[i];
					}
					else
					{
						state = ParseState.ParseValue;
						value = tag[i].ToString() ?? "";
					}
				}
				break;
			case ParseState.ParseValue:
				if (isWhiteSpace)
				{
					attributes[key.ToLowerInvariant()] = value;
					state = ParseState.SeekKey;
				}
				else
				{
					ReadOnlySpan<char> readOnlySpan4 = value;
					char reference = tag[i];
					value = string.Concat(readOnlySpan4, new ReadOnlySpan<char>(in reference));
				}
				break;
			case ParseState.ParseQuotedValue:
				if (tag[i] == insideQuotedValueChar && tag[i - 1] != '\\')
				{
					attributes[key.ToLowerInvariant()] = value;
					state = ParseState.SeekKey;
				}
				else
				{
					ReadOnlySpan<char> readOnlySpan = value;
					char reference = tag[i];
					value = string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference));
				}
				break;
			}
		}
		if (state == ParseState.ParseValue || state == ParseState.SeekValue)
		{
			attributes[key] = value;
		}
		return new VtmlTagToken
		{
			Name = tagName,
			Attributes = attributes
		};
	}
}
