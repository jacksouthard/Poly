using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part : MonoBehaviour {
	public bool master = false;
	public bool detaching = false;
	public bool meleeResistant;

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

	void OnCollisionEnter2D (Collision2D coll) { // part hit by melee weapon
		if (master && !meleeResistant) {
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
			if (projectile.live && projectile.playerNetID != pc.netId.Value) { // projectile must still be live and have been shot my a different player
				if (!projectile.idSet) {
					print ("Projectile player net id not set");
					return;
				}

				projectile.Hit (); // only needs to be assigned locally as same projectile cannot really hit 2 different players
				HitByProjectile();
				if (master) {
//					print ("Part " + pc.netId + " hit by " + projectile.playerNetID + " | " + projectile.idSet);
					pc.RelayDestoryProjectile (projectile.gameObject);
					TakeDamage (coll.gameObject.GetComponentInParent<Damaging>().damage, false);
				}
			}
		}
	}

	protected virtual void HitByProjectile () {} // mainly for visual effects (shields)

	public virtual void TakeDamage (float damage, bool melee) {
		health -= damage;
		if (health <= 0f) {
//			if (pc.isLocalPlayer) {
//				print ("Dest");
//			}
			pc.DestroyPartRequest (transform.parent.gameObject);
		}
	}
}
