using UnityEngine;
using System.Collections;

public class SegmentController : MonoBehaviour {
	public bool fake; // tell whether segment is being collected as the master or as a pure visual on another players screen
	public bool collected = false;
	public bool attracting = false;

	private Transform target;
	private float attractSpeed = 10f;

	void Start () {
		transform.parent = GameObject.Find("SegmentsContainer").transform;
		transform.position = new Vector3 (transform.position.x, transform.position.y, 5f);
	}

	public void StartTracking (Transform other, bool _fake)
	{
		attracting = true;
		fake = _fake;

		target = other;
		gameObject.layer = 12;
		GetComponent<BoxCollider2D> ().isTrigger = true;
	}

	void OnTriggerEnter2D (Collider2D coll) {
		if (attracting && !collected) {
			StartCollect ();
		}
	}

	void StartCollect () {
		// called if attracting on any players clients (not only master)
		collected = true;
		target.GetComponent<PolyController> ().HandleSegmentStartDestory (this.gameObject);
	}

	void Update ()
	{
		if (attracting && !collected && target != null) {
			gameObject.GetComponent<Rigidbody2D>().AddForce((target.position - gameObject.transform.position) * attractSpeed);
		}
	}
}
