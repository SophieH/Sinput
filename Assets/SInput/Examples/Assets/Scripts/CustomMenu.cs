using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomMenu : MonoBehaviour {

	public CustomMenuItem currentMenuItem;
	public Transform cam;
	public Transform cursor;

	// Use this for initialization
	void Start () {
		currentMenuItem.highlighted = true;
	}
	
	// Update is called once per frame
	void Update () {
		if (Sinput.GetButtonDownRepeating("Up")) {
			//highlight item above
			currentMenuItem.highlighted = false;
			currentMenuItem = currentMenuItem.itemAbove;
			currentMenuItem.highlighted = true;
		}
		if (Sinput.GetButtonDownRepeating("Down")) {
			//highlight item below
			currentMenuItem.highlighted = false;
			currentMenuItem = currentMenuItem.itemBelow;
			currentMenuItem.highlighted = true;
		}
		if (Sinput.GetButtonDown("Submit")) {
			//select this item
			currentMenuItem.Select();
			Sinput.ResetInputs();
		}

		cam.position = Vector3.Lerp(cam.position, currentMenuItem.camTargetPos.position, Time.deltaTime * 4f);
		cam.rotation = Quaternion.Slerp(cam.rotation, currentMenuItem.camTargetPos.rotation, Time.deltaTime * 4f);

		cursor.position = currentMenuItem.cursorTarget.position;
	}
}
