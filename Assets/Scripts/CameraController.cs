using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {

	private float dampTime = 0f;
    private Vector3 velocity = Vector3.zero;
    public Transform target;

    void Start ()
	{

	}
    // Update is called once per frame
    void Update () 
    {
//    	print (target);
        if (target)
        {
        	Vector3 point = GetComponent<Camera>().WorldToViewportPoint(target.position);
            Vector3 delta = target.position - GetComponent<Camera>().ViewportToWorldPoint(new Vector3(0.5f, 0.5f, point.z)); //(new Vector3(0.5, 0.5, point.z));
            Vector3 destination = transform.position + delta;
            transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, dampTime);
		}     
	}
}
