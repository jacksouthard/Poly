using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectilePart : Part {
	bool shouldFire = true;

	public int projectileIndex;
	public float spawnDistance;
	public float fireRate;
	float timeUntilFire = 0f;

	void Update () {
		if (master) {
			if (timeUntilFire > 0f) {
				timeUntilFire -= Time.deltaTime;
			}
			if (shouldFire) {
				if (timeUntilFire <= 0f) {
					// fire
					timeUntilFire = fireRate;
					Fire ();
				}
			}
		}
	}

	void Fire () {
		Vector3 spawnPos = transform.position + transform.up * spawnDistance;
		Quaternion spawnRot = transform.rotation;
		pc.RelayProjectileSpawn (projectileIndex, spawnPos, spawnRot);
	}
	
	void OnTriggerEnter2D (Collider2D coll) {
		if (master && coll.GetComponentInParent<PolyController> () != null) {
			// poly enter fire zone
			shouldFire = true;
		}
	}

	void OnTriggerExit2D (Collider2D coll) {
		if (master && coll.GetComponentInParent<PolyController> () != null) {
			// poly leave fire zone
//			shouldFire = false;
		}
	}
}
