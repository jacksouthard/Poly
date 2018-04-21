﻿using UnityEngine;
using System.Collections;

public class SegmentController : MonoBehaviour {
	public bool fake; // tell whether segment is being collected as the master or as a pure visual on another players screen
	public bool collected = false;

	private Transform target;
	private float attractSpeed = 5f;

	float collectTimer = 0.5f;

	// Use this for initialization
	void Start () {
		transform.parent = GameObject.Find("SegmentsContainer").transform;
		GetComponent<Rigidbody2D>().angularVelocity = Random.Range (-45f, 45f); 
		GetComponent<Rigidbody2D>().velocity = new Vector2(Random.Range (-0.5f, 0.5f), Random.Range (-0.5f, 0.5f));
	}

	public void StartTracking (Transform other, bool _fake)
	{
		fake = _fake;

		target = other;
		Destroy (GetComponent<BoxCollider2D> ());
	}

	void Update ()
	{
		if (target != null && !collected) {
			collectTimer -= Time.deltaTime;
			if (collectTimer <= 0f) {
				// collect
				if (!fake) {
					target.GetComponent<PolyController> ().CmdSegmentStartDestory (this.gameObject);
				}
				collected = true;
			}

			gameObject.GetComponent<Rigidbody2D>().AddForce((target.position - gameObject.transform.position) * attractSpeed);
		}
	}
}
