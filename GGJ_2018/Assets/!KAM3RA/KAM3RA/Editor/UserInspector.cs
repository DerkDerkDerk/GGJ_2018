//////////////////////////////////////////////////////////////
//  KAM3RA Third-Person Camera System
// 	Copyright © 2013 Regress Software
//////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace KAM3RA 
{ 
	[CustomEditor(typeof(User))]
	public class UserInspector : Editor 
	{
		public override void OnInspectorGUI() 
		{ 
			EditorGUIUtility.LookLikeControls();
			User user				= target as User;
			user.sensitivity 		= Vector3Field("Sensitivity", user.sensitivity);
			user.damping 			= Vector3Field("Damping", user.damping);
			user.lookRange 			= UserInspector.RangeField("Look Range", user.lookRange);
			user.zoomRange 			= UserInspector.RangeField("Zoom Range", user.zoomRange);
			user.zoomLerpSpeed  	= EditorGUILayout.FloatField("Zoom Lerp Speed", user.zoomLerpSpeed);
			EditorGUILayout.LabelField("Mouse Button Control");
			EditorGUI.indentLevel 	++;
			user.actorControl 		= (User.Button)EditorGUILayout.EnumPopup("Actor",  user.actorControl);
			user.cameraControl 		= (User.Button)EditorGUILayout.EnumPopup("Camera", user.cameraControl);
			EditorGUI.indentLevel 	--;
			user.maxMouseVelocity  	= Vector3Field("Max Mouse Velocity", user.maxMouseVelocity);
			EditorGUILayout.Space();
			if (GUILayout.Button("Align"))
			{
				AlignSceneCamera(user.transform);
			}
			// uncomment to see basic info
			/*
			EditorGUILayout.LabelField("Status:");
			EditorGUI.indentLevel 	++;
			EditorGUILayout.TextArea(user.Status, GUILayout.Height(50));		
			*/
			if (GUI.changed) EditorUtility.SetDirty(user);
		}
		public static Vector3 Vector3Field(string label, Vector3 v)
		{
			int indent = EditorGUI.indentLevel;
			EditorGUILayout.LabelField(label);
			EditorGUI.indentLevel += 1;
			GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("X", GUILayout.Width(22)); v.x = EditorGUILayout.FloatField(v.x);
			EditorGUILayout.LabelField("Y", GUILayout.Width(22)); v.y = EditorGUILayout.FloatField(v.y);
			EditorGUILayout.LabelField("Z", GUILayout.Width(22)); v.z = EditorGUILayout.FloatField(v.z);
			GUILayout.EndHorizontal();
			EditorGUI.indentLevel = indent;
			return v;
		}
		public static Range RangeField(string label, Range range)
		{
			int indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			EditorGUILayout.LabelField(label);
			EditorGUI.indentLevel = 1;
			GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Min", GUILayout.Width(34)); range.min = EditorGUILayout.FloatField(range.min);
			EditorGUILayout.LabelField("Max", GUILayout.Width(34)); range.max = EditorGUILayout.FloatField(range.max);
			GUILayout.EndHorizontal();
			EditorGUI.indentLevel = indent;
			return range;
		}
		public static void AlignSceneCamera(Transform transform) 
		{ 
			ArrayList views = SceneView.sceneViews;
			if (views != null)
			{
				for (int i = 0; i < views.Count; i++)
				{
					SceneView m = (SceneView)views[i];
					m.AlignViewToObject(transform);
				}
			}
		}
	}
}