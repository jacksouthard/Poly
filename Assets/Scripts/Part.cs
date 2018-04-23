using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part : MonoBehaviour {
	public bool master = false;
	public bool detaching = false;

	public float maxHealth;
	float health;

	void Start () {
		health = maxHealth;
		if (GetComponentInParent<PolyController> ().isLocalPlayer) {
			master = true;
		}
	}

	void OnCollisionEnter2D (Collision2D coll) {
		Damaging damaging = coll.gameObject.GetComponentInParent<Damaging> ();
		if (damaging != null) {
			TakeDamage (damaging.damage);
		}
	}

	void TakeDamage (float damage) {
		health -= damage;
		if (health <= 0f) {
			print ("Part Destoryed: " + gameObject);
		}
	}
}
