using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Damaging : MonoBehaviour {
	public float damage;
	public bool authorative;

	public float hitCooldown;
	float cooldownTimer;
	bool onCooldown = false;

	PolyController pc;
	bool master = false;

	void Start () {
		if (authorative) {
			pc = GetComponentInParent<PolyController> ();
			if (pc.master) {
				master = true;
			}
		}
	}

	void Update () {
		if (authorative && master && onCooldown) {
			cooldownTimer -= Time.deltaTime;
			if (cooldownTimer <= 0f) {
				onCooldown = false;
			}
		}
	}

	void StartCooldown () {
		onCooldown = true;
		cooldownTimer = hitCooldown;
	}

	void OnCollisionStay2D (Collision2D coll) {
		if (!master || !authorative || onCooldown) {
			return;
		}
		// only happens if this damaging object is authoritive over its damage assignment
		int colliderLayer = coll.collider.gameObject.layer;
		GameObject rootGO = coll.gameObject;
		if (rootGO.name == "Part(Clone)") {
			return;
		}

		if (colliderLayer == 9 && rootGO.name != "PartsContainer") { // part layer
			// check to make sure part isnt melee resistant
			if (coll.collider.transform.GetComponent<Part> ().meleeResistant) {
				return;
			}
		
			int sideIndex = int.Parse (coll.collider.transform.parent.name);
			pc.HandleAssignPartDamage (rootGO, sideIndex, int.Parse (transform.parent.name), damage);
			StartCooldown ();
		} else if (colliderLayer == 8) { // side layer
			int sideIndex = int.Parse (coll.collider.name);
			pc.HandleAssignSideDamage (rootGO, sideIndex, int.Parse (transform.parent.name), damage, false);
			StartCooldown ();
		} else if (colliderLayer == 11) { // weakspot layer
			pc.HandleAssignSideDamage (rootGO, 0, int.Parse (transform.parent.name), 0, true);
			StartCooldown ();
		}
	}
}
