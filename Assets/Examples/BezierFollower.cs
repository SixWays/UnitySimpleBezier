#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;

namespace Sigtrap {
	[ExecuteInEditMode]
	public class BezierFollower : MonoBehaviour {
		private enum CorrectionMode {NONE, PIECEWISE, DIFFERENTIAL}
		public BezierSpline path;
		[Range(0f,1f)]
		public float t;
		[Header("Autofollow")]
		public bool auto = false;
		public float speed = 0.1f;
		private int dir = 1;
		[Tooltip("Rotate transform to align with spline")]
		public bool rotate = true;
		[SerializeField]
		[Tooltip("If PIECEWISE or DIFFERENTIAL, speed will be linearly corrected for stretching of spline.")]
		private CorrectionMode speedCorrection = CorrectionMode.PIECEWISE;

		void Update(){
			if (path){
				// Only use auto mode when playing; makes no sense when manually sliding t in editor
				if (auto && Application.isPlaying){
					switch (speedCorrection){
					case CorrectionMode.NONE:
					case CorrectionMode.PIECEWISE:
						// Increment t manually
						t += (speed * Time.deltaTime * dir);
						// Clamp t and flip direction if necessary
						UpdateT();
						// Get position along curve, using piecewise correction or no correction
						transform.position = path.Spline(t, speedCorrection==CorrectionMode.PIECEWISE);
						break;
					case CorrectionMode.DIFFERENTIAL:
						// Get position along curve using differential correction
						// Bezier method automatically increments t by corrected amount
						// MUST store t!
						transform.position = path.Spline(ref t, speed * Time.deltaTime * dir);
						// Clamp t and flip direction if necessary
						UpdateT();
						break;
					}
				} else {
					// If not auto-following, just apply t manually
					transform.position = path.Spline(t, speedCorrection!=CorrectionMode.NONE);
				}
				transform.forward = path.Tangent(t, speedCorrection!=CorrectionMode.NONE);
			}
		}

		void UpdateT(){
			if (t >= 1){
				t = 1;
				dir = -1;
			} else if (t <= 0){
				t = 0;
				dir = 1;
			}
		}

		#if UNITY_EDITOR
		void OnDrawGizmosSelected(){
			path.DrawPath();
		}
		#endif
	}
}