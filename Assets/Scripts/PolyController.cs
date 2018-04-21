using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public struct Side
{
	public NetworkInstanceId partNetId;
	public bool hasPartAttached;

	// Pass in 0 to indicate no part attached
	public Side(NetworkInstanceId partNetId ) {
		this.partNetId = partNetId;
		this.hasPartAttached = (partNetId != NetworkInstanceId.Invalid);
		// TODO: Add health, etc.
	}

};

public class PolyController : NetworkBehaviour {

	private float sidesCountMin = 2.0f;
	private int sidesCountMax = 12;
	private float sidesCountIncrement = 0.05f;
	private float[] angles;
	private float radius;

	private float speed = 10f;
	private float maxVelocity = 2f;
//	private float maxAngularVelocity = 0.05f;
	private float rotationSpeed = 5f;

	private GameObject playerCamera;
	private GameObject leftTouchArea;
	private GameObject rightTouchArea;

	public Material fillMaterial;

	private GameObject[] sidesGOArray;
	public GameObject sidePrefab;
	public GameObject sidesContainer;

	private bool useTouch = true;
	private bool useKeyboard = true;

	private Color[] playerColors = new Color[] {
		new Color(0.30f, 0.63f, 0.73f),
		new Color(0.85f, 0.63f, 0.28f),
		new Color(0.77f, 0.32f, 0.38f),
		new Color(0.59f, 0.45f, 0.82f),
		new Color(0.33f, 0.72f, 0.33f)
	};

	// NETWORK SYNCING ---------------------------------------------------------------------------

	// SidesCount network syncing

	[SyncVar(hook="SetSidesCount")]
	private float sidesCount;

	[Command]
	void CmdChangeSidesCount (float newValue) {
		sidesCount = newValue;
	}

	void SetSidesCount (float newValue)
	{
//		print ("Sides count set at " + newValue);
		sidesCount = Mathf.Clamp (newValue, sidesCountMin, sidesCountMax);

		UpdateRendering();
		gameObject.GetComponent<CircleCollider2D>().radius = radius + 0.75f;
	}

	// PlayerNumber network syncing

	[SyncVar(hook="SetPlayerNumber")]
	private int playerNumber;

	// Tell server version of object to change player number/color
	[Command]
	void CmdChangePlayerNumber(int newValue) {
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
		
	// SideList network syncing
	public class SideList : SyncListStruct<Side> {}
	SideList sideList = new SideList();
			
	public override void OnStartClient () {
		sideList.Callback = OnSideListChange;
	}
		
	// GENERAL ---------------------------------------------------------------------------------------

	void Start() {
		SetupRendering();
		if (isLocalPlayer) {
			CmdChangeSidesCount (2.2f);
			CmdChangePlayerNumber (Random.Range (0, 4));
		} else {
			SetSidesCount (sidesCount);
		}
	}

	public override void OnStartLocalPlayer() {
		CameraSetup();	
	}
	
	void Update ()
	{
		if (isLocalPlayer) {
			Move ();
			Rotate ();

			if (Input.GetKey("=")) {
				CmdChangeSidesCount(sidesCount + sidesCountIncrement);
			}
			if (Input.GetKey("-")) {
				CmdChangeSidesCount(sidesCount - sidesCountIncrement);
			}

			if (Input.GetKey("0")) {
				CmdChangePlayerNumber(0);
			}
			if (Input.GetKey("1")) {
				CmdChangePlayerNumber(1);
			}
			if (Input.GetKey("2")) {
				CmdChangePlayerNumber(2);
			}
			if (Input.GetKey("3")) {
				CmdChangePlayerNumber(3);
			}
			if (Input.GetKey("4")) {
				CmdChangePlayerNumber(4);
			}
		}
	}

	void CameraSetup () {			
		// sets up game camera for individual player
		GameObject playerCameraPrefab = Resources.Load("PlayerCamera") as GameObject;
		Instantiate(playerCameraPrefab, new Vector3 (0,0, -10), Quaternion.Euler(0,0,0));

		playerCamera = GameObject.Find("PlayerCamera(Clone)");
		playerCamera.GetComponent<CameraController>().target = this.gameObject.transform;

		leftTouchArea = playerCamera.transform.Find("LeftTouchArea").gameObject;
		rightTouchArea = playerCamera.transform.Find("RightTouchArea").gameObject;
	}

	void Move ()
	{
		Rigidbody2D rigidbody = gameObject.GetComponent<Rigidbody2D> ();
		var moveOffset = Vector2.zero;

		// faking touch for now with mouse controls
		if (useTouch) {
			moveOffset = rightTouchArea.GetComponentInChildren<TouchAreaController> ().offset;
		}

		if (useKeyboard && moveOffset == Vector2.zero) {
			moveOffset = new Vector2 (Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		}

		// add force to poly
		if (moveOffset != Vector2.zero) {
			rigidbody.AddForce (moveOffset * speed);
		}

		// limit velocity
		rigidbody.velocity = Vector2.ClampMagnitude(rigidbody.velocity, maxVelocity);
	}

	void Rotate ()
	{
		Rigidbody2D rigidbody = gameObject.GetComponent<Rigidbody2D> ();
		var rotateOffset = Vector2.zero;

		if (useTouch) {
			rotateOffset = leftTouchArea.GetComponentInChildren<TouchAreaController> ().offset;
		}

		if (useKeyboard && rotateOffset == Vector2.zero) {
			rotateOffset = new Vector2 (0, Input.GetAxis("Rotation"));
		}

		if (rotateOffset != Vector2.zero) {
//			transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3 (0, 0, leftOffset.y * 20f)), rotationSpeed * Time.deltaTime);
			rigidbody.AddTorque(rotateOffset.y * rotationSpeed);
		}

		// limit angularVelocity
//		rigidbody.angularVelocity = Vector2.ClampMagnitude(rigidbody.angularVelocity, maxAngularVelocity);
	}

	void SetupRendering ()
	{
		gameObject.AddComponent (typeof(MeshFilter));

		MeshRenderer mr = gameObject.AddComponent (typeof(MeshRenderer)) as MeshRenderer;
		mr.material = fillMaterial;

		gameObject.AddComponent (typeof(PolygonCollider2D));
		SetupSides();
	}

	// Setup sides, both game object array and sideSyncList 
	void SetupSides() {

		// setup pool of sides in the sidesContainer
		sidesGOArray = new GameObject[sidesCountMax];

		for (var i = 0; i < sidesCountMax; i++) {

			// setup sync list
			sideList.Add( new Side( NetworkInstanceId.Invalid) );

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
		var center = new Vector3 (0, 0, 0);
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

		var mf = gameObject.GetComponent<MeshFilter> ();
		mf.mesh = m;

		var mr = gameObject.GetComponent<MeshRenderer>();
		mr.material.color = playerColors[playerNumber];

		// UPDATE POLYGON COLLIDER
		// the points are all the verticies, minus V0 (center vertex of mesh)

		// build array of points for poly collider
		var points = new Vector2[anglesCount];
		for (var i = 0; i < anglesCount; i++) {
			var vertex = vertices [i + 1];
			points [i] = new Vector2 (vertex.x, vertex.y);
		}

		// set path
		var pc = gameObject.GetComponent<PolygonCollider2D> ();
		pc.SetPath (0, points);

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
			} else {
//				// make inactive, but first check if it has an attached part
				if (isLocalPlayer && !sideList[i].hasPartAttached ) {
					CmdDetachPartOnSide(i);
				}
				sideGO.SetActive(false);
			}
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

//		print(ArrayToString(angles) + "growAngle:" + growAngle.ToString("0"));

		return angles;
	}

	// Handle a collectable segment entering collect zone around poly
	void OnTriggerEnter2D (Collider2D other) {
		if (other.CompareTag("Collectable")) {
			if (isLocalPlayer) {
				other.gameObject.GetComponent<SegmentController> ().StartTracking (gameObject.transform, false);
			} else {
				other.gameObject.GetComponent<SegmentController> ().StartTracking (gameObject.transform, true);
			}
		}
	}

	[Command]
	public void CmdSegmentStartDestory (GameObject segment) {
		Destroy (segment);
		GameObject.Find ("SegmentsManager").GetComponent<SegmentsManager> ().SegmentDestoryed ();
		Collect(0.2f);
	}

	void Collect (float sidesIncrement) {
		CmdChangeSidesCount (sidesCount + sidesIncrement);
	}

	// HELPERS ------------------------------------------------------

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

	// PARTS ------------------------------------------------------------------------------------------

	// First, on server, a part hits a side...

	public void OnPartHitSide (GameObject part, GameObject side)
	{
		if (isServer) {
			// if part is not attached AND side doesn't have part attached, attach part to side
			int sideIndex = int.Parse(side.name);
			if (!part.GetComponent<PartController>().isAttached() && !sideList[sideIndex].hasPartAttached ) {
				NetworkInstanceId netId = part.GetComponent<NetworkIdentity>().netId;
//				print("Call CmdAttachPartToSide(" + netId + ", "+sideIndex+")");
				AttachPartToSide(netId, sideIndex);
			}
		}
	}

	// Second, the server updates the sideList for that item...

	void AttachPartToSide(NetworkInstanceId netId, int sideIndex) {
		sideList[sideIndex] = new Side( netId );
		sideList.Dirty(sideIndex); // needed to make change propagate to clients
		// TODO: Also need to call the OnPartIDChanged for server change?
		RenderPartChangeOnSide(sideIndex);
	}
		
	// Third, on the clients, the change of sideList is detected...

	private void OnSideListChange(SideList.Operation op, int sideIndex)
	{
//		Debug.Log("OnSideListChange[" + sideIndex + "] " + op + " hasPart = " + sideList[sideIndex].hasPartAttached + " netId = " + sideList[sideIndex].partNetId);
		if (op == SideList.Operation.OP_DIRTY) {
			RenderPartChangeOnSide(sideIndex);
		}
	}

	// And the visual change is rendered...

	void RenderPartChangeOnSide(int sideIndex) {
		// TODO: add check about attached or not
		NetworkInstanceId netId = sideList[sideIndex].partNetId;

		GameObject part = NetworkServer.FindLocalObject( netId );
		GameObject side = sidesGOArray[sideIndex];
		print("RenderPartChangeOnSide(" + sideIndex + "); partID = " + netId + " go = " + part);
		part.GetComponent<PartController>().Attach(side);
	}



//	[ClientRpc]
//	void RpcAttachPart (GameObject part, GameObject side)
//	{
//		print("debug:" + part + side);
//		part.GetComponent<PartController>().Attach(side);
//	}

	// runs on server
	[Command]
	void CmdDetachPartOnSide(int sideIndex) {
		sideList[sideIndex] = new Side(NetworkInstanceId.Invalid);
//		part.GetComponent<PartController>().Detach();
	}
}