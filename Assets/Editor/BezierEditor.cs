using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Sigtrap {
	[CustomEditor(typeof(BezierNode))]
	public class BezierEditor : Editor {
		/*Quaternion _lookH1 = Quaternion.identity;
		Quaternion _lookH2 = Quaternion.identity;*/

		[DrawGizmo(GizmoType.Selected)]
		public void OnSceneGUI(){
			BezierNode bn = (BezierNode)target;
			Undo.RecordObject(bn, "Edit Bezier Handles");
			Quaternion _lookH1 = Quaternion.identity;
			Quaternion _lookH2 = Quaternion.identity;
			if (Tools.pivotRotation == PivotRotation.Local){
				_lookH1 = Quaternion.LookRotation(bn.h1 - bn.transform.position);
				_lookH2 = Quaternion.LookRotation(bn.h2 - bn.transform.position);
			}
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