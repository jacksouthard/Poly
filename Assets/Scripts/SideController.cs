using UnityEngine;
using System.Collections;

public class SideController : MonoBehaviour {
	PolyController pc;

	void Start () {
		pc = GetComponentInParent<PolyController> ();
	}

	void OnCollisionEnter2D (Collision2D coll)
	{
		if (coll.gameObject.tag == "Attachable" && transform.childCount == 0) {
			PartController partController = coll.gameObject.GetComponent<PartController> ();
			if (!partController.collected) {
//				print ("Queing part:" + partController.id);
				partController.AttachQued ();
				pc.AttachPartRequest (coll.gameObject, gameObject);
			}
		}
	}
}
