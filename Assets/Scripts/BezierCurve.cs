using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Sigtrap {
	public class BezierCurve : MonoBehaviour {
		public enum HandleType {CUBE, SPHERE}

		#region Curve settings
		[Header("Curve Settings")]
		[SerializeField]
		private float _strengthScale = 1;
		public float strengthScale {get {return _strengthScale;}}

		[SerializeField]
		[Tooltip("Internally corrects for path stretch. Makes position along path linearly proportional to t (approx).")]
		private bool _constantSpeed = true;

		[SerializeField]
		[Tooltip("Higher values increase accuracy of constant speed mode. Shape is always accurate.")]
		private int _integrationSegments = 100;
		#endregion

		#region Cached curve data
		private BezierNode[] _nodes;
		private float _pathLength = 1;
		private float[] _sectors;
		private float[,] _segments;

		/// <summary>
		/// When true, curve data gets recached. Called by child nodes when changed.
		/// </summary>
		[HideInInspector]
		public bool dirty = true;
		#endregion

#if UNITY_EDITOR
		#region Editor settings
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
		#endregion

		/// <summary>
		/// Called by editor and/or child nodes when selected to draw path and all nodes
		/// </summary>
		public void OnDrawGizmosSelected(){
			_nodes = GetComponentsInChildren<BezierNode>();
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
				case BezierCurve.HandleType.CUBE:
					drawSolid = (h) => {
						Gizmos.DrawCube(h, Vector3.one * handleScale);
					};
					drawWire = (h) => {
						Gizmos.DrawWireCube(h, Vector3.one * handleScale);
					};
					break;
				case BezierCurve.HandleType.SPHERE:
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
					Gizmos.color = Color.blue;
					Gizmos.DrawLine(bn.transform.position, bn.transform.position + (bn.transform.right * _nodeAxesScale));
					Gizmos.DrawLine(bn.transform.position, bn.transform.position - (bn.transform.right * _nodeAxesScale));
					Gizmos.color = Color.red;
					Gizmos.DrawLine(bn.transform.position, bn.transform.position + (bn.transform.forward * _nodeAxesScale));
				}
			}

			// Draw spline
			DrawPath(_nodes);
			Gizmos.color = gcol;
		}
		/// <summary>
		/// Call this in OnDrawGizmosSelected() to display path in editor without handles
		/// e.g. call from a script that follows this path
		/// Must be used in #if UNITY_EDITOR #endif preprocessor command!
		/// </summary>
		public void DrawPath(){
			_nodes = GetComponentsInChildren<BezierNode>();
			Color gcol = Gizmos.color;
			DrawPath(_nodes);
			Gizmos.color = gcol;
		}
		private void DrawPath(BezierNode[] nodes){
			Gizmos.color = Color.magenta;
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
			if (dirty){
				_nodes = GetComponentsInChildren<BezierNode>();
			}
			if (_nodes == null || _nodes.Length == 0){
				throw new MissingComponentException("No BezierNodes found parented to BezierCurve. Cannot calculate spline.");
			}

			t = Mathf.Clamp(t, 0f, 1f);
			if (t==0){
				return _nodes[0].transform.position;
			}
			if (t==1){
				return _nodes[_nodes.Length-1].transform.position;
			}

			if (dirty){
				ProcessCurve();
			}

			float length = _pathLength;
			// Work out which sector t falls in
			t *= length;
			length = 0;
			int sector = 0;
			for (; sector<_sectors.Length; ++sector){
				if (length + _sectors[sector] > t){
					break;
				}
				length += _sectors[sector];
			}

			// Remove offset of previous sector length and get fractional value of current sector length
			t = (t - length) / _sectors[sector];
			// Estimate mapping of t_linear to t_constSpeed using segment lengths
			float segTotal = 0;
			for (int segment=0; segment<_integrationSegments; ++segment){
				float t0 = segTotal;
				float t1 = segTotal + _segments[sector, segment];
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
			dirty = false;
			return Bezier(t, _nodes[sector].transform.position, _nodes[sector].h2, _nodes[sector+1].h1, _nodes[sector+1].transform.position);
		}

		private void ProcessCurve(){
			// Estimate length of each sector
			_sectors = new float[_nodes.Length-1];
			_segments = new float[_sectors.Length,_integrationSegments];
			// Loop over node pairs
			for (int i=0; i<_nodes.Length-1; ++i){
				BezierNode prev = _nodes[i];
				BezierNode next = _nodes[i+1];
				
				Vector3 lastPos = prev.transform.position;
				Vector3 nextPos = next.transform.position;
				
				_sectors[i] = 0;
				Vector3 p0 = lastPos;
				Vector3 p1 = lastPos;
				float p = 0;
				// Loop over integration segments along sector
				for (int j=0; j<_integrationSegments; ++j){
					p1 = Bezier(p, lastPos, prev.h2, next.h1, nextPos);
					// Store local length
					_segments[i,j] = Vector3.Distance(p1,p0);
					_sectors[i] += _segments[i,j];
					// Move to next segment
					p0 = p1;
					p += 1f/(float)_integrationSegments;
				}
				// Get each local length as fraction, for approximate t remapping
				for (int j=0; j<_integrationSegments; ++j){
					_segments[i,j] /= _sectors[i];
				}
			}
			
			// Estimate total length
			_pathLength = 0;
			foreach (float f in _sectors){
				_pathLength += f;
			}
		}
	}
}
