using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class PolyController : NetworkBehaviour {
	public bool ai = false;
	AIController aiCon;
	PlayerController playerCon;
	public bool master = false;

	[SyncVar]
	public int attackPartsScore;

	// movement
	const float speed = 20f; // 1000f
	const float rotationSpeed = 15f; // 300f
	public float speedBoost = 1f; // speed modifier from active boosters. Normally 1
	const float minSpeedMultiplier = 0.6f;
	public float sizeSpeedMultiplier;

	// sides
	const float damageToSidesRatio = 0.01f;
	const float startingSides = 2.4f;
	const float sidesCountMin = 2.2f;
	const int sidesCountMax = 12;

	// side prefabs
	public GameObject[] sidesGOArray;
	public GameObject sidePrefab;
	public GameObject sidesContainer;

	// for optimizing frequent GetComponents
	Rigidbody2D rb;
	CircleCollider2D attractZone;
	MeshFilter mf;
	MeshRenderer mr;
	PolygonCollider2D weakZoneCollider;

	// visuals
	bool rendered = true;
	float renderDistance = 20f;
	public Material fillMaterial;

	// collection
	float segmentValue = 0.2f;
	float collectionCooldownAfterDamage = 2f;
	float collectionCooldown;
	bool hasCooldown = false;

	// misc
	public bool alive = true; // only matters locally
	float knockBackMultiplier = 3f;

	// STARTING ---------------------------------------------------------------------------------------

	void Awake () {

		SetupRendering();

	}

	void Start() {
		aiCon = GetComponent<AIController> ();
		if (aiCon != null && isServer) {
			ai = true;
		}
		playerCon = GetComponent<PlayerController> ();
		if (isLocalPlayer || ai) {
			master = true;
		}
		if (isLocalPlayer && !ai) {
			transform.position = MapManager.instance.GetSpawnPoint ();
		}

		// set up references
		rb = GetComponent<Rigidbody2D>();
		attractZone = GetComponent<CircleCollider2D> ();

		if (master) {
			if (isLocalPlayer) {
				CmdChangeSidesCount (startingSides);
				CmdChangePlayerNumber (Random.Range (0, 5));
			} else if (ai) {
				ChangeSidesCount (startingSides);
				ChangePlayerNumber (Random.Range (0, 5));
			}
		} else {
			SetSidesCount (sidesCount);
			ExpressPartData (partData);
		}
	}

	// NETWORK SYNCING ---------------------------------------------------------------------------

	// SidesCount network syncing
	[SyncVar(hook="SetSidesCount")]
	public float sidesCount;

	[SyncVar(hook="SetPlayerNumber")]
	private int playerNumber;

	[Command]
	public void CmdChangeSidesCount (float newValue) {
		ChangeSidesCount (newValue);
	}
	public void ChangeSidesCount (float newValue) {
		if (newValue < sidesCountMin) {
			DetachAllParts ();

			if (ai) { // b/c AI dont respawn
				MapManager.instance.AIDie();
				Destroy (gameObject, 2f);
			}
			partData = "------------------------";
			if (!isClient) {
				ExpressPartData (partData); // only needs to happen on server, not host
			}

			PlayerDie ();
			RpcPlayerDie ();
			return;
		}

		// if poly isnt dying...
		sidesCount = Mathf.Clamp (newValue, sidesCountMin, sidesCountMax);

		if (!isClient) {
			UpdateRendering (); // for server and not host
		}
	}

	public void TakeDamage (float damage, Transform side) {
		float damageInSides = damage * damageToSidesRatio;
		if (ai) {
			ChangeSidesCount (sidesCount - damageInSides);
		} else if (isLocalPlayer) {
			CmdChangeSidesCount (sidesCount - damageInSides);
		}

		StartCollectionCooldown ();
		int segmentsCount = Mathf.CeilToInt((damageInSides / segmentValue) / 2f); // the value of segments
		if (ai) {
			RelayBurstSpawn (segmentsCount, new Vector2 (side.position.x, side.position.y), side.rotation.eulerAngles.z);
		} else if (isLocalPlayer) {
			CmdRelayBurstSpawn (segmentsCount, new Vector2 (side.position.x, side.position.y), side.rotation.eulerAngles.z);
		}

		if (master) {
			// assign knockback
			rb.AddForce (-side.up * damage * knockBackMultiplier);
		}
 	}

	public void HitInWeakSpot () {
		int segmentsCount = Mathf.CeilToInt((sidesCount / segmentValue) / 2f); // the value of segments
		 
		if (ai) {
			RpcSpawnDeathExplosion ();
			ChangeSidesCount (sidesCountMin - 0.1f);
			RelayBurstSpawn (segmentsCount, new Vector2 (transform.position.x, transform.position.y), -1);
		} else if (isLocalPlayer) {
			CmdSpawnDeathExplosion ();
			CmdChangeSidesCount (sidesCountMin - 0.01f);
			CmdRelayBurstSpawn (segmentsCount, new Vector2 (transform.position.x, transform.position.y), -1);
		}
	}

	[Command]
	void CmdSpawnDeathExplosion () {
		RpcSpawnDeathExplosion ();
		if (!isClient) {
			SpawnDeathExplosion ();
		}
	}

	[ClientRpc]
	void RpcSpawnDeathExplosion () {
		SpawnDeathExplosion ();
	}

	void SpawnDeathExplosion () {
		// spawn explosion
		GameObject prefab = Resources.Load ("Explosion") as GameObject;
		Vector3 spawnPos = new Vector3 (transform.position.x, transform.position.y, -1f);
		GameObject explosion = Instantiate (prefab, spawnPos, Quaternion.identity);
		explosion.GetComponent<Explosion> ().Init (500f, GetPlayerColor()); // 500f for max size explosion
	}

	[Command]
	void CmdRelayBurstSpawn (int count, Vector2 spawnPos, float spawnZ) {
		RelayBurstSpawn (count, spawnPos, spawnZ);
	}
	public void RelayBurstSpawn (int count, Vector2 spawnPos, float spawnZ) {
		SegmentsManager.instance.SpawnSegmentBurst(count, spawnPos, spawnZ);
	}

	void SetSidesCount (float newValue)
	{
		if (newValue < sidesCountMin) { // for case where sides == 0 on start
			newValue = sidesCountMin;
		}
		sidesCount = newValue;

		if (master || rendered) {
			UpdateRendering ();
		}

		if (attractZone != null && !hasCooldown) {
			attractZone.radius = radius + 0.75f;
		}

		if (isLocalPlayer && playerCon != null) {
			playerCon.UpdatedPolySides (newValue);
		}
	}

	// Tell server version of object to change player number/color
	[Command]
	void CmdChangePlayerNumber(int newValue) {
		ChangePlayerNumber (newValue);
	}
	public void ChangePlayerNumber(int newValue) {
		SetPlayerNumber(newValue);
	}

	// recieves server synced value and updates object locally
	void SetPlayerNumber (int newValue) {
		if (playerNumber == newValue) {
			return;
		}
		playerNumber = newValue;
		UpdateRendering();
	}
		


	// UPDATE CHECKS ------------------------------------------------------------------------------------------------------------
	void Update ()
	{
		if (master) {
			// collection cooldown
			if (hasCooldown) {
				collectionCooldown -= Time.deltaTime;
				if (collectionCooldown <= 0f) {
					EndCollectionCooldown ();
				}
			}
		} else {
			// if random other poly in players game
			float dstToPlayer = DistanceToPlayer();
			if (!rendered && dstToPlayer < renderDistance) { // just came into render distance
				UpdateRendering();
				rendered = true;
			} else if (rendered && dstToPlayer > renderDistance) { // just left render distance
				rendered = false;
			}
		}
	}

	float DistanceToPlayer () {
		Vector3 playerPos = MapManager.instance.playerTransform.position;
		return (playerPos - transform.position).magnitude;
	}

	// MOVEMENT AND ROTATION ------------------------------------------------------------------------------------

	public void Move (Vector2 input)
	{
		// add force to poly
		rb.AddForce (input * speed * speedBoost * sizeSpeedMultiplier);
	}

	public void Rotate (float input)
	{
		rb.AddTorque(input * rotationSpeed);
	}

	// DEATH AND RESPAWNING ------------------------------------------------------------------------------------------------------------

	[ClientRpc]
	void RpcPlayerDie () {
		PlayerDie ();
	}

	void PlayerDie () {
		alive = false;

		if (isLocalPlayer) {
			playerCon.PolyDied ();
		}
		// disable visuals
		SetPolyActive (false);
	}

	[Command]
	public void CmdResetPlayer () {
		ResetPlayer ();
	}
	public void ResetPlayer () {
		SetSidesCount (startingSides);

		if (!isClient) {
			ResetPlayerLocal ();
		}
		RpcResetPlayer ();
	}

	[ClientRpc]
	void RpcResetPlayer () {
		// local player (sometimes client) has authority over its own movement
		ResetPlayerLocal();
	}

	void ResetPlayerLocal () {
		if (master) {
			transform.position = MapManager.instance.GetSpawnPoint();
			GetComponent<Rigidbody2D> ().velocity = Vector2.zero;
			GetComponent<Rigidbody2D> ().angularVelocity = 0f;

			if (hasCooldown) {
				EndCollectionCooldown ();
			}
		}

		alive = true;

		// renable poly
		SetPolyActive (true);

		speedBoost = 1f;
	}

	void SetPolyActive (bool active) {
		foreach (var side in sidesGOArray) {
			side.GetComponent<Collider2D> ().enabled = active;
			side.GetComponent<SpriteRenderer> ().enabled = active;
		}
		transform.Find ("WeakSpot").GetComponent<Collider2D> ().enabled = active;
		attractZone.enabled = active;
		mr.enabled = active;
	}

	// SEGMENTS AND COLLECTION ----------------------------------------------------------------------------------------

	// Handle a collectable segment entering collect zone around poly
	void OnTriggerEnter2D (Collider2D other) {
		if (other.CompareTag("Collectable") && master) {
			other.gameObject.GetComponent<SegmentController> ().StartTracking (gameObject.transform, false);
		}
	}

	public void HandleSegmentStartDestory (GameObject segment) {
		if (isLocalPlayer) {
			CmdSegmentStartDestory (segment);
		} else if (ai) {
			SegmentStartDestory (segment);
		}
	}
	[Command]
	public void CmdSegmentStartDestory (GameObject segment) {
		SegmentStartDestory (segment);
	}
	public void SegmentStartDestory (GameObject segment) {
		Destroy (segment);
		SegmentsManager.instance.SegmentDestoryed ();
		Collect(segmentValue);
	}

	void Collect (float sidesIncrement) {
		sidesCount += sidesIncrement;
		if (sidesCount > sidesCountMax) {
			sidesCount = sidesCountMax;
		}

		if (!isClient) {
			UpdateRendering (); // only has to happen on server
		}
	}

	void StartCollectionCooldown () {
		hasCooldown = true;
		collectionCooldown = collectionCooldownAfterDamage;
		attractZone.radius = 0f;
	}

	void EndCollectionCooldown () {
		hasCooldown = false;
		attractZone.radius = radius + 0.75f;
	}

	void UpdateSizeSpeedMultiplier () {
		float sizeRange = sidesCountMax - sidesCountMin;
		float sizeRatio = (sidesCount - sidesCountMin) / sizeRange;
		sizeSpeedMultiplier = minSpeedMultiplier * (2 - sizeRatio);
	}
		
	// PARTS --------------------------------------------------------------------------------------------------------------------------------

	[SyncVar(hook="ExpressPartData")]
	string partData = "------------------------"; // each set of 00's represents the part data of one side

	public void AttachPartRequest (GameObject part, GameObject side) {
		if (isLocalPlayer) {
			CmdPartStartDestroy (part.GetComponent<NetworkIdentity> ().netId, int.Parse (side.name));
		} else if (ai) {
			PartStartDestroy (part.GetComponent<NetworkIdentity> ().netId, int.Parse (side.name));
		}
	}

	[Command]
	void CmdPartStartDestroy (NetworkInstanceId partNetID, int sideIndex) {
		PartStartDestroy (partNetID, sideIndex);
	}
	void PartStartDestroy (NetworkInstanceId partNetID, int sideIndex) {
		GameObject part = NetworkServer.FindLocalObject (partNetID);
		AlterPartInData (part.GetComponent<PartController> ().id, sideIndex);
		Destroy (part);
		PartsManager.instance.PartDestoryed ();
	}

	void DestroyPart (int sideIndex) {
		DestoryPart (sideIndex);
	}
	void DestoryPart (int sideIndex) {
		AlterPartInData (-1, sideIndex); // -1 is code for --
	}

	public void DestroyPartRequest (GameObject side) {
		if (isLocalPlayer) {
			DestroyPart (int.Parse (side.name));
		} else if (ai) {
			DestoryPart (int.Parse (side.name));
		}
	}

	[Command]
	void CmdRelaySpawnDetatchedPart (int partID, Vector3 spawnPos, Quaternion spawnRot) {
		RelaySpawnDetatchedPart (partID, spawnPos, spawnRot);
	}
	void RelaySpawnDetatchedPart (int partID, Vector3 spawnPos, Quaternion spawnRot) {
		PartsManager.instance.SpawnDetachedPart (partID, spawnPos, spawnRot);
	}

	void DetachAllParts () {
		for (int i = 0; i < sidesGOArray.Length; i++) {
			if (sidesGOArray[i].activeInHierarchy && sidesGOArray[i].transform.childCount > 0) {
				DetachPart (i);
			}
		}
	}

	void DetachPart (int sideIndex) {
		// spawn detached part
		GameObject sideGO = sidesGOArray[sideIndex];
		sideGO.GetComponentInChildren<Part> ().detaching = true;
		int detachedID = PartsManager.instance.GetIDFromName (sideGO.transform.GetChild (0).name);
		if (isLocalPlayer) {
			CmdRelaySpawnDetatchedPart (detachedID, sideGO.transform.position, sideGO.transform.rotation);
		} else if (ai) {
			RelaySpawnDetatchedPart (detachedID, sideGO.transform.position, sideGO.transform.rotation);
		}

		// destory part on poly
		if (isServer) {
			DestroyPart (sideIndex);
			RelayManualBackupExpressPartData ();
		}
	}

	void AlterPartInData (int partID, int sideIndex) {
		string IDString = "";

		if (partID != -1) { // -1 code for replace with -- (no part)
			if (partID < 10) {
				IDString = "0" + partID.ToString ();
			} else {
				IDString = partID.ToString ();
			}
		} else {
			IDString = "--";
		}
		int partSetIndex = sideIndex * 2;
		string before = partData.Substring(0, partSetIndex);
		string after = partData.Substring (partSetIndex + 2);
		string newData = before + IDString + after;
		partData = newData;

		if (!isClient) {
			ExpressPartData (partData); // not for host, only server
		}
	}

	void ExpressPartData (string newPartData) {
		int[] sidePartIDs = new int[sidesCountMax];
		for (int i = 0; i < sidePartIDs.Length; i++) {
			string strPartID = newPartData.Substring (i * 2, 2);
			if (strPartID == "--") {
				sidePartIDs [i] = -1; // code for no part
			} else {
				sidePartIDs [i] = int.Parse (strPartID);
			}
		}
//		print ("Expressing: " + newPartData);
		for (int i = 0; i < sidesGOArray.Length; i++) {
			if (sidePartIDs [i] != -1) {
				if (sidesGOArray [i].transform.childCount == 0) {
					// spawnpart on empty side
					PartData data = PartsManager.instance.GetDataWithID (sidePartIDs [i]);
					GameObject newPart = Instantiate (data.prefab, sidesGOArray [i].transform);
					newPart.name = data.prefab.name;
					newPart.transform.localPosition = Vector3.zero;
					newPart.transform.localRotation = Quaternion.identity; 

					if (isServer && (data.type == PartData.PartType.melee || data.type == PartData.PartType.ranged)) {
						attackPartsScore++;
					}
					if (ai) {
						aiCon.UpdatePartTypes (data.type, i);
					}
				}
			} else {
				for (int c = 0; c < sidesGOArray[i].transform.childCount; c++) {
					// destory part on now empty side
					GameObject part = sidesGOArray [i].transform.GetChild (c).gameObject;
					BoosterPart possibleBooster = part.GetComponent<BoosterPart> ();
					if (possibleBooster != null) {
						possibleBooster.Deactivate ();
					}

					if (isServer && (PartsManager.instance.GetDataWithName(part.name).type == PartData.PartType.melee || PartsManager.instance.GetDataWithName(part.name).type == PartData.PartType.ranged)) {
						attackPartsScore--;
					}
					if (ai) {
						aiCon.UpdatePartTypes (PartData.PartType.none, i);
					}

					Destroy (part);
				}
			}
		}
	}

	void RelayManualBackupExpressPartData () {
		RpcManualBackupExpressPartData ();
	}

	[ClientRpc]
	void RpcManualBackupExpressPartData () {
		ExpressPartData (partData);
	}

	public void RelayDestoryProjectile (GameObject projectile) {
		if (isLocalPlayer) {
			CmdDestoryProjectile (projectile.GetComponent<NetworkIdentity> ().netId);
		} else if (ai) {
			DestoryProjectile (projectile.GetComponent<NetworkIdentity> ().netId);
		}
	}

	[Command]
	void CmdDestoryProjectile (NetworkInstanceId netID) {
		DestoryProjectile (netID);
	}
	void DestoryProjectile (NetworkInstanceId netID) {
		Destroy (NetworkServer.FindLocalObject (netID));
	} 

	public void RelayPartFire (int partIndex) {
		if (isLocalPlayer) {
			CmdRelayPartFire (partIndex, transform.position, transform.eulerAngles.z);
		} else if (ai) {
			ExecutePartFire (partIndex, transform.position, transform.eulerAngles.z);
		}
	}
	[Command]
	void CmdRelayPartFire (int partIndex, Vector3 playerPos, float playerRotZ) {
		ExecutePartFire (partIndex, playerPos, playerRotZ);
	}
	void ExecutePartFire (int partIndex, Vector3 playerPos, float playerRotZ) { // happens only on server
		ProjectilePart projectilePart = sidesGOArray [partIndex].GetComponentInChildren<ProjectilePart> ();
		int projectileIndex = projectilePart.projectileIndex;

//		Vector3 posDiff = playerPos - transform.position;
//		float rotDiff = playerRotZ - transform.eulerAngles.z;

		transform.position = playerPos;
		transform.rotation = Quaternion.Euler (new Vector3 (0f, 0f, playerRotZ));

		List<Transform> projectileSpawns = projectilePart.GetProjectileSpawns ();
		foreach (Transform spawn in projectileSpawns) {
			Vector3 spawnPos = spawn.position; // positions on server version
			Quaternion spawnRot = spawn.rotation;

			PartsManager.instance.SpawnProjectile (projectileIndex, spawnPos, spawnRot, playerNumber, netId);
		}

		if (!isClient) { // if deticated server
			AnimatePartFire(partIndex);
		}
		RpcRelayAnimatePartFire (partIndex);
	}
	[ClientRpc]
	void RpcRelayAnimatePartFire (int partIndex) {
		AnimatePartFire (partIndex);
	}
	void AnimatePartFire (int partIndex) {
		Animator anim = sidesGOArray [partIndex].GetComponentInChildren<Animator> ();
		if (anim != null) {
			anim.SetTrigger ("Fire");
		}
	}

	public Color GetPlayerColor () {
		return PartsManager.instance.playerColors [playerNumber];
	}

	// RENDERING --------------------------------------------------------------------------------------------------------------

	float[] angles;
	float radius;

	void SetupRendering ()
	{
		mf = gameObject.AddComponent (typeof(MeshFilter)) as MeshFilter;

		mr = gameObject.AddComponent (typeof(MeshRenderer)) as MeshRenderer;
		mr.material = fillMaterial;

		weakZoneCollider = transform.Find ("WeakSpot").GetComponent<PolygonCollider2D>();
		SetupSides();
	}

	// Setup sides
	void SetupSides() {

		// setup pool of sides in the sidesContainer
		sidesGOArray = new GameObject[sidesCountMax];

		for (var i = 0; i < sidesCountMax; i++) {
			// Create game object
			var newSide = Instantiate(sidePrefab, Vector3.zero, Quaternion.identity) as GameObject;
			newSide.transform.parent = sidesContainer.transform;
			newSide.SetActive(false);
			newSide.name = i.ToString();
			sidesGOArray[i] = newSide;
		}

	}
		
	// Update mesh and polygon collider, and sides geometry
	void UpdateRendering ()
	{
//		print ("Updating Rendering for " + gameObject.name);
		UpdateSizeSpeedMultiplier ();

		float[] angles = CalculateAngles (sidesCount);
		int anglesCount = angles.Length;
		int vertexCount = anglesCount + 1;  //includes center
		//for every angle there is own vertex; along the unit circle

		// assume that difference between first and second angles is otherAngle
		radius = PolyRadiusFromSidesCount (angles [1] - angles [0]);

		// Setup temporary arrays
		var vertices = new Vector3[vertexCount]; 
		var uv = new Vector2[vertexCount];

		// Create a vertex for center of polygon
		var center = Vector3.zero;
		var centerIndex = 0;
		vertices [centerIndex] = center; 
		uv [centerIndex] = new Vector2 (0, 0);

		// For each angle, create a vertex and matching UV texture mapping information
		// V1-Vn are the outside vertices in clockwise order, V1 is top at 0 degrees
		for (var i = 0; i < anglesCount; i++) {
			var angle = angles [i];

			// Vertex
			var x = Mathf.Cos (angle * Mathf.Deg2Rad) * radius;
			var y = Mathf.Sin (angle * Mathf.Deg2Rad) * radius;
			vertices [i + 1] = new Vector3 (x, y, 0);

			// UV: Read about texture mapping here: https://en.wikipedia.org/wiki/UV_mapping
			var u = Mathf.Cos (angle * Mathf.Deg2Rad); // Does this work?
			var v = Mathf.Sin (angle * Mathf.Deg2Rad); // Does this work?
			uv [i + 1] = new Vector2 (u, v);

		}

		// For each vertex, not including center (V0), create a triangle in
		// the mesh from center to next vertex to it (counterclockwise)
		var triangles = new int[anglesCount * 3];
		// iterate through vertices
		for (var i = 0; i < anglesCount; i++) {

			triangles [i * 3 + 0] = centerIndex;
			triangles [i * 3 + 1] = (i == anglesCount - 1) ? 1 : i + 2; // in last case, use 0 degrees (V1);
			triangles [i * 3 + 2] = i + 1;

		}

		// UPDATE MESH and FILTER
		Mesh m = new Mesh ();
		m.vertices = vertices;
		m.uv = uv;
		m.triangles = triangles;
		m.RecalculateBounds ();
		m.RecalculateNormals ();

		mf.mesh = m;

		mr.material.color = PartsManager.instance.playerColors[playerNumber];

		// UPDATE POLYGON COLLIDER
		// the points are all the verticies, minus V0 (center vertex of mesh)

		// build array of points for poly collider
		// OLD FROM GENERATING FULL POLY COLLIDER
//		var points = new Vector2[anglesCount];
//		for (var i = 0; i < anglesCount; i++) {
//			var vertex = vertices [i + 1];
//			points [i] = new Vector2 (vertex.x, vertex.y);
//		}

		var weakPoints = new Vector2[3];
		weakPoints [0] = Vector2.zero;
		weakPoints [1] = new Vector2(vertices[1].x, vertices[1].y);
		int lastIndex = vertices.Length - 1; 
		weakPoints [2] = new Vector2(vertices[lastIndex].x, vertices[lastIndex].y);


		// set path
		weakZoneCollider.SetPath (0, weakPoints);

		// POSITION SIDE PREFABS

		// generate side midpoint angles and distances
		// these will be registration points for borders (sides)
		var midpointAngles = new float[anglesCount];
		var midpointDistances = new float[anglesCount];

		for (var i = 0; i < anglesCount; i++) {
			var a = angles [i];
			// if last angle, use first angle; otherwise use next angle
			var b = (i == anglesCount - 1) ? angles [0] : angles [i + 1]; 

			// if b is less than a then add 360 to b
			if (a > b) b += 360;

			// store what angle the side midpoint is at
			midpointAngles[i] = (a + b) / 2;
			// store distance from center
			midpointDistances[i] = DistanceFromCenterForSideWithArcAngle(b-a);
		}

		// activate and place sides
		for (var i = 0; i < sidesCountMax; i++) {
			var sideGO = sidesGOArray [i];
			if (i+1 < anglesCount) {
				// make active
				sideGO.SetActive (true);
				// set transform
				var angle = midpointAngles[i];
				var distance = midpointDistances[i];
				var x = Mathf.Cos(angle * Mathf.Deg2Rad) * distance;
				var y = Mathf.Sin(angle * Mathf.Deg2Rad) * distance;
				sideGO.transform.localPosition = new Vector3 (x, y, 0);
				sideGO.transform.localRotation = Quaternion.Euler (0, 0, angle-90);

				// update colliders
				PolygonCollider2D coll = sideGO.GetComponent<PolygonCollider2D> ();
				Vector2[] curPath = coll.GetPath (0);
				curPath [2] = new Vector2 (0f, -distance);
				coll.SetPath (0, curPath);
			} else {
				if (sideGO.activeInHierarchy) {
					// make inactive, but first check if it has an attached part
					if (master && sideGO.transform.childCount > 0) {
						// if part is not already detaching
						if (!sideGO.GetComponentInChildren<Part> ().detaching) {
							DetachPart (i);
						}
					}
					sideGO.SetActive (false);
				}
			}
		}

		// update AI if possible
		if (ai) {
			aiCon.UpdateFullSideStatus ();
		}
	}

	// returns angles of all verticies like [0, 120, 240] for triangle
	float[] CalculateAngles (float sides)
	{
		int vertices = Mathf.CeilToInt(sides);

		// special case when shape is complete like sides = 4.0
		bool isEqualSides = (sides == vertices);

		// for sides = 3.1, grow = .1 and grow is the growing side
		float grow = sides - Mathf.Floor (sides);
		float growAngleMax = 360f / sides;
		float growAngle = growAngleMax * grow;
		float anglesOffset = 0f;
		//		float anglesOffset = growAngle / 2;
		//		float anglesOffset = growAngle / 2 + ((Mathf.Floor(sides) % 2) * 180);

		// gives all other angles rest of the circle
		float otherAnglesTotal = 360f - growAngle;
		int otherVerticesCount = vertices;
		if (!isEqualSides) {
			otherVerticesCount -= 1;
		}
		//		print (otherVerticesCount);

		float otherAngle = otherAnglesTotal / otherVerticesCount;

		// builds array of angles to construct polygon
		var angles = new float[vertices];

		for (var i = 0; i < otherVerticesCount; i++) {
			angles [i] = (i * otherAngle) + anglesOffset;
		}

		if (!isEqualSides) {
			angles[otherVerticesCount] = (360f - growAngle) + anglesOffset;
		}

		return angles;
	}

	// HELPERS ---------------------------------------------------------------------------------------------------------------

	float PolyRadiusFromSidesCount(float otherAngle) {
		return 1/(2 * Mathf.Sin(otherAngle * Mathf.Deg2Rad/2));
	}

	float DistanceFromCenterForSideWithArcAngle(float arcAngle) {
		return 1/(2 * Mathf.Tan(arcAngle * Mathf.Deg2Rad/2));
	}

	string ArrayToString (float[] array) {
		var s = "";
		for (var i = 0; i < array.Length; i++) {
			s += array[i].ToString("0");
			s += ",";
		}

		return s;
	}
}