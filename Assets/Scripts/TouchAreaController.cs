using UnityEngine;
using System.Collections;

public class TouchAreaController : MonoBehaviour {
	
	private Vector2 initialPos;
	public Vector2 offset;
	private bool mouseDownInZone = false;
	private float touchRadius = 50f;

	// Update is called once per frame
	void Update ()
	{
		if (Input.GetMouseButton (0)) {
			Vector3 mousePos3D = GameObject.Find ("PlayerCamera(Clone)").GetComponent<Camera>().ScreenToWorldPoint (Input.mousePosition);
			Vector2 mousePos2D = new Vector2 (mousePos3D.x, mousePos3D.y);

			RaycastHit2D hit = Physics2D.Raycast (mousePos2D, Vector2.zero);

			if (hit.collider == gameObject.GetComponent<BoxCollider2D> ()) {
				mouseDownInZone = true;
			} 
		} else {
			mouseDownInZone = false;
		}

		if (mouseDownInZone) {
			offset = new Vector2(Input.mousePosition.x, Input.mousePosition.y) - initialPos;
			offset = Vector2.ClampMagnitude((offset / touchRadius), 1);
		} else {
			offset = Vector2.zero;
		}

//		print(touchOffset);
	}

	void OnMouseDown ()
	{
		initialPos = Input.mousePosition;
	}
}
