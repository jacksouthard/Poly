using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MapManager : NetworkBehaviour {
	public static MapManager instance;

	public Transform playerTransform; 

	Transform bordersContainer;
	Transform[] borders = new Transform[4];
	public int mapSize;
	public float spawnRange;
	float renderDistance = 20f;
	float spawnCheckRange = 4f; // distance away from others polys a poly tries to spawn

	[Header("AI")]
	public int targetPolyCount;
	public GameObject aiPrefab;

	[Header("Debug")]
	public int aiCount = 0;

	void Awake () {
		instance = this;

		spawnRange = (mapSize / 2f) - 5f; 
	}

	void Start () {
		SetupBorders ();

		if (isServer) {
			Populate ();
		}
	}

	void Populate () {
		for (int i = 0; i < targetPolyCount; i++) {
			SpawnAI ();
		}
	}

	public void AIDie () {
		aiCount--;
		while (NetworkManagerScript.instance.playerCount + aiCount < targetPolyCount) {
			SpawnAI ();
		}
	}


	void SpawnAI () {
		GameObject newAI = Instantiate (aiPrefab, GetSpawnPoint(), Quaternion.identity);
		NetworkServer.Spawn (newAI);

		aiCount++;
	}

	public Vector3 GetSpawnPoint () {
		int attemps = 0;
		Vector2 possibleSpawn = new Vector2 (Random.Range (-spawnRange, spawnRange), Random.Range (-spawnRange, spawnRange));
		while (!SpawnPosAvailable (possibleSpawn)) {
			possibleSpawn = new Vector2 (Random.Range (-spawnRange, spawnRange), Random.Range (-spawnRange, spawnRange));

			attemps++;
			if (attemps > 5f) {
				return new Vector3 (possibleSpawn.x, possibleSpawn.y, 0f);
			}
		}

		return new Vector3 (possibleSpawn.x, possibleSpawn.y, 0f);
	}

	public bool ShouldRender (Vector3 position) {
		if (!isClient) { // servers dont render stuff
			return false;
		}

		float dstFromPlayer = (playerTransform.position - position).magnitude;
		if (dstFromPlayer > renderDistance) {
			return false;
		} else {
			return true;
		}
	}

	bool SpawnPosAvailable (Vector2 spawn) {
		Collider2D[] collsInRange = Physics2D.OverlapCircleAll (spawn, spawnCheckRange);
		foreach (var coll in collsInRange) {
			if (coll.transform.root.tag == "Player") {
				return false;
			}
		}

		return true;
	}

	void SetupBorders () {
		bordersContainer = transform.Find ("Borders");
		for (int i = 0; i < bordersContainer.childCount; i++) {
			borders [i] = bordersContainer.GetChild (i);
		}

		UpdateBorders ();
	}

	void UpdateBorders () {
		// let index 0 and 1 be top and bottom and 2 and 3 be right and left
		borders[0].transform.position = Vector3.up * (mapSize / 2f);
		borders [0].transform.localScale = new Vector3 (mapSize + 1f, 1f, 1f);

		borders[1].transform.position = Vector3.down * (mapSize / 2f);
		borders [1].transform.localScale = new Vector3 (mapSize + 1f, 1f, 1f);

		borders[2].transform.position = Vector3.right * (mapSize / 2f);
		borders [2].transform.localScale = new Vector3 (1f, mapSize + 1f, 1f);

		borders[3].transform.position = Vector3.left * (mapSize / 2f);
		borders [3].transform.localScale = new Vector3 (1f, mapSize + 1f, 1f);
	}
}
