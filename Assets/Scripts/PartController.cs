using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class PartController : NetworkBehaviour {

	private Transform target;
	enum State {Detached, Attached}; // Maybe add "Attracting"
	private State state;
	private float detachForce = 100f;
	public int cachedNetId;

	// Use this for initialization
	void Start () {
		state = State.Detached;

		transform.parent = GameObject.Find("PartsContainer").transform;
		GetComponent<Rigidbody2D>().angularVelocity = Random.Range (-45f, 45f); 
		GetComponent<Rigidbody2D>().velocity = new Vector2(Random.Range (-0.5f, 0.5f), Random.Range (-0.5f, 0.5f));

		uint id = GetComponent<NetworkIdentity>().netId.Value;
		cachedNetId = (int)id;
	}

	public bool isAttached () {
		return (state == State.Attached);
	}

	public void Attach(GameObject side) {
		state = State.Attached;
//		gameObject.GetComponent<Rigidbody2D>().isKinematic = true;
		var rb = gameObject.GetComponent<Rigidbody2D>();
		Destroy(rb);
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.transform.localRotation = Quaternion.identity;
		gameObject.transform.SetParent(side.transform, false);
	}

	public void Detach () {
		if (isAttached()) {
			state = State.Detached;
			Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
			rb.gravityScale = 0f;
			transform.SetParent(GameObject.Find("PartsManager").transform, true);
			rb.AddForce(transform.up * detachForce);
		}
	}
}
