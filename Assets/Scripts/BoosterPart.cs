using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoosterPart : Part {
	public float boost;
	public float angleRange;
	float minVelocity = 1f;
	bool active;

	ParticleSystem ps;
	Rigidbody2D rb;

	void Start () {
		rb = transform.GetComponentInParent<Rigidbody2D> ();
		ps = GetComponentInChildren<ParticleSystem> ();
		ParticleSystem.MainModule ma = ps.main;
		ma.startColor = GetComponentInParent<PolyController> ().GetPlayerColor ();
	}
	
	void Update () {
		if (active && !ShouldBeActive ()) {
			Deactivate ();
		} else if (!active && ShouldBeActive ()) {
			Activate ();
		}
	}

	void Activate () {
		pc.speedBoost += boost;
		active = true;

		if (GameManager.instance.ShouldRender (transform.position)) {
			ps.Play ();
		}
	}

	public void Deactivate () {
		if (active) {
			pc.speedBoost -= boost;
			active = false;

			if (ps.isPlaying) {
				ps.Stop ();
			}
		}
	}

	bool ShouldBeActive () {
		if (rb.velocity.magnitude < minVelocity) {
			return false;
		}
		float playerVelocityAngle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg + 180f;
		float partEulerAngle = transform.eulerAngles.z;
		float angleDiff = Mathf.DeltaAngle (playerVelocityAngle, partEulerAngle) + 90f;
		return (Mathf.Abs(angleDiff) <= angleRange);
	}
}
