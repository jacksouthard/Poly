using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
	float spawnTimer;
	float spawnInterval = 0.5f; 
	int partCount = 0;
	int partCountMax = 20;

	float partSpawnZ = 5;
	float projectileSpawnZ = 5;

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
			part.GetComponent<PartController> ().Init(GetBalancedPartSpawnID());
			partCount++;

			NetworkServer.Spawn (part);
		}
	}

	int GetBalancedPartSpawnID () {
		// get random part type
		PartData.PartType partType;
		int random = Random.Range (0, 100);
		if (random < 30) {
			partType = PartData.PartType.melee;
		} else if (random < 60) {
			partType = PartData.PartType.ranged;
		} else {
			partType = PartData.PartType.booster;
		}

		// get random part of type
		List<int> partIndexesOfType = new List<int>();
		for (int i = 0; i < partsData.Length; i++) {
			if (partsData[i].type == partType) {
				partIndexesOfType.Add (i);
			}
		}

		if (partIndexesOfType.Count != 0) {
			return partIndexesOfType [Random.Range (0, partIndexesOfType.Count)];
		} else {
			print ("Could not find part of type: " + partType);
			return 0;
		}
	}

	public void SpawnDetachedPart (int partID, Vector2 spawnPos, Quaternion spawnRot) {
		Vector3 spawnPos3D = new Vector3 (spawnPos.x, spawnPos.y, partSpawnZ);
		GameObject part = Instantiate (partPrefab, spawnPos3D, spawnRot) as GameObject;
		part.transform.position += part.transform.up * initialDetachDistance;
		part.GetComponent<Rigidbody2D> ().AddRelativeForce (Vector2.up * detachForce);
		part.GetComponent<PartController> ().Init(partID);

		NetworkServer.Spawn (part);
		partCount++;
	}

	public void SpawnProjectile (int projectileIndex, Vector2 spawnPos, Quaternion spawnRot, int playerNum, NetworkInstanceId playerNetID) {
		GameObject prefab = projectiles [projectileIndex];
		Vector3 spawnPos3D = new Vector3 (spawnPos.x, spawnPos.y, projectileSpawnZ);

		GameObject newProjectile = Instantiate (prefab, spawnPos3D, spawnRot);
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