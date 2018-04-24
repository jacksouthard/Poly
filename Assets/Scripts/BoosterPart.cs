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
	PolyController pc;

	void Start () {
		pc = GetComponentInParent<PolyController> ();
		rb = transform.GetComponentInParent<Rigidbody2D> ();
		ps = GetComponentInChildren<ParticleSystem> ();
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
		ps.Play ();
		active = true;
	}

	public void Deactivate () {
		pc.speedBoost -= boost;
		ps.Stop ();
		active = false;
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
