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
		[Header("Autofollow")]
		public bool auto = false;
		public float speed = 0.1f;
		private int dir = 1;

		void Update(){
			if (path){
				if (auto){
					t += (speed * Time.deltaTime * dir);
					if (t >= 1){
						t = 1;
						dir = -1;
					} else if (t <= 0){
						t = 0;
						dir = 1;
					}
				}
				transform.position = path.Bezier(t);
			}
		}

		#if UNITY_EDITOR
		void OnDrawGizmosSelected(){
			path.DrawPath();
		}
		#endif
	}
}