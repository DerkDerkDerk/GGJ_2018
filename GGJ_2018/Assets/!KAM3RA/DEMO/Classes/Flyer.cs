//////////////////////////////////////////////////////////////
//  KAM3RA Third-Person Camera System
// 	Copyright © 2013 Regress Software
//////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using KAM3RA;

public class Flyer : Kart 
{
	// propellor
	protected GameObject prop = null;
	protected override void Start() 
	{
		base.Start();	
		prop = transform.Find("Prop").gameObject;
	}
	protected override void Update()
	{
		base.Update();
		// turn the propellor
		if (player && prop != null) prop.transform.localEulerAngles -= new Vector3(0f, 0f, Mathf.Max(Speed, Time.deltaTime * 500f));
	}
}
