﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Projectile : NetworkBehaviour {
	[SyncVar]
	public NetworkInstanceId playerNetID;

	public bool live = true;
	public float speed;
	public float lifeTime;

	Color playerColor;

	public void Hit () {
		live = false;
		GetComponentInChildren<SpriteRenderer> ().enabled = false;

		// spawn explosion
		GameObject prefab = Resources.Load ("Explosion") as GameObject;
		Vector3 spawnPos = new Vector3 (transform.position.x, transform.position.y, -1f);
		GameObject explosion = Instantiate (prefab, spawnPos, Quaternion.identity);
		explosion.GetComponent<Explosion> ().Init (GetComponent<Damaging> ().damage, playerColor);
	}

	void OnTriggerEnter2D (Collider2D coll) { // projectile must check for hit on random objects like floating parts
		if (coll.tag == "Attachable") {
			Hit ();
		}
	}

	public void RelaySetColor (int colorIndex) {
		if (!isClient) {
			SetColor (colorIndex);
		}
		RpcSetColor (colorIndex);
	}
	[ClientRpc]
	void RpcSetColor (int colorIndex) {
		SetColor (colorIndex);
	}
		
	void SetColor (int colorIndex) {
		playerColor = PartsManager.instance.playerColors [colorIndex];
		foreach (var renderer in GetComponentsInChildren<SpriteRenderer>()) {
			renderer.color = playerColor;
		}
	}


}
