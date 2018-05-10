using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ScoreManager : NetworkBehaviour {
	public static ScoreManager instance;

	Dictionary<NetworkInstanceId, PlayerData> playerDatas = new Dictionary<NetworkInstanceId, PlayerData>();

	void Awake () {
		instance = this;
	}

	void Start () {
		if (!isServer) {
			return;
		}
		// set data for all connected players at start of server (generally only AI)
//		PolyController[] pcs = GameObject.FindObjectsOfType<PolyController>();
//		foreach (var pc in pcs) {
//			bool isBot = false;
//			if (pc.ai) {
//				isBot = true;
//			}
//			playerDatas.Add (pc.netId, new PlayerData (GenerateName(isBot), 0));
//		}
	}

	public void AddPlayerData (NetworkInstanceId netID, bool isBot) {
		playerDatas.Add (netID, new PlayerData (GenerateName(isBot), 0));
	}

	public void RemovePlayerData (NetworkInstanceId netID) {
		playerDatas.Remove (netID);
	}

	public void AddKill (NetworkInstanceId netID) {
		PlayerData curData = playerDatas [netID];
		curData.kills += 1;
		playerDatas [netID] = curData; 
		print (curData.name + " now has " + curData.kills + " kills");
	}

	public void ResetKills (NetworkInstanceId netID) {
		PlayerData curData = playerDatas [netID];
		curData.kills = 0;
		playerDatas [netID] = curData;
		print ("Reset kills for " + curData.name);
	}

	public string GetName (NetworkInstanceId netID) {
		return playerDatas [netID].name;
	}

	public struct PlayerData {
		public string name;
		public int kills;

		public PlayerData (string _name, int _kills) {
			name = _name;
			kills = _kills;
		}
	}

	public string[] adjectives;
	public string[] nouns;
	string GenerateName (bool bot) {
		string randomAdj = adjectives [Random.Range (0, adjectives.Length)];
		string randomNoun = (bot) ? "Bot" : nouns [Random.Range (0, nouns.Length)];
		return randomAdj + " " + randomNoun;
	}
}
