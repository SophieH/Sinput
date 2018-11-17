using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SinputSystems.Touch {
	public class SinputTouch_Stick : MonoBehaviour {

		public string virtualInputID_UP = "Touch_UP";
		public string virtualInputID_DOWN = "Touch_DOWN";
		public string virtualInputID_LEFT = "Touch_LEFT";
		public string virtualInputID_RIGHT = "Touch_RIGHT";

		public float collisionRadius = 0.6f;

		//does the joystick follow a touch controlling it, and jump to any new unclaimed touches?
		public bool followTouch = false;

		//the part of the joystick moved by a finger (this transform is the base)
		public SpriteRenderer stickTop;
		private Transform stickTopTransform;

		private SpriteRenderer stickBase;

		private Vector3 stickInputVector = Vector3.zero;

		private int claimedTouch = -1;
		private Vector3 touchOrigin = Vector3.zero;
		private Plane stickPlane;

		private bool debugMouse = false;

		
		//how far can the stick travel
		public float stickRange = 0.25f;
		private float rangeFactor = 1f;

		private float fade = 1f;

		// Use this for initialization
		void Start() {
			if (stickRange!=0f) rangeFactor = 1f / stickRange;

			stickBase = GetComponent<SpriteRenderer>();
			stickTopTransform = stickTop.transform;

			stickPlane = new Plane(transform.forward, transform.position);
			if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.WindowsEditor) {
				debugMouse = true;
			}
		}

		// Update is called once per frame
		void Update() {
			

			//if we have a touch, make sure it's still there
			if (claimedTouch != -1) {
				bool stillExists = false;
				if (SinputTouchManager.TouchExists(claimedTouch)) stillExists = true;
				/*for (int i = 0; i < Input.touchCount; i++) {
					if (Input.touches[i].fingerId == claimedTouch) stillExists = true;
				}*/
				if (debugMouse && claimedTouch == -2 && Input.GetKey("mouse 0")) stillExists = true;
				if (!stillExists) claimedTouch = -1;
			}

			//lets find a touch press if we don't have a touch yet
			if (claimedTouch == -1) {
				for (int i=0; i<Input.touchCount; i++) {
					if (claimedTouch ==-1 && Input.touches[i].phase == TouchPhase.Began && !SinputTouchManager.IsClaimed(Input.touches[i].fingerId)) {
						//this touch press is free, but does it touch the stick?

						Vector3 hitPoint = Vector3.zero;
						if (TouchCollision(Input.touches[i].position, out hitPoint, !followTouch)) {
							if (followTouch) {
								transform.position = hitPoint;
							}
							claimedTouch = Input.touches[i].fingerId;
							SinputTouchManager.ClaimTouch(claimedTouch);
							touchOrigin = transform.InverseTransformPoint(hitPoint);
						}

					}
				}
				
				if (debugMouse && claimedTouch == -1 && Input.GetKeyDown("mouse 0")) {
					Vector3 hitPoint = Vector3.zero;
					if (TouchCollision(Input.mousePosition, out hitPoint, !followTouch)) {
						if (followTouch) {
							transform.position = hitPoint;
						}
						claimedTouch = -2;
						SinputTouchManager.ClaimTouch(claimedTouch);
						touchOrigin = transform.InverseTransformPoint(hitPoint);
					}
				}
				
			}

			

			if (claimedTouch != -1) {
				//still touching, work out distance from the origin (in local space) and that's our stick value
				Vector3 touchPosition = touchOrigin;

				for (int i = 0; i < Input.touchCount; i++) {
					if (claimedTouch == Input.touches[i].fingerId) {
						//this is our touch
						Vector3 hitPoint = Vector3.zero;
						if (TouchCollision(Input.touches[i].position, out hitPoint, false)) {
							touchPosition = hitPoint;
						}

					}
				}
				if (debugMouse && claimedTouch == -2) {
					Vector3 hitPoint = Vector3.zero;
					if (TouchCollision(Input.mousePosition, out hitPoint, false)) {
						touchPosition = hitPoint;
					}
				}

				stickInputVector = transform.InverseTransformPoint(touchPosition) - touchOrigin;
				stickInputVector *= rangeFactor;

				if (stickInputVector.magnitude > 1f) {
					stickInputVector.Normalize();

					if (followTouch) {
						transform.position = touchPosition - transform.right * (stickInputVector.x * stickRange) - transform.up * (stickInputVector.y * stickRange);
						touchOrigin = Vector3.zero;
					}
				}
			} else {
				stickInputVector = Vector3.zero;
			}

			//set virtual input values now
			if (virtualInputID_UP != "") VirtualInputs.SetVirtualAxis(virtualInputID_UP, Mathf.Clamp(stickInputVector.y, 0f, 1f));
			if (virtualInputID_DOWN != "") VirtualInputs.SetVirtualAxis(virtualInputID_DOWN, Mathf.Clamp(stickInputVector.y * -1f, 0f, 1f));
			if (virtualInputID_RIGHT != "") VirtualInputs.SetVirtualAxis(virtualInputID_RIGHT, Mathf.Clamp(stickInputVector.x, 0f, 1f));
			if (virtualInputID_LEFT != "") VirtualInputs.SetVirtualAxis(virtualInputID_LEFT, Mathf.Clamp(stickInputVector.x * -1f, 0f, 1f));
			
			//animate stick
			stickTopTransform.localPosition = stickInputVector * stickRange;
			if (claimedTouch == -1) {
				fade -= Time.deltaTime;
				if (followTouch) {
					fade = Mathf.Clamp(fade, 0.05f, 1f);
				} else {
					fade = Mathf.Clamp(fade, 0.5f, 1f);
				}
			} else {
				fade += Time.deltaTime;
				fade = Mathf.Clamp(fade, 0f, 1f);
			}
			Color col = stickBase.color;
			col.a = fade;
			stickBase.color = col;

			col = stickTop.color;
			col.a = fade;
			stickTop.color = col;
		}

		bool TouchCollision(Vector3 touchPos, out Vector3 hitPoint, bool limitRadius) {
			Ray ray = Camera.main.ScreenPointToRay(touchPos);

			float hitDistance = 0f;
			stickPlane.SetNormalAndPosition(transform.forward, transform.position);
			if (stickPlane.Raycast(ray, out hitDistance)) {
				if (limitRadius) {
					if (Vector3.Distance(transform.position, ray.origin + ray.direction.normalized * hitDistance) < collisionRadius) {
						hitPoint = ray.origin + ray.direction.normalized * hitDistance;
						return true;
					}
				} else {
					hitPoint = ray.origin + ray.direction.normalized * hitDistance;
					return true;
				}

			}

			hitPoint = Vector3.zero;
			return false;
		}
	}

	

}
