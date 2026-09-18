using System;
using System.Collections.Generic;
using UnityEngine;
public static class DAnimationManager
{
	public static void Update()
	{
		float deltaTime = Time.deltaTime;
		foreach (KeyValuePair<string, AnimationState> keyValuePair in DAnimationManager.activeAnimations)
		{
			AnimationState value = keyValuePair.Value;
			bool flag = !value.IsPlaying;
			if (!flag)
			{
				value.ElapsedTime += deltaTime * value.SpeedMultiplier;
				value.Progress = Mathf.Clamp01(value.ElapsedTime / value.Duration);
				bool flag2 = value.OnUpdate != null;
				if (flag2)
				{
					float num = DAnimationManager.ApplyEasing(value.Progress, value.EasingType);
					value.OnUpdate(num);
				}
				bool flag3 = value.ElapsedTime >= value.Duration;
				if (flag3)
				{
					bool isLooping = value.IsLooping;
					if (isLooping)
					{
						value.ElapsedTime = 0f;
						value.Progress = 0f;
					}
					else
					{
						value.IsPlaying = false;
						value.Progress = 1f;
						bool flag4 = value.OnComplete != null;
						if (flag4)
						{
							value.OnComplete();
						}
						DAnimationManager.animationsToRemove.Add(keyValuePair.Key);
					}
				}
			}
		}
		foreach (string text in DAnimationManager.animationsToRemove)
		{
			DAnimationManager.activeAnimations.Remove(text);
		}
		DAnimationManager.animationsToRemove.Clear();
	}
	public static void RegisterAnimation(string key, AnimationType type, float duration, Action<float> onUpdate, Action onComplete = null, bool looping = false, float speedMultiplier = 1f)
	{
		AnimationState animationState = new AnimationState
		{
			Key = key,
			Type = type,
			Duration = duration,
			ElapsedTime = 0f,
			Progress = 0f,
			IsPlaying = true,
			IsLooping = looping,
			SpeedMultiplier = speedMultiplier,
			OnUpdate = onUpdate,
			OnComplete = onComplete,
			EasingType = EasingType.EaseInOutCubic
		};
		DAnimationManager.activeAnimations[key] = animationState;
	}
	public static float GetAnimationValue(string key, float defaultValue = 0f)
	{
		bool flag = DAnimationManager.activeAnimations.ContainsKey(key);
		float num;
		if (flag)
		{
			num = DAnimationManager.activeAnimations[key].Progress;
		}
		else
		{
			num = defaultValue;
		}
		return num;
	}
	public static bool HasAnimation(string key)
	{
		return DAnimationManager.activeAnimations.ContainsKey(key) && DAnimationManager.activeAnimations[key].IsPlaying;
	}
	public static void CancelAnimation(string key)
	{
		bool flag = DAnimationManager.activeAnimations.ContainsKey(key);
		if (flag)
		{
			DAnimationManager.activeAnimations.Remove(key);
		}
	}
	public static void CancelAllAnimations()
	{
		DAnimationManager.activeAnimations.Clear();
	}
	private static float ApplyEasing(float t, EasingType easing)
	{
		float num;
		switch (easing)
		{
		case EasingType.Linear:
			num = t;
			break;
		case EasingType.EaseInQuad:
			num = t * t;
			break;
		case EasingType.EaseOutQuad:
			num = t * (2f - t);
			break;
		case EasingType.EaseInOutQuad:
			num = ((t < 0.5f) ? (2f * t * t) : (-1f + (4f - 2f * t) * t));
			break;
		case EasingType.EaseInCubic:
			num = t * t * t;
			break;
		case EasingType.EaseOutCubic:
			num = (t -= 1f) * t * t + 1f;
			break;
		case EasingType.EaseInOutCubic:
			num = ((t < 0.5f) ? (4f * t * t * t) : ((t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f));
			break;
		case EasingType.EaseOutBack:
		{
			float num2 = 1.70158f;
			float num3 = num2 + 1f;
			num = 1f + num3 * Mathf.Pow(t - 1f, 3f) + num2 * Mathf.Pow(t - 1f, 2f);
			break;
		}
		case EasingType.ElasticOut:
		{
			float num4 = 2.0943952f;
			num = Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * num4) + 1f;
			break;
		}
		case EasingType.BounceOut:
		{
			float num5 = 7.5625f;
			float num6 = 2.75f;
			bool flag = t < 1f / num6;
			if (flag)
			{
				num = num5 * t * t;
			}
			else
			{
				bool flag2 = t < 2f / num6;
				if (flag2)
				{
					num = num5 * (t -= 1.5f / num6) * t + 0.75f;
				}
				else
				{
					bool flag3 = t < 2.5f / num6;
					if (flag3)
					{
						num = num5 * (t -= 2.25f / num6) * t + 0.9375f;
					}
					else
					{
						num = num5 * (t -= 2.625f / num6) * t + 0.984375f;
					}
				}
			}
			break;
		}
		default:
			num = t;
			break;
		}
		return num;
	}
	public static float EaseOutBack(float t)
	{
		float num = 1.70158f;
		float num2 = num + 1f;
		return 1f + num2 * Mathf.Pow(t - 1f, 3f) + num * Mathf.Pow(t - 1f, 2f);
	}
	public static float EaseInOutCubic(float t)
	{
		return (t < 0.5f) ? (4f * t * t * t) : ((t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f);
	}
	public static float ElasticOut(float t)
	{
		float num = 2.0943952f;
		return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * num) + 1f;
	}
	public static float BounceOut(float t)
	{
		float num = 7.5625f;
		float num2 = 2.75f;
		bool flag = t < 1f / num2;
		float num3;
		if (flag)
		{
			num3 = num * t * t;
		}
		else
		{
			bool flag2 = t < 2f / num2;
			if (flag2)
			{
				num3 = num * (t -= 1.5f / num2) * t + 0.75f;
			}
			else
			{
				bool flag3 = t < 2.5f / num2;
				if (flag3)
				{
					num3 = num * (t -= 2.25f / num2) * t + 0.9375f;
				}
				else
				{
					num3 = num * (t -= 2.625f / num2) * t + 0.984375f;
				}
			}
		}
		return num3;
	}
	private static Dictionary<string, AnimationState> activeAnimations = new Dictionary<string, AnimationState>();
	private static List<string> animationsToRemove = new List<string>();
}
