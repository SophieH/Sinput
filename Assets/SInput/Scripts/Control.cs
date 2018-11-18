using System;
using System.Collections.Generic;
using UnityEngine;

namespace SinputSystems{
	// If this class inherits from ScriptableObject, Instantiate of this easily makes a clone
	public class Control : ISerializationCallbackReceiver {
		//name of control
		public string name;
		public int nameHashed { get; private set; }

		//is this control a hold or a toggle type
		public bool isToggle = false;

		//list of inputs we will check when the control is polled
		public List<DeviceInput> inputs;

		public List<CommonGamepadInputs> commonMappings = new List<CommonGamepadInputs>();
		public List<CommonXRInputs> commonXRMappings = new List<CommonXRInputs>();

		//control constructor
		public Control(string controlName){
			name = controlName;
			inputs = new List<DeviceInput>();
			Hash();
		}

		public void OnBeforeSerialize() { }
		public void OnAfterDeserialize() { Hash(); }

		public void Hash() {
			nameHashed = Animator.StringToHash(name);
		}

		private ControlState[] controlStates;
		//called no more than once a frame from Sinput.SinputUpdate
		public void Update() {

			if (null == controlStates) {
				controlStates = new ControlState[Sinput.totalPossibleDeviceSlots];
				for (int i = 0; i < controlStates.Length; i++) {
					controlStates[i] = new ControlState();
				}
				ResetControlStates();
			}

			//do update here
			//int connectedGamepads = Sinput.gamepads.Length;
			for (int i = 1; i < controlStates.Length; i++) {
				// Check if slot represents any of this:
				bool checkslot = i <= Sinput.connectedGamepads || // a connected pad?
					i >= 17; // a keyboard, mouse, or virtual slot?

				if (checkslot) {
					//this is a (probably) connected device so lets update it
					UpdateControlState(controlStates[i], (InputDeviceSlot)i);
				} else {
					//this device isn't connected & shouldn't be influencing this control
					//reset it
					controlStates[i].Reset();
				}
				
			}
			UpdateAnyControlState();//checked other slots, now check the 'any' slot
			//UpdateControlState(0, InputDeviceSlot.any);//checked other slots, now check the 'any' slot
		}

		void UpdateControlState(ControlState controlState, InputDeviceSlot slot) {
			var wasHeld = controlState.held;
			controlState.held = false;

			controlState.value = 0f;
			controlState.valuePrefersDeltaUse = true;
			controlState.axisAsButtonHeld = false;
			
			foreach (var input in inputs) {
				var v = input.AxisCheck(slot);

				//update axis-as-button state
				if (input.inputType == InputDeviceType.GamepadAxis) {
					controlState.axisAsButtonHeld |= v > input.axisButtoncompareVal;
				}
				else if (input.inputType == InputDeviceType.Mouse) {
					controlState.axisAsButtonHeld |= Math.Abs(v) > 0.5f;
				}

				if (Math.Abs(v) > controlState.value) {
					//this is the value we're going with
					controlState.value = v;
					//now find out if what set this value was something we shouldn't multiply by deltaTime
					controlState.valuePrefersDeltaUse =
						input.inputType != InputDeviceType.Mouse ||
						input.mouseInputType < MouseInputType.MouseMoveLeft ||
						input.mouseInputType > MouseInputType.MouseScroll;
				}

				//check if this control is held
				controlState.held |= input.ButtonHeldCheck(slot);
			}

			//check if this control is held
			controlState.held |= controlState.axisAsButtonHeld;

			UpdateButtonStates(controlState, wasHeld);
		}

		void UpdateAnyControlState() {
			ControlState controlState = controlStates[0];

			var wasHeld = controlState.held;
			controlState.held = false;

			controlState.value = 0f;
			controlState.axisAsButtonHeld = false;

			for (int i = 1; i < controlStates.Length; i++) {
				var v = controlStates[i].value;

				if (Math.Abs(v) > controlState.value) {
					//this is the value we're going with
					controlState.value = v;
					//now find out if what set this value was something we shouldn't multiply by deltaTime
					controlState.valuePrefersDeltaUse = controlStates[i].valuePrefersDeltaUse;
				}

				//check if this control is held
				controlState.held |= controlStates[i].held;
			}

			UpdateButtonStates(controlState, wasHeld);
		}

		private void UpdateButtonStates(ControlState controlState, bool wasHeld) {

			//held state
			controlState.pressed = !wasHeld && controlState.held;
			controlState.released = wasHeld && !controlState.held;

			//toggled state
			controlState.togglePressed = false;
			controlState.toggleReleased = false;
			if (controlState.pressed) {
				controlState.toggleHeld = !controlState.toggleHeld;
				controlState.togglePressed = controlState.toggleHeld;
				controlState.toggleReleased = !controlState.toggleHeld;
			}

			//repeating press state
			controlState.repeatPressed = false;
			if (controlState.pressed) {
				controlState.repeatPressed = true;//repeat press returns true on first frame down
				controlState.repeatTime = Sinput.buttonRepeatWait + Sinput.buttonRepeat;
			}
			if (controlState.held) {
				controlState.repeatTime -= Time.deltaTime;
				if (controlState.repeatTime < 0f) {
					controlState.repeatTime = Sinput.buttonRepeat;
					controlState.repeatPressed = true;
				}
			}
			else {
				controlState.repeatTime = 0f;
			}

		}

		public void ResetControlStates() {
			//set all values for this control to 0
			foreach (var controlState in controlStates) controlState.Reset();
		}

		//button checks
		public bool GetButtonState(ButtonAction bAction, InputDeviceSlot slot, bool getRaw) {

			if (!getRaw && isToggle) {
				if (bAction == ButtonAction.HELD) return controlStates[(int)slot].toggleHeld;
				if (bAction == ButtonAction.DOWN) return controlStates[(int)slot].togglePressed;
				if (bAction == ButtonAction.UP) return controlStates[(int)slot].toggleReleased;
			} else { 
				if (bAction == ButtonAction.HELD) return controlStates[(int)slot].held;
				if (bAction == ButtonAction.DOWN) {
					if (null==controlStates) Debug.Log("yup");
					return controlStates[(int)slot].pressed;

				}
				if (bAction == ButtonAction.UP) return controlStates[(int)slot].released;
			}
			if (bAction == ButtonAction.REPEATING) return controlStates[(int)slot].repeatPressed;

			return false;
		}
	

		//axis checks
		public float GetAxisState(InputDeviceSlot slot, out bool prefersDeltaUse) {
			prefersDeltaUse = controlStates[(int)slot].valuePrefersDeltaUse;
			return controlStates[(int)slot].value;
		}
		public bool GetAxisStateDeltaPreference(InputDeviceSlot slot) {
			return controlStates[(int)slot].valuePrefersDeltaUse;
		}


		public void AddKeyboardInput(KeyCode keyCode){
			DeviceInput input = new DeviceInput(InputDeviceType.Keyboard);
			input.keyboardKeyCode = keyCode;
			input.commonMappingType = CommonGamepadInputs.NOBUTTON;//don't remove this input when gamepads are unplugged/replugged
			inputs.Add(input);
		}

		public void AddGamepadInput(CommonGamepadInputs gamepadButtonOrAxis) { AddGamepadInput(gamepadButtonOrAxis, true); }
		public void AddGamepadInput(CommonXRInputs gamepadButtonOrAxis) { AddGamepadInput(gamepadButtonOrAxis, true); }
		private void AddGamepadInput(CommonGamepadInputs gamepadButtonOrAxis, bool isNewBinding) {
			Sinput.CheckGamepads();

			if (isNewBinding) commonMappings.Add(gamepadButtonOrAxis);
			List<DeviceInput> applicableMapInputs = CommonGamepadMappings.GetApplicableMaps(gamepadButtonOrAxis, CommonXRInputs.NOBUTTON, Sinput.gamepads);

			for (int i = 0; i < applicableMapInputs.Count; i++) {
				applicableMapInputs[i].commonXRMappingType = CommonXRInputs.NOBUTTON;
			}

			AddGamepadInputs(applicableMapInputs);
		}
		private void AddGamepadInput(CommonXRInputs gamepadButtonOrAxis, bool isNewBinding) {
			Sinput.CheckGamepads();

			if (isNewBinding) commonXRMappings.Add(gamepadButtonOrAxis);
			List<DeviceInput> applicableMapInputs = CommonGamepadMappings.GetApplicableMaps(CommonGamepadInputs.NOBUTTON, gamepadButtonOrAxis, Sinput.gamepads);

			for (int i = 0; i < applicableMapInputs.Count; i++) {
				applicableMapInputs[i].commonMappingType = CommonGamepadInputs.NOBUTTON;
			}

			AddGamepadInputs(applicableMapInputs);
		}
		private void AddGamepadInputs(List<DeviceInput> applicableMapInputs) { 

			string[] gamepads = Sinput.gamepads;

			//find which common mapped inputs apply here, but already have custom binding loaded, and disregard those common mappings
			for (int ai=0; ai<applicableMapInputs.Count; ai++){
				bool samePad = false;
				for (int i=0; i<inputs.Count; i++){
					if (inputs[i].inputType == InputDeviceType.GamepadAxis || inputs[i].inputType == InputDeviceType.GamepadButton){
						if (inputs[i].isCustom){
							for (int ais=0; ais<applicableMapInputs[ai].allowedSlots.Length; ais++){
								for (int toomanyints=0; toomanyints<inputs[i].allowedSlots.Length; toomanyints++){
									if (applicableMapInputs[ai].allowedSlots[ais] == inputs[i].allowedSlots[toomanyints]) samePad = true;
								}
								if (gamepads[applicableMapInputs[ai].allowedSlots[ais]] == inputs[i].deviceName.ToUpper()) samePad = true;
							}
							if (samePad){
								//if I wanna be copying input display names, here's the place to do it
								//TODO: decide if I wanna do this
								//pro: it's good if the common mapping is accurate but the user wants to rebind
								//con: it's bad if the common mapping is bad or has a generic gamepad name and so it mislables different inputs
								//maybe I should do this, but with an additional check so it's not gonna happen with say, a device labelled "wireless controller"?
							}
						}
					}
				}
				if (samePad){
					//we already have a custom bound control for this input, we don't need more
					applicableMapInputs.RemoveAt(ai);
					ai--;
				}
			}

			

			//add whichever common mappings still apply
			for (int i=0; i<applicableMapInputs.Count; i++){


				inputs.Add(applicableMapInputs[i]);
			}
		}

		/*public void AddXRInput(CommonXRInputs xrInputType) {
			AddXRInput(xrInputType, true);
		}
		private void AddXRInput(CommonXRInputs xrInputType, bool isNewBinding) {

			if (isNewBinding) commonXRMappings.Add(xrInputType);

			DeviceInput input = new DeviceInput(InputDeviceType.XR);
			input.commonMappingType = CommonGamepadInputs.NOBUTTON;
			input.commonXRMappingType = xrInputType;
			inputs.Add(input);
		}*/

		public void AddMouseInput(MouseInputType mouseInputType){
			DeviceInput input = new DeviceInput(InputDeviceType.Mouse);
			input.mouseInputType = mouseInputType;
			input.commonMappingType = CommonGamepadInputs.NOBUTTON;
			input.commonXRMappingType = CommonXRInputs.NOBUTTON;
			inputs.Add(input);
		}

		

		public void AddVirtualInput(string virtualInputID){
			DeviceInput input = new DeviceInput(InputDeviceType.Virtual);
			input.virtualInputID = virtualInputID;
			input.commonMappingType = CommonGamepadInputs.NOBUTTON;
			input.commonXRMappingType = CommonXRInputs.NOBUTTON;
			inputs.Add(input);
			VirtualInputs.AddInput(virtualInputID);
		}

		public void ReapplyCommonBindings(){
			//connected gamepads have changed, so we want to remove all old common bindings, and replace them now new mapping information has been loaded
			for (int i=0; i<inputs.Count; i++){
				if (inputs[i].commonMappingType != CommonGamepadInputs.NOBUTTON) {
					inputs.RemoveAt(i);
					i--;
				}
			}
			for (int i = 0; i < inputs.Count; i++) {
				if (inputs[i].commonXRMappingType != CommonXRInputs.NOBUTTON) {
					inputs.RemoveAt(i);
					i--;
				}
			}



			for (int i = 0; i < commonMappings.Count; i++) {
				AddGamepadInput(commonMappings[i], false);
			}
			for (int i = 0; i < commonXRMappings.Count; i++) {
				AddGamepadInput(commonXRMappings[i], false);
			}

			//also recheck allowed slots for custom bound pads (their inputs have a device name, common bound stuff don't)
			//need to do this anyway so we can check if common & custom bindings are about to match on the same slot
			string[] gamepads= Sinput.gamepads;
			for (int i=0; i<inputs.Count; i++){
				if (inputs[i].deviceName!=""){
					List<int> allowedSlots = new List<int>();
					for (int g=0; g<gamepads.Length; g++){
						if (gamepads[g] == inputs[i].deviceName.ToUpper()) allowedSlots.Add(i);
					}
					inputs[i].allowedSlots = allowedSlots.ToArray();
				}
			}
		}

		public void SetAllowedInputSlots() {
			//custom gamepad inputs need to know which gamepad slots they can look at to match the gamepad they are for
			for (int i = 0; i < inputs.Count; i++) {
				if (inputs[i].isCustom) {
					if (inputs[i].inputType == InputDeviceType.GamepadAxis || inputs[i].inputType == InputDeviceType.GamepadButton) {
						//Debug.Log("Finding slot for gamepad: " + controls[c].inputs[i].displayName + " of " + controls[c].inputs[i].deviceName);
						//find applicable gamepad slots for this device
						List<int> allowedSlots = new List<int>();
						for (int g = 0; g < Sinput.connectedGamepads; g++) {
							if (Sinput.gamepads[g] == inputs[i].deviceName.ToUpper()) {
								allowedSlots.Add(g);
							}
						}
						inputs[i].allowedSlots = allowedSlots.ToArray();
					}
				}
			}
		}


	}

	//state of control, for a frame, for one slot
	class ControlState {
		//basic cacheing of all relevant inputs for this slot
		public float value;
		public bool axisAsButtonHeld;
		public bool held;
		public bool released;
		public bool pressed;

		//for toggle checks
		public bool toggleHeld;
		public bool toggleReleased;
		public bool togglePressed;

		//for checking if the value is something that should be multiplied by deltaTime or not
		public bool valuePrefersDeltaUse = true;

		//for Sinput.ButtonPressRepeat() checks
		public bool repeatPressed;
		public float repeatTime;

		public void Reset() {
			value = 0f;
			axisAsButtonHeld = false;
			held = false;
			released = false;
			pressed = false;
			repeatPressed = false;
			valuePrefersDeltaUse = true;
			repeatTime = 0f;
			toggleHeld = false;
			togglePressed = false;
			toggleReleased = false;
		}
	}
}
