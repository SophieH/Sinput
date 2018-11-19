using System;
using UnityEngine;

namespace SinputSystems{
	public class DeviceInput{

		public InputDeviceType inputType;
		public string displayName;

		//custom bound stuff
		public bool isCustom = false;
		public string deviceName = "";

		public string GetDisplayName(){
			if (inputType == InputDeviceType.Keyboard){
				return keyboardKeyCode.ToString();
			}
			if (inputType == InputDeviceType.Mouse){
				return mouseInputType.ToString();
			}
			return displayName;
		}


		public DeviceInput(InputDeviceType type){
			inputType = type;

			//if (inputType==InputDeviceType.Virtual){
			//	virtualAxisValue = 0f;
				//virtualInputState = ButtonAction.NOTHING;
			//}
		}

		//////////// ~ keyboard specific stuff ~ ////////////
		public KeyCode keyboardKeyCode; //keycode for if this input is controlled by a keyboard key

		//////////// ~ mouse specific stuff ~ ////////////
		public MouseInputType mouseInputType;

		//////////// ~ gamepad specific stuff ~ ////////////
		public int[] allowedSlots; //list of gamepad slots that this input is allowed to check (they will be ones with a matching name to the known binding
		public CommonGamepadInputs commonMappingType; //if this is set, this input is a preset/default
		public CommonXRInputs commonXRMappingType;
		public int gamepadButtonNumber; //button number for if this input is controlled by a gamepad button

		public int gamepadAxisNumber; //axis number for if this input is controlled by a gamepad axis
		public bool invertAxis;
		public bool clampAxis;
		public bool rescaleAxis;//for rescaling input axis from something else to 0-1
		public float rescaleAxisMin;
		public float rescaleAxisMax;

		//stuff for treating axis like a button
		//ButtonAction[] axisButtonState; //state of the axis for when used as a button, updated on the first button checks of a frame. list contains state of this axis for each gamepad slot
		public float axisButtoncompareVal; //axis button is 'pressed' if (axisValue [compareType] compareVal)

		//all GetAxis() checks will return default value until a measured change occurs, since readings before then can be wrong
		private bool useDefaultAxisValue = true;
		public float defaultAxisValue;
		private float measuredAxisValue=-54.321f;

		//////////// ~ virtual specific stuff ~ ////////////
		public string virtualInputID;
		//private ButtonAction virtualInputState;
		//public float virtualAxisValue;

		public float AxisCheck(InputDeviceSlot slot){

			//keyboard checks
			if (inputType == InputDeviceType.Keyboard){
				if (slot == InputDeviceSlot.any || slot == InputDeviceSlot.keyboard || slot == InputDeviceSlot.keyboardAndMouse){
					if (Input.GetKey( keyboardKeyCode )) return 1f;
				}

				return 0f;
			}

			//gamepad button and axis checks
			if (inputType == InputDeviceType.GamepadButton || inputType == InputDeviceType.GamepadAxis){
				if (slot == InputDeviceSlot.keyboard || slot == InputDeviceSlot.mouse || slot == InputDeviceSlot.keyboardAndMouse) return 0f;

				//if checking any slot, call this function for each possible slot
				if (slot == InputDeviceSlot.any){
					float greatestV = 0f;
					for (int i=1; i<=Sinput.connectedGamepads; i++){
						greatestV = Math.Max(greatestV, Math.Abs( AxisCheck((InputDeviceSlot)i) ));
					}
					return greatestV;
				}

				int slotIndex = ((int)slot)-1;



				//don't check slots without a connected gamepad
				if (Sinput.connectedGamepads <= slotIndex) return 0f;

				//make sure the gamepad in this slot is one this input is allowed to check (eg don't check PS4 pad bindings for an XBOX pad)
				bool allowInputFromThisPad=false;
				for (int i=0; i<allowedSlots.Length; i++){
					if (slotIndex == allowedSlots[i]) {
						allowInputFromThisPad = true;
						break;
					}
				}

				if (!allowInputFromThisPad) return 0f;

				//button as axis checks
				if (inputType == InputDeviceType.GamepadButton){
					//button check now
					if (Input.GetKey(SInputEnums.GetGamepadKeyCode(slotIndex, gamepadButtonNumber))) return 1f;
				}

				//gamepad axis check
				if (inputType == InputDeviceType.GamepadAxis){
					float axisValue = Input.GetAxisRaw(SInputEnums.GetAxisString(slotIndex, gamepadAxisNumber - 1));
					if (invertAxis) axisValue*=-1f;
					if (rescaleAxis){
						//some gamepad axis are -1 to 1 or something when you want them as 0 to 1, EG; triggers on XBONE pad on OSX
						axisValue = Mathf.InverseLerp(rescaleAxisMin, rescaleAxisMax, axisValue);
					}

					if (clampAxis) axisValue = Mathf.Clamp01(axisValue);

					//we return every axis' default value unless we measure a change first
					//this prevents weird snapping and false button presses if the pad is reporting a weird value to start with
					if (useDefaultAxisValue){
						if (measuredAxisValue!=-54.321f){
							if (axisValue!=measuredAxisValue) useDefaultAxisValue = false;
						}else{
							measuredAxisValue=axisValue;
						}
						if (useDefaultAxisValue) axisValue = defaultAxisValue;
					}

					return axisValue;
				}

				return 0f;
			}


			//virtual device axis input checks
			if (inputType == InputDeviceType.Virtual){
				if (slot == InputDeviceSlot.any || slot == InputDeviceSlot.virtual1) {
					return VirtualInputs.GetVirtualAxis(virtualInputID);
				}
				//return virtualAxisValue;
			}

			//mouseaxis button checks (these don't happen)
			if (inputType == InputDeviceType.Mouse){
				if (slot != InputDeviceSlot.any && slot != InputDeviceSlot.mouse && slot != InputDeviceSlot.keyboardAndMouse) return 0f;

				switch (mouseInputType){
					case MouseInputType.MouseHorizontal:
						return Input.GetAxisRaw("Mouse Horizontal") * Sinput.mouseSensitivity;
					case MouseInputType.MouseMoveLeft:
						return Math.Min(Input.GetAxisRaw("Mouse Horizontal") * Sinput.mouseSensitivity, 0f) * -1f;
					case MouseInputType.MouseMoveRight:
						return Math.Max(Input.GetAxisRaw("Mouse Horizontal") * Sinput.mouseSensitivity, 0f);
					case MouseInputType.MouseMoveUp:
						return Math.Max(Input.GetAxisRaw("Mouse Vertical") * Sinput.mouseSensitivity, 0f);
					case MouseInputType.MouseMoveDown:
						return Math.Min(Input.GetAxisRaw("Mouse Vertical") * Sinput.mouseSensitivity, 0f) * -1f;
					case MouseInputType.MouseVertical:
						return Input.GetAxisRaw("Mouse Vertical") * Sinput.mouseSensitivity;
					case MouseInputType.MouseScroll:
						return Input.GetAxisRaw("Mouse Scroll");
					case MouseInputType.MouseScrollUp:
						return Math.Max(Input.GetAxisRaw("Mouse Scroll"), 0f);
					case MouseInputType.MouseScrollDown:
						return Math.Min(Input.GetAxisRaw("Mouse Scroll"), 0f) * -1f;
					case MouseInputType.MousePositionX:
						return Input.mousePosition.x;
					case MouseInputType.MousePositionY:
						return Input.mousePosition.y;
					default:
						//it's a click type mouse input
						if (Input.GetKey(SInputEnums.GetMouseButton(mouseInputType))) return 1f;
						break;
				}
				//return Input.GetAxisRaw(mouseAxis);
			}

			return 0f;

		}

		//called max once per frame
		/*public void UpdateAxisButtonStates(){

			if (inputType == InputDeviceType.GamepadAxis){
				float axisValue;
				bool held;
				for (int i=1; i<=Sinput.gamepads.Length; i++){
					axisValue = AxisCheck( (InputDeviceSlot)i );
					held = false;
					if (axisValue>axisButtoncompareVal) held = true;
					axisButtonState[i]= AxisButtonChange(axisButtonState[i], held);
				}
				return;
			}
		
			if (inputType == InputDeviceType.Mouse){
				//ignore clicky inputs
				if (mouseInputType == MouseInputType.Mouse0 || mouseInputType == MouseInputType.Mouse1 || mouseInputType == MouseInputType.Mouse2 || mouseInputType == MouseInputType.Mouse3 || mouseInputType == MouseInputType.Mouse4 || mouseInputType == MouseInputType.Mouse5 || mouseInputType == MouseInputType.Mouse6) return;
				float axisValue = AxisCheck( InputDeviceSlot.mouse );
				bool held = false;
				if (Mathf.Abs(axisValue)>0.5f) held = true;
				axisButtonState[0]= AxisButtonChange(axisButtonState[0], held);

			}
		}
		public void ResetAxisButtonStates(){

			if (inputType == InputDeviceType.GamepadAxis){
				if (null==axisButtonState || axisButtonState.Length != Sinput.gamepads.Length+1){
					axisButtonState = new ButtonAction[Sinput.gamepads.Length+1];
				}

				for (int i=1; i<=Sinput.gamepads.Length; i++){
					axisButtonState[i]=ButtonAction.NOTHING;
				}
			}

			

			if (inputType == InputDeviceType.Mouse){
				//ignore clicky inputs
				if (mouseInputType == MouseInputType.Mouse0 || mouseInputType == MouseInputType.Mouse1 || mouseInputType == MouseInputType.Mouse2 || mouseInputType == MouseInputType.Mouse3 || mouseInputType == MouseInputType.Mouse4 || mouseInputType == MouseInputType.Mouse5 || mouseInputType == MouseInputType.Mouse6) return;

				if (null==axisButtonState){
					axisButtonState = new ButtonAction[1];
				}
				axisButtonState[0]=ButtonAction.NOTHING;
			}
		}
		ButtonAction AxisButtonChange(ButtonAction fromState, bool buttonHeld){
			if (buttonHeld){
				switch (fromState){
				case ButtonAction.NOTHING: return ButtonAction.DOWN;
				case ButtonAction.UP: return ButtonAction.DOWN;
				case ButtonAction.DOWN: return ButtonAction.HELD;
				default: return ButtonAction.HELD;
				}
			}else{
				switch (fromState){
				case ButtonAction.NOTHING: return ButtonAction.NOTHING;
				case ButtonAction.UP: return ButtonAction.NOTHING;
				case ButtonAction.DOWN: return ButtonAction.UP;
				case ButtonAction.HELD: return ButtonAction.UP;
				default: return ButtonAction.NOTHING;
				}
			}
		}
		*/

		
	}
}
