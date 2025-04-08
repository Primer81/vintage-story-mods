using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract]
public class PropickReading
{
	[ProtoMember(1)]
	public Vec3d Position = new Vec3d();

	[ProtoMember(2)]
	public Dictionary<string, OreReading> OreReadings = new Dictionary<string, OreReading>();

	public static double MentionThreshold = 0.002;

	[ProtoMember(3)]
	public string Guid { get; set; }

	public double HighestReading
	{
		get
		{
			double maxreading = 0.0;
			foreach (KeyValuePair<string, OreReading> val in OreReadings)
			{
				maxreading = GameMath.Max(maxreading, val.Value.TotalFactor);
			}
			return maxreading;
		}
	}

	public string ToHumanReadable(string languageCode, Dictionary<string, string> pageCodes)
	{
		List<KeyValuePair<double, string>> readouts = new List<KeyValuePair<double, string>>();
		List<string> traceamounts = new List<string>();
		string[] names = new string[6] { "propick-density-verypoor", "propick-density-poor", "propick-density-decent", "propick-density-high", "propick-density-veryhigh", "propick-density-ultrahigh" };
		foreach (KeyValuePair<string, OreReading> val3 in OreReadings)
		{
			OreReading reading = val3.Value;
			if (reading.DepositCode == "unknown")
			{
				string text2 = Lang.GetL(languageCode, "propick-reading-unknown", val3.Key);
				readouts.Add(new KeyValuePair<double, string>(1.0, text2));
			}
			else if (reading.TotalFactor > 0.025)
			{
				readouts.Add(new KeyValuePair<double, string>(value: (!pageCodes.TryGetValue(val3.Key, out var pageCode2)) ? val3.Key : Lang.GetL(languageCode, "propick-reading", Lang.GetL(languageCode, names[(int)GameMath.Clamp(reading.TotalFactor * 7.5, 0.0, 5.0)]), pageCode2, Lang.GetL(languageCode, "ore-" + val3.Key), reading.PartsPerThousand.ToString("0.##")), key: reading.TotalFactor));
			}
			else if (reading.TotalFactor > MentionThreshold)
			{
				traceamounts.Add(val3.Key);
			}
		}
		StringBuilder sb = new StringBuilder();
		if (readouts.Count >= 0 || traceamounts.Count > 0)
		{
			IOrderedEnumerable<KeyValuePair<double, string>> elems = readouts.OrderByDescending((KeyValuePair<double, string> val) => val.Key);
			sb.AppendLine(Lang.GetL(languageCode, "propick-reading-title", readouts.Count));
			foreach (KeyValuePair<double, string> item in elems)
			{
				sb.AppendLine(item.Value);
			}
			if (traceamounts.Count > 0)
			{
				StringBuilder sbTrace = new StringBuilder();
				int i = 0;
				foreach (string val2 in traceamounts)
				{
					if (i > 0)
					{
						sbTrace.Append(", ");
					}
					if (!pageCodes.TryGetValue(val2, out var pageCode))
					{
						pageCode = val2;
					}
					string text = string.Format("<a href=\"handbook://{0}\">{1}</a>", pageCode, Lang.GetL(languageCode, "ore-" + val2));
					sbTrace.Append(text);
					i++;
				}
				sb.Append(Lang.GetL(languageCode, "Miniscule amounts of {0}", sbTrace.ToString()));
				sb.AppendLine();
			}
		}
		else
		{
			sb.Append(Lang.GetL(languageCode, "propick-noreading"));
		}
		return sb.ToString();
	}

	internal double GetTotalFactor(string orecode)
	{
		if (!OreReadings.TryGetValue(orecode, out var reading))
		{
			return 0.0;
		}
		return reading.TotalFactor;
	}
}
