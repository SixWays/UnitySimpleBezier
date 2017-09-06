using UnityEngine;
using UnityEditor;
using System.Collections;
using Sigtrap.Bezier;

namespace Sigtrap.Bezier.Editors {
	[CustomEditor(typeof(BezierSpline))]
	public class BezierSplineEditor : Editor {
		public override void OnInspectorGUI(){
			base.OnInspectorGUI();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Nodes");
			if (GUILayout.Button("Add Node")){
				GameObject node = new GameObject("Node");
				node.AddComponent<BezierNode>();
				node.transform.SetParent((target as BezierSpline).transform, false);
				Undo.RegisterCreatedObjectUndo(node, "Create Bezier Node");
			}
		}
	}
}