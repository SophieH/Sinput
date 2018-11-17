using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(SinputSystems.Touch.SinputTouch_Button))]
[CanEditMultipleObjects]
public class SinputTouch_ButtonEditor : Editor {

	private SinputSystems.Touch.SinputTouch_Button btn;

	public void OnSceneGUI() {
		btn = this.target as SinputSystems.Touch.SinputTouch_Button;
		Handles.color = Color.green;
		if (btn.collisionRadius < 0f) {
			Handles.color = Color.red;
		}
		Handles.DrawWireDisc(btn.transform.position, btn.transform.forward, btn.collisionRadius);

		
	}

}

[CustomEditor(typeof(SinputSystems.Touch.SinputTouch_Stick))]
[CanEditMultipleObjects]
public class SinputTouch_StickEditor : Editor {

	private SinputSystems.Touch.SinputTouch_Stick stick;

	public void OnSceneGUI() {
		stick = this.target as SinputSystems.Touch.SinputTouch_Stick;
		Handles.color = Color.green;
		if (stick.collisionRadius < 0f) {
			Handles.color = Color.red;
		}
		Handles.DrawWireDisc(stick.transform.position, stick.transform.forward, stick.collisionRadius);
		
	}

}
