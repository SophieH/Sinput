using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SinputSystems.Touch {
	public class SinputTouchManager {

		private static List<int> claimedTouchIDs = new List<int>();

		private static int lastTouchUpdate = -99;
		private static void TouchUpdate() {
			if (lastTouchUpdate == Time.frameCount) return;
			lastTouchUpdate = Time.frameCount;

			//clear out claimed touches that no longer exist
			bool removeID = false;
			for (int i = 0; i < claimedTouchIDs.Count; i++) {
				removeID = true;
				for (int k = 0; k < Input.touchCount; k++) {
					if (claimedTouchIDs[i] == Input.touches[k].fingerId) removeID = false;
				}
				if (removeID) {
					claimedTouchIDs.RemoveAt(i);
					i--;
				}
			}

		}

		public static bool IsClaimed(int ID) {
			TouchUpdate();

			for (int i = 0; i < claimedTouchIDs.Count; i++) {
				if (claimedTouchIDs[i] == ID) return true;
			}
			return false;
		}
		public static bool TouchExists(int ID) {
			TouchUpdate();

			for (int i = 0; i < Input.touchCount; i++) {
				if (Input.touches[i].fingerId == ID) return true;
			}
			return false;
		}

		public static void ClaimTouch(int ID) {
			TouchUpdate();

			claimedTouchIDs.Add(ID);
		}
		public static void ReleaseTouch(int ID) {
			TouchUpdate();

			for (int i=0; i<claimedTouchIDs.Count; i++) {
				if (claimedTouchIDs[i] == ID) {
					claimedTouchIDs.RemoveAt(i);
					i--;
				}
			}
		}
	}
}
