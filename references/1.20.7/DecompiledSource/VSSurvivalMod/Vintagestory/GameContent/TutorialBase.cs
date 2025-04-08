using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public abstract class TutorialBase : ITutorial
{
	protected ICoreClientAPI capi;

	protected JsonObject stepData;

	protected List<TutorialStepBase> steps = new List<TutorialStepBase>();

	public int currentStep;

	public string pageCode;

	public JsonObject StepDataForSaving
	{
		get
		{
			if (stepData == null)
			{
				stepData = new JsonObject(new JObject());
			}
			return stepData;
		}
	}

	public string PageCode => pageCode;

	public bool Complete => steps[steps.Count - 1].Complete;

	public float Progress
	{
		get
		{
			if (steps.Count != 0)
			{
				return (float)steps.Sum((TutorialStepBase t) => t.Complete ? 1 : 0) / (float)steps.Count;
			}
			return 0f;
		}
	}

	protected TutorialBase(ICoreClientAPI capi, string pageCode)
	{
		this.capi = capi;
		this.pageCode = pageCode;
	}

	public void Restart()
	{
		stepData = new JsonObject(new JObject());
		foreach (TutorialStepBase step in steps)
		{
			step.Restart();
			step.ToJson(StepDataForSaving);
		}
		Save();
	}

	public bool OnStateUpdate(ActionBoolReturn<TutorialStepBase> stepCall)
	{
		bool anyDirty = false;
		bool anyNowCompleted = false;
		foreach (TutorialStepBase step in steps)
		{
			if (!step.Complete)
			{
				bool dirty = stepCall(step);
				anyDirty = anyDirty || dirty;
				if (dirty)
				{
					step.ToJson(StepDataForSaving);
				}
				if (step.Complete)
				{
					anyNowCompleted = true;
				}
			}
		}
		if (anyNowCompleted)
		{
			capi.Gui.PlaySound(new AssetLocation("sounds/tutorialstepsuccess.ogg"));
			Save();
		}
		return anyDirty;
	}

	public void addSteps(params TutorialStepBase[] steps)
	{
		for (int i = 0; i < steps.Length; i++)
		{
			steps[i].index = i;
		}
		this.steps.AddRange(steps);
	}

	public List<TutorialStepBase> GetTutorialSteps(bool skipOld)
	{
		if (this.steps.Count == 0)
		{
			initTutorialSteps();
		}
		List<TutorialStepBase> steps = new List<TutorialStepBase>();
		int showActive = 1;
		foreach (TutorialStepBase step in this.steps)
		{
			if (showActive <= 0)
			{
				break;
			}
			if (stepData != null)
			{
				step.FromJson(stepData);
			}
			steps.Add(step);
			if (!step.Complete)
			{
				showActive--;
			}
		}
		if (skipOld)
		{
			while (steps.Count > 1 && steps[0].Complete)
			{
				steps.RemoveAt(0);
			}
		}
		return steps;
	}

	protected abstract void initTutorialSteps();

	public void Skip(int cnt)
	{
		while (cnt-- > 0)
		{
			TutorialStepBase step = steps.FirstOrDefault((TutorialStepBase s) => !s.Complete);
			if (step != null)
			{
				step.Skip();
				step.ToJson(StepDataForSaving);
			}
		}
		capi.Gui.PlaySound(new AssetLocation("sounds/tutorialstepsuccess.ogg"));
	}

	public void Save()
	{
		JsonObject job = new JsonObject(new JObject());
		foreach (TutorialStepBase step in steps)
		{
			step.ToJson(job);
		}
		capi.StoreModConfig(job, "tutorial-" + PageCode + ".json");
	}

	public void Load()
	{
		stepData = capi.LoadModConfig("tutorial-" + PageCode + ".json");
		if (stepData == null)
		{
			return;
		}
		foreach (TutorialStepBase step in steps)
		{
			step.FromJson(stepData);
		}
	}
}
