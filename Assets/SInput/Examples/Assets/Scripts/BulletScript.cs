using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SinputSystems.Examples {
	public class BulletScript : MonoBehaviour {

		public Vector3 moveDir;
		public float moveSpeed;
		public float life = 1f;

		// Use this for initialization
		void Start() {

		}

		// Update is called once per frame
		void Update() {
			transform.position += moveDir * moveSpeed * Time.deltaTime;
			transform.LookAt(transform.position + moveDir);

			life -= Time.deltaTime;
			if (life < 0f) Destroy(gameObject);
		}
	}

}