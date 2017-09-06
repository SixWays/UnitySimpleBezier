using UnityEngine;
using UnityEditor;
using System.Collections;
using Sigtrap.Bezier;

namespace Sigtrap.Bezier.Editors {
	[CustomEditor(typeof(BezierNode))]
	public class BezierNodeEditor : Editor {
		[DrawGizmo(GizmoType.Selected)]
		public void OnSceneGUI(){
			BezierNode bn = (BezierNode)target;
			Undo.RecordObject(bn, "Edit Bezier Handles");
			Quaternion _lookH1 = Quaternion.identity;
			Quaternion _lookH2 = Quaternion.identity;
			// 'Local' mode doesn't work - results in spiral behaviour on transverse handle movement
			/*
			if (Tools.pivotRotation == PivotRotation.Local){
				_lookH1 = Quaternion.LookRotation(bn.h1 - bn.transform.position, bn.transform.up);
				_lookH2 = Quaternion.LookRotation(bn.h2 - bn.transform.position, bn.transform.up);
			}
			*/
			Vector3 h1 = Handles.PositionHandle(bn.h1, _lookH1);
			if (h1 != bn.h1){
				bn.h1 = h1;
				EditorUtility.SetDirty(bn);
			}
			Vector3 h2 = Handles.PositionHandle(bn.h2, _lookH2);
			if (h2 != bn.h2){
				bn.h2 = h2;
				EditorUtility.SetDirty(bn);
			}
		}
	}
}