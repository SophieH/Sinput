using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomMenuItem : MonoBehaviour {

	public CustomMenuItem itemAbove;
	public CustomMenuItem itemBelow;

	public Transform camTargetPos;

	public Transform cursorTarget;

	private Vector3 startPosition;

	public bool highlighted = false;

	// Use this for initialization
	void Start () {
		startPosition = transform.position;
		camTargetPos.parent = null;
	}
	
	// Update is called once per frame
	void Update () {
		if (highlighted) {
			transform.position = Vector3.Lerp(transform.position, startPosition - transform.forward, Time.deltaTime * 10f);
		} else {
			transform.position = Vector3.Lerp(transform.position, startPosition, Time.deltaTime * 3f);
		}
	}

	public void Select() {
		transform.position = startPosition + transform.forward;
		Debug.Log("I was selected!");
	}
}
