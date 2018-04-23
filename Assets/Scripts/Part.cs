using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part : MonoBehaviour {
	public bool master = false;
	public bool detaching = false;

	public float maxHealth;
	float health;

	void Start () {
		health = maxHealth;
		if (GetComponentInParent<PolyController> ().isLocalPlayer) {
			master = true;
//			Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D> ();
//			rb.isKinematic = true;
		}
	}

	void OnCollisionEnter2D (Collision2D coll) {
		if (master) {
			print ("Part Collision: " + coll.collider.gameObject + " GO: " + coll.gameObject);
			Damaging damaging = coll.collider.gameObject.GetComponentInParent<Damaging> ();
			if (damaging != null) {
				TakeDamage (damaging.damage);
			}
		}
	}

	void TakeDamage (float damage) {
		if (master) {
			health -= damage;
			if (health <= 0f) {
//			print ("Part Destoryed: " + gameObject);
				GetComponentInParent<PolyController> ().DestroyPartRequest (transform.parent.gameObject);
			}
		}
	}
}
