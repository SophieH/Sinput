using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchControlExample : MonoBehaviour {

	//this script just ensure the example control scheme is loaded, and not the default control scheme (which has no virtual inputs)

	public SinputSystems.ControlScheme controlScheme;

	void Awake () {
		Sinput.LoadControlScheme(controlScheme, false);
	}
	
}
