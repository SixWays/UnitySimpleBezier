#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;

namespace Sigtrap.Bezier {
	[ExecuteInEditMode]
	public class BezierFollower : MonoBehaviour {
		public enum LoopMode {NONE, LOOP, BOUNCE}
		public enum RotateMode {NONE, TANGENT, TRANSFORM}
		public BezierSpline path;
		[Tooltip("+1 to follow path forward, -1 to follow backwards")]
		public int forward = 1;
		[Range(0f,1f)]
		public float t;
		[Header("Autofollow")]
		public bool auto = false;
		public bool autofollow {
			get {return auto;}
			set {auto = value;}
		}
		public float speed = 0.1f;
		private int dir = 1;
		[Tooltip("If on, speed will be linearly corrected for stretching of spline.")]
		private bool speedCorrection = true;
		public LoopMode loopMode = LoopMode.LOOP;
		[Tooltip("TANGENT: Rotate transform to align with spline\nTRANSFORM: Slerp rotation between node transforms")]
		public RotateMode rotate = RotateMode.NONE;

		void LateUpdate(){
			if (path){
				// Only use auto mode when playing; makes no sense when manually sliding t in editor
				if (auto && Application.isPlaying){
					if (speedCorrection){
						// Increment t manually
						t += (speed * Time.deltaTime * dir * forward);
						// Clamp t and flip direction if necessary
						UpdateT();
						// Get position along curve, using piecewise correction or no correction
						transform.position = path.Spline(t, true);
					}
				} else {
					// If not auto-following, just apply t manually
					transform.position = path.Spline(t, speedCorrection);
				}
				switch (rotate){
					case RotateMode.TANGENT:
						transform.forward = forward * path.Tangent(t, speedCorrection);
						break;
					case RotateMode.TRANSFORM:
						transform.rotation = path.Rotation(t);
						break;
				}
			}
		}

		void UpdateT(){
			if (t >= 1){
				switch (loopMode){
					case LoopMode.NONE:
						t = 1;
						dir = 0;
						break;
					case LoopMode.BOUNCE:
						t = 1;
						dir = -1 * forward;
						break;
					case LoopMode.LOOP:
						t = 0;
						dir = 1 * forward;
						break;
				}
			} else if (t <= 0){
				switch (loopMode){
					case LoopMode.NONE:
						t = 0;
						dir = 0;
						break;
					case LoopMode.BOUNCE:
						t = 0;
						dir = 1 * forward;
						break;
					case LoopMode.LOOP:
						t = 1;
						dir = -1 * forward;
						break;
				}
			}
		}

		#if UNITY_EDITOR
		void OnDrawGizmosSelected(){
			if (path) {
				path.DrawPath();
			}
		}
		#endif
	}
}