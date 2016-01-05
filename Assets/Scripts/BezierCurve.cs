using UnityEngine;
using UnityEditor;
using System.Collections;

public class BezierCurve : MonoBehaviour {
	public enum HandleType {CUBE, SPHERE}
	
	[SerializeField]
	private float _strengthScale = 1;
	public float strengthScale {get {return _strengthScale;}}

#if UNITY_EDITOR
	[Header("Handles Preview")]
	[SerializeField]
	private float _previewSegments = 10;
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

		Gizmos.color = gcol;
	}
#endif

	public static Vector3 Bezier(float t, Vector3 start, Vector3 handle1, Vector3 handle2, Vector3 end){
		float u = 1f - t;
		Vector3 result = start * u*u*u;
		result += (3 * u*u * t * handle1);
		result += (3 * u * t*t * handle2);
		result += (t*t*t * end);
		return result;
	}
}
