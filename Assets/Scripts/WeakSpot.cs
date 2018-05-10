using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeakSpot : MonoBehaviour {
	PolyController pc;

	void Start () {
		pc = GetComponentInParent<PolyController> ();
	}

	void OnCollisionEnter2D (Collision2D coll)
	{
		if (pc.master) {
			Damaging possibleDamage = coll.collider.gameObject.GetComponentInParent<Damaging> ();
			if (possibleDamage != null) {
				if (coll.gameObject.tag == "Projectile") {
					Projectile projectile = coll.gameObject.GetComponentInParent<Projectile> ();
					if (projectile.playerNetID != pc.netId) {
						pc.HitInWeakSpot (projectile.playerNetID);
					}
				} else {
					pc.HitInWeakSpot (possibleDamage.GetComponentInParent<PolyController>().netId);
				}
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
				if (pc.master) {
					pc.HitInWeakSpot (projectile.playerNetID);
					pc.RelayDestoryProjectile (projectile.gameObject);
				}
			}
		}
	}
	
}
