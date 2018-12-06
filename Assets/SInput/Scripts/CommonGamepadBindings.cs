using System.Collections.Generic;
using UnityEngine;

namespace SinputSystems {
	public static class CommonGamepadMappings {


		static List<CommonMapping> commonMappings;
		static MappingSlots[] mappingSlots;

		public static void ReloadCommonMaps() {
			//called when gamepads are plugged in or removed, also when Sinput is first called

			//Debug.Log("Loading common mapping");

			OSFamily thisOS;
			switch (Application.platform) {
				case RuntimePlatform.OSXEditor: thisOS = OSFamily.MacOSX; break;
				case RuntimePlatform.OSXPlayer: thisOS = OSFamily.MacOSX; break;
				case RuntimePlatform.WindowsEditor: thisOS = OSFamily.Windows; break;
				case RuntimePlatform.WindowsPlayer: thisOS = OSFamily.Windows; break;
				case RuntimePlatform.LinuxEditor: thisOS = OSFamily.Linux; break;
				case RuntimePlatform.LinuxPlayer: thisOS = OSFamily.Linux; break;
				case RuntimePlatform.Android: thisOS = OSFamily.Android; break;
				case RuntimePlatform.IPhonePlayer: thisOS = OSFamily.IOS; break;
				case RuntimePlatform.PS4: thisOS = OSFamily.PS4; break;
				case RuntimePlatform.PSP2: thisOS = OSFamily.PSVita; break;
				case RuntimePlatform.XboxOne: thisOS = OSFamily.XboxOne; break;
				case RuntimePlatform.Switch: thisOS = OSFamily.Switch; break;
				default: thisOS = OSFamily.Other; break;
			}

			CommonMapping[] commonMappingAssets = Resources.LoadAll<CommonMapping>("");
			commonMappings = new List<CommonMapping>();
			string[] gamepads = Sinput.gamepads;
			int defaultMappingIndex = -1;
			for (int i = 0; i < commonMappingAssets.Length; i++) {
				//Debug.Log("HELLOOOOO");
				//if ((commonMappingAssets[i]).isXRdevice) Debug.Log("XR deviiiiiice");

				if ((commonMappingAssets[i]).os == thisOS) {
					bool gamepadConnected = false;
					bool partialMatch = false;
					for (int k = 0; k < (commonMappingAssets[i]).names.Count; k++) {
						for (int g = 0; g < gamepads.Length; g++) {
							if ((commonMappingAssets[i]).names[k].ToUpper() == gamepads[g]) gamepadConnected = true;
						}
					}

					for (int k = 0; k < (commonMappingAssets[i]).partialNames.Count; k++) {
						for (int g = 0; g < gamepads.Length; g++) {
							if (gamepads[g].Contains((commonMappingAssets[i]).partialNames[k].ToUpper())) partialMatch = true;
						}
					}

					if (gamepadConnected) commonMappings.Add(commonMappingAssets[i]);
					if (partialMatch && !gamepadConnected) commonMappings.Add(commonMappingAssets[i]);
					if (!partialMatch && !gamepadConnected && (commonMappingAssets[i]).isDefault) commonMappings.Add((CommonMapping)commonMappingAssets[i]);

					if ((commonMappingAssets[i]).isDefault) defaultMappingIndex = commonMappings.Count - 1;
				}
			}



			//for each common mapping, find which gamepad slots it applies to
			//inputs built from common mappings will only check slots which match
			mappingSlots = new MappingSlots[commonMappings.Count];
			for (int i = 0; i < mappingSlots.Length; i++) {
				mappingSlots[i].slots = new List<int>();
			}
			//string[] gamepads = Sinput.GetGamepads();
			for (int i = 0; i < commonMappings.Count; i++) {
				for (int k = 0; k < commonMappings[i].names.Count; k++) {
					for (int g = 0; g < gamepads.Length; g++) {
						if (gamepads[g] == commonMappings[i].names[k].ToUpper()) {
							mappingSlots[i].slots.Add(g);
						}
					}
				}
			}

			//find out if there are any connected gamepads that dont match anything in mappingSlots
			//then find 
			for (int g = 0; g < gamepads.Length; g++) {
				bool mappingMatch = false;
				for (int b = 0; b < mappingSlots.Length; b++) {
					for (int s = 0; s < mappingSlots[b].slots.Count; s++) {
						if (mappingSlots[b].slots[s] == g) mappingMatch = true;
					}
				}
				if (!mappingMatch) {
					//check for partial name matches with this gamepad slot
					for (int i = 0; i < commonMappings.Count; i++) {
						for (int k = 0; k < commonMappings[i].partialNames.Count; k++) {
							if (!mappingMatch && gamepads[g].Contains(commonMappings[i].partialNames[k])) {
								mappingMatch = true;
								mappingSlots[i].slots.Add(g);
							}
						}
					}
					if (!mappingMatch && defaultMappingIndex != -1) {
						//apply default common mapping to this slot
						mappingSlots[defaultMappingIndex].slots.Add(g);
					}
				}
			}

		}
		struct MappingSlots {
			public List<int> slots;
		}

		/*public static List<DeviceInput> GetApplicableMaps(CommonXRInputs t, string[] connectedGamepads) {
			Debug.LogError("DO THISSSSS");
			return new List<DeviceInput>();
		}*/
		public static List<DeviceInput> GetApplicableMaps(CommonGamepadInputs commonInputType, CommonXRInputs commonXRInputType, string[] connectedGamepads) {
			//builds input mapping of type t for all known connected gamepads


			List<DeviceInput> applicableInputs = new List<DeviceInput>();

			bool addthis = false;
			for (int i = 0; i < commonMappings.Count; i++) {

				//if (commonMappings[i].isXRdevice) Debug.Log("Found XR device");

				//add any applicable button mappings
				for (int k = 0; k < commonMappings[i].buttons.Count; k++) {
					addthis = false;
					if (!commonMappings[i].isXRdevice && commonMappings[i].buttons[k].buttonType != CommonGamepadInputs.NOBUTTON) {
						if (commonMappings[i].buttons[k].buttonType == commonInputType) addthis = true;
					}
					if (commonMappings[i].isXRdevice && commonMappings[i].buttons[k].vrButtonType != CommonXRInputs.NOBUTTON) {
						//Debug.Log("Adding XR button from common mapping");
						if (commonMappings[i].buttons[k].vrButtonType == commonXRInputType) addthis = true;
					}
					if (addthis) {
						//add this button input
						DeviceInput newInput = new DeviceInput(InputDeviceType.GamepadButton);
						newInput.gamepadButtonNumber = commonMappings[i].buttons[k].buttonNumber;
						newInput.commonMappingType = commonInputType;
						newInput.displayName = commonMappings[i].buttons[k].displayName;

						newInput.allowedSlots = mappingSlots[i].slots.ToArray();

						applicableInputs.Add(newInput);
					}
				}
				//add any applicable axis bingings
				for (int k = 0; k < commonMappings[i].axis.Count; k++) {
					addthis = false;
					if (!commonMappings[i].isXRdevice && commonMappings[i].axis[k].buttonType != CommonGamepadInputs.NOBUTTON) {
						if (commonMappings[i].axis[k].buttonType == commonInputType) addthis = true;
					}
					if (commonMappings[i].isXRdevice && commonMappings[i].axis[k].vrButtonType != CommonXRInputs.NOBUTTON) {
						//Debug.Log("Adding XR Axis from common mapping");
						if (commonMappings[i].axis[k].vrButtonType == commonXRInputType) addthis = true;
					}
					if (addthis) {
						//add this axis input
						DeviceInput newInput = new DeviceInput(InputDeviceType.GamepadAxis);
						newInput.gamepadAxisNumber = commonMappings[i].axis[k].axisNumber;
						newInput.commonMappingType = commonInputType;
						newInput.displayName = commonMappings[i].axis[k].displayName;
						newInput.invertAxis = commonMappings[i].axis[k].invert;
						newInput.clampAxis = commonMappings[i].axis[k].clamp;
						newInput.axisButtoncompareVal = commonMappings[i].axis[k].compareVal;
						newInput.defaultAxisValue = commonMappings[i].axis[k].defaultVal;

						newInput.allowedSlots = mappingSlots[i].slots.ToArray();

						if (commonMappings[i].axis[k].rescaleAxis) {
							newInput.rescaleAxis = true;
							newInput.rescaleAxisMin = commonMappings[i].axis[k].rescaleAxisMin;
							newInput.rescaleAxisMax = commonMappings[i].axis[k].rescaleAxisMax;
						}

						applicableInputs.Add(newInput);
					}
				}

			}



			return applicableInputs;
		}

	}
}