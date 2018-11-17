using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SinputSystems.Touch {
	[RequireComponent(typeof(SpriteRenderer))]
	public class SinputTouch_Button : MonoBehaviour {

		public string virtualInputID = "Touch_BUTTON";

		//touch collisions are checked against a collider if one is set
		public Collider touchCollider;
		//otherwise they are compared against the plane of this transform, within a radius
		public float collisionRadius = 0.5f;

		//If true, doesn't allow touches to press the button if they touched somewhere else first
		public bool onlyPressingTouches = false;

		[HideInInspector]
		public SpriteRenderer spriteRenderer;

		public Sprite heldSprite;
		public Sprite releasedSprite;

		private Color color;
		private Color pressColor = Color.white;

		public Transform labelContainer;
		private Vector3 labelOffset;

		private bool wasHeld = false;
		private bool isHeld = false;

		private Plane buttonPlane;


		private List<int> claimedTouches = new List<int>();

		private bool debugMouse = false;

		// Use this for initialization
		void Start() {
			spriteRenderer = GetComponent<SpriteRenderer>();

			labelOffset = labelContainer.localPosition;
			color = spriteRenderer.color;

			buttonPlane = new Plane(transform.forward, transform.position);

			if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.WindowsEditor) {
				debugMouse = true;
			}
		}

		

		// Update is called once per frame
		void Update() {
			wasHeld = isHeld;
			isHeld = false;

			//clear out any touches we have claimed that no longer exist
			if (onlyPressingTouches) {
				for (int k = 0; k < claimedTouches.Count; k++) {
					if (!SinputTouchManager.TouchExists(claimedTouches[k])) {
						claimedTouches.RemoveAt(k);
						k--;
					}
				}
			}


			//detect touches
			bool canUseThisTouch = false;
			bool claimThisTouch = false;
			for (int i = 0; i < Input.touchCount; i++) {
				canUseThisTouch = false;
				claimThisTouch = false;
				if (!onlyPressingTouches) canUseThisTouch = true;
				if (onlyPressingTouches) {
					//if this touch is one we have already claimed, we can use it
					for (int k=0; k<claimedTouches.Count; k++) {
						if (Input.touches[i].fingerId == claimedTouches[k]) canUseThisTouch = true;
					}

					//if this touch is a press and has not yet been claimed we can use it
					if (Input.touches[i].phase == TouchPhase.Began && !SinputTouchManager.IsClaimed(Input.touches[i].fingerId)) {
						canUseThisTouch = true;
						claimThisTouch = true;
					}
				}

				if (canUseThisTouch) {
					//see if this touch, touches this button
					Ray ray = Camera.main.ScreenPointToRay(Input.touches[i].position);

					if (touchCollider) {
						//test against a collider
						RaycastHit hit = new RaycastHit();
						if (touchCollider.Raycast(ray, out hit, 9999f)) {
							isHeld = true;

							if (claimThisTouch) {
								SinputTouchManager.ClaimTouch(Input.touches[i].fingerId);
								claimedTouches.Add(Input.touches[i].fingerId);
							}
						}
					} else {
						//test against button plane within a radius
						float hitDistance = 0f;
						buttonPlane.SetNormalAndPosition(transform.forward, transform.position);
						if (buttonPlane.Raycast(ray, out hitDistance)) {
							if (Vector3.Distance(transform.position, ray.origin + ray.direction.normalized * hitDistance) < collisionRadius) {
								isHeld = true;
								if (claimThisTouch) {
									SinputTouchManager.ClaimTouch(Input.touches[i].fingerId);
									claimedTouches.Add(Input.touches[i].fingerId);
								}
							}

						}
					}
				}
			}


			//make it work with mouse for debug
			if (debugMouse) {
				if (Input.GetKey("mouse 0")) {
					Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
					if (touchCollider) {
						RaycastHit hit = new RaycastHit();
						if (touchCollider.Raycast(ray, out hit, 9999f)) {
							isHeld = true;
						}
					} else {
						float hitDistance = 0f;
						buttonPlane.SetNormalAndPosition(transform.forward, transform.position);
						if (buttonPlane.Raycast(ray, out hitDistance)) {
							if (Vector3.Distance(transform.position, ray.origin + ray.direction.normalized * hitDistance) < collisionRadius) {
								isHeld = true;
							}
						}
					}
				}
			}

			//update virtual input
			SinputSystems.VirtualInputs.SetVirtualButton(virtualInputID, isHeld);

			//make the button pretty
			spriteRenderer.color = Color.Lerp(spriteRenderer.color, color, Time.deltaTime * 10f);
			if (wasHeld != isHeld) {
				//change
				if (isHeld) {
					spriteRenderer.sprite = heldSprite;
					spriteRenderer.color = pressColor;
					labelContainer.localPosition = Vector3.zero;
				} else {
					spriteRenderer.sprite = releasedSprite;
					spriteRenderer.color = pressColor;
					//spriteRenderer.color = color;
					labelContainer.localPosition = labelOffset;
				}
			}

		}
	}
}
