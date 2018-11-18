using System.Collections.Generic;
using UnityEngine;
using System;
using SinputSystems;


public static class Sinput {

	//Fixed number of gamepad things unity can handle, used mostly by GamepadDebug and InputManagerReplacementGenerator.
	//Sinput can handle as many of these as you want to throw at it buuuuut, unty can only handle so many and Sinput is wrapping unity input for now
	//You can try bumping up the range of these but you might have trouble
	//(EG, you can probably get axis of gamepads in slots over 8, but maybe not buttons?)
	public static int MAXCONNECTEDGAMEPADS { get {return 11; } }
	public static int MAXAXISPERGAMEPAD { get {return 28; } }
	public static int MAXBUTTONSPERGAMEPAD { get {return 20; } }
	

	//are keyboard & mouse used by two seperate players (distinct=true) or by a single player (distinct=false)
	private static bool keyboardAndMouseAreDistinct = false;
	/// <summary>
	/// Total possible device slots that Sinput may detect. (Including keyboard, mouse, virtual, and any slots)
	/// </summary>
	public static int totalPossibleDeviceSlots { get; private set; }

	//overall mouse sensitivity
	/// <summary>
	/// Overall mouse sensitivity (effects all Controls bound to mouse movements)
	/// </summary>
	public static float mouseSensitivity = 1f;

	/// <summary>
	/// Name of control scheme used when saving/loading custom control schemes
	/// <para>unless you're doing fancy stuff like switching between various control schemes, this is probably best left alone.</para>
	/// </summary>
	public static string controlSchemeName = "ControlScheme";

	//the control scheme, set it with SetControlScheme()
	private static Control[] _controls;
	/// <summary>
	/// Returns a copy of the current Sinput control list
	/// <para>Note: This is not the fastest thing so don't go calling it in a loop every frame, make yourself a local copy.</para>
	/// </summary>
	public static Control[] controls {
		get {
			//make a copy of the controls so we're definitely not returning something that will effect _controls
			Control[] returnControlList = new Control[_controls.Length];
			for (int i = 0; i < _controls.Length; i++) {
				returnControlList[i] = new Control(_controls[i].name);
				for (int k = 0; k < _controls[i].commonMappings.Count; k++) {
					returnControlList[i].commonMappings.Add(_controls[i].commonMappings[k]);
				}

				returnControlList[i].inputs = new List<DeviceInput>();
				for (int k = 0; k < _controls[i].inputs.Count; k++) {
					returnControlList[i].inputs.Add(_controls[i].inputs[k]);
				}
			}

			return returnControlList;
		}
		//set { Init(); _controls = value; }
	}
	public static SmartControl[] smartControls { get; private set; }

	//gamepads list is checked every GetButton/GetAxis call, when it updates all common mapped inputs are reapplied appropriately
	static int nextGamepadCheck=-99;
	private static string[] _gamepads = new string[0];
	/// <summary>
	/// List of connected gamepads that Sinput is aware of.
	/// </summary>
	public static string[] gamepads { get { CheckGamepads(); return _gamepads; } }
	/// <summary>
	/// Number of connected gamepads that Sinput is aware of.
	/// </summary>
	public static int connectedGamepads { get { return _gamepads.Length; } }

	//XR stuff
	private static SinputSystems.XR.SinputXR _xr;
	public static SinputSystems.XR.SinputXR XR {
		get {
			SinputUpdate();
			return _xr;
		}
	}

	//init
	[RuntimeInitializeOnLoadMethod]
	static void Init(){
		Debug.Log("Initializing SInput");

		totalPossibleDeviceSlots = System.Enum.GetValues(typeof(InputDeviceSlot)).Length;

		zeroInputWaits = new float[totalPossibleDeviceSlots];
		zeroInputs = new bool[totalPossibleDeviceSlots];

		_xr = new SinputSystems.XR.SinputXR();
	}

	//public static ControlScheme controlScheme;
	private static bool schemeLoaded = false;
	/// <summary>
	/// Load a Control Scheme asset.
	/// </summary>
	/// <param name="schemeName"></param>
	/// <param name="loadCustomControls"></param>
	public static void LoadControlScheme(string schemeName, bool loadCustomControls) {
		schemeLoaded = false;
		//Debug.Log("load scheme name!");
		UnityEngine.Object[] projectControlSchemes = Resources.LoadAll("", typeof(ControlScheme));

		int schemeIndex = -1;
		for (int i=0; i<projectControlSchemes.Length; i++){
			if (projectControlSchemes[i].name == schemeName) schemeIndex = i;
		}
		if (schemeIndex==-1){
			Debug.LogError("Couldn't find control scheme \"" + schemeName + "\" in project resources.");
			return;
		}
		//controlScheme = (ControlScheme)projectControlSchemes[schemeIndex];
		LoadControlScheme((ControlScheme)projectControlSchemes[schemeIndex], loadCustomControls);
	}
	/// <summary>
	/// Load a Control Scheme.
	/// </summary>
	/// <param name="scheme"></param>
	/// <param name="loadCustomControls"></param>
	public static void LoadControlScheme(ControlScheme scheme, bool loadCustomControls) {
		//Debug.Log("load scheme asset!");

		schemeLoaded = false;


		//make sure we know what gamepads are connected
		//and load their common mappings if they are needed
		CheckGamepads(true);

		//Generate controls from controlScheme asset
		List<Control> loadedControls = new List<Control>();
		for (int i=0; i<scheme.controls.Count; i++){
			Control newControl = new Control(scheme.controls[i].name);

			for (int k=0; k<scheme.controls[i].keyboardInputs.Count; k++){
				newControl.AddKeyboardInput( (KeyCode)Enum.Parse(typeof(KeyCode), scheme.controls[i].keyboardInputs[k].ToString()) );
			}
			for (int k=0; k<scheme.controls[i].gamepadInputs.Count; k++){
				newControl.AddGamepadInput( scheme.controls[i].gamepadInputs[k] );
			}
			for (int k=0; k<scheme.controls[i].mouseInputs.Count; k++){
				newControl.AddMouseInput( scheme.controls[i].mouseInputs[k] );
			}
			for (int k=0; k<scheme.controls[i].virtualInputs.Count; k++){
				newControl.AddVirtualInput( scheme.controls[i].virtualInputs[k] );
			}
			for (int k = 0; k < scheme.controls[i].xrInputs.Count; k++) {
				newControl.AddGamepadInput(scheme.controls[i].xrInputs[k]);
			}

			loadedControls.Add(newControl);
		}
		_controls = loadedControls.ToArray();

		//Generate smartControls from controlScheme asset
		List<SmartControl> loadedSmartControls = new List<SmartControl>();
		for (int i=0; i<scheme.smartControls.Count; i++){
			SmartControl newControl = new SmartControl(scheme.smartControls[i].name);

			newControl.positiveControl = scheme.smartControls[i].positiveControl;
			newControl.negativeControl = scheme.smartControls[i].negativeControl;
			newControl.gravity = scheme.smartControls[i].gravity;
			newControl.deadzone = scheme.smartControls[i].deadzone;
			newControl.speed = scheme.smartControls[i].speed;
			newControl.snap = scheme.smartControls[i].snap;
			//newControl.scale = scheme.smartControls[i].scale;

			newControl.inversion = new bool[totalPossibleDeviceSlots];
			newControl.scales = new float[totalPossibleDeviceSlots];
			for (int k = 0; k < totalPossibleDeviceSlots; k++) {
				newControl.inversion[k] = scheme.smartControls[i].invert;
				newControl.scales[k] = scheme.smartControls[i].scale;
			}

			loadedSmartControls.Add(newControl);
		}
		smartControls = loadedSmartControls.ToArray();
		for (int i=0; i<smartControls.Length; i++) smartControls[i].Init();

		//now load any saved control scheme with custom rebound inputs
		if (loadCustomControls && SinputFileIO.SaveDataExists(controlSchemeName)){
			//Debug.Log("Found saved binding!");
			_controls = SinputFileIO.LoadControls( _controls, controlSchemeName);
		}

		//make sure controls have any gamepad-relevant stuff set correctly
		RefreshGamepadControls();

		schemeLoaded = true;
		lastUpdateFrame = -99;
	}

	static int lastUpdateFrame = -99;
	/// <summary>
	/// Update Sinput.
	/// <para>This is called by all other Sinput functions so it is not necessary for you to call it in most circumstances.</para>
	/// </summary>
	public static void SinputUpdate() {
		if (lastUpdateFrame == Time.frameCount) return;

		lastUpdateFrame = Time.frameCount;

		if (!schemeLoaded) LoadControlScheme("MainControlScheme", true);

		//check if connected gamepads have changed
		CheckGamepads();

		//update XR stuff
		_xr.Update();

		//update controls
		if (null != _controls) {
			for (int i = 0; i < _controls.Length; i++) {
				_controls[i].Update();//resetAxisButtonStates);
			}
		}

		//update our smart controls
		if (null != smartControls) {
			for (int i = 0; i < smartControls.Length; i++) {
				smartControls[i].Update();
			}
		}

		//count down till we can stop zeroing inputs
		for (int i = 0; i < totalPossibleDeviceSlots; i++) {
			if (zeroInputs[i]) {
				zeroInputWaits[i] -= Time.deltaTime;
				if (zeroInputWaits[i] <= 0f) zeroInputs[i] = false;
			}
		}
	}


	//tells sinput to return false/0f for any input checks until the wait time has passed
	static float[] zeroInputWaits;
	static bool[] zeroInputs;
	/// <summary>
	/// tells Sinput to return false/0f for any input checks until half a second has passed
	/// </summary>
	/// <param name="slot"></param>
	public static void ResetInputs(InputDeviceSlot slot = InputDeviceSlot.any) { ResetInputs(0.5f, slot); } //default wait is half a second
	/// <summary>
	/// tells Sinput to return false/0f for any input checks until the wait time has passed
	/// </summary>
	/// <param name="waitTime"></param>
	/// <param name="slot"></param>
	public static void ResetInputs(float waitTime, InputDeviceSlot slot=InputDeviceSlot.any) {
		SinputUpdate();
		
		if (slot == InputDeviceSlot.any) {
			//reset all slots' input
			for (int i=0; i<totalPossibleDeviceSlots; i++) {
				zeroInputWaits[i] = waitTime;
				zeroInputs[i] = true;
			}
		} else {
			//reset only a specific slot's input
			zeroInputWaits[(int)slot] = waitTime;
			zeroInputs[(int)slot] = true;
		}
		
		//reset smartControl values
		if (smartControls != null) {
			for (int i = 0; i < smartControls.Length; i++) {
				smartControls[i].ResetAllValues(slot);
			}
		}
	}
    

	//update gamepads
	static int lastCheckedGamepadRefreshFrame = -99;
	/// <summary>
	/// Checks whether connected gamepads have changed.
	/// <para>This is called before every input check so it is uneccesary for you to use it.</para>
	/// </summary>
	public static void CheckGamepads(bool refreshGamepadsNow = false){
		if (Time.frameCount == lastCheckedGamepadRefreshFrame && !refreshGamepadsNow) return;
		lastCheckedGamepadRefreshFrame = Time.frameCount;

		//Debug.Log("checking gamepads");

		var tempInputGamepads = Input.GetJoystickNames();
		if (connectedGamepads != tempInputGamepads.Length) refreshGamepadsNow = true; //number of connected gamepads has changed
		if (!refreshGamepadsNow && nextGamepadCheck < Time.frameCount){
			//this check is for the rare case gamepads get re-ordered in a single frame & the length of GetJoystickNames() stays the same
			nextGamepadCheck = Time.frameCount + 500;
			for (int i=0; i<connectedGamepads; i++){
				if (!_gamepads[i].Equals(tempInputGamepads[i], StringComparison.InvariantCultureIgnoreCase)) refreshGamepadsNow = true;
			}
		}
		if (refreshGamepadsNow){
			//Debug.Log("Refreshing gamepads");

			//connected gamepads have changed, lets update them
			_gamepads = tempInputGamepads; // reuse array given that we already have generated it using Input.GetJoystickNames()
			for (int i=0; i<_gamepads.Length; i++){
				_gamepads[i] = tempInputGamepads[i].ToUpper();
			}

			//reload common mapping information for any new gamepads
			CommonGamepadMappings.ReloadCommonMaps();
			
			//refresh control information relating to gamepads
			if (schemeLoaded) RefreshGamepadControls();

			//xr stuff too
			_xr.UpdateJoystickIndeces();

			refreshGamepadsNow = false;
		}
	}

	private static void RefreshGamepadControls() {
		//if (null != _controls) {
			for (int i = 0; i < _controls.Length; i++) {
				//reapply common bindings
				_controls[i].ReapplyCommonBindings();

				//reset axis button states
				//_controls[i].ResetAxisButtonStates();

				//make sure inputs are linked to correct gamepad slots
				_controls[i].SetAllowedInputSlots();
			}
		//}
		//if (null != smartControls) {
			for (int i = 0; i < smartControls.Length; i++) {
				smartControls[i].Init();
			}
		//}
	}



	/// <summary>
	/// like GetButtonDown() but returns ~which~ keyboard/gamepad input slot pressed the control
	/// <para>will return InputDeviceSlot.any if no device pressed the button this frame</para>
	/// <para>use it for 'Pres A to join!' type multiplayer, and instantiate a player for the returned slot (if it isn't DeviceSlot.any)</para>
	/// </summary>
	/// <param name="controlName"></param>
	/// <returns></returns>
	public static InputDeviceSlot GetSlotPress(string controlName){
		//like GetButtonDown() but returns ~which~ keyboard/gamepad input slot pressed the control
		//use it for 'Pres A to join!' type multiplayer, and instantiate a player for the returned slot (if it isn't DeviceSlot.any)

		SinputUpdate();

		if (keyboardAndMouseAreDistinct){
			if (GetButtonDown(controlName, InputDeviceSlot.keyboard)) return InputDeviceSlot.keyboard;
			if (GetButtonDown(controlName, InputDeviceSlot.mouse)) return InputDeviceSlot.mouse;
		}else{
			if (GetButtonDown(controlName, InputDeviceSlot.keyboardAndMouse)) return InputDeviceSlot.keyboardAndMouse;
			if (!zeroInputs[(int)InputDeviceSlot.keyboardAndMouse] && GetButtonDown(controlName, InputDeviceSlot.keyboard)) return InputDeviceSlot.keyboardAndMouse;
			if (!zeroInputs[(int)InputDeviceSlot.keyboardAndMouse] && GetButtonDown(controlName, InputDeviceSlot.mouse)) return InputDeviceSlot.keyboardAndMouse;
		}

		for (int i = (int) InputDeviceSlot.gamepad1; i <= (int) InputDeviceSlot.gamepad11; i++) {
			if (GetButtonDown(controlName, (InputDeviceSlot) i)) return (InputDeviceSlot) i;
		}
		
		if (GetButtonDown(controlName, InputDeviceSlot.virtual1)) return InputDeviceSlot.virtual1;

		return InputDeviceSlot.any;
	}


	//Button control checks
	private static bool controlFound = false;//used in control checks to tell whether a control was found or not
	/// <summary>
	/// Returns true if a Sinput Control or Smart Control is Held this frame
	/// </summary>
	public static bool GetButton(string controlName, InputDeviceSlot slot = InputDeviceSlot.any) { return ButtonCheck(controlName, slot, ButtonAction.HELD); }

	/// <summary>
	/// Returns true if a Sinput Control or Smart Control was Pressed this frame
	/// </summary>
	public static bool GetButtonDown(string controlName, InputDeviceSlot slot = InputDeviceSlot.any) { return ButtonCheck(controlName, slot, ButtonAction.DOWN); }

	/// <summary>
	/// Returns true if a Sinput Control or Smart Control was Released this frame
	/// </summary>
	public static bool GetButtonUp(string controlName, InputDeviceSlot slot = InputDeviceSlot.any) { return ButtonCheck(controlName, slot, ButtonAction.UP); }

	/// <summary>
	/// Returns true if a Sinput Control or Smart Control is Held this frame, regardless of the Control's toggle setting.
	/// </summary>
	public static bool GetButtonRaw(string controlName, InputDeviceSlot slot = InputDeviceSlot.any) { return ButtonCheck(controlName, slot, ButtonAction.HELD, true); }
	/// <summary>
	/// Returns true if a Sinput Control or Smart Control was Pressed this frame, regardless of the Control's toggle setting.
	/// </summary>
	public static bool GetButtonDownRaw(string controlName, InputDeviceSlot slot = InputDeviceSlot.any) { return ButtonCheck(controlName, slot, ButtonAction.DOWN, true); }
	/// <summary>
	/// Returns true if a Sinput Control or Smart Control was Released this frame, regardless of the Control's toggle setting.
	/// </summary>
	public static bool GetButtonUpRaw(string controlName, InputDeviceSlot slot = InputDeviceSlot.any) { return ButtonCheck(controlName, slot, ButtonAction.UP, true); }

	//repeating button checks
	/// <summary>
	/// How long a Control must be held before GetButtonDownRepeating() starts repeating
	/// </summary>
	public static float buttonRepeatWait = 0.75f;
	/// <summary>
	/// How quickly GetButtonDownRepeating() will repeat.
	/// </summary>
	public static float buttonRepeat = 0.1f;
	/// <summary>
	/// Returns true if a Sinput Control or Smart Control was Pressed this frame, or if it has been held long enough to start repeating.
	/// <para>Use this for menu scrolling inputs</para>
	/// </summary>
	public static bool GetButtonDownRepeating(string controlName) { return ButtonCheck(controlName, InputDeviceSlot.any, ButtonAction.REPEATING); }
	/// <summary>
	/// Returns true if a Sinput Control or Smart Control was Pressed this frame, or if it has been held long enough to start repeating.
	/// <para>Use this for menu scrolling inputs</para>
	/// </summary>
	public static bool GetButtonDownRepeating(string controlName, InputDeviceSlot slot) { return ButtonCheck(controlName, slot, ButtonAction.REPEATING); }

	static bool ButtonCheck(string controlName, InputDeviceSlot slot, ButtonAction bAction, bool getRawValue = false){
		
		SinputUpdate();
		if (zeroInputs[(int)slot]) return false;

		controlFound = false;

		for (int i=0; i<_controls.Length; i++){
			if (_controls[i].name == controlName){
				controlFound=true;
				if (_controls[i].GetButtonState(bAction, slot, getRawValue)) return true;
			}
		}

		for (int i=0; i<smartControls.Length; i++){
			if (smartControls[i].name == controlName){
				controlFound=true;
				if (smartControls[i].ButtonCheck(bAction, slot)) return true;
			}
		}

		if (!controlFound) Debug.LogError("Sinput Error: Control \"" + controlName + "\" not found in list of controls or SmartControls.");

		return false;
	}


	//Axis control checks
	private static float returnV; //used in axis checks to hold the return value
	private static float v;

	/// <summary>
	/// Returns the value of a Sinput Control or Smart Control.
	/// </summary>
	public static float GetAxis(string controlName) { return AxisCheck(controlName, InputDeviceSlot.any); }
	/// <summary>
	/// Returns the value of a Sinput Control or Smart Control.
	/// </summary>
	public static float GetAxis(string controlName, InputDeviceSlot slot = InputDeviceSlot.any) { return AxisCheck(controlName, slot); }


	/// <summary>
	/// Returns the raw value of a Sinput Control or Smart Control
	/// </summary>
	public static float GetAxisRaw(string controlName, InputDeviceSlot slot = InputDeviceSlot.any) { return AxisCheck(controlName, slot, true); }

	static float AxisCheck(string controlName, InputDeviceSlot slot, bool getRawValue=false){

		SinputUpdate();
		if (zeroInputs[(int)slot]) return 0f;

		controlFound = false;

		if (controlName=="") return 0f;

		returnV = 0f;
		for (int i=0; i<_controls.Length; i++){
			if (_controls[i].name == controlName){
				controlFound=true;
				v = _controls[i].GetAxisState(slot);
				if (Mathf.Abs(v) > returnV) returnV = v;
			}
		}

		for (int i=0; i<smartControls.Length; i++){
			if (smartControls[i].name == controlName){
				controlFound=true;
				v = smartControls[i].GetValue(slot, getRawValue);
				if (Mathf.Abs(v) > returnV) returnV = v;
			}
		}

		if (!controlFound) Debug.LogError("Sinput Error: Control \"" + controlName + "\" not found in list of Controls or SmartControls.");

		return returnV;
	}

	//vector checks
	private static Vector2 returnVec2;
	/// <summary>
	/// Returns a Vector2 made with GetAxis() values applied to x and y
	/// </summary>
	/// <returns></returns>
	public static Vector2 GetVector(string controlNameA, string controlNameB, bool normalClip = true) { return Vector2Check(controlNameA, controlNameB, InputDeviceSlot.any, normalClip); }
	/// <summary>
	/// Returns a Vector2 made with GetAxis() values applied to x and y
	/// </summary>
	/// <returns></returns>
	public static Vector2 GetVector(string controlNameA, string controlNameB, InputDeviceSlot slot) { return Vector2Check(controlNameA, controlNameB, slot, true); }
	/// <summary>
	/// Returns a Vector2 made with GetAxis() values applied to x and y
	/// </summary>
	/// <returns></returns>
	public static Vector2 GetVector(string controlNameA, string controlNameB, InputDeviceSlot slot, bool normalClip) { return Vector2Check(controlNameA, controlNameB, slot, normalClip); }

	static Vector2 Vector2Check(string controlNameA, string controlNameB, InputDeviceSlot slot, bool normalClip){

		SinputUpdate();

		returnVec2 = Vector2.zero;
		returnVec2.x = AxisCheck(controlNameA, slot);
		returnVec2.y = AxisCheck(controlNameB, slot);

		if (normalClip && returnVec2.magnitude > 1f) {
			returnVec2.Normalize();
		}

		return returnVec2;
	}

	private static Vector3 returnVec3;
	/// <summary>
	/// Returns a Vector3 made with GetAxis() values applied to x, y, and z
	/// </summary>
	public static Vector3 GetVector(string controlNameA, string controlNameB, string controlNameC, bool normalClip = true) { return Vector3Check(controlNameA, controlNameB, controlNameC, InputDeviceSlot.any, true); }
	/// <summary>
	/// Returns a Vector3 made with GetAxis() values applied to x, y, and z
	/// </summary>
	public static Vector3 GetVector(string controlNameA, string controlNameB, string controlNameC, InputDeviceSlot slot) { return Vector3Check(controlNameA, controlNameB, controlNameC, slot, true); }
	/// <summary>
	/// Returns a Vector3 made with GetAxis() values applied to x, y, and z
	/// </summary>
	public static Vector3 GetVector(string controlNameA, string controlNameB, string controlNameC, InputDeviceSlot slot, bool normalClip) { return Vector3Check(controlNameA, controlNameB, controlNameC, slot, normalClip); }

	static Vector3 Vector3Check(string controlNameA, string controlNameB, string controlNameC, InputDeviceSlot slot, bool normalClip){

		SinputUpdate();

		returnVec3 = Vector3.zero;
		returnVec3.x = AxisCheck(controlNameA, slot);
		returnVec3.y = AxisCheck(controlNameB, slot);
		returnVec3.z = AxisCheck(controlNameC, slot);

		if (normalClip && returnVec3.magnitude > 1f) {
			returnVec3.Normalize();
		}

		return returnVec3;
	}
	
	//frame delta preference
	private static bool preferDelta;
	/// <summary>
	/// Returns false if the value returned by GetAxis(controlName) on this frame should NOT be multiplied by delta time.
	/// <para>For example, this will return true for gamepad stick values, false for mouse movement values</para>
	/// </summary>
	public static bool PrefersDeltaUse(string controlName, InputDeviceSlot slot = InputDeviceSlot.any) {

		SinputUpdate();

		preferDelta = true;

		controlFound = false;

		if (controlName == "") return false;
		returnV = 0f;
		for (int i = 0; i < _controls.Length; i++) {
			if (_controls[i].name == controlName) {
				controlFound = true;
				v = _controls[i].GetAxisState(slot);
				if (Mathf.Abs(v) > returnV) {
					returnV = v;
					preferDelta = _controls[i].GetAxisStateDeltaPreference(slot);
				}
			}
		}

		//now check smart controls for framerate independence
		for (int i = 0; i < smartControls.Length; i++) {
			if (smartControls[i].name == controlName) {
				controlFound = true;
				v = smartControls[i].GetValue(slot, true);
				if (Mathf.Abs(v) > returnV) {
					returnV = v;

					if (!PrefersDeltaUse(smartControls[i].positiveControl, slot) || !PrefersDeltaUse(smartControls[i].negativeControl, slot)) preferDelta = false;
				}
			}
		}

		if (!controlFound) Debug.LogError("Sinput Error: Control \"" + controlName + "\" not found in list of Controls or SmartControls.");

		return preferDelta;
	}

	
	/// <summary>
	/// sets whether a control treats GetButton() calls with press or with toggle behaviour
	/// </summary>
	public static void SetToggle(string controlName, bool toggle) {
		SinputUpdate();
		controlFound = false;
		for (int i = 0; i < _controls.Length; i++) {
			if (_controls[i].name == controlName) {
				controlFound = true;
				_controls[i].isToggle = toggle;
			}
		}
		if (!controlFound) Debug.LogError("Sinput Error: Control \"" + controlName + "\" not found in list of Controls or SmartControls.");
	}
	
	/// <summary>
	/// returns true if a control treats GetButton() calls with toggle behaviour
	/// </summary>
	public static bool GetToggle(string controlName) {
		SinputUpdate();
		for (int i = 0; i < _controls.Length; i++) {
			if (_controls[i].name == controlName) {
				return _controls[i].isToggle;
			}
		}
		Debug.LogError("Sinput Error: Control \"" + controlName + "\" not found in list of Controls or SmartControls.");
		return false;
	}

	
	/// <summary>
	/// set a smart control to be inverted or not
	/// </summary>
	public static void SetInverted(string smartControlName, bool invert, InputDeviceSlot slot=InputDeviceSlot.any) {
		SinputUpdate();
		controlFound = false;
		for (int i = 0; i < smartControls.Length; i++) {
			if (smartControls[i].name == smartControlName) {
				controlFound = true;
				if (slot == InputDeviceSlot.any) {
					for (int k=0; k<totalPossibleDeviceSlots; k++) {
						smartControls[i].inversion[k] = invert;
					}
				} else {
					smartControls[i].inversion[(int)slot] = invert;
				}
			}
		}
		if (!controlFound) Debug.LogError("Sinput Error: Smart Control \"" + smartControlName + "\" not found in list of SmartControls.");
	}
	
	/// <summary>
	/// returns true if a smart control is inverted
	/// </summary>
	public static bool GetInverted(string smartControlName, InputDeviceSlot slot = InputDeviceSlot.any) {
		SinputUpdate();
		for (int i = 0; i < smartControls.Length; i++) {
			if (smartControls[i].name == smartControlName) {
				return smartControls[i].inversion[(int)slot];
			}
		}
		Debug.LogError("Sinput Error: Smart Control \"" + smartControlName + "\" not found in list of SmartControls.");
		return false;
	}

	
	/// <summary>
	/// sets scale ("sensitivity") of a smart control
	/// </summary>
	public static void SetScale(string smartControlName, float scale, InputDeviceSlot slot = InputDeviceSlot.any) {
		SinputUpdate();
		controlFound = false;
		for (int i = 0; i < smartControls.Length; i++) {
			if (smartControls[i].name == smartControlName) {
				controlFound = true;
				if (slot == InputDeviceSlot.any) {
					for (int k = 0; k < totalPossibleDeviceSlots; k++) {
						smartControls[i].scales[k] = scale;
					}
				} else {
					smartControls[i].scales[(int)slot] = scale;
				}
			}
		}
		if (!controlFound) Debug.LogError("Sinput Error: Smart Control \"" + smartControlName + "\" not found in list of SmartControls.");
	}
	
	/// <summary>
	/// gets scale of a smart control
	/// </summary>
	public static float GetScale(string smartControlName, InputDeviceSlot slot = InputDeviceSlot.any) {
		for (int i = 0; i < smartControls.Length; i++) {
			if (smartControls[i].name == smartControlName) {
				return smartControls[i].scales[(int)slot];
			}
		}
		Debug.LogError("Sinput Error: Smart Control \"" + smartControlName + "\" not found in list of SmartControls.");
		return 1f;
	}

}


