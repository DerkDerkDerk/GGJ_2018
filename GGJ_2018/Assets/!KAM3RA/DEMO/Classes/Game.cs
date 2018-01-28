//////////////////////////////////////////////////////////////
//  KAM3RA Third-Person Camera System
// 	Copyright © 2013 Regress Software
//////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using KAM3RA;

public class Game : MonoBehaviour 
{
	User user 			= null;
	bool showNameTag 	= true;
	List<Actor>actors 	= null;
	void Start()
	{
		user = transform.GetComponent<User>();
	}
	void Update() 
	{
		if (user == null || user.Player == null) return;	
		if (actors == null)
		{
			Object[] objects = GameObject.FindObjectsOfType(typeof(Actor));
			if (objects != null && objects.Length > 0) 
			{
				actors = new List<Actor>();
				foreach (Object m in objects) actors.Add((Actor)m);
			}
		}
		else 
		{
			foreach (Actor m in actors)
			{
				if (m == user.Player) continue;
				if (m.Distance(user.Player) < 5f) m.SetNameTagColor(Color.red);
				else                              m.ResetNameTagColor();
			}	
		}
		if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); RaycastHit hit;
			if (Physics.Raycast(ray, out hit)) 
			{
				Actor actor = hit.transform.GetComponent<Actor>();
				if (actor != null && actor != user.Player && actor.Distance(user.Player) < 5f)
				{
					if (Input.GetMouseButtonDown(0))
					{
						if (!(actor is Kart))
						{
							actor.SetTarget(actor.target == null ? user.Player : null, true); 
						}
					}
					else if (Input.GetMouseButtonDown(1))
					{
						actor.SetTarget(null);
						user.Player.SetTarget(actor, false);
						user.Player = actor;
					}
				}
			}
		}
		if (Input.GetKey(KeyCode.Comma))
		{
			user.Player.AddUniformScale(-Time.deltaTime);
		}
		else if (Input.GetKey(KeyCode.Period))
		{
			user.Player.AddUniformScale(Time.deltaTime);
		}
		if (Input.GetKeyDown(KeyCode.N))
		{
			if (actors != null)
			{
				showNameTag = !showNameTag;
				foreach (Actor m in actors) m.showNameTag = showNameTag;
			}
		}				
		if (Input.GetKeyDown(KeyCode.F2))
		{
			AudioSource sound = User.Instance.GetComponent<AudioSource>();
			if (sound != null) if (sound.isPlaying) sound.Stop(); else sound.Play();
		}				
	}
}
