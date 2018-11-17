using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SinputSystems.Examples{
	public class MultiplayerSpawner : MonoBehaviour {

		public GameObject playerPrefab;

		private List<ShootyPlayer> players = new List<ShootyPlayer>();

		// Use this for initialization
		void Start () {
			
		}
		
		// Update is called once per frame
		void Update () {
			SinputSystems.InputDeviceSlot slot = Sinput.GetSlotPress("Join");

			if (slot != SinputSystems.InputDeviceSlot.any) {
				//a player has pressed join!

				//first we check if this player has already joined
				bool alreadyJoined = false;
				for (int i = 0; i < players.Count; i++) {
					if (players[i].playerSlot == slot) {
						alreadyJoined = true;
						//lets assume this player is trying to unjoin, and remove them :)
						Destroy(players[i].gameObject);
						players.RemoveAt(i);
						i--;
					}
				}

				if (!alreadyJoined) { 
					//this is a new player looking to join, so lets let them!
					GameObject newPlayer = (GameObject)GameObject.Instantiate(playerPrefab);
					newPlayer.transform.position = new Vector3(Random.Range(-4f, 4f), 3f, Random.Range(-4f, 4f));
					newPlayer.GetComponent<ShootyPlayer>().playerSlot = slot;
					players.Add(newPlayer.GetComponent<ShootyPlayer>());

					//lets prevent any new inputs from this slot for a few frames
					//This isn't necessary, but will prevent people accidentally pressing join twice quickly :)
					Sinput.ResetInputs(slot);
				}

			}
		}

	}
}
