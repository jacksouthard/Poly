using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectilePart : Part {
	bool shouldFire = false;

	public int projectileIndex;
	public float fireRate;
	float timeUntilFire = 0f;

	public Transform[] projectileSpawns;
	int spawnIndex = 0;

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
		Vector3 spawnPos = GetProjectileSpawnSpawn ();
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
			shouldFire = false;
		}
	}

	Vector3 GetProjectileSpawnSpawn () {
		if (projectileSpawns.Length == 1) {
			return projectileSpawns [0].position;
		} else {
			spawnIndex++;
			if (spawnIndex >= projectileSpawns.Length) {
				spawnIndex = 0;
			}
			return projectileSpawns [spawnIndex].position;
		}
	}
}
