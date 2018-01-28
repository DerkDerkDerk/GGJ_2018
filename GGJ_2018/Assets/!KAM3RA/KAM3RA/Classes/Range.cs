//////////////////////////////////////////////////////////////
//  KAM3RA Third-Person Camera System
// 	Copyright © 2013 Regress Software
//////////////////////////////////////////////////////////////

using UnityEngine;

namespace KAM3RA
{
	[System.Serializable]
	public class Range
	{
		public float min, max;
		public Range(float min, float max)
		{
			this.min = min;
			this.max = max;
		}
		public float GetRandom()
		{
			return Random.Range(min, max);
		}
		public Vector3 GetRandomVector3(bool uniform)
		{
			if (uniform)
			{
				float v = GetRandom();
				return new Vector3(v, v, v);
			}
			return new Vector3(GetRandom(), GetRandom(), GetRandom());
		}
		public int GetRandom(int clampMin, int clampMax)
		{
			int i = Random.Range((int)min, (int)max);
			return Mathf.Clamp(i, clampMin, clampMax);
		}
		public static Vector3 GetRandomVector3(Range x, Range y, Range z)
		{
			return new Vector3(x.GetRandom(), y.GetRandom(), z.GetRandom());
		}
		public static Vector3 GetRandomVector3(float min, float max)
		{
			return new Vector3(Random.Range(min, max), Random.Range(min, max), Random.Range(min, max));
		}
	}
}