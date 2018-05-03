using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectilePart : Part {
	int polysInRange = 0;

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
			if (polysInRange > 0) {
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
		if (master && coll.tag == "Player") {
			// poly enter fire zone
			polysInRange++;
		}
	}

	void OnTriggerExit2D (Collider2D coll) {
		if (master && coll.tag == "Player") {
			// poly leave fire zone
			polysInRange--;
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
