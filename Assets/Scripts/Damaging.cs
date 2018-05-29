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

	void OnTriggerStay2D (Collider2D coll) {
		if (!master || !authorative || onCooldown) {
			return;
		}

		// only happens if this damaging object is authoritive over its damage assignment
		if (coll.transform.gameObject.layer == 9 && coll.transform.root.name != "PartsContainer") { // part layer
			int sideIndex = int.Parse (coll.transform.parent.name);
			pc.HandleAssignPartDamage (coll.transform.root.gameObject, sideIndex, damage);
			StartCooldown ();
		} else if (coll.transform.gameObject.layer == 8) { // side layer
			int sideIndex = int.Parse (coll.name);
			pc.HandleAssignSideDamage (coll.transform.root.gameObject, sideIndex, damage, false);
			StartCooldown ();
		} else if (coll.transform.gameObject.layer == 11) { // weakspot layer
			pc.HandleAssignSideDamage (coll.transform.root.gameObject, 0, 0, true);
			StartCooldown ();
		}
	}
}
