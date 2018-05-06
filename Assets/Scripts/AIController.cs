﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AIController : NetworkBehaviour {
	PolyController pc;
	bool master = false;

	bool shouldRotate = false;
	bool sidesFull = false;

	public float visionRange;
	public float rangedHoldDst;
	public float wanderTime;
	public float combatUpdateTime;
	public float maxTimeBeforeUpdate;

	float timeUntilTargetUpdate;

	PartData.PartType[] partTypes = new PartData.PartType[12];

	[Header("Debug")]
	public int meleeScore = 0;
	public int rangedScore = 0;
	bool ranged = false;

	Transform target;
	Vector2 dirToTarget;
	float targetRotDir;

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
		if (!master || Time.timeScale == 0) {
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
	}
	
	void FixedUpdate () {
		if (!master || Time.timeScale == 0 && target != null) {
			return;
		}

		if (shouldRotate) {
			float rotInput = CalculateRotation ();
			pc.Rotate (rotInput);
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
				if (pc.attackPartsScore + 1 >= playerScore && (GetIndexOfPartType (PartData.PartType.melee, false) != -1 || GetIndexOfPartType (PartData.PartType.ranged, false) != -1)) {
					EnterAttack (objectOfInterest.distance);
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

	void EnterAttack (float dstToTarget) {
		shouldRotate = true;
		timeUntilTargetUpdate = combatUpdateTime;
		state = AIState.attacking;
		if (ranged) {
			if (dstToTarget > rangedHoldDst) {
				// rush with ranged
				UpdateDirToTarget ();
				GetIndexOfPartType (PartData.PartType.ranged);
			} else {
				// hold from a distance with ranged
				UpdateDirToTarget (flip: true);
				int indexOfRangedWeapon = GetIndexOfPartType (PartData.PartType.ranged, false);
				UpdateTargetRotation (pc.sidesGOArray [indexOfRangedWeapon].transform);
			}
		} else {
			// melee rush
			UpdateDirToTarget ();
			GetIndexOfPartType (PartData.PartType.melee);
		}
	}

	void EnterFlee () {
		shouldRotate = true;
		timeUntilTargetUpdate = combatUpdateTime;
		state = AIState.fleeing;
		UpdateDirToTarget (flip: true);

		int attackSideIndex = GetIndexOfPartType (PartData.PartType.ranged, false);
		if (attackSideIndex == -1) {
			attackSideIndex = GetIndexOfPartType (PartData.PartType.melee, false);
		}

		if (attackSideIndex != -1) {
			// fleeing with weapon
			UpdateTargetRotation (pc.sidesGOArray[attackSideIndex].transform);
		}
	}

	void UpdateTargetRotation (Transform targetFrontFacingSide) {
		targetRotDir = (targetFrontFacingSide.localEulerAngles.z % 360f) - 90f;
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
		Vector2 targetDirRealtime = (target.position - transform.position).normalized;
		float frontFacingAngle = Mathf.Atan2(targetDirRealtime.y, targetDirRealtime.x) * Mathf.Rad2Deg + 180f;
		float targetPolyAngle = -targetRotDir + frontFacingAngle;
		float currentPolyAngle = transform.eulerAngles.z % 360f;
		float angleDiff = Mathf.DeltaAngle (targetPolyAngle, currentPolyAngle);
//		print ("C: " + currentPolyAngle + " T: " + targetPolyAngle + " Diff: " + angleDiff);

		angleDiff = Mathf.Clamp (angleDiff * 2f, -180f, 180f); // * 2 to increase the rotation speed
		float smoothDiff = -angleDiff / 180f;
		return smoothDiff;
	}

	int GetIndexOfPartType (PartData.PartType targetType, bool directSet = true) {
		int lastIndex = -1;
		for (int i = 0; i < partTypes.Length; i++) {
			Transform side = pc.sidesGOArray [i].transform;
			if (partTypes [i] == targetType && side.gameObject.activeInHierarchy) {
				// if the sides's part type matches input and side is active
				if (directSet) {
					UpdateTargetRotation (side.transform);
					break;
				}
				// set last index to a valid index of side with input part
				lastIndex = i;
			}
		}

		return lastIndex;
	}
				
	ObjectOfInterest GetObjectOfInterestInRange () {
		Collider2D[] collsInRange = Physics2D.OverlapCircleAll (new Vector2 (transform.position.x, transform.position.y), visionRange);
		if (collsInRange.Length == 0) {
			return new ObjectOfInterest (ObjectOfInterest.Type.none, null, 0f);;
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
					return new ObjectOfInterest (ObjectOfInterest.Type.player, coll.transform, dstToColl);
				}
			}
		}

		if (closestCollectable == null) {
			return new ObjectOfInterest (ObjectOfInterest.Type.none, null, 0f);
		} else if (closestAttachable != null && !sidesFull) {
			return new ObjectOfInterest (ObjectOfInterest.Type.part, closestAttachable, 0f);
		} else {
			return new ObjectOfInterest (ObjectOfInterest.Type.segment, closestCollectable, 0f);
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
		public float distance;

		public ObjectOfInterest (Type _type, Transform _transform, float _distance) {
			type = _type;
			transf = _transform;
			distance = _distance;
		}
	}

	public void UpdatePartTypes (PartData.PartType modifiedType, int index) {
		if (modifiedType == PartData.PartType.none) {
			// part removed
			if (partTypes [index] == PartData.PartType.ranged) {
				// ranged part removed
				rangedScore--;
			} else if (partTypes [index] == PartData.PartType.melee) {
				// melee part removed
				meleeScore--;
			}
		}
		if (modifiedType == PartData.PartType.ranged) {
			// ranged part added
			rangedScore++;
		} else if (modifiedType == PartData.PartType.melee) {
			// melee part added
			meleeScore++;
		} 

		if (meleeScore > rangedScore) {
			ranged = false;
		} else {
			ranged = true;
		}
		// set new value
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
