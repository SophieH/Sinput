/*
Test results:
GetKey is ALWAYS true if (GetKeyDown)
GetKey is ALWAYS false if (GetKeyUp && !GetKeyDown)
GetKeyDown and GetKeyUp can be true one the same frame
*/

using UnityEngine;

public class TestGetKey : MonoBehaviour {
	private int frameCount;

	private void Update() {
		frameCount++;

		if (Input.GetKey(KeyCode.Space)) {
			Debug.Log(string.Format("-- GetKey @FrameNumber:{0}", frameCount));
		}
		if (Input.GetKeyDown(KeyCode.Space)) {
			Debug.Log(string.Format("__ GetKeyDown @FrameNumber:{0}", frameCount));
		}
		if (Input.GetKeyUp(KeyCode.Space)) {
			Debug.Log(string.Format("|| GetKeyUp @FrameNumber:{0}", frameCount));
		}
	}
}
