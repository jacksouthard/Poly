using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part : MonoBehaviour {
	public bool master = false;
	public bool detaching = false;

	public float maxHealth;
	protected float health;

	protected PolyController pc;

	void Awake () {
		health = maxHealth;
		pc = GetComponentInParent<PolyController> ();
		if (GetComponentInParent<PolyController> ().master) {
			master = true;
		}
	}

	void OnCollisionEnter2D (Collision2D coll) {
		if (master) {
//			print ("Part Collision: " + coll.collider.gameObject + " GO: " + coll.gameObject);
			Damaging damaging = coll.collider.gameObject.GetComponentInParent<Damaging> ();
			if (damaging != null && !damaging.authorative) {
				TakeDamage (damaging.damage, true);
			}
		}
	}

	void OnTriggerEnter2D (Collider2D coll) {
		if (coll.tag == "Projectile") {
			Projectile projectile = coll.gameObject.GetComponentInParent<Projectile> ();
			if (!projectile.live || projectile.playerNetID == pc.netId) {
				return; // projectile is either nonexistant or already has hit something
			} else {
				projectile.Hit (); // only needs to be assigned locally as same projectile cannot really hit 2 different players
				if (master) {
					pc.RelayDestoryProjectile (projectile.gameObject);
					TakeDamage (coll.gameObject.GetComponentInParent<Damaging>().damage, false);
				}
			}
		}
	}

	public virtual void TakeDamage (float damage, bool melee) {
		health -= damage;
		if (health <= 0f) {
			pc.DestroyPartRequest (transform.parent.gameObject);
		}
	}
}
