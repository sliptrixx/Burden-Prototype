using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
	public static float Angle(Vector2 from, Vector2 to)
	{
		Vector2 dir = to - from;
		float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		if (angle < 0.0f) angle += 360.0f;
		return angle;
	}

	public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
	{
		return ((toMax - toMin) * ((value - fromMin) / (fromMax - fromMin))) + toMin;
	}

	public static Vector3 Down(this Transform value)
	{
		return -value.up;
	}

	public static Vector3 Direction(this Vector3 from, Vector3 to)
	{
		return (to - from).normalized;
	}
}
