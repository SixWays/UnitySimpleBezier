using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class BezierNode : MonoBehaviour {
	public enum Symmetry {FULL, ANGLE, NONE}

	#region Settings
	[SerializeField]
	private Symmetry _symmetry = Symmetry.FULL;
	private Symmetry _lastSymmetry = Symmetry.FULL;
	public Symmetry symmetry {get {return _symmetry;}}
	#endregion

	#region Handle properties
	[SerializeField][HideInInspector]
	private Vector3 _h1 = new Vector3(0,0,-1f);
	public Vector3 h1 {
		get {
			return LocalToGlobal(_h1);
		}
		set {
			_h1 = GlobalToLocal(value);
			DoSymmetry(_h1, ref _h2);
		}
	}
	[SerializeField][HideInInspector]
	private Vector3 _h2 = new Vector3(0,0,1f);
	public Vector3 h2 {
		get {
			return LocalToGlobal(_h2);
		}
		set {
			_h2 = GlobalToLocal(value);
			DoSymmetry(_h2, ref _h1);
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
			}
			return _parent;
		}
	}
	#endregion

	#region Handle Manipulation Methods
	private Vector3 LocalToGlobal(Vector3 lHandle){
		return transform.TransformPoint(parent.strengthScale * lHandle);
	}
	private Vector3 GlobalToLocal(Vector3 gHandle){
		return transform.InverseTransformPoint(gHandle) / parent.strengthScale;
	}
	private void DoSymmetry(Vector3 master, ref Vector3 slave){
		Symmetry hm = _symmetry;
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
	private void Update(){
		if (_symmetry != _lastSymmetry){
			_lastSymmetry = _symmetry;
			DoSymmetry(_h1, ref _h2);
		}
	}
#endif
}
