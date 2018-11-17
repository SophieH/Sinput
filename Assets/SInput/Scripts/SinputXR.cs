using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace SinputSystems.XR {

	public class SinputXR {

		private List<SinputXRNode> nodes = new List<SinputXRNode>();
		private List<XRNodeState> xrNodeStates = new List<XRNodeState>();

		public bool XRenabled = false;

		private int lastXRUpdate = -1;

		public void Update() {
			if (lastXRUpdate == Time.frameCount) return;
			lastXRUpdate = Time.frameCount;

			XRenabled = UnityEngine.XR.XRSettings.enabled;

			InputTracking.GetNodeStates(xrNodeStates);
			for (int i=0; i<xrNodeStates.Count; i++) {
				int nodeIndex = -1;
				for (int k=0; k<nodes.Count; k++) {
					if (nodes[k].nodeType == xrNodeStates[i].nodeType) nodeIndex = k;
				}
				if (nodeIndex == -1) {
					nodeIndex = xrNodeStates.Count;
					nodes.Add(new SinputXRNode(xrNodeStates[i].nodeType));
				} else {

					nodes[nodeIndex].Update(xrNodeStates[i]);
				}
			}
		}

		public void UpdateJoystickIndeces() {
			for (int i = 0; i < nodes.Count; i++) {
				nodes[i].UpdateJoystickIndeces();
			}
		}

		public SinputXRNode GetNode(XRNode nodeType) {

			for (int i = 0; i < nodes.Count; i++) {
				if (nodes[i].nodeType == nodeType) {
					//TODO: return a copy of the node, not the node itself
					return nodes[i];
				}
			}

			return new SinputXRNode(nodeType);
		}
		
	}

	public class SinputXRNode {
		public XRNode nodeType;

		public bool isTracked = false;

		public List<int> joystickIndeces = new List<int>();

		public Vector3 position = Vector3.zero;
		public Quaternion rotation = new Quaternion();
		public Vector3 acceleration = Vector3.zero;
		public Vector3 angularAcceleration = Vector3.zero;
		public Vector3 velocity = Vector3.zero;
		public Vector3 angularVelocity = Vector3.zero;

		private Vector3 _position = Vector3.zero;
		private Quaternion _rotation = new Quaternion();
		private Vector3 _acceleration = Vector3.zero;
		private Vector3 _angularAcceleration = Vector3.zero;
		private Vector3 _velocity = Vector3.zero;
		private Vector3 _angularVelocity = Vector3.zero;


		public SinputXRNode(XRNode newNodeType) {
			nodeType = newNodeType;

			UpdateJoystickIndeces();

			string vrEnvironmentType = UnityEngine.XR.XRSettings.loadedDeviceName.ToLower();
			if (vrEnvironmentType.Contains("openvr")) {
				if (nodeType == XRNode.LeftHand) {
					AddBinding("Trigger", (int)OpenVRButtons.TRIGGER_TOUCH_L);
				}
				if (nodeType == XRNode.RightHand) {
					AddBinding("Trigger", (int)OpenVRButtons.TRIGGER_TOUCH_R);
				}
			}
			if (vrEnvironmentType.Contains("oculus")) {
				if (nodeType == XRNode.LeftHand) {
					AddBinding("Trigger", (int)OculusButtons.INDEXTRIGGER_PRIMARY_TOUCH);
				}
				if (nodeType == XRNode.RightHand) {
					AddBinding("Trigger", (int)OculusButtons.INDEXTRIGGER_SECONDARY_TOUCH);
				}
			}
		}

		public void UpdateJoystickIndeces() {
			//find which joysticks this node can accept input from
			string[] joystickNames = Input.GetJoystickNames();

			joystickIndeces = new List<int>();
			
			for (int i = 0; i < joystickNames.Length; i++) {
				//Debug.Log(joystickNames[i]);
				if (nodeType == XRNode.LeftHand) {
					if (joystickNames[i].ToLower().Contains("left")) joystickIndeces.Add(i);
					//if (joystickNames[i] == "OpenVR Controller - Left") joystickIndeces.Add(i);
					//if (joystickNames[i] == "Oculus Touch - Left") joystickIndeces.Add(i);
					if (joystickNames[i] == "Oculus Remote") joystickIndeces.Add(i);
				}
				if (nodeType == XRNode.RightHand) {
					if (joystickNames[i].ToLower().Contains("right")) joystickIndeces.Add(i);
					//if (joystickNames[i] == "OpenVR Controller - Right") joystickIndeces.Add(i);
					//if (joystickNames[i] == "Oculus Touch - Right") joystickIndeces.Add(i);
					if (joystickNames[i] == "Oculus Remote") joystickIndeces.Add(i);
				}

				if (joystickNames[i] == "") {
					//we accept any input from unnamed joysticks because even though unity docs ~claim~
					//XR input devices can be identified that way, it's nonsense actually
					joystickIndeces.Add(i);
				}
			}

			//joystick name numbers begin at 1
			//because perish the idea that things in unity might be consistent
			for (int i=0; i<joystickIndeces.Count; i++) {
				joystickIndeces[i] += 1;
			}
		}


		public void Update(XRNodeState nodeState) {
			_position = Vector3.zero;
			_rotation = new Quaternion();
			_acceleration = Vector3.zero;
			_angularAcceleration = Vector3.zero;
			_velocity = Vector3.zero;
			_angularVelocity = Vector3.zero;
			
			isTracked = nodeState.tracked;

			if (nodeState.TryGetPosition(out _position)) {
				position = _position;
			}
		
			if (nodeState.TryGetRotation(out _rotation)) {
				rotation = _rotation;
			}

			if (nodeState.TryGetVelocity(out _velocity)) {
				velocity = _velocity;
			}

			if (nodeState.TryGetAngularVelocity(out _angularVelocity)) {
				angularVelocity = _angularVelocity;
			}

			if (nodeState.TryGetAcceleration(out _acceleration)) {
				acceleration = _acceleration;
			}

			if (nodeState.TryGetAngularAcceleration(out _angularAcceleration)) {
				angularAcceleration = _angularAcceleration;
			}

		}

		public bool GetButton(string controlName) { return ButtonCheck(controlName, ButtonAction.HELD); }
		public bool GetButtonDown(string controlName) { return ButtonCheck(controlName, ButtonAction.HELD); }
		public bool GetButtonUp(string controlName) { return ButtonCheck(controlName, ButtonAction.HELD); }
		public bool GetButtonDownRepeating(string controlName) { return ButtonCheck(controlName, ButtonAction.HELD); }

		private bool ButtonCheck(string controlname, ButtonAction bAction) {
			Sinput.SinputUpdate();

			for (int i = 0; i < joystickIndeces.Count; i++) {
				if (bAction == ButtonAction.DOWN && Sinput.GetButtonDown(controlname, (InputDeviceSlot)joystickIndeces[i])) return true;
				if (bAction == ButtonAction.HELD && Sinput.GetButton(controlname, (InputDeviceSlot)joystickIndeces[i])) return true;
				if (bAction == ButtonAction.UP && Sinput.GetButtonUp(controlname, (InputDeviceSlot)joystickIndeces[i])) return true;
				if (bAction == ButtonAction.REPEATING && Sinput.GetButtonDownRepeating(controlname, (InputDeviceSlot)joystickIndeces[i])) return true;
			}


			return false;
		}

		public float GetAxis(string controlName) { return AxisCheck(controlName, false); }
		public float GetAxisRaw(string controlName) { return AxisCheck(controlName, true); }
		private float AxisCheck(string controlname, bool getRawValue = false) {
			Sinput.SinputUpdate();

			float returnV = 0f;
			float v = 0f;
			for (int i = 0; i < joystickIndeces.Count; i++) {
				if (!getRawValue) {
					v = Sinput.GetAxis(controlname, (InputDeviceSlot)joystickIndeces[i]);
				} else {
					v = Sinput.GetAxisRaw(controlname, (InputDeviceSlot)joystickIndeces[i]);
				}
				if (Mathf.Abs(v) > Mathf.Abs(returnV)) returnV = v;
			}


			return returnV;
		}


		private struct QuickVRButtonBinding {
			//this should REALLY be deleted
			public string controlName;
			public int buttonIndex;
		}
		private List<QuickVRButtonBinding> quickVRButtons = new List<QuickVRButtonBinding>();
		void AddBinding(string name, int buttonIndex) {
			QuickVRButtonBinding b = new QuickVRButtonBinding();
			b.controlName = name;
			b.buttonIndex = buttonIndex;
			quickVRButtons.Add(b);
		}

	}



	public enum OpenVRButtons {
		MENU_L = 2,//inner face button (valve knuckles) / X button (oculus touch)
		MENU_R = 0,//inner face button (valve knuckles) / A button (oculus touch)
		OUTERFACE_L = 3,//knuckles only
		OUTERFACE_R = 1,//knuckles only
		TRACKPAD_L = 8,//valve trackpad == oculus thumbstick
		TRACKPAD_R = 9,
		TRACKPAD_TOUCH_L = 16,
		TRACKPAD_TOUCH_R = 17,
		TRIGGER_TOUCH_L = 14,
		TRIGGER_TOUCH_R = 15
	}
	public enum OpenVRAxis {
		TRACKPAD_HORIZONTAL_L = 1,//valve trackpad == oculus thumbstick
		TRACKPAD_VERTICAL_L = 2,
		TRACKPAD_HORIZONTAL_R = 3,
		TRACKPAD_VERTICAL_R = 4,
		TRIGGER_L = 9,
		TRIGGER_R = 10,
		GRIP_L = 11,//average grip (valve knuckles) - hand trigger (oculus touch)
		GRIP_R = 12,
		CAP_INDEX_L = 20,//no oculus touch equivelants for this or any axis after
		CAP_INDEX_R = 21,
		CAP_MIDDLE_L = 22,
		CAP_MIDDLE_R = 23,
		CAP_RING_L = 24,
		CAP_RING_R = 25,
		CAP_PINKY_L = 26,
		CAP_PINKY_R = 27
	}

	public enum OculusButtons {
		ONE = 0,
		TWO = 1,
		THREE = 2,
		FOUR = 3,
		ONE_TOUCH = 10,
		TWO_TOUCH = 11,
		THREE_TOUCH = 12,
		FOUR_TOUCH = 13,
		START = 7,
		STICK_PRIMARY = 8,
		STICK_PRIMARY_TOUCH = 16,
		STICK_SECONDARY = 9,
		STICK_SECONDARY_TOUCH = 17,
		THUMBREST_PRIMARY = 18,
		THUMBREST_SECONDARY = 19,
		INDEXTRIGGER_PRIMARY_TOUCH = 14,
		INDEXTRIGGER_SECONDARY_TOUCH = 15
	}

	public enum OculusAxis {
		STICK_PRIMARY_NEARTOUCH = 15,
		STICK_SECONDARY_NEARTOUCH = 16,
		THUMBREST_PRIMARY_NEARTOUCH = 15,//I know these are the same as the stick near touches
		THUMBREST_SECONDARY_NEARTOUCH = 16,//I know that is stupid, not my fault tho
		INDEXTRIGGER_PRIMARY_NEARTOUCH = 13,
		INDEXTRIGGER_PRIMARY = 9,
		INDEXTRIGGER_SECONDARY_NEARTOUCH = 14,
		INDEXTRIGGER_SECONDARY = 10,
		HANDTRIGGER_PRIMARY = 11,
		HANDTRIGGER_SECONDARY = 12,
		STICK_HORIZONTAL_PRIMARY = 1,
		STICK_VERTICAL_PRIMARY = 2,
		STICK_HORIZONTAL_SECONDARY = 4,
		STICK_VERTICAL_SECONDARY = 5,
		DPAD_VERTICAL = 6, //dpad is oculus remote only
		DPAD_HORIZONTAL = 5
	}
}