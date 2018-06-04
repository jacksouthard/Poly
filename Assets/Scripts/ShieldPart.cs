using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldPart : Part {
	public float shieldHealth;
	float maxShieldHealth;
	public float regenTime;
	float regenTimer;
	bool shieldBroken;

	GameObject shieldGO;
	LineRenderer shieldLine;
	PolygonCollider2D shieldColl;

	Vector3[] originalPositions;
	float curAnimateTime = 0f;
	float scrambleDistance = 0.1f;
//	float scrambleSpeed;

	void Start () {
		maxShieldHealth = shieldHealth;
		shieldGO = transform.Find ("Shield").gameObject;
		shieldLine = shieldGO.GetComponent<LineRenderer> ();
		shieldLine.material.color = pc.GetPlayerColor ();
		shieldColl = GetComponent<PolygonCollider2D> ();
		originalPositions = new Vector3[shieldLine.positionCount];
		shieldLine.GetPositions (originalPositions);
		ShieldBreak ();
	}

	void Update () {
		if (curAnimateTime > 0f) {
			curAnimateTime -= Time.deltaTime;
			if (curAnimateTime <= 0f) {
				ResetShieldVisuals ();
			} else if (MapManager.instance.ShouldRender (transform.position)) {
				Scramble ();
			}
		}

		if (master) {
			if (shieldBroken) {
				regenTimer -= Time.deltaTime;
				if (regenTimer <= 0f) {
					ShieldDeploy();
					pc.RelaySheildStateChange (int.Parse (transform.parent.name), true); // deploy
				}
			}
		}
	}

	public override void TakeDamage (float damage, bool melee) {
//		print ("Sheild damaged. Dmg: " + damage + " Melee: " + melee + " LP: " + pc.isLocalPlayer); 
		if (shieldBroken || melee) {
			health -= damage;
			if (health <= 0f) {
				pc.DestroyPartRequest (transform.parent.gameObject);
			}
		} else {
			shieldHealth -= damage;
//			print (shieldHealth);
			if (shieldHealth <= 0f) {
				ShieldBreak();
				pc.RelaySheildStateChange (int.Parse (transform.parent.name), false); // break
			}
		}
	}

	void ShieldBreak () {
		shieldBroken = true;
		regenTimer = regenTime;
		shieldGO.SetActive (false);
		shieldColl.enabled = false;
	}

	void ShieldDeploy () {
		shieldHealth = maxShieldHealth;
		shieldBroken = false;
		shieldGO.SetActive (true);
		shieldColl.enabled = true;
	}

	public void AlterShieldState (bool deploying) {
		if (!master) {
			print ("Alter State");
			if (deploying) {
				ShieldDeploy ();
			} else {
				ShieldBreak ();
			}
		}
	}

	void Scramble () {
		Vector3[] scrambledPositions = new Vector3[originalPositions.Length];
		for (int i = 0; i < originalPositions.Length; i++) {
			Vector3 scrambleVector = Random.insideUnitCircle * scrambleDistance;
			scrambledPositions [i] = originalPositions [i] + scrambleVector;
		}
		shieldLine.SetPositions (scrambledPositions);
	}

	void ResetShieldVisuals () {
		shieldLine.SetPositions (originalPositions);
	}

	void OnTriggerStay2D (Collider2D coll) {
		curAnimateTime = 0.5f;
	}
}
