using System.Collections.Generic;

namespace Vintagestory.API.Common;

public class VtmlTagToken : VtmlToken
{
	public List<VtmlToken> ChildElements { get; set; } = new List<VtmlToken>();


	/// <summary>
	/// Name of this tag
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Collection of attribute names and values for this tag
	/// </summary>
	public Dictionary<string, string> Attributes { get; set; }

	public string ContentText
	{
		get
		{
			string text = "";
			foreach (VtmlToken val in ChildElements)
			{
				text = ((!(val is VtmlTextToken)) ? (text + (val as VtmlTagToken).ContentText) : (text + (val as VtmlTextToken).Text));
			}
			return text;
		}
	}
}
