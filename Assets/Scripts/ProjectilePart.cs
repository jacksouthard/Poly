using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectilePart : Part {
	int polysInRange = 0;

	public int projectileIndex;
	public float fireRate;
	public bool multishot;
	float timeUntilFire = 0f;

	public List<Transform> projectileSpawns;
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
		pc.RelayPartFire (int.Parse(transform.parent.name));
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
		
	public List<Transform> GetProjectileSpawns () {
		List<Transform> returnedProjectileSpawns = new List<Transform> ();
		if (projectileSpawns.Count == 1) {
			returnedProjectileSpawns.Add (projectileSpawns [0]);
			return returnedProjectileSpawns;
		} else if (!multishot) {
			spawnIndex++;
			if (spawnIndex >= projectileSpawns.Count) {
				spawnIndex = 0;
			}
			returnedProjectileSpawns.Add (projectileSpawns [spawnIndex]);
			return returnedProjectileSpawns;
		} else {
			// multishot
			return projectileSpawns;
		}
	}
}
