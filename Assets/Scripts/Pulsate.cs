using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pulsate : MonoBehaviour {
	public float cycleTime;
	public float scaleChange;

	float timer = 0f;

	Transform[] children;
	float[] timeOffsets;

	void Start () {
		children = new Transform[transform.childCount];
		for (int i = 0; i < transform.childCount; i++) {
			children [i] = transform.GetChild (i);
		}
		timeOffsets = new float[transform.childCount];
		for (int i = 0; i < children.Length; i++) {
			timeOffsets [i] = Random.Range (0f, cycleTime);
		}
	}
	
	void Update () {
		timer += Time.deltaTime;
		for (int i = 0; i < children.Length; i++) {
			float newScale = scaleChange * Mathf.Sin (cycleTime * timer + timeOffsets [i]) + 1;
			children [i].localScale = new Vector3 (newScale, 1f, 1f);
		}
	}
}
