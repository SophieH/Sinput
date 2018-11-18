using System;
using System.Collections.Generic;
using UnityEngine;
// Don't use `using SinputSystems;` to avoid conflicts


public static class Sinput {

	//Fixed number of gamepad things unity can handle, used mostly by GamepadDebug and InputManagerReplacementGenerator.
	//Sinput can handle as many of these as you want to throw at it buuuuut, unty can only handle so many and Sinput is wrapping unity input for now
	//You can try bumping up the range of these but you might have trouble
	//(EG, you can probably get axis of gamepads in slots over 8, but maybe not buttons?)
	public static int MAXCONNECTEDGAMEPADS { get {return 11; } }
	public static int MAXAXISPERGAMEPAD { get {return 28; } }
	public static int MAXBUTTONSPERGAMEPAD { get {return 20; } }
	private static readonly int HashedEmptyString = Animator.StringToHash("");
	

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
	private static SinputSystems.Control[] _controls;
	/// <summary>
	/// Returns a copy of the current Sinput control list
	/// <para>Note: This is not the fastest thing so don't go calling it in a loop every frame, make yourself a local copy.</para>
	/// </summary>
	public static SinputSystems.Control[] controls {
		get {
			//make a copy of the controls so we're definitely not returning something that will effect _controls
			SinputSystems.Control[] returnControlList = new SinputSystems.Control[_controls.Length];
			for (int i = 0; i < _controls.Length; i++) {
				returnControlList[i] = new SinputSystems.Control(_controls[i].name);
				for (int k = 0; k < _controls[i].commonMappings.Count; k++) {
					returnControlList[i].commonMappings.Add(_controls[i].commonMappings[k]);
				}

				returnControlList[i].inputs = new List<SinputSystems.DeviceInput>();
				for (int k = 0; k < _controls[i].inputs.Count; k++) {
					returnControlList[i].inputs.Add(_controls[i].inputs[k]);
				}
			}

			return returnControlList;
		}
		//set { Init(); _controls = value; }
	}
	public static SinputSystems.SmartControl[] smartControls { get; private set; }

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

		totalPossibleDeviceSlots = Enum.GetValues(typeof(SinputSystems.InputDeviceSlot)).Length;

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
		UnityEngine.Object[] projectControlSchemes = Resources.LoadAll("", typeof(SinputSystems.ControlScheme));

		int schemeIndex = -1;
		for (int i=0; i<projectControlSchemes.Length; i++){
			if (projectControlSchemes[i].name == schemeName) schemeIndex = i;
		}
		if (schemeIndex==-1){
			Debug.LogError("Couldn't find control scheme \"" + schemeName + "\" in project resources.");
			return;
		}
		//controlScheme = (ControlScheme)projectControlSchemes[schemeIndex];
		LoadControlScheme((SinputSystems.ControlScheme)projectControlSchemes[schemeIndex], loadCustomControls);
	}
	/// <summary>
	/// Load a Control Scheme.
	/// </summary>
	/// <param name="scheme"></param>
	/// <param name="loadCustomControls"></param>
	public static void LoadControlScheme(SinputSystems.ControlScheme scheme, bool loadCustomControls) {
		//Debug.Log("load scheme asset!");

		schemeLoaded = false;


		//make sure we know what gamepads are connected
		//and load their common mappings if they are needed
		CheckGamepads(true);

		//Generate controls from controlScheme asset
		List<SinputSystems.Control> loadedControls = new List<SinputSystems.Control>();
		for (int i=0; i<scheme.controls.Count; i++){
			SinputSystems.Control newControl = new SinputSystems.Control(scheme.controls[i].name);

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
		List<SinputSystems.SmartControl> loadedSmartControls = new List<SinputSystems.SmartControl>();
		for (int i=0; i<scheme.smartControls.Count; i++){
			SinputSystems.SmartControl newControl = new SinputSystems.SmartControl(scheme.smartControls[i].name);

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

			newControl.Hash();

			loadedSmartControls.Add(newControl);
		}
		smartControls = loadedSmartControls.ToArray();
		for (int i=0; i<smartControls.Length; i++) smartControls[i].Init();

		//now load any saved control scheme with custom rebound inputs
		if (loadCustomControls && SinputSystems.SinputFileIO.SaveDataExists(controlSchemeName)){
			//Debug.Log("Found saved binding!");
			_controls = SinputSystems.SinputFileIO.LoadControls( _controls, controlSchemeName);
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
	public static void ResetInputs(SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { ResetInputs(0.5f, slot); } //default wait is half a second
	/// <summary>
	/// tells Sinput to return false/0f for any input checks until the wait time has passed
	/// </summary>
	public static void ResetInputs(float waitTime, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) {
		SinputUpdate();
		
		if (slot == SinputSystems.InputDeviceSlot.any) {
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
			SinputSystems.CommonGamepadMappings.ReloadCommonMaps();
			
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
	public static SinputSystems.InputDeviceSlot GetSlotPress(string controlName){
		return GetSlotPress(Animator.StringToHash(controlName));
	}
	/// <summary>
	/// like GetButtonDown() but returns ~which~ keyboard/gamepad input slot pressed the control
	/// <para>will return InputDeviceSlot.any if no device pressed the button this frame</para>
	/// <para>use it for 'Pres A to join!' type multiplayer, and instantiate a player for the returned slot (if it isn't DeviceSlot.any)</para>
	/// </summary>
	public static SinputSystems.InputDeviceSlot GetSlotPress(int controlNameHashed) {
		//like GetButtonDown() but returns ~which~ keyboard/gamepad input slot pressed the control
		//use it for 'Pres A to join!' type multiplayer, and instantiate a player for the returned slot (if it isn't DeviceSlot.any)

		SinputUpdate();

		if (keyboardAndMouseAreDistinct){
			if (GetButtonDown(controlNameHashed, SinputSystems.InputDeviceSlot.keyboard)) return SinputSystems.InputDeviceSlot.keyboard;
			if (GetButtonDown(controlNameHashed, SinputSystems.InputDeviceSlot.mouse)) return SinputSystems.InputDeviceSlot.mouse;
		}else{
			if (GetButtonDown(controlNameHashed, SinputSystems.InputDeviceSlot.keyboardAndMouse)) return SinputSystems.InputDeviceSlot.keyboardAndMouse;
			if (!zeroInputs[(int) SinputSystems.InputDeviceSlot.keyboardAndMouse] && GetButtonDown(controlNameHashed, SinputSystems.InputDeviceSlot.keyboard)) return SinputSystems.InputDeviceSlot.keyboardAndMouse;
			if (!zeroInputs[(int) SinputSystems.InputDeviceSlot.keyboardAndMouse] && GetButtonDown(controlNameHashed, SinputSystems.InputDeviceSlot.mouse)) return SinputSystems.InputDeviceSlot.keyboardAndMouse;
		}

		for (int i = (int) SinputSystems.InputDeviceSlot.gamepad1; i <= (int) SinputSystems.InputDeviceSlot.gamepad11; i++) {
			if (GetButtonDown(controlNameHashed, (SinputSystems.InputDeviceSlot) i)) return (SinputSystems.InputDeviceSlot) i;
		}
		
		if (GetButtonDown(controlNameHashed, SinputSystems.InputDeviceSlot.virtual1)) return SinputSystems.InputDeviceSlot.virtual1;

		return SinputSystems.InputDeviceSlot.any;
	}


	//Button control checks
	/// <summary>
	/// Returns true if a Sinput Control or Smart Control is Held this frame
	/// </summary>
	public static bool GetButton(string controlName, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return GetButton(Animator.StringToHash(controlName), slot); }
	/// <summary>
	/// Returns true if a Sinput Control or Smart Control is Held this frame
	/// </summary>
	public static bool GetButton(int controlNameHashed, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return ButtonCheck(controlNameHashed, slot, SinputSystems.ButtonAction.HELD); }

	/// <summary>
	/// Returns true if a Sinput Control or Smart Control was Pressed this frame
	/// </summary>
	public static bool GetButtonDown(string controlName, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return GetButtonDown(Animator.StringToHash(controlName), slot); }
	/// <summary>
	/// Returns true if a Sinput Control or Smart Control was Pressed this frame
	/// </summary>
	public static bool GetButtonDown(int controlNameHashed, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return ButtonCheck(controlNameHashed, slot, SinputSystems.ButtonAction.DOWN); }

	/// <summary>
	/// Returns true if a Sinput Control or Smart Control was Released this frame
	/// </summary>
	public static bool GetButtonUp(string controlName, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return GetButtonUp(Animator.StringToHash(controlName), slot); }
	/// <summary>
	/// Returns true if a Sinput Control or Smart Control was Released this frame
	/// </summary>
	public static bool GetButtonUp(int controlNameHashed, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return ButtonCheck(controlNameHashed, slot, SinputSystems.ButtonAction.UP); }

	/// <summary>
	/// Returns true if a Sinput Control or Smart Control is Held this frame, regardless of the Control's toggle setting.
	/// </summary>
	public static bool GetButtonRaw(string controlName, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return GetButtonRaw(Animator.StringToHash(controlName), slot); }
	/// <summary>
	/// Returns true if a Sinput Control or Smart Control is Held this frame, regardless of the Control's toggle setting.
	/// </summary>
	public static bool GetButtonRaw(int controlNameHashed, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return ButtonCheck(controlNameHashed, slot, SinputSystems.ButtonAction.HELD, true); }

	/// <summary>
	/// Returns true if a Sinput Control or Smart Control was Pressed this frame, regardless of the Control's toggle setting.
	/// </summary>
	public static bool GetButtonDownRaw(string controlName, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return GetButtonDownRaw(Animator.StringToHash(controlName), slot); }
	/// <summary>
	/// Returns true if a Sinput Control or Smart Control was Pressed this frame, regardless of the Control's toggle setting.
	/// </summary>
	public static bool GetButtonDownRaw(int controlNameHashed, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return ButtonCheck(controlNameHashed, slot, SinputSystems.ButtonAction.DOWN, true); }

	/// <summary>
	/// Returns true if a Sinput Control or Smart Control was Released this frame, regardless of the Control's toggle setting.
	/// </summary>
	public static bool GetButtonUpRaw(string controlName, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return GetButtonUpRaw(Animator.StringToHash(controlName), slot); }
	/// <summary>
	/// Returns true if a Sinput Control or Smart Control was Released this frame, regardless of the Control's toggle setting.
	/// </summary>
	public static bool GetButtonUpRaw(int controlNameHashed, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return ButtonCheck(controlNameHashed, slot, SinputSystems.ButtonAction.UP, true); }

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
	public static bool GetButtonDownRepeating(string controlName, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return GetButtonDownRepeating(Animator.StringToHash(controlName), slot); }
	/// <summary>
	/// Returns true if a Sinput Control or Smart Control was Pressed this frame, or if it has been held long enough to start repeating.
	/// <para>Use this for menu scrolling inputs</para>
	/// </summary>
	public static bool GetButtonDownRepeating(int controlNameHashed, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return ButtonCheck(controlNameHashed, slot, SinputSystems.ButtonAction.REPEATING); }

	static bool ButtonCheck(int controlNameHashed, SinputSystems.InputDeviceSlot slot, SinputSystems.ButtonAction bAction, bool getRawValue = false) {
		
		SinputUpdate();
		if (zeroInputs[(int)slot]) return false;

		var controlFound = false;

		for (int i=0; i<_controls.Length; i++){
			if (_controls[i].nameHashed == controlNameHashed) {
				controlFound=true;
				if (_controls[i].GetButtonState(bAction, slot, getRawValue)) return true;
			}
		}

		for (int i=0; i<smartControls.Length; i++){
			if (smartControls[i].nameHashed == controlNameHashed) {
				controlFound=true;
				if (smartControls[i].ButtonCheck(bAction, slot)) return true;
			}
		}

		if (!controlFound) Debug.LogError("Sinput Error: Hashed Control \"" + controlNameHashed + "\" not found in list of controls or SmartControls.");

		return false;
	}


	//Axis control checks
	/// <summary>
	/// Returns the value of a Sinput Control or Smart Control.
	/// </summary>
	public static float GetAxis(string controlName, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return GetAxis(Animator.StringToHash(controlName), slot); }
	/// <summary>
	/// Returns the value of a Sinput Control or Smart Control.
	/// </summary>
	public static float GetAxis(int controlNameHashed, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return AxisCheck(controlNameHashed, slot); }

	/// <summary>
	/// Returns the raw value of a Sinput Control or Smart Control
	/// </summary>
	public static float GetAxisRaw(string controlName, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return GetAxisRaw(Animator.StringToHash(controlName), slot); }
	/// <summary>
	/// Returns the raw value of a Sinput Control or Smart Control
	/// </summary>
	public static float GetAxisRaw(int controlNameHashed, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return AxisCheck(controlNameHashed, slot, true); }

	static float AxisCheck(int controlNameHashed, SinputSystems.InputDeviceSlot slot, bool getRawValue = false) {
		SinputUpdate();
		if (zeroInputs[(int)slot]) return 0f;

		if (controlNameHashed == HashedEmptyString) return 0f;

		var controlFound = false;

		var returnV = 0f;
		for (int i=0; i<_controls.Length; i++){
			if (_controls[i].nameHashed == controlNameHashed) {
				controlFound=true;
				var v = _controls[i].GetAxisState(slot);
				if (Mathf.Abs(v) > returnV) returnV = v;
			}
		}

		for (int i=0; i<smartControls.Length; i++){
			if (smartControls[i].nameHashed == controlNameHashed) {
				controlFound=true;
				var v = smartControls[i].GetValue(slot, getRawValue);
				if (Mathf.Abs(v) > returnV) returnV = v;
			}
		}

		if (!controlFound) Debug.LogError("Sinput Error: Hashed Control \"" + controlNameHashed + "\" not found in list of Controls or SmartControls.");

		return returnV;
	}

	//vector checks
	/// <summary>
	/// Returns a Vector2 made with GetAxis() values applied to x and y
	/// </summary>
	public static Vector2 GetVector(string controlNameA, string controlNameB, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any, bool normalClip = true) { return Vector2Check(Animator.StringToHash(controlNameA), Animator.StringToHash(controlNameB), slot, normalClip); }

	static Vector2 Vector2Check(int controlNameAHashed, int controlNameBHashed, SinputSystems.InputDeviceSlot slot, bool normalClip) {
		SinputUpdate();

		Vector2 returnVec2;
		returnVec2.x = AxisCheck(controlNameAHashed, slot);
		returnVec2.y = AxisCheck(controlNameBHashed, slot);

		if (normalClip) {
			var magnitude = returnVec2.magnitude;
			if (magnitude > 1f) {
				returnVec2 = returnVec2 / magnitude; // Normalize reusing magnitude (optimization)
			}
		}

		return returnVec2;
	}

	/// <summary>
	/// Returns a Vector3 made with GetAxis() values applied to x, y, and z
	/// </summary>
	public static Vector3 GetVector(string controlNameA, string controlNameB, string controlNameC, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any, bool normalClip = true) { return Vector3Check(Animator.StringToHash(controlNameA), Animator.StringToHash(controlNameB), Animator.StringToHash(controlNameC), slot, normalClip); }

	static Vector3 Vector3Check(int controlNameAHashed, int controlNameBHashed, int controlNameCHashed, SinputSystems.InputDeviceSlot slot, bool normalClip) {
		SinputUpdate();

		Vector3 returnVec3;
		returnVec3.x = AxisCheck(controlNameAHashed, slot);
		returnVec3.y = AxisCheck(controlNameBHashed, slot);
		returnVec3.z = AxisCheck(controlNameCHashed, slot);

		if (normalClip) {
			var magnitude = returnVec3.magnitude;
			if (magnitude > 1f) {
				returnVec3 = returnVec3 / magnitude; // Normalize reusing magnitude (optimization)
			}
		}

		return returnVec3;
	}
	
	//frame delta preference
	/// <summary>
	/// Returns false if the value returned by GetAxis(controlName) on this frame should NOT be multiplied by delta time.
	/// <para>For example, this will return true for gamepad stick values, false for mouse movement values</para>
	/// </summary>
	public static bool PrefersDeltaUse(string controlName, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return PrefersDeltaUse(Animator.StringToHash(controlName), slot); }
	/// <summary>
	/// Returns false if the value returned by GetAxis(controlName) on this frame should NOT be multiplied by delta time.
	/// <para>For example, this will return true for gamepad stick values, false for mouse movement values</para>
	/// </summary>
	public static bool PrefersDeltaUse(int controlNameHashed, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) {

		SinputUpdate();

		if (controlNameHashed == HashedEmptyString) return false;

		var preferDelta = true;
		var controlFound = false;
		var v = 0f;

		var returnV = 0f;
		for (int i = 0; i < _controls.Length; i++) {
			if (_controls[i].nameHashed == controlNameHashed) {
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
			if (smartControls[i].nameHashed == controlNameHashed) {
				controlFound = true;
				v = smartControls[i].GetValue(slot, true);
				if (Mathf.Abs(v) > returnV) {
					returnV = v;

					preferDelta &= PrefersDeltaUse(smartControls[i].positiveControlHashed, slot) && PrefersDeltaUse(smartControls[i].negativeControlHashed, slot);
				}
			}
		}

		if (!controlFound) Debug.LogError("Sinput Error: Hashed Control \"" + controlNameHashed + "\" not found in list of Controls or SmartControls.");

		return preferDelta;
	}

	
	/// <summary>
	/// sets whether a control treats GetButton() calls with press or with toggle behaviour
	/// </summary>
	public static void SetToggle(string controlName, bool toggle) { SetToggle(Animator.StringToHash(controlName), toggle); }
	/// <summary>
	/// sets whether a control treats GetButton() calls with press or with toggle behaviour
	/// </summary>
	public static void SetToggle(int controlNameHashed, bool toggle) {
		SinputUpdate();
		var controlFound = false;
		for (int i = 0; i < _controls.Length; i++) {
			if (_controls[i].nameHashed == controlNameHashed) {
				controlFound = true;
				_controls[i].isToggle = toggle;
			}
		}
		if (!controlFound) Debug.LogError("Sinput Error: Hashed Control \"" + controlNameHashed + "\" not found in list of Controls or SmartControls.");
	}
	
	/// <summary>
	/// returns true if a control treats GetButton() calls with toggle behaviour
	/// </summary>
	public static bool GetToggle(string controlName) { return GetToggle(Animator.StringToHash(controlName)); }
	/// <summary>
	/// returns true if a control treats GetButton() calls with toggle behaviour
	/// </summary>
	public static bool GetToggle(int controlNameHashed) {
		SinputUpdate();
		for (int i = 0; i < _controls.Length; i++) {
			if (_controls[i].nameHashed == controlNameHashed) {
				return _controls[i].isToggle;
			}
		}
		Debug.LogError("Sinput Error: Hashed Control \"" + controlNameHashed + "\" not found in list of Controls or SmartControls.");
		return false;
	}

	
	/// <summary>
	/// set a smart control to be inverted or not
	/// </summary>
	public static void SetInverted(string smartControlName, bool invert, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { SetInverted(Animator.StringToHash(smartControlName), invert, slot); }
	/// <summary>
	/// set a smart control to be inverted or not
	/// </summary>
	public static void SetInverted(int smartControlNameHashed, bool invert, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) {
		SinputUpdate();
		var controlFound = false;
		for (int i = 0; i < smartControls.Length; i++) {
			if (smartControls[i].nameHashed == smartControlNameHashed) {
				controlFound = true;
				if (slot == SinputSystems.InputDeviceSlot.any) {
					for (int k=0; k<totalPossibleDeviceSlots; k++) {
						smartControls[i].inversion[k] = invert;
					}
				} else {
					smartControls[i].inversion[(int)slot] = invert;
				}
			}
		}
		if (!controlFound) Debug.LogError("Sinput Error: Hashed Smart Control \"" + smartControlNameHashed + "\" not found in list of SmartControls.");
	}
	
	/// <summary>
	/// returns true if a smart control is inverted
	/// </summary>
	public static bool GetInverted(string smartControlName, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return GetInverted(Animator.StringToHash(smartControlName), slot); }
	/// <summary>
	/// returns true if a smart control is inverted
	/// </summary>
	public static bool GetInverted(int smartControlNameHashed, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) {
		SinputUpdate();
		for (int i = 0; i < smartControls.Length; i++) {
			if (smartControls[i].nameHashed == smartControlNameHashed) {
				return smartControls[i].inversion[(int)slot];
			}
		}
		Debug.LogError("Sinput Error: Hashed Smart Control \"" + smartControlNameHashed + "\" not found in list of SmartControls.");
		return false;
	}

	
	/// <summary>
	/// sets scale ("sensitivity") of a smart control
	/// </summary>
	public static void SetScale(string smartControlName, float scale, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { SetScale(Animator.StringToHash(smartControlName), scale, slot); }
	/// <summary>
	/// sets scale ("sensitivity") of a smart control
	/// </summary>
	public static void SetScale(int smartControlNameHashed, float scale, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) {
		SinputUpdate();
		var controlFound = false;
		for (int i = 0; i < smartControls.Length; i++) {
			if (smartControls[i].nameHashed == smartControlNameHashed) {
				controlFound = true;
				if (slot == SinputSystems.InputDeviceSlot.any) {
					for (int k = 0; k < totalPossibleDeviceSlots; k++) {
						smartControls[i].scales[k] = scale;
					}
				} else {
					smartControls[i].scales[(int)slot] = scale;
				}
			}
		}
		if (!controlFound) Debug.LogError("Sinput Error: Hashed Smart Control \"" + smartControlNameHashed + "\" not found in list of SmartControls.");
	}
	
	/// <summary>
	/// gets scale of a smart control
	/// </summary>
	public static float GetScale(string smartControlName, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) { return GetScale(Animator.StringToHash(smartControlName), slot); }
	/// <summary>
	/// gets scale of a smart control
	/// </summary>
	public static float GetScale(int smartControlNameHashed, SinputSystems.InputDeviceSlot slot = SinputSystems.InputDeviceSlot.any) {
		for (int i = 0; i < smartControls.Length; i++) {
			if (smartControls[i].nameHashed == smartControlNameHashed) {
				return smartControls[i].scales[(int)slot];
			}
		}
		Debug.LogError("Sinput Error: Hashed Smart Control \"" + smartControlNameHashed + "\" not found in list of SmartControls.");
		return 1f;
	}

}


