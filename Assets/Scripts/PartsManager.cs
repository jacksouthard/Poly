﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class PartsManager : NetworkBehaviour {
	public static PartsManager instance;

	public PartData[] partsData;
	public GameObject partPrefab;

	[Header("Detaching")]
	public float initialDetachDistance;
	public float detachForce;

	[Header("Spawning")]
	public float spawnRadius;
	private float spawnTimer;
	private float spawnInterval = 1f;
	private int partCount = 0;
	private int partCountMax = 5;

	void Awake () {
		instance = this;
	}

	void Start () {
		spawnTimer = spawnInterval;
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
}