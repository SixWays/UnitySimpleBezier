using UnityEngine;
using System.Collections;

namespace Sigtrap {
	[ExecuteInEditMode]
	public class BezierNode : MonoBehaviour {
		public enum Symmetry {FULL, ANGLE, NONE}

		#region Settings
		[SerializeField]
		private Symmetry _symmetry = Symmetry.FULL;
		private Symmetry _lastSymmetry = Symmetry.FULL;
		public Symmetry symmetry {get {return _symmetry;}}
		#endregion

		private bool _dirty1 = true;
		private bool _dirty2 = true;
		public bool dirty {get {return _dirty1 || _dirty2;}}

		#region Handle properties
		[SerializeField][HideInInspector]
		private Vector3 _h1 = new Vector3(0,0,-1f);
		[SerializeField][HideInInspector]
		private Vector3 _h1g = Vector3.zero;
		public Vector3 h1 {
			get {
				if (_dirty1 || _lastStrength != parent.strengthScale){
					_h1g = LocalToGlobal(_h1);
					_dirty1 = false;
				}
				return _h1g;
			}
			set {
				_h1 = GlobalToLocal(value);
				DoSymmetry(_h1, ref _h2, ref _dirty2);
				_dirty1 = true;
				parent.dirty = true;
			}
		}
		[SerializeField][HideInInspector]
		private Vector3 _h2 = new Vector3(0,0,1f);
		[SerializeField][HideInInspector]
		private Vector3 _h2g = Vector3.zero;
		public Vector3 h2 {
			get {
				if (_dirty2 || _lastStrength != parent.strengthScale){
					_h2g = LocalToGlobal(_h2);
					_dirty2 = false;
				}
				return _h2g;
			}
			set {
				_h2 = GlobalToLocal(value);
				DoSymmetry(_h2, ref _h1, ref _dirty1);
				_dirty2 = true;
				parent.dirty = true;
			}
		}
		#endregion

		#region Curve parent
		private Transform _parentTrans;
		private BezierCurve _parent;
		private BezierCurve parent {
			get {
				// Check for changed parent
				if (_parent == null || transform.parent != _parentTrans){
					_parentTrans = transform.parent;
					_parent = _parentTrans.GetComponent<BezierCurve>();
					if (_parent == null){
						throw new MissingComponentException("BezierNode must be direct child of a BezierCurve!");
					}
					_parent.dirty = true;
					_lastStrength = _parent.strengthScale;
				}
				return _parent;
			}
		}
		private float _lastStrength = 1;	// Used to check when parent strengthScale changes and recalc handles
		#endregion

		#region Handle Manipulation Methods
		private Vector3 LocalToGlobal(Vector3 lHandle){
			return transform.TransformPoint(parent.strengthScale * lHandle);
		}
		private Vector3 GlobalToLocal(Vector3 gHandle){
			return transform.InverseTransformPoint(gHandle) / parent.strengthScale;
		}
		/// <summary>
		/// Apply symmetry, if needed, from master to slave. Mark dirty if altered.
		/// </summary>
		/// <param name="master">Master.</param>
		/// <param name="slave">Slave.</param>
		/// <param name="dirt">Dirty flag.</param>
		private void DoSymmetry(Vector3 master, ref Vector3 slave, ref bool dirt){
			Symmetry hm = _symmetry;
			dirt = _symmetry != Symmetry.NONE;
			_symmetry = Symmetry.NONE;	// Temporarily disable symmetry to avoid recursion
			switch (hm){
			case Symmetry.FULL:
				// Mirror entire local vector
				slave = -master;
				break;
			case Symmetry.ANGLE:
				// Mirror local vector, then scale to original length
				slave = (-master * (slave.magnitude / master.magnitude));
				break;
			}
			_symmetry = hm;
		}
		#endregion

	#if UNITY_EDITOR
		private void OnDrawGizmosSelected(){
			// Tell parent to draw gizmos. Parent will tell this node and all others to DrawGizmos.
			parent.OnDrawGizmosSelected();
		}
	#endif
		private void Update(){
	#if UNITY_EDITOR
			if (_symmetry != _lastSymmetry){
				_lastSymmetry = _symmetry;
				DoSymmetry(_h1, ref _h2, ref _dirty2);
			}
	#endif
			if (transform.hasChanged){
				_dirty1 = _dirty2 = true;
				transform.hasChanged = false;
				parent.dirty = true;
			}
		}
	}
}