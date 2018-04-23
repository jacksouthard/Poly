using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class PartController : NetworkBehaviour {
	public bool inited = false;
	public bool collected = false;

	[SyncVar(hook="UpdateData")]
	public int id;

	void Start () {
		transform.parent = GameObject.Find("PartsContainer").transform;
		if (!inited) {
			UpdateData (id);
		}
	}

	void Update () {
		if (Input.GetKeyDown (KeyCode.O)) {
			UpdateData (id);
		}
	}

	public void Init (int partID) {
		id = partID;
		UpdateData (id);
		inited = true;
	}

	public void AttachQued () {
		collected = true;
	}

	void UpdateData (int newID) {
		PartData newData = PartsManager.instance.GetDataWithID (newID);
		GetComponent<SpriteRenderer> ().sprite = newData.sprite; 
	}
		
//	void OnCollisionEnter2D (Collision2D coll) {
//		PolyController controller = coll.gameObject.GetComponentInParent<PolyController> ();
//		if (controller != null) {
//			controller.CmdSegmentStartDestory (this.gameObject);
//		}
//	}
}
