#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;

namespace Sigtrap {
	[ExecuteInEditMode]
	public class BezierFollower : MonoBehaviour {
		public enum LoopMode {NONE, LOOP, BOUNCE}
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
		[Tooltip("Rotate transform to align with spline")]
		public bool rotate = true;
		[Tooltip("If on, speed will be linearly corrected for stretching of spline.")]
		private bool speedCorrection = true;
		public LoopMode loopMode = LoopMode.LOOP;

		void Update(){
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
				if (rotate){
					transform.forward = forward * path.Tangent(t, speedCorrection);
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
			path.DrawPath();
		}
		#endif
	}
}