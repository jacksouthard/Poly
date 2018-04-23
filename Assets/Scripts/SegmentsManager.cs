using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class SegmentsManager : NetworkBehaviour {
	public static SegmentsManager instance;

	public GameObject segmentPrefab;
	public float spawnRadius;

	private float spawnTimer;
	private float spawnInterval = 1f;
	private int segmentCount = 0;
	private int segmentCountMax = 10;

	void Awake () {
		instance = this;
	}

	void Start () {
		spawnTimer = spawnInterval;
	}
	
	void Update ()
	{
		if (spawnTimer <= 0f) {
			spawnTimer = spawnInterval;
			SpawnSegment ();
		} else {
			spawnTimer -= Time.deltaTime;
		}
	}
	void SpawnSegment ()
	{
		if (segmentCount < segmentCountMax) {
			Vector3 spawnPos = new Vector3 (Random.Range (-spawnRadius, spawnRadius), Random.Range (-spawnRadius, spawnRadius), 1f);
			Quaternion spawnRot = Quaternion.Euler (new Vector3 (0f, 0f, Random.Range (0f, 180f)));
			GameObject segment = Instantiate (segmentPrefab, spawnPos, spawnRot) as GameObject;
			segmentCount++;

			NetworkServer.Spawn (segment);
		}
	}

	public void SegmentDestoryed ()
	{
		segmentCount--;
	}
}
