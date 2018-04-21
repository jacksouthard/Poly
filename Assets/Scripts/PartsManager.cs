using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class PartsManager : NetworkBehaviour {

	public GameObject partPrefab;

	private float spawnTimer;
	private float spawnInterval = 1f;
	private int partCount = 0;
	private int partCountMax = 5;

	void Start () {
		spawnTimer = spawnInterval;
	}
	
	void Update ()
	{
		if (spawnTimer <= 0f) {
			spawnTimer = spawnInterval;
			SpawnPart ();
		} else {
			spawnTimer -= Time.deltaTime;
		}
	}
	void SpawnPart ()
	{
		if (isServer) {
			if (partCount < partCountMax) {
				var part = Instantiate (partPrefab, Vector3.zero, Quaternion.identity) as GameObject;
				partCount++;

				NetworkServer.Spawn (part);
			}
		}
	}
}






//using UnityEngine;
//using System.Collections;
//using UnityEngine.Networking;
//
//public class PartsManager : NetworkBehaviour {
//
//	public GameObject partPrefab;
////	private GameObject parts;
//
//	private float spawnTimer;
//	private float spawnInterval = 1f;
//	private int partCount = 0;
//	private int partCountMax = 30;
//	// Use this for initialization
//	void Start () {
//		spawnTimer = spawnInterval;
////		parts = GameObject.Find("Parts");
//	}
//	
//	// Update is called once per frame
//	void Update ()
//	{
//		if (isServer) {
//			if (spawnTimer <= 0f) {
//				spawnTimer = spawnInterval;
//				SpawnPart ();
//			} else {
//				spawnTimer -= Time.deltaTime;
//			}
//		}
//	}
//	void SpawnPart ()
//	{	
//		if (isServer) {
//			if (partCount < partCountMax) {
//				var part = Instantiate (partPrefab, Vector3.zero, Quaternion.identity) as GameObject;
////			part.transform.parent = this.gameObject.transform;
//				partCount++;
//
//				NetworkServer.Spawn (part);
//			}
//		}
//	}
////	[Command]
////	public void CmdCollectPart (GameObject part)
////	{
////		partCount--;
////		NetworkServer.Destroy(part);
////	}
//}
