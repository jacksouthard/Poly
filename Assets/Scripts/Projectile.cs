using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Projectile : NetworkBehaviour {
	public bool live = true;
	public float speed;
	public float lifeTime;

	public void Hit () {
		live = false;
		GetComponentInChildren<SpriteRenderer> ().enabled = false;
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
		Color newColor = PartsManager.instance.playerColors [colorIndex];
		foreach (var renderer in GetComponentsInChildren<SpriteRenderer>()) {
			renderer.color = newColor;
		}
	}


}
