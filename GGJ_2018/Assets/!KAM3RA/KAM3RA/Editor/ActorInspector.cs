//////////////////////////////////////////////////////////////
//  KAM3RA Third-Person Camera System
// 	Copyright © 2013 Regress Software
//////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace KAM3RA 
{ 
	[CustomEditor(typeof(Actor), true)]
	public class ActorInspector : Editor 
	{
		bool showMap 	= true;
		string newState = "State";
		string newName	= "Name";
		GUIStyle style  = null;
		public override void OnInspectorGUI()
		{
			EditorGUIUtility.LookLikeControls();
			Actor actor				= target as Actor;
			actor.type 				= (Actor.Type)EditorGUILayout.EnumPopup("Type", actor.type);
			actor.player 			= EditorGUILayout.Toggle("Player", 				actor.player);
			actor.drag				= EditorGUILayout.FloatField("Drag", 			actor.drag);
			actor.maxSpeed			= EditorGUILayout.FloatField("Max Speed", 		actor.maxSpeed);
			actor.acceleration		= EditorGUILayout.FloatField("Acceleration", 	actor.acceleration);
			actor.momentum			= EditorGUILayout.FloatField("Momentum", 		actor.momentum);
			actor.jumpHeight		= EditorGUILayout.FloatField("Jump Height", 	actor.jumpHeight);
			actor.eyeHeightScale	= EditorGUILayout.FloatField("Eye Height", 		actor.eyeHeightScale);
			actor.maxSlope			= EditorGUILayout.FloatField("Max Slope", 		actor.maxSlope);
			actor.radius			= EditorGUILayout.FloatField("Radius", 			actor.radius);
			actor.ShowNameTag(EditorGUILayout.Foldout(actor.showNameTag, "Name Tag"));
			EditorGUI.indentLevel	+= 1;
			if (actor.showNameTag)
			{
				actor.SetNameTag(EditorGUILayout.TextField("Name",					actor.nameTag));
				actor.SetNameTagColor(EditorGUILayout.ColorField("Color",			actor.nameTagColor));
				actor.nameTagOffset = UserInspector.Vector3Field("Offset",			actor.nameTagOffset);
			}
			EditorGUI.indentLevel	-= 1;
			showMap 				= EditorGUILayout.Foldout(showMap, "Animation Map"); 
			if (showMap)
			{
				style = new GUIStyle(GUI.skin.textField);
				style.normal.textColor = new Color32(255, 163, 0, 255);
				EditorGUI.indentLevel	+= 1;
				List<Actor.StateName>del = new List<Actor.StateName>();
				List<Actor.StateName>map = actor.States.map;
				foreach (Actor.StateName m in map)
				{
					GUILayout.BeginHorizontal();
					m.name  = EditorGUILayout.TextField(m.state, m.name);
					if (GUILayout.Button("-", GUILayout.MaxWidth(20), GUILayout.Height(14))) del.Add(m);
					GUILayout.EndHorizontal();
				}
				GUILayout.BeginHorizontal();
				newState = EditorGUILayout.TextField(newState, style, GUILayout.MaxWidth(165));
				newName  = EditorGUILayout.TextField(newName, style);
				style = new GUIStyle(GUI.skin.button);
				style.normal.textColor = new Color32(255, 163, 0, 255);
				if (GUILayout.Button("+", style, GUILayout.MaxWidth(20), GUILayout.Height(14))) actor.States.Add(newState, newName);
				GUILayout.EndHorizontal();
				if (del.Count > 0) foreach (Actor.StateName m in del) map.Remove(m);
			}
			if (GUI.changed) EditorUtility.SetDirty(actor);
		}
	}
}