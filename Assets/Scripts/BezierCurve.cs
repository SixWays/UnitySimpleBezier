using UnityEngine;
using UnityEditor;
using System.Collections;

public class BezierCurve : MonoBehaviour {
	public enum HandleType {CUBE, SPHERE}
	
	[SerializeField]
	private float _strengthScale = 1;
	public float strengthScale {get {return _strengthScale;}}

#if UNITY_EDITOR
	[Header("Preview")]
	[SerializeField]
	private float _previewSegments = 10;
	[SerializeField]
	private float _handleScale = 1;
	public float handleSize {get {return _handleScale * 0.1f;}}
	[SerializeField]
	private Color _curveColor = Color.magenta;
	public Color curveColor {get {return _curveColor;}}
	[SerializeField]
	private Color _handleColor = Color.green;
	public Color handleColor {get {return _handleColor;}}
	[SerializeField]
	private HandleType _handleWidget = HandleType.SPHERE;
	public HandleType handleWidget {get {return _handleWidget;}}

	public void OnDrawGizmosSelected(){
		BezierNode[] nodes = GetComponentsInChildren<BezierNode>();
		foreach (BezierNode bn in nodes){
			bn.DrawGizmos();
		}
		for (int i=0; i<nodes.Length-1; ++i){
			BezierNode prev = nodes[i];
			BezierNode next = nodes[i+1];
			
			Vector3 lastPos = prev.transform.position;
			Vector3 nextPos;
			float t=0;
			
			for (int j=0; j<_previewSegments; ++j){
				t += 1f/_previewSegments;
				nextPos = Bezier(t, prev.transform.position, prev.h2, next.h1, next.transform.position);
				Gizmos.color = curveColor;
				Gizmos.DrawLine(lastPos, nextPos);
				lastPos = nextPos;
			}
		}
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
