﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class PartsManager : NetworkBehaviour {
	public static PartsManager instance;

	public GameObject[] projectiles;
	public PartData[] partsData;
	public GameObject partPrefab;

	[Header("Detaching")]
	public float initialDetachDistance;
	public float detachForce;

	public Color[] playerColors = new Color[] {
		new Color(0.30f, 0.63f, 0.73f),
		new Color(0.85f, 0.63f, 0.28f),
		new Color(0.77f, 0.32f, 0.38f),
		new Color(0.59f, 0.45f, 0.82f),
		new Color(0.33f, 0.72f, 0.33f)
	};

	[Header("Spawning")]
	float spawnRadius;
	private float spawnTimer;
	private float spawnInterval = 0.5f; 
	private int partCount = 0;
	private int partCountMax = 20;

	void Awake () {
		instance = this;
	}

	void Start () {
		spawnTimer = spawnInterval;
		spawnRadius = MapManager.instance.spawnRange;
	}
	
	void Update ()
	{
		if (isServer) {
			if (spawnTimer <= 0f) {
				spawnTimer = spawnInterval;
				SpawnPart ();
			} else {
				spawnTimer -= Time.deltaTime;
			}
		}
	}
	void SpawnPart ()
	{
		if (partCount < partCountMax) {
			Vector3 spawnPos = new Vector3 (Random.Range (-spawnRadius, spawnRadius), Random.Range (-spawnRadius, spawnRadius), 1f);
			Quaternion spawnRot = Quaternion.Euler (new Vector3 (0f, 0f, Random.Range (0f, 180f)));
			GameObject part = Instantiate (partPrefab, spawnPos, spawnRot) as GameObject;
			part.GetComponent<PartController> ().Init(Random.Range(0, partsData.Length));
			partCount++;

			NetworkServer.Spawn (part);
		}
	}

	public void SpawnDetachedPart (int partID, Vector3 spawnPos, Quaternion spawnRot) {
		GameObject part = Instantiate (partPrefab, spawnPos, spawnRot) as GameObject;
		part.transform.position += part.transform.up * initialDetachDistance;
		part.GetComponent<Rigidbody2D> ().AddRelativeForce (Vector2.up * detachForce);
		part.GetComponent<PartController> ().Init(partID);

		NetworkServer.Spawn (part);
		partCount++;
	}

	public void SpawnProjectile (int projectileIndex, Vector3 spawnPos, Quaternion spawnRot, int playerNum, NetworkInstanceId playerNetID) {
		GameObject prefab = projectiles [projectileIndex];
		spawnPos += Vector3.forward * 1f; // shift back in layers

		GameObject newProjectile = Instantiate (prefab, spawnPos, spawnRot);
		Projectile projectileScript = newProjectile.GetComponent<Projectile> ();
		projectileScript.playerNetID = playerNetID;
		newProjectile.GetComponent<Rigidbody2D> ().velocity = newProjectile.transform.up * projectileScript.speed;

		NetworkServer.Spawn (newProjectile);
		Destroy (newProjectile, projectileScript.lifeTime);

		projectileScript.RelaySetColor (playerNum);
	}
		
	public void PartDestoryed ()
	{
		partCount--;
	}

	public PartData GetDataWithID (int id) {
		foreach (var data in partsData) {
			if (data.partID == id) {
				return data;
			}
		}
		print ("No part data found with ID: " + id);
		return partsData[0];
	}

	public PartData GetDataWithName (string name) {
		foreach (var part in partsData) {
			if (part.prefab.name == name) {
				return part;
			}
		}
		print ("No part found with name: " + name);
		return partsData[0];
	}

	public int GetIDFromName (string name) {
		foreach (var part in partsData) {
			if (part.prefab.name == name) {
				return part.partID;
			}
		}
		print ("No part found with name: " + name);
		return 0;
	}
}

[System.Serializable]
public class PartData {
	public int partID;
	public Sprite sprite;
	public GameObject prefab;

	public enum PartType {
		melee,
		ranged,
		booster,
		none
	}
	public PartType type;
}