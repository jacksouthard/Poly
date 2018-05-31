using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class SegmentsManager : NetworkBehaviour {
	public static SegmentsManager instance;

	public GameObject segmentPrefab;
	float spawnRadius;

	float burstSpawnFrequency = 0.05f;
	float burstSpawnSpeed = 3f;
	float burstSpread = 15f;
	float burstSpawnDst = 0.2f;

	public float segmentValue;
	public float netMass = 0f;
	public float maxMass;

	// net mass checking
	float netMassCheckFrequency = 90f;
	float massCheckTimer;

	float spawnTimer;
	float spawnInterval = 0.01f;

	void Awake () {
		instance = this;
	}

	void Start () {
		spawnTimer = spawnInterval;
		massCheckTimer = netMassCheckFrequency;
		spawnRadius = MapManager.instance.spawnRange;
	}
	
	void Update ()
	{
		if (isServer && Time.timeScale != 0) {
			if (spawnTimer <= 0f) {
				spawnTimer = spawnInterval;
				SpawnSegment ();
			} else {
				spawnTimer -= Time.deltaTime;
			}

			massCheckTimer -= Time.deltaTime;
			if (massCheckTimer <= 0f) {
				massCheckTimer = netMassCheckFrequency;
				RecalculateNetMass ();
			}
		}
	}
	void SpawnSegment () {
		if (netMass < maxMass) {
			Vector3 spawnPos = new Vector3 (Random.Range (-spawnRadius, spawnRadius), Random.Range (-spawnRadius, spawnRadius), 1f);
			Quaternion spawnRot = Quaternion.Euler (new Vector3 (0f, 0f, Random.Range (0f, 180f)));
			GameObject segment = Instantiate (segmentPrefab, spawnPos, spawnRot) as GameObject;
			netMass += segmentValue;
			NetworkServer.Spawn (segment);
		}
	}

	public void SpawnSegmentBurst (int count, Vector2 spawnPos, float spawnZ = -1) {
		if (spawnZ == -1) { // code for poly insta death
			StartCoroutine (SpawnBurst (count, spawnPos, spawnZ, true));
		} else {
			StartCoroutine(SpawnBurst (count, spawnPos, spawnZ, false));
		}
	}

	public void MassDestroyed (float destroyedMass) {
		netMass -= destroyedMass;
	}

	IEnumerator SpawnBurst(int count, Vector2 spawnPos, float spawnZ, bool randomDirections) {
		for (int i = 0; i < count; i++) {
			Quaternion spawnRot;
			if (randomDirections) {
				spawnRot = Quaternion.Euler (0f, 0f, Random.Range (0f, 360f));
			} else {
				spawnRot = Quaternion.Euler (0f, 0f, spawnZ + Random.Range (-burstSpread, burstSpread));
			}
			Vector3 spawnPos3d = new Vector3 (spawnPos.x, spawnPos.y, 0f);
			spawnPos3d += spawnRot * (Vector3.up * burstSpawnDst);
			Quaternion randomSpawnRot = Quaternion.Euler(new Vector3 (0f, 0f, Random.Range (0f, 180f)));

			GameObject segment = Instantiate (segmentPrefab, spawnPos3d, randomSpawnRot) as GameObject;
			segment.GetComponent<Rigidbody2D> ().velocity = spawnRot * (Vector2.up * burstSpawnSpeed);

			NetworkServer.Spawn (segment);

			yield return new WaitForSeconds (burstSpawnFrequency);
		}

	}

	void RecalculateNetMass () {
		float newNetMass = 0;
		PolyController[] allPolies = FindObjectsOfType<PolyController> ();
		foreach (var poly in allPolies) {
			newNetMass += poly.GetNetMass ();
		}
		SegmentController[] allSegments = FindObjectsOfType<SegmentController> ();
		newNetMass += allSegments.Length * segmentValue;
		print ("Old: " + netMass + " New: " + newNetMass);
		netMass = newNetMass;
	}
}
