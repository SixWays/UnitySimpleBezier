#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;

// Editor code for Bezier Curve
namespace Sigtrap {
	public partial class BezierSpline : MonoBehaviour {
		[Header("Editor Settings")]

		[SerializeField]
		[Range(1,25)]
		[Tooltip("Only affects preview. Does not affect shape at runtime.")]
		private int _previewSegments = 10;

		[SerializeField]
		private float _handleScale = 1;
		private float handleScale {get {return _handleScale * 0.1f;}}
		[SerializeField]
		private HandleType _handleWidget = HandleType.SPHERE;
		[SerializeField]
		private bool _showNodeAxes = true;
		[SerializeField]
		private float _nodeAxesScale = 1;
		[SerializeField]
		private bool _showTangents = false;
		[SerializeField]
		private float _tangentScale = 1f;

		/// <summary>
		/// Called by editor and/or child nodes when selected to draw path and all nodes
		/// </summary>
		public void OnDrawGizmosSelected(){
			GetNodes();
			Color gcol = Gizmos.color;

			// Draw handle vectors and caps per node
			// Handle position gizmos are drawn by BezierEditor
			foreach (BezierNode bn in _nodes){
				Gizmos.color = Color.green;
				Gizmos.DrawLine(bn.transform.position, bn.h1);
				Gizmos.DrawLine(bn.transform.position, bn.h2);
				
				System.Action<Vector3> drawSolid = null;
				System.Action<Vector3> drawWire = null;
				
				// Select handle primitive
				switch (_handleWidget){
				case BezierSpline.HandleType.CUBE:
					drawSolid = (h) => {
						Gizmos.DrawCube(h, Vector3.one * handleScale);
					};
					drawWire = (h) => {
						Gizmos.DrawWireCube(h, Vector3.one * handleScale);
					};
					break;
				case BezierSpline.HandleType.SPHERE:
					drawSolid = (h) => {
						Gizmos.DrawSphere(h, handleScale);
					};
					drawWire = (h) => {
						Gizmos.DrawWireSphere(h, handleScale);
					};
					break;
				}
				
				// Select combination of solid/wireframe according to symmetry, and draw gizmos
				switch (bn.symmetry){
				case BezierNode.Symmetry.FULL:
					drawSolid(bn.h1);
					drawSolid(bn.h2);
					break;
				case BezierNode.Symmetry.ANGLE:
					drawSolid(bn.h1);
					drawWire(bn.h2);
					break;
				case BezierNode.Symmetry.NONE:
					drawWire(bn.h1);
					drawWire(bn.h2);
					break;
				}

				if (_showNodeAxes){
					Gizmos.color = Color.red;
					Gizmos.DrawLine(bn.transform.position, bn.transform.position + (bn.transform.right * _nodeAxesScale));
					Gizmos.DrawLine(bn.transform.position, bn.transform.position - (bn.transform.right * _nodeAxesScale));
					Gizmos.color = Color.blue;
					Gizmos.DrawLine(bn.transform.position, bn.transform.position + (bn.transform.forward * _nodeAxesScale));
				}
			}

			// Draw spline
			DrawPath(_nodes);
			try {
				Gizmos.DrawLine(SceneView.lastActiveSceneView.camera.ViewportToWorldPoint(new Vector3(0.1f,0.5f,0f)),
				                SceneView.lastActiveSceneView.camera.ViewportToWorldPoint(new Vector3(0.9f,0.5f,0f)));
			} catch {}
			Gizmos.color = gcol;
		}

		/// <summary>
		/// Call this in OnDrawGizmosSelected() elsewhere to display path in editor without handles
		/// e.g. call from a script that follows this path
		/// Must be used in #if UNITY_EDITOR #endif preprocessor command!
		/// </summary>
		public void DrawPath(){
			GetNodes();
			Color gcol = Gizmos.color;
			DrawPath(_nodes);
			Gizmos.color = gcol;
		}
		private void DrawPath(BezierNode[] nodes){
			CacheIfDirty();
			Gizmos.color = Color.magenta;
			for (int i=0; i<nodes.Length-1; ++i){
				BezierNode prev = nodes[i];
				BezierNode next = nodes[i+1];
				
				Vector3 lastPos = prev.transform.position;
				Vector3 nextPos;
				float t=0;
				
				for (int j=0; j<_previewSegments; ++j){
					t += 1f/_previewSegments;
					nextPos = Curve(t, prev.transform.position, prev.h2, next.h1, next.transform.position);

					/*Color col = Color.green.ToHSV();
					col.r = Mathf.Clamp(col.r + 180 * ((1/diff)-1), 0, 360);
					col = col.ToRGB();
					Gizmos.color = col;*/
					Gizmos.color = Color.magenta;
					Gizmos.DrawLine(lastPos, nextPos);

					if (_showTangents){
						Vector3 tang = (_sectors[i].TanLocal(t,false)) * _tangentScale * 0.1f;
						Gizmos.color = Color.cyan;
						Gizmos.DrawLine(nextPos, tang+nextPos);
					}

					lastPos = nextPos;
				}
			}
		}

		[UnityEditor.Callbacks.DidReloadScripts]
		public static void OnCompiled(){
			foreach (BezierSpline bc in FindObjectsOfType<BezierSpline>()){
				bc.dirty = true;
			}
		}
	}
}
#endif