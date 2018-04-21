using UnityEngine;
using System.Collections;

public class SideController : MonoBehaviour {

	void OnCollisionEnter2D (Collision2D coll)
	{
		if (coll.gameObject.tag == "Attachable") {
			transform.parent.parent.gameObject.GetComponent<PolyController>().OnPartHitSide (coll.gameObject, gameObject);
		}
	}

	public bool HasPartAttached () {
		return ( transform.childCount > 0 );
	}
}
