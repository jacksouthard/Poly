using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour {
	bool inited = false;

	float normalStartSize = 0.3f;
	float normalStartSpeed = 3f;
	float normalBurstSize = 10f;
	float normalLifetime = 0.5f;

	float minSize = 0.25f;
	float maxSize = 3f;
	float damgeToSizeRatio = 0.01f;

	float timer;

	ParticleSystem ps;

	void Update () {
		if (inited) {
			timer -= Time.deltaTime;
			if (timer <= 0f) {
				Destroy (gameObject);
			}
		}
	}

	public void Init (float damage, Color color) {
		float size = Mathf.Clamp (damage * damgeToSizeRatio, minSize, maxSize);

		ps = GetComponent<ParticleSystem> ();
		ParticleSystem.MainModule main = ps.main;

		main.startColor = color;
		main.startSpeed = normalStartSpeed * size;
		main.startSize = normalStartSize * size;
		main.startLifetime = normalLifetime * size;
		ParticleSystem.Burst burst = ps.emission.GetBurst (0);
		burst.count = normalBurstSize * size;
		ps.emission.SetBurst (0, burst);

		ps.Play ();

		timer = main.duration;
		inited = true;
	}
}
