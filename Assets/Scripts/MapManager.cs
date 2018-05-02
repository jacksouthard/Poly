using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MapManager : NetworkBehaviour {
	public static MapManager instance;

	Transform bordersContainer;
	Transform[] borders = new Transform[4];
	public int mapSize;
	float spawnRange;

	[Header("AI")]
	public int targetPolyCount;
	public GameObject aiPrefab;

	[Header("Debug")]
	public int aiCount = 0;

	void Awake () {
		instance = this;
	}

	void Start () {
		SetupBorders ();

		if (isServer) {
			spawnRange = (mapSize / 2f) - 5f;
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
		Vector3 randomPos = new Vector3 (Random.Range (-spawnRange, spawnRange), Random.Range (-spawnRange, spawnRange), 0f);
		GameObject newAI = Instantiate (aiPrefab, randomPos, Quaternion.identity);
		NetworkServer.Spawn (newAI);

		aiCount++;
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
