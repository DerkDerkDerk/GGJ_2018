//////////////////////////////////////////////////////////////
//  KAM3RA Third-Person Camera System
// 	Copyright © 2013 Regress Software
//////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using KAM3RA;

public class NPC : Actor
{
	// simple point-to-point path points
	public TextAsset randomNames			= null;
	protected static List<string> names 	= new List<string>();

	// simple point-to-point path points
	public TextAsset pathNodes				= null;
	protected List<Vector3> path 			= new List<Vector3>();
	protected int curPath					= 0;

	// random scale 
	public Range randomScale  				= new Range(0.75f, 1.5f);
	
	//////////////////////////////////////////////////////////////
	// Init 
	//////////////////////////////////////////////////////////////
	protected override void Awake()
	{
		base.Awake();
		transform.localScale = User.MakeUniformScale(randomScale.GetRandom());
	}
	protected override void Start()
	{
		base.Start();
		SetNameTag(RandomName);
		transform.position 	= StartPosition;
		State 				= "RunForward";
		// we're giving all NPC's hover ability
		this.type 			= Type.Hover;
	}
	
	//////////////////////////////////////////////////////////////
	// Updates 
	//////////////////////////////////////////////////////////////
	protected override void Update()
	{
		base.Update();
		if (player) 
		{
			return;
		}
		if (State == "Idle")
		{
			return;
		}
		if (Colliding)
		{
			State = Speed > (2f * transform.localScale.y) ? "RunForward" : "WalkForward";
			if (PathExists)
			{
				Vector3 position 	= transform.position;
				Vector3 destination = path[curPath];
				float distance		= Vector3.Distance(User.VectorXZ(position), User.VectorXZ(destination));
				if (distance < Radius)
				{
					curPath++; if (curPath == path.Count) curPath = 0; 
				}
				if (!TooFast) 
				{
					velocity = (destination - position).normalized * ScaledSpeed;
				}
				User.LookAt2D(transform, destination);
			}
		}
	}
	protected override void FixedUpdate()
	{
		base.FixedUpdate();	
		if (Colliding)
		{
			if (!rigidbody.freezeRotation)
			{
				rigidbody.freezeRotation 	= true;
				rigidbody.angularVelocity 	= Vector3.zero;
				rigidbody.rotation 			= Quaternion.identity;
			}
		}
	}	
	protected override void OnCollisionExit()
	{
		base.OnCollisionExit();
		rigidbody.freezeRotation = false;
	}

	//////////////////////////////////////////////////////////////
	// Path 
	//////////////////////////////////////////////////////////////
	public bool PathExists 
	{ 
		get { return path.Count > 0; }	
	}
	public Vector3 StartPosition						
	{ 
		get 
		{ 
			if (pathNodes != null && path.Count == 0) 
			{
				path = GetPathNodes(pathNodes.text);
				curPath = Random.Range(0, Mathf.Min(3, path.Count - 1));
			}
			return path.Count > 0 ? path[curPath] : transform.position; 
		} 
	}
	
	//////////////////////////////////////////////////////////////
	// Names 
	//////////////////////////////////////////////////////////////
	public string RandomName						
	{ 
		get 
		{ 
			if (randomNames != null && names.Count == 0) names = GetNames(randomNames.text);
			if (names.Count == 0) return nameTag;
			string name = names[Random.Range(0, names.Count-1)];
			names.Remove(name); 
			return name;
		} 
	}
	
	//////////////////////////////////////////////////////////////
	// Other 
	//////////////////////////////////////////////////////////////
	public override void SetTarget(Actor target)
	{
		SetTarget(target, true);
	}
	public override void SetTarget(Actor target, bool watch)
	{
		base.SetTarget(target, watch);
		State = target != null ? "Idle" : "RunForward";
	}
	
	//////////////////////////////////////////////////////////////
	// Static 
	//////////////////////////////////////////////////////////////
	public static List<Vector3> GetPathNodes(string points)
	{
		List<Vector3> path = new List<Vector3>();
		if (points != null)
		{
			points = points.Replace(" ", "");
			string[] lines = points.Split(new string[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries); 
			if (lines != null) 
			{
				string[] p = null; Vector3 pos;
				foreach (string s in lines) 
				{ 
					p = s.Split(','); 
					if (p.Length > 2) 
					{
						pos.x = float.Parse(p[0]);
						pos.y = float.Parse(p[1]);
						pos.z = float.Parse(p[2]);
						path.Add(pos); 
					}
				}
			}
		}
		return path;
	}
	public static List<string> GetNames(string names)
	{
		if (names != null)
		{
			names = names.Replace(" ", "");
			string[] lines = names.Split(new string[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries); 
			if (lines != null) return new List<string>(lines);
		}
		return new List<string>();
	}
}
