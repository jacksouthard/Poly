using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AIController : NetworkBehaviour {
	PolyController pc;
	bool master = false;

	bool shouldRotate = false;
	bool sidesFull = false;

	public float visionRange;
	public float wanderTime;
	public float combatUpdateTime;
	public float maxTimeBeforeUpdate;

	float timeUntilTargetUpdate;

	PartData.PartType[] partTypes = new PartData.PartType[12];

	[Header("Debug")]
	Transform target;
	Vector2 dirToTarget;
	Transform targetFrontFacingSide; 
	public enum AIState {
		wandering,
		collecting,
		attacking,
		fleeing
	};
	public AIState state = AIState.wandering;

	void Awake () {
		pc = GetComponent<PolyController> ();
	}

	void Start () {
		if (pc.isServer) {
			master = true;
			for (int i = 0; i < partTypes.Length; i++) {
				partTypes [i] = PartData.PartType.none;
			}
			UpdateTarget ();
		}
	}
	
	void Update () {
		if (!master) {
			return;
		}

		if (target == null && state != AIState.wandering) {
			// target must have been destoryed
			UpdateTarget();
		}

		if (timeUntilTargetUpdate > 0f) {
			timeUntilTargetUpdate -= Time.deltaTime;
			if (timeUntilTargetUpdate <= 0f) {
				UpdateTarget ();
			}
		}

		if (targetFrontFacingSide != null && shouldRotate) {
			pc.Rotate (CalculateRotation ());
		}
		pc.Move (dirToTarget);
	}

	void UpdateTarget () {
		ObjectOfInterest objectOfInterest = GetObjectOfInterestInRange ();
		if (objectOfInterest.type != ObjectOfInterest.Type.none) {
			// something of interest in range
			target = objectOfInterest.transf;
			if (objectOfInterest.type == ObjectOfInterest.Type.player) {
				int playerScore = objectOfInterest.transf.GetComponent<PolyController> ().attackPartsScore;
				if (pc.attackPartsScore + 1 >= playerScore && GetIndexOfPartType (PartData.PartType.attack, false) != -1) {
					EnterAttack ();
				} else {
					EnterFlee ();
				}
			} else if (objectOfInterest.type == ObjectOfInterest.Type.segment) {
				EnterCollect ();
			} else if (objectOfInterest.type == ObjectOfInterest.Type.part) {
				EnterCollect (part: true);
			}
		} else {
			// nothing interesting in range
			EnterWander ();
		}

		if (shouldRotate) {
			CalculateMostValuableFrontFacingPart ();
		}
//		print ("AI Update. Target: " + target + " State: " + state);
	}

	void EnterWander () {
		shouldRotate = false;
		timeUntilTargetUpdate = wanderTime;
		state = AIState.wandering;
		dirToTarget = Random.insideUnitCircle.normalized;
	}

	void EnterCollect (bool part = false) {
		timeUntilTargetUpdate = maxTimeBeforeUpdate;
		state = AIState.collecting;
		UpdateDirToTarget ();

		if (part) {
			GetIndexOfPartType (PartData.PartType.none);
			shouldRotate = true;
		} else {
			shouldRotate = false;
		}
	}

	void EnterAttack () {
		shouldRotate = true;
		timeUntilTargetUpdate = combatUpdateTime;
		state = AIState.attacking;
		UpdateDirToTarget();
	}

	void EnterFlee () {
		shouldRotate = true;
		timeUntilTargetUpdate = combatUpdateTime;
		state = AIState.fleeing;
		UpdateDirToTarget (flip: true);
	}

	void UpdateDirToTarget (bool flip = false) {
		Vector3 dir3D = (target.position - transform.position).normalized;
		Vector2 dir2D = new Vector2 (dir3D.x, dir3D.y);
		if (flip) {
			dirToTarget = -dir2D;
		} else {
			dirToTarget = dir2D;
		}
	}

	float CalculateRotation () {
		float sideAngle = targetFrontFacingSide.eulerAngles.z;
		float frontFacingAngle = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg + 180f;
		float angleDiff = Mathf.DeltaAngle (sideAngle, frontFacingAngle) + 90f;
		angleDiff = Mathf.Clamp (angleDiff, -180f, 180f);
		float smoothDiff = angleDiff / 180f;
		return smoothDiff;
	}

	void CalculateMostValuableFrontFacingPart () {
		if (state == AIState.wandering && state == AIState.collecting) {
//			// move to put booster behind player
//			int boosterIndex = GetIndexOfPartType (PartData.PartType.booster, false);
//			if (boosterIndex != -1) {
//				targetFrontFacingSide = pc.sidesGOArray [GetOppisiteSideIndex (boosterIndex)].transform;
//			}
		} else {
			// must be attacking or fleeing
			GetIndexOfPartType (PartData.PartType.attack);	
		}
	}

	int GetIndexOfPartType (PartData.PartType targetType, bool directSet = true) {
		for (int i = 0; i < partTypes.Length; i++) {
			Transform side = pc.sidesGOArray [i].transform;
			if (partTypes [i] == targetType && side.gameObject.activeInHierarchy) {
				if (directSet) {
					targetFrontFacingSide = side;
				}
				return i;
			}
		}

//		print ("No part of type: " + targetType);
		return -1;
	}
				
	ObjectOfInterest GetObjectOfInterestInRange () {
		Collider2D[] collsInRange = Physics2D.OverlapCircleAll (new Vector2 (transform.position.x, transform.position.y), visionRange);
		if (collsInRange.Length == 0) {
			return new ObjectOfInterest (ObjectOfInterest.Type.none, null);;
		}

		Transform closestCollectable = null;
		Transform closestAttachable = null;
		float closestDstToSegment = Mathf.Infinity;
		float closestDstToPart = Mathf.Infinity;
	
		foreach (var coll in collsInRange) {
			if (coll.gameObject != gameObject) { // dont detect itself
				float dstToColl = (transform.position - coll.transform.position).magnitude;
			
				if (coll.tag == "Collectable") {
					if (dstToColl < closestDstToSegment) {
						closestCollectable = coll.transform;
						closestDstToSegment = dstToColl;
					}
				} else if (coll.tag == "Attachable") {
					if (dstToColl < closestDstToPart) {
						closestAttachable = coll.transform;
						closestDstToPart = dstToColl;
					}
				} else if (coll.tag == "Player") {
					return new ObjectOfInterest (ObjectOfInterest.Type.player, coll.transform);
				}
			}
		}

		if (closestCollectable == null) {
			return new ObjectOfInterest (ObjectOfInterest.Type.none, null);
		} else if (closestAttachable != null && !sidesFull) {
			return new ObjectOfInterest (ObjectOfInterest.Type.part, closestAttachable);
		} else {
			return new ObjectOfInterest (ObjectOfInterest.Type.segment, closestCollectable);
		}
	}

	public struct ObjectOfInterest {
		public enum Type {
			player,
			part,
			segment,
			none
		}
		public Type type;
		public Transform transf;

		public ObjectOfInterest (Type _type, Transform _transform) {
			type = _type;
			transf = _transform;
		}
	}

	public void UpdatePartTypes (PartData.PartType modifiedType, int index) {
		partTypes [index] = modifiedType;

		UpdateFullSideStatus ();

//		string str = "|";
//		foreach (var partType in partTypes) {
//			str += "" + partType + "|";
//		}
//		print (str);
	}

	int GetDistanceBetweenIndexes (int index1, int index2) {
		int distFromMid1 = index1 - 6;
		int distFromMid2 = index2 - 6;
		return Mathf.Abs (distFromMid1 - distFromMid2) + 1;
	}

	int GetOppisiteSideIndex (int index) {
		return Mathf.Abs (6 - index);
	}

	public void UpdateFullSideStatus () {
		if (GetIndexOfPartType (PartData.PartType.none, false) == -1) {
			sidesFull = true;
		} else {
			sidesFull = false;
		}
	}
}
