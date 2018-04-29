using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {
	[Header("Zooming")]
	public float zoomedInSize;
	public float zoomedOutSize;
	float startingZoom;
	float targetZoom;
	bool zooming;
	public float zoomOutTime;
	public float zoomInTime;
	float zoomTime;
	float zoomTimer;

	[Header("Smooth Follow")]
	private float dampTime = 0.25f;
    private Vector3 velocity = Vector3.zero;
    public Transform target;
	Camera cam;
   
	void Start () {
		cam = GetComponent<Camera> ();
		cam.orthographicSize = zoomedInSize;
		targetZoom = zoomedInSize;
	}

	void LateUpdate () {
        if (target) {
        	Vector3 point = GetComponent<Camera>().WorldToViewportPoint(target.position);
            Vector3 delta = target.position - GetComponent<Camera>().ViewportToWorldPoint(new Vector3(0.5f, 0.5f, point.z)); //(new Vector3(0.5, 0.5, point.z));
            Vector3 destination = transform.position + delta;
            transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, dampTime);
		}  
		if (zooming) {
			UpdateZoom ();
		}
	}

	public void ZoomOut () {
		targetZoom = zoomedOutSize;
		zoomTimer = 0f;
		zoomTime = zoomOutTime;
		startingZoom = cam.orthographicSize;
		zooming = true;
	}

	public void ZoomIn () {
		targetZoom = zoomedInSize;
		zoomTimer = 0f;
		zoomTime = zoomInTime;
		startingZoom = cam.orthographicSize;
		zooming = true;
	}

	void UpdateZoom () {
		zoomTimer += Time.deltaTime;
		if (zoomTimer >= zoomTime) {
			// zoom complete
			cam.orthographicSize = targetZoom;
			zooming = false;
		}

		// if not complete, update the zoom
		float timeRatio = zoomTimer / zoomTime;
		cam.orthographicSize = Mathf.Lerp (startingZoom, targetZoom, timeRatio);
	}
}
