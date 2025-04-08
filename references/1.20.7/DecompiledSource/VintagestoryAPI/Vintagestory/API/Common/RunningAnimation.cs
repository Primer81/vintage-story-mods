using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class RunningAnimation
{
	public AnimationMetaData meta;

	public float CurrentFrame;

	public Animation Animation;

	public bool Active;

	public bool Running;

	public int Iterations;

	public bool ShouldRewind;

	public bool ShouldPlayTillEnd;

	public float EasingFactor;

	public float BlendedWeight;

	public ShapeElementWeights[] ElementWeights;

	public int SoundPlayedAtIteration = -1;

	/// <summary>
	/// Between 0 and 1
	/// </summary>
	public float AnimProgress => CurrentFrame / (float)(Animation.QuantityFrames - 1);

	public void LoadWeights(ShapeElement[] rootElements)
	{
		ElementWeights = new ShapeElementWeights[rootElements.Length];
		LoadWeights(rootElements, ElementWeights, meta.ElementWeight, meta.ElementBlendMode);
	}

	private void LoadWeights(ShapeElement[] elements, ShapeElementWeights[] intoList, Dictionary<string, float> elementWeight, Dictionary<string, EnumAnimationBlendMode> elementBlendMode)
	{
		for (int i = 0; i < elements.Length; i++)
		{
			ShapeElement elem = elements[i];
			intoList[i] = new ShapeElementWeights();
			if (elementWeight.TryGetValue(elem.Name, out var w))
			{
				intoList[i].Weight = w;
			}
			else
			{
				intoList[i].Weight = meta.Weight;
			}
			if (elementBlendMode.TryGetValue(elem.Name, out var blendMode))
			{
				intoList[i].BlendMode = blendMode;
			}
			else
			{
				intoList[i].BlendMode = meta.BlendMode;
			}
			if (elem.Children != null)
			{
				intoList[i].ChildElements = new ShapeElementWeights[elem.Children.Length];
				LoadWeights(elem.Children, intoList[i].ChildElements, elementWeight, elementBlendMode);
			}
		}
	}

	internal void CalcBlendedWeight(float weightSum, EnumAnimationBlendMode blendMode)
	{
		if (weightSum == 0f)
		{
			BlendedWeight = EasingFactor;
		}
		else
		{
			BlendedWeight = GameMath.Clamp((blendMode == EnumAnimationBlendMode.Add) ? EasingFactor : (EasingFactor / Math.Max(meta.WeightCapFactor, weightSum)), 0f, 1f);
		}
	}

	public void Progress(float dt, float walkspeed)
	{
		dt *= meta.GetCurrentAnimationSpeed(walkspeed);
		if ((Active && (Iterations == 0 || Animation.OnAnimationEnd != EnumEntityAnimationEndHandling.EaseOut)) || (Iterations == 0 && Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.PlayTillEnd))
		{
			EasingFactor = Math.Min(1f, EasingFactor + (1f - EasingFactor) * Math.Abs(dt) * meta.EaseInSpeed);
		}
		else
		{
			EasingFactor = Math.Max(0f, EasingFactor - (EasingFactor - 0f) * Math.Abs(dt) * meta.EaseOutSpeed);
		}
		float newFrame = CurrentFrame + 30f * (ShouldRewind ? (0f - dt) : dt) * (Animation.EaseAnimationSpeed ? EasingFactor : 1f);
		if (!Active && Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.PlayTillEnd && (Iterations >= 1 || newFrame >= (float)(Animation.QuantityFrames - 1)))
		{
			EasingFactor = 0f;
			CurrentFrame = Animation.QuantityFrames - 1;
			Stop();
			return;
		}
		if ((Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.Hold || Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.EaseOut) && newFrame >= (float)(Animation.QuantityFrames - 1) && dt >= 0f)
		{
			Iterations = 1;
			CurrentFrame = Animation.QuantityFrames - 1;
			return;
		}
		if (Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.EaseOut && newFrame < 0f && dt < 0f)
		{
			Iterations = 1;
			CurrentFrame = Animation.QuantityFrames - 1;
			return;
		}
		if (dt >= 0f && newFrame <= 0f)
		{
			Iterations--;
			CurrentFrame = 0f;
			return;
		}
		CurrentFrame = newFrame;
		if (dt >= 0f && CurrentFrame >= (float)Animation.QuantityFrames)
		{
			Iterations++;
			CurrentFrame = GameMath.Mod(newFrame, Animation.QuantityFrames);
		}
		if (dt < 0f && CurrentFrame < 0f)
		{
			Iterations++;
			CurrentFrame = GameMath.Mod(newFrame, Animation.QuantityFrames);
		}
		if (Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.Stop && Iterations > 0)
		{
			CurrentFrame = Animation.QuantityFrames - 1;
		}
	}

	public void Stop()
	{
		Active = false;
		Running = false;
		CurrentFrame = 0f;
		Iterations = 0;
		EasingFactor = 0f;
		SoundPlayedAtIteration = -1;
	}

	public void EaseOut(float dt)
	{
		EasingFactor = Math.Max(0f, EasingFactor - (EasingFactor - 0f) * dt * meta.EaseOutSpeed);
	}
}
