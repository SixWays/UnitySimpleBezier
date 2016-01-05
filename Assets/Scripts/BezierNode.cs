using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class BezierNode : MonoBehaviour {
	private enum Symmetry {FULL, ANGLE, NONE}

	#region Settings
	[SerializeField]
	private float _strengthScale = 1;
	[SerializeField]
	private Symmetry _symmetry = Symmetry.FULL;
	private Symmetry _lastSymmetry = Symmetry.FULL;
	#endregion

	#region Handle properties
	[SerializeField][HideInInspector]
	private Vector3 _h1 = new Vector3(0,0,-1f);
	public Vector3 h1 {
		get {
			return transform.TransformPoint(_h1);
		}
		set {
			_h1 = transform.InverseTransformPoint(value);
			DoSymmetry(_h1,_h2,(h)=>{
				h2=h;
			});
		}
	}
	[SerializeField][HideInInspector]
	private Vector3 _h2 = new Vector3(0,0,1f);
	public Vector3 h2 {
		get {
			return transform.TransformPoint(_h2);
		}
		set {
			_h2 = transform.InverseTransformPoint(value);
			DoSymmetry(_h2,_h1,(h)=>{
				h1=h;
			});
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

	#region Handle manipulation methods
	private void DoSymmetry(Vector3 master, Vector3 slave, System.Action<Vector3> setProperty){
		Symmetry hm = _symmetry;
		_symmetry = Symmetry.NONE;	// Temporarily disable symmetry to avoid recursion
		switch (hm){
		case Symmetry.FULL:
			// Mirror entire local vector
			setProperty( transform.TransformPoint(-master) );
			break;
		case Symmetry.ANGLE:
			// Mirror local vector, then scale to original length
			setProperty( transform.TransformPoint(-master * (slave.magnitude / master.magnitude)) );
			break;
		}
		_symmetry = hm;
	}
	private Vector3 LocalToGlobal(Vector3 handle){
		return transform.InverseTransformPoint(transform.localPosition + (handle * _strengthScale * parent.strengthScale));
	}
	private Vector3 GlobalToLocal(Vector3 pos){
		return transform.TransformPoint((pos - transform.position) / (_strengthScale * parent.strengthScale));
	}
	#endregion

#if UNITY_EDITOR
	private void OnDrawGizmosSelected(){
		// Tell parent to draw gizmos. Parent will tell this node and all others to DrawGizmos.
		parent.OnDrawGizmosSelected();
	}
	/// <summary>
	/// Draw bezier handle gizmos. Does NOT draw movement axis gizmos - done by BezierEditor
	/// Should be called only by BezierCurve parent.
	/// </summary>
	public void DrawGizmos(){
		Color gcol = Gizmos.color;
		Gizmos.color = parent.handleColor;
		Gizmos.DrawLine(transform.position, h1);
		Gizmos.DrawLine(transform.position, h2);

		System.Action<Vector3> drawSolid = null;
		System.Action<Vector3> drawWire = null;

		// Select handle primitive
		switch (parent.handleWidget){
		case BezierCurve.HandleType.CUBE:
			drawSolid = (h) => {
				Gizmos.DrawCube(h, Vector3.one * parent.handleSize);
			};
			drawWire = (h) => {
				Gizmos.DrawWireCube(h, Vector3.one * parent.handleSize);
			};
			break;
		case BezierCurve.HandleType.SPHERE:
			drawSolid = (h) => {
				Gizmos.DrawSphere(h, parent.handleSize);
			};
			drawWire = (h) => {
				Gizmos.DrawWireSphere(h, parent.handleSize);
			};
			break;
		}

		// Select combination of solid/wireframe according to symmetry, and draw gizmos
		switch (_symmetry){
		case Symmetry.FULL:
			drawSolid(h1);
			drawSolid(h2);
			break;
		case Symmetry.ANGLE:
			drawSolid(h1);
			drawWire(h2);
			break;
		case Symmetry.NONE:
			drawWire(h1);
			drawWire(h2);
			break;
		}
		Gizmos.color = gcol;
	}
	private void Update(){
		if (_symmetry != _lastSymmetry){
			_lastSymmetry = _symmetry;
			DoSymmetry(_h1, _h2, (h)=>{
				h2 = h;
			});
		}
	}
#endif
}
