#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;

namespace Sigtrap {
	[ExecuteInEditMode]
	public class BezierFollower : MonoBehaviour {
		public BezierCurve path;
		[Range(0f,1f)]
		public float t;

		void Update(){
			if (path)
				transform.position = path.Bezier(t);
		}

		#if UNITY_EDITOR
		void OnDrawGizmosSelected(){
			path.DrawPath();
		}
		#endif
	}
}