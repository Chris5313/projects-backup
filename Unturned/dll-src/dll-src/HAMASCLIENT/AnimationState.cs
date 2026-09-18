using System;
public class AnimationState
{
	public string Key;
	public AnimationType Type;
	public float Duration;
	public float ElapsedTime;
	public float Progress;
	public bool IsPlaying;
	public bool IsLooping;
	public float SpeedMultiplier;
	public Action<float> OnUpdate;
	public Action OnComplete;
	public EasingType EasingType;
}
