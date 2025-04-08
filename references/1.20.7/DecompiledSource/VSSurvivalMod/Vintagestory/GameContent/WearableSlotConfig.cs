using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class WearableSlotConfig
{
	public SeatConfig SeatConfig;

	public string Code;

	public string[] ForCategoryCodes;

	public string[] CanMergeWith;

	public string AttachmentPointCode;

	public Dictionary<string, StepParentElementTo> StepParentTo;

	public string ProvidesSeatId;

	public bool EmptyInteractPassThrough { get; set; }

	public bool CanHold(string code)
	{
		for (int i = 0; i < ForCategoryCodes.Length; i++)
		{
			if (ForCategoryCodes[i] == code)
			{
				return true;
			}
		}
		return false;
	}
}
