using UnityEngine;
using System.Collections;

public class SideController : MonoBehaviour {
	PolyController pc;

	void Start () {
		pc = GetComponentInParent<PolyController> ();
	}

	void OnCollisionEnter2D (Collision2D coll)
	{
		if (pc.isLocalPlayer) {
			print ("Side Collision: " + coll.collider.gameObject + " GO: " + coll.gameObject);
			if (coll.gameObject.tag == "Attachable" && transform.childCount == 0) {
				PartController partController = coll.gameObject.GetComponent<PartController> ();
				if (!partController.collected) {
					partController.AttachQued ();
					pc.AttachPartRequest (coll.gameObject, gameObject);
				}
			} else {
				Damaging possibleDamage = coll.collider.gameObject.GetComponentInParent<Damaging> ();
				if (possibleDamage != null) {
					pc.TakeDamage (possibleDamage.damage);
				}
			}
		}
	}
}
