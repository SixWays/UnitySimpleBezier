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
		public float length {get; private set;}
		private BezierNode[] _nodes;
		private Sector[] _sectors;

		private bool _dirty = true;
		/// <summary>
		/// When true, curve data gets recached. Called by child nodes when changed.
		/// </summary>
		public bool dirty {
			// Property to avoid serialisation
			get {return _dirty;}
			set {_dirty = value;}
		}
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
			t = Mathf.Clamp01(t);
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

			t = Mathf.Clamp01(t);
			if (t==0){
				return _nodes[0].transform.position;
			}
			if (t==1){
				return _nodes[_nodes.Length-1].transform.position;
			}

			if (dirty){
				// Setup sectors, calculate constant stuff etc
				_sectors = new Sector[_nodes.Length-1];
				length = 0;
				float offset = 0;
				// Loop over node pairs
				for (int i=0; i<_nodes.Length-1; ++i){
					offset = length;
					// Setup sector. Constructor does piecewise length integration.
					_sectors[i] = new Sector(_nodes[i], _nodes[i+1], offset, _integrationSegments);
					length += _sectors[i].length;
				}
				foreach (Sector s in _sectors){
					// Set global pathlength for each sector for local transformations
					s.pathLength = length;
				}
				dirty = false;
			}

			// Work out which sector t falls in
			for (int i=0; i<_sectors.Length; ++i){
				if (_sectors[i].InSector(t)){
					return _sectors[i].Bezier(t, _constantSpeed);
				}
			}
			return Vector3.zero;
		}

		private class Sector {
			private BezierNode _start;
			private BezierNode _end;

			private float _offset;
			public float length {get; private set;}
			private float _tOffset;
			private float _tLength;
			private float[] _segmentT1s;
			public float pathLength {
				set {
					_tOffset = _offset/value;
					_tLength = length/value;
				}
			}

			private Vector3 _d1;
			private Vector3 _d2;
			private Vector3 _d3;

			public Sector(BezierNode start, BezierNode end, float offset, int integrationSegments){
				_start = start;
				_end = end;
				_offset = offset;
				_segmentT1s = new float[integrationSegments];

				// Get constant terms of derivative
				_d1 = -(3 * _start.transform.position) + (9 * _start.h2) - (9 * _end.h1) + (3 * _end.transform.position);
				_d2 = (6 * _start.transform.position) - (12 * _start.h2) + (6 * _end.h1);
				_d3 = -(3 * _start.transform.position) + (3 * _start.h2);

				// Calculate segment lengths
				Vector3 s = _start.transform.position;
				Vector3 e = _end.transform.position;
				Vector3 h2 = _start.h2;
				Vector3 h1 = _end.h1;
				Vector3 p0 = _start.transform.position;
				Vector3 p1 = _start.transform.position;
				float t = 0;
				
				// Loop over integration segments along sector
				for (int i=0; i<_segmentT1s.Length; ++i){
					p1 = BezierCurve.Bezier(t, s, h2, h1, e);
					// Store local length
					_segmentT1s[i] = Vector3.Distance(p1,p0);
					length += _segmentT1s[i];
					// Move to next segment
					p0 = p1;
					t += 1f/(float)_segmentT1s.Length;
				}
				
				// Get each local length as fraction, for approximate t remapping
				for (int i=0; i<_segmentT1s.Length; ++i){
					_segmentT1s[i] /= length;
				}
			}
			private float GlobalToLocalT(float t){
				if (_tOffset < 0 || _tLength <= 0){
					throw new System.Exception("Sector.pathLength must be set after calling Setup!");
				}
				return (t - _tOffset) / _tLength;
			}
			private float RemapPiecewise(float tLocal){
				float t0 = 0;
				float t1 = 0;
				// Find segment t resides in, and remap according to that
				for (int segment=0; segment<_segmentT1s.Length; ++segment){
					t1 += _segmentT1s[segment];
					if (t1 > tLocal){
						// Remove offset to get remainder
						tLocal -= t0;
						// Get scale of segment length relative to average
						// Equivalent to seglength / (1/intSegs)
						float segScale = (t1 - t0) * (float)_segmentT1s.Length;
						// Rescale remainder
						tLocal /= segScale;
						// Add linear offset back on
						tLocal += ((float)segment/(float)_segmentT1s.Length);
						break;
					}
					t0 = t1;
				}
				return tLocal;
			}

			/// <summary>
			/// Is the given global t within this sector?
			/// </summary>
			/// <returns><c>true</c> if given global t falls within this sector</returns>
			/// <param name="tGlobal">Global t</param>
			public bool InSector(float tGlobal){
				return (tGlobal >= _tOffset && tGlobal < (_tOffset + _tLength));
			}
			/// <summary>
			/// Calculate position from given global t, using piecewise stretch correction (or none)
			/// </summary>
			/// <param name="tGlobal">Global t</param>
			/// <param name="unstretch">If true, correct stretch with piecewise approximation</param>
			public Vector3 Bezier(float tGlobal, bool unstretch=true){
				tGlobal = GlobalToLocalT(tGlobal);
				if (unstretch){
					// Remap t
					tGlobal = RemapPiecewise(tGlobal);
				}
				return BezierCurve.Bezier(tGlobal, _start.transform.position, _start.h2, _end.h1, _end.transform.position);
			}
			/// <summary>
			/// Calculate position from current global t and global dT using differential stretch correction
			/// If no dT given, remaps t using piecewise approximation
			/// </summary>
			/// <param name="tGlobal">T global.</param>
			/// <param name="dtGlobal">Dt global.</param>
			public Vector3 Bezier(ref float tGlobal, float dtGlobal=0){
				tGlobal = Mathf.Clamp01(tGlobal);
				float tLocal = GlobalToLocalT(tGlobal);

				// Remap local t
				if (dtGlobal == 0){
					// If no dT, get piecewise-remapped approx of t
					tLocal = RemapPiecewise(tLocal);
				} else {
					// Get local derivative
					float dCdT = ((tLocal * tLocal * _d1) + (tLocal * _d2) + _d3).magnitude;
					// Transform global dT to local, then multiply by local derivative. Add transformed increment to t.
					tLocal += (dtGlobal * _tLength * dCdT);
				}

				// Transform local t back to global.
				tGlobal = _tOffset + (tLocal * _tLength);

				return Bezier(tLocal, false);
			}
		}
	}
}
