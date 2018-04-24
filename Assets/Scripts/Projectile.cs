using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {
	public bool live = true;
	public float speed;
	public float lifeTime;

	public void Hit () {
		live = false;
		GetComponentInChildren<SpriteRenderer> ().enabled = false;
	}

	void OnTriggerEnter2D (Collider2D coll) { // projectile must check for hit on random objects like floating parts
		if (coll.tag == "Attachable") {
			Hit ();
		}
	}
}
