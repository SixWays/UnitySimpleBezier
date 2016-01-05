using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Sigtrap {
	public class BezierCurve : MonoBehaviour {
		public enum HandleType {CUBE, SPHERE}

		[Header("Runtime Settings")]
		[SerializeField]
		private float _strengthScale = 1;
		public float strengthScale {get {return _strengthScale;}}
		[SerializeField][Tooltip("Higher values give more accurate instantaneous speeds/lengths along path. Shape is always accurate.")]
		private int _integrationSegments = 10;

	#if UNITY_EDITOR
		[Header("Handles Preview")]
		[SerializeField][Range(1,25)][Tooltip("Only affects preview. Does not affect shape at runtime.")]
		private int _previewSegments = 10;
		[SerializeField]
		private float _handleScale = 1;
		public float handleSize {get {return _handleScale * 0.1f;}}
		//[SerializeField]
		private Color _curveColor = Color.magenta;
		public Color curveColor {get {return _curveColor;}}
		//[SerializeField]
		private Color _handleColor = Color.green;
		public Color handleColor {get {return _handleColor;}}
		[SerializeField]
		private HandleType _handleWidget = HandleType.SPHERE;
		public HandleType handleWidget {get {return _handleWidget;}}
		[Header("Node Preview")]
		[SerializeField]
		private bool _showAxes = true;
		[SerializeField]
		private float _axesScale = 1;

		/// <summary>
		/// Called by editor and/or child nodes when selected to draw path and all nodes
		/// </summary>
		public void OnDrawGizmosSelected(){
			BezierNode[] nodes = GetComponentsInChildren<BezierNode>();
			Color gcol = Gizmos.color;

			// Draw handle vectors and caps per node
			// Handle position gizmos are drawn by BezierEditor
			foreach (BezierNode bn in nodes){
				Gizmos.color = handleColor;
				Gizmos.DrawLine(bn.transform.position, bn.h1);
				Gizmos.DrawLine(bn.transform.position, bn.h2);
				
				System.Action<Vector3> drawSolid = null;
				System.Action<Vector3> drawWire = null;
				
				// Select handle primitive
				switch (handleWidget){
				case BezierCurve.HandleType.CUBE:
					drawSolid = (h) => {
						Gizmos.DrawCube(h, Vector3.one * handleSize);
					};
					drawWire = (h) => {
						Gizmos.DrawWireCube(h, Vector3.one * handleSize);
					};
					break;
				case BezierCurve.HandleType.SPHERE:
					drawSolid = (h) => {
						Gizmos.DrawSphere(h, handleSize);
					};
					drawWire = (h) => {
						Gizmos.DrawWireSphere(h, handleSize);
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

				if (_showAxes){
					Gizmos.color = Color.blue;
					Gizmos.DrawLine(bn.transform.position, bn.transform.position + (bn.transform.right * _axesScale));
					Gizmos.DrawLine(bn.transform.position, bn.transform.position - (bn.transform.right * _axesScale));
					Gizmos.color = Color.red;
					Gizmos.DrawLine(bn.transform.position, bn.transform.position + (bn.transform.forward * _axesScale));
				}
			}

			// Draw spline
			DrawPath(nodes);
			Gizmos.color = gcol;
		}
		/// <summary>
		/// Call this in OnDrawGizmosSelected() to display path in editor without handles
		/// e.g. call from a script that follows this path
		/// Must be used in #if UNITY_EDITOR #endif preprocessor command!
		/// </summary>
		public void DrawPath(){
			BezierNode[] nodes = GetComponentsInChildren<BezierNode>();
			Color gcol = Gizmos.color;
			DrawPath(nodes);
			Gizmos.color = gcol;
		}
		private void DrawPath(BezierNode[] nodes){
			Gizmos.color = curveColor;
			for (int i=0; i<nodes.Length-1; ++i){
				BezierNode prev = nodes[i];
				BezierNode next = nodes[i+1];
				
				Vector3 lastPos = prev.transform.position;
				Vector3 nextPos;
				float t=0;
				
				for (int j=0; j<_previewSegments; ++j){
					t += 1f/_previewSegments;
					nextPos = Bezier(t, prev.transform.position, prev.h2, next.h1, next.transform.position);
					Gizmos.DrawLine(lastPos, nextPos);
					lastPos = nextPos;
				}
			}
		}
	#endif

		/// <summary>
		/// Calculate position at t along a manually defined Bezier curve
		/// </summary>
		/// <param name="t">T.</param>
		/// <param name="start">Start.</param>
		/// <param name="handle1">Handle1.</param>
		/// <param name="handle2">Handle2.</param>
		/// <param name="end">End.</param>
		public static Vector3 Bezier(float t, Vector3 start, Vector3 handle1, Vector3 handle2, Vector3 end){
			float u = 1f - t;
			Vector3 result = start * u*u*u;
			result += (3 * u*u * t * handle1);
			result += (3 * u * t*t * handle2);
			result += (t*t*t * end);
			return result;
		}
		/// <summary>
		/// Calculate position at t along this Bezier curve
		/// </summary>
		/// <param name="t">T.</param>
		public Vector3 Bezier(float t){
			if (t< 0 || t > 1f){
				throw new System.ArgumentOutOfRangeException("t is fraction along total curve and must be 0<=t<=1");
			}

			BezierNode[] nodes = GetComponentsInChildren<BezierNode>();
			if (nodes == null || nodes.Length == 0){
				throw new MissingComponentException("No BezierNodes found parented to BezierCurve. Cannot calculate spline.");
			}

			if (t==0){
				return nodes[0].transform.position;
			}
			if (t==1){
				return nodes[nodes.Length-1].transform.position;
			}

			// Estimate length of each sector
			float[] sectors = new float[nodes.Length-1];
			float[,] segments = new float[sectors.Length,_integrationSegments];
			// Loop over node pairs
			for (int i=0; i<nodes.Length-1; ++i){
				BezierNode prev = nodes[i];
				BezierNode next = nodes[i+1];
				
				Vector3 lastPos = prev.transform.position;
				Vector3 nextPos = next.transform.position;

				sectors[i] = 0;
				Vector3 p0 = lastPos;
				Vector3 p1 = lastPos;
				float p = 0;
				// Loop over integration segments along sector
				for (int j=0; j<_integrationSegments; ++j){
					p1 = Bezier(p, lastPos, prev.h2, next.h1, nextPos);
					// Store local length
					segments[i,j] = Vector3.Distance(p1,p0);
					sectors[i] += segments[i,j];
					// Move to next segment
					p0 = p1;
					p += 1f/(float)_integrationSegments;
				}
				// Get each local length as fraction, for approximate t remapping
				for (int j=0; j<_integrationSegments; ++j){
					segments[i,j] /= sectors[i];
				}
			}

			// Estimate total length
			float length = 0;
			foreach (float f in sectors){
				length += f;
			}

			// Work out which sector t falls in
			t *= length;
			length = 0;
			int sector = 0;
			for (; sector<sectors.Length; ++sector){
				if (length + sectors[sector] > t){
					break;
				}
				length += sectors[sector];
			}

			// Remove offset of previous sector length and get fractional value of current sector length
			t = (t - length) / sectors[sector];
			float t_0 = t;
			// Estimate mapping of t_linear to t_constSpeed using segment lengths
			float segTotal = 0;
			for (int segment=0; segment<_integrationSegments; ++segment){
				float t0 = segTotal;
				float t1 = segTotal + segments[sector, segment];
				if (t1 > t){
					// Remap t
					// Remove offset to get remainder
					t -= t0;
					// Get scale of segment length relative to average
					// Equivalent to seglength / (1/intSegs)
					float segScale = (t1 - t0) * (float)_integrationSegments;
					// Rescale remainder
					t /= segScale;
					// Add linear offset back on
					t += ((float)segment/(float)_integrationSegments);
					break;
				}
				segTotal = t1;
			}
			Debug.Log(t_0.ToString()+" "+t.ToString());
			return Bezier(t, nodes[sector].transform.position, nodes[sector].h2, nodes[sector+1].h1, nodes[sector+1].transform.position);
		}
	}
}
