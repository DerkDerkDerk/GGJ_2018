//////////////////////////////////////////////////////////////
//  KAM3RA Third-Person Camera System
// 	Copyright © 2013 Regress Software
//////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using KAM3RA;

public class Props : MonoBehaviour 
{
	public bool uniformScale  = false;
	public Range randomScale  = new Range(0.75f, 1.5f);
	public Range randomEulerX = new Range(0, 5);
	public Range randomEulerY = new Range(0, 360);
	public Range randomEulerZ = new Range(0, 5);
	public int seed 		  = 1;
	protected virtual void Awake() 
	{
		Generate();
	}
	protected virtual void Generate()
	{
		Random.seed = seed;
		foreach (Transform t in transform)
		{
			t.eulerAngles 	+= Range.GetRandomVector3(randomEulerX, randomEulerY, randomEulerZ);
			t.localScale   	= Vector3.Scale(t.localScale, randomScale.GetRandomVector3(uniformScale));
		}
	}
}
