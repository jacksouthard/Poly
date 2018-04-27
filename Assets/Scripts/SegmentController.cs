using UnityEngine;
using System.Collections;

public class SegmentController : MonoBehaviour {
	public bool fake; // tell whether segment is being collected as the master or as a pure visual on another players screen
	public bool collected = false;

	private Transform target;
	private float attractSpeed = 5f;

	float collectTimer = 0.5f;

	void Start () {
		transform.parent = GameObject.Find("SegmentsContainer").transform;
		transform.position = new Vector3 (transform.position.x, transform.position.y, 5f);
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
					target.GetComponent<PolyController> ().HandleSegmentStartDestory (this.gameObject);
				}
				collected = true;
			}

			gameObject.GetComponent<Rigidbody2D>().AddForce((target.position - gameObject.transform.position) * attractSpeed);
		}
	}
}
