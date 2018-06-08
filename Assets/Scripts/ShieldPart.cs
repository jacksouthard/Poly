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
	bool shouldAnimate = false; // for optimization

	// changing size when deloying and retracting
	bool changingSize = false;
	float changeSizeTime = 0.2f;
	float changeSizeTimer;

	void Start () {
		maxShieldHealth = shieldHealth;
		shieldGO = transform.Find ("Shield").gameObject;
		shieldLine = shieldGO.GetComponent<LineRenderer> ();
		shieldLine.material.color = pc.GetPlayerColor ();
		shieldColl = GetComponent<PolygonCollider2D> ();
		originalPositions = new Vector3[shieldLine.positionCount];
		shieldLine.GetPositions (originalPositions);
		ShieldBreak (false);
	}

	void Update () {
		if (curAnimateTime > 0f && !changingSize) {
			curAnimateTime -= Time.deltaTime;
			if (curAnimateTime <= 0f) {
				ResetShieldVisuals ();
			} else if (shouldAnimate) {
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
				ShieldBreak(true);
				pc.RelaySheildStateChange (int.Parse (transform.parent.name), false); // break
			}
		}
	}

	void ShieldBreak (bool animate) {
		shieldBroken = true;
		regenTimer = regenTime;
		shieldColl.enabled = false;
		if (animate) {
			StartCoroutine ("ChangeSize", false);
		} else {
			shieldGO.SetActive (false);
		}
	}

	void ShieldDeploy () {
		shieldHealth = maxShieldHealth;
		shieldBroken = false;
		shieldColl.enabled = true;
		shouldAnimate = GameManager.instance.ShouldRender (transform.position);
		StartCoroutine ("ChangeSize", true);
	}

	public void AlterShieldState (bool deploying) {
		if (!master) {
			print ("Alter State");
			if (deploying) {
				ShieldDeploy ();
			} else {
				ShieldBreak (true);
			}
		}
	}

	IEnumerator ChangeSize (bool deploying) {
		changeSizeTimer = changeSizeTime;
		shieldGO.SetActive (true);
		changingSize = true;
		if (shouldAnimate) {
			while (changeSizeTimer > 0f) {
				changeSizeTimer -= Time.deltaTime;
				float sizeRatio = changeSizeTimer / changeSizeTime;
				if (deploying) {
					sizeRatio = 1f - sizeRatio;
				}
				SetShieldSize (sizeRatio);

				yield return new WaitForEndOfFrame ();
			}
		}

		changingSize = false;
		shieldGO.SetActive (deploying);
	}

	void SetShieldSize (float sizeRatio) {
		Vector3[] newPositions = new Vector3[originalPositions.Length];
		for (int i = 0; i < originalPositions.Length; i++) {
			newPositions [i] = Vector3.Lerp (Vector3.zero, originalPositions [i], sizeRatio);
		}
		shieldLine.SetPositions (newPositions);
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

	protected override void HitByProjectile () {
		shouldAnimate = GameManager.instance.ShouldRender (transform.position);
		curAnimateTime = 0.5f;
	}
}
