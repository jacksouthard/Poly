﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class PartController : NetworkBehaviour {
	public bool inited = false;
	public bool collected = false;

	// despawning
	float maxLifetime = 180f;
	float minLifetime = 30f;
	float curLifetime;

	[SyncVar(hook="UpdateData")]
	public int id;

	void Start () {
		transform.parent = GameObject.Find("PartsContainer").transform;
		if (!inited) {
			UpdateData (id);
		}

		if (isServer) {
			curLifetime = Random.Range (minLifetime, maxLifetime);
		}
	}

	void Update () {
		if (isServer) {
			curLifetime -= Time.deltaTime;
			if (curLifetime <= 0f) {
				// despawn
				PartsManager.instance.PartDestoryed ();
				Destroy (gameObject);
			}
		}
	}

	public void Init (int partID) {
		id = partID;
		UpdateData (id);
		inited = true;
	}

	public void AttachQued () {
		collected = true;
	}

	void UpdateData (int newID) {
		PartData newData = PartsManager.instance.GetDataWithID (newID);
		GetComponent<SpriteRenderer> ().sprite = newData.sprite; 
		inited = true;
	}
		
//	void OnCollisionEnter2D (Collision2D coll) {
//		PolyController controller = coll.gameObject.GetComponentInParent<PolyController> ();
//		if (controller != null) {
//			controller.CmdSegmentStartDestory (this.gameObject);
//		}
//	}
}
