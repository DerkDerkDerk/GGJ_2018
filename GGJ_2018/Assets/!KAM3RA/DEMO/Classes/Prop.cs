//////////////////////////////////////////////////////////////
//  KAM3RA Third-Person Camera System
// 	Copyright © 2013 Regress Software
//////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using KAM3RA;

public class Prop : MonoBehaviour 
{
	float drag = 10f;
	void Start()
	{
		if (GetComponent<Rigidbody>() != null && GetComponent<Collider>() != null)
		{
			GetComponent<Rigidbody>().mass = User.CalculateMass(GetComponent<Collider>().bounds.extents.magnitude * 2f);
			GetComponent<Rigidbody>().drag = 0f;
			if (GetComponent<Collider>() is SphereCollider)
			{
				GetComponent<Rigidbody>().angularDrag 	= 1f;
				drag 					= 1f;
				GetComponent<Rigidbody>().isKinematic   = true;
				GetComponent<Collider>().isTrigger 		= true;
			}
			else 
			{
				GetComponent<Rigidbody>().angularDrag 	= 5f;
				drag 					= 10f;
			}
		}
	}
	void OnTriggerEnter(Collider other)
	{
		GetComponent<Rigidbody>().isKinematic = false;
		GetComponent<Collider>().isTrigger    = false;
		GetComponent<Rigidbody>().AddRelativeForce(new Vector3(0, 10, 0));
	}
	void OnCollisionEnter()
	{
		GetComponent<Rigidbody>().drag = drag;
	}
	void OnCollisionExit()
	{
		GetComponent<Rigidbody>().drag = 0f;
	}
}
