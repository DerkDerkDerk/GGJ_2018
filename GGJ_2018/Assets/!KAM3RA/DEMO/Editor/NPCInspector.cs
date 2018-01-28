//////////////////////////////////////////////////////////////
//  KAM3RA Third-Person Camera System
// 	Copyright © 2013 Regress Software
//////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;
using KAM3RA;

[CustomEditor(typeof(NPC))]
public class NPCInspector : ActorInspector 
{
	public override void OnInspectorGUI() 
	{ 
		base.OnInspectorGUI(); 		
		EditorGUI.indentLevel 	-= 3;
		NPC npc					= target as NPC;
		npc.randomNames 		= (TextAsset)EditorGUILayout.ObjectField("Random Names", npc.randomNames, typeof(TextAsset), true);
		npc.pathNodes 			= (TextAsset)EditorGUILayout.ObjectField("Path Nodes",   npc.pathNodes,   typeof(TextAsset), true);
		npc.randomScale 		= UserInspector.RangeField("Random Scale", npc.randomScale);
		if (GUI.changed) EditorUtility.SetDirty(npc);
	}
}