using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part : MonoBehaviour {
	public bool master = false;
	public bool detaching = false;

	public float maxHealth;
	float health;

	protected PolyController pc;

	void Awake () {
		health = maxHealth;
		pc = GetComponentInParent<PolyController> ();
		if (GetComponentInParent<PolyController> ().isLocalPlayer) {
			master = true;
		}
	}

	void OnCollisionEnter2D (Collision2D coll) {
		if (master) {
//			print ("Part Collision: " + coll.collider.gameObject + " GO: " + coll.gameObject);
			Damaging damaging = coll.collider.gameObject.GetComponentInParent<Damaging> ();
			if (damaging != null) {
				TakeDamage (damaging.damage);
			}
		}
	}

	void OnTriggerEnter2D (Collider2D coll) {
		if (coll.tag == "Projectile") {
			Projectile projectile = coll.gameObject.GetComponentInParent<Projectile> ();
			if (!projectile.live) {
				return; // projectile is either nonexistant or already has hit something
			} else {
				projectile.Hit (); // only needs to be assigned locally as same projectile cannot really hit 2 different players
				if (master) {
					pc.RelayDestoryProjectile (projectile.gameObject);
					TakeDamage (coll.gameObject.GetComponentInParent<Damaging>().damage);
				}
			}
		}
	}

	void TakeDamage (float damage) {
		if (master) {
			health -= damage;
			if (health <= 0f) {
//			print ("Part Destoryed: " + gameObject);
				pc.DestroyPartRequest (transform.parent.gameObject);
			}
		}
	}
}
