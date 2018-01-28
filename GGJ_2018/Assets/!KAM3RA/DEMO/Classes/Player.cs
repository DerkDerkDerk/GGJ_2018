//////////////////////////////////////////////////////////////
//  KAM3RA Third-Person Camera System
// 	Copyright © 2013 Regress Software
//////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using KAM3RA;

public class Player : Actor 
{
	protected AudioSource[] sounds	= null;
	protected AudioSource sound		= null;

	//////////////////////////////////////////////////////////////
	// Init 
	//////////////////////////////////////////////////////////////
	protected override void Awake()
	{
		sounds = GetComponents<AudioSource>();
	}
	
	//////////////////////////////////////////////////////////////
	// Sound 
	//////////////////////////////////////////////////////////////
	protected override void UpdateSound()
	{
		if (sounds == null) return;
		if (States.Moving)
		{
			if (minCollisionPoint.otherCollider != null) 
			{
				if (sounds != null && sounds.Length > 1 && minCollisionPoint.point.y < FootPosition.y)
				{
					if (minCollisionPoint.otherCollider.gameObject.GetComponent<Terrain>() != null)
					{
						if (sound != sounds[1]) 
						{
							if (sound != null) sound.Pause();
							sound = sounds[1];
						}
					}
					else 
					{
						if (sound != sounds[0]) 
						{
							if (sound != null) sound.Pause();
							sound = sounds[0];
						}
					}
				}
			}
			if (sound != null)
			{
				sound.pitch = States.Walking ? 1f : 1.5f;
				if (!sound.isPlaying) sound.Play();
			}
		}
		else 
		{
			if (sound != null) sound.Pause();
		}
	}
}
