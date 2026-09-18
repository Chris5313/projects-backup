using System;
public class ScrollState
{
	public ScrollState(int scrollPosition, int maxSizeY, int startedOffset, int predictedSize)
	{
		this.startY = 0;
		this.endY = 0;
		this.maxSizeY = maxSizeY;
		this.scrollPosition = scrollPosition;
		this.startedOffset = startedOffset;
		this.predictedSize = predictedSize;
	}
	public int startY;
	public int endY;
	public int maxSizeY;
	public int predictedSize;
	public int scrollPosition;
	public int startedOffset;
}
