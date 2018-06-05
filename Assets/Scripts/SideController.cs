using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class SideController : NetworkBehaviour {
	PolyController pc;

	void Start () {
		pc = GetComponentInParent<PolyController> ();
	}

	void OnCollisionEnter2D (Collision2D coll)
	{
		if (pc.master) {
//			print ("Side Collision: " + coll.collider.gameObject + " GO: " + coll.gameObject);
			if (coll.gameObject.tag == "Attachable" && transform.childCount == 0) {
				PartController partController = coll.gameObject.GetComponent<PartController> ();
				if (!partController.collected) {
					partController.AttachQued ();
					pc.AttachPartRequest (coll.gameObject, gameObject);
				}
			}
		}
	}

	void OnTriggerEnter2D (Collider2D coll) {
		Damaging possibleDamage = coll.gameObject.GetComponentInParent<Damaging> ();
		if (possibleDamage != null && !possibleDamage.authorative) {
			// case where daming object is projectile
			if (coll.tag == "Projectile") {
				Projectile projectile = coll.gameObject.GetComponentInParent<Projectile> ();

				if (!projectile.live || projectile.playerNetID == pc.netId.Value) {
					return; // projectile is either nonexistant or already has hit something
				} else {
					projectile.Hit (); // only needs to be assigned locally as same projectile cannot really hit 2 different players
					if (pc.master) {
						pc.TakeDamage (possibleDamage.damage, transform, projectile.playerNetID);
						pc.RelayDestoryProjectile (projectile.gameObject);
					}
				}
			} else if (pc.master) {
				// case where daming object is not a projectile (like a spike)
				pc.TakeDamage (possibleDamage.damage, transform, possibleDamage.GetComponentInParent<PolyController>().netId.Value);
			}
		}
	}
}
