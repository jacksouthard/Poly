using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ScoreManager : NetworkBehaviour {
	public static ScoreManager instance;

	Dictionary<uint, PlayerData> playerDatas = new Dictionary<uint, PlayerData>();

	void Awake () {
		instance = this;
	}

	// ALTERING PLAYER DATA

	public void AddPlayerData (uint netID, bool isBot) {
		playerDatas.Add (netID, new PlayerData (GenerateName(isBot), 0));

		if (leaderboardString == "-" && playerDatas.Count >= 3) {
			CalculateScoreboard ();
		}
	}

	public void RemovePlayerData (uint netID) {
		PlayerData curData = playerDatas [netID];
		int killsBeforeReset = curData.kills;

		playerDatas.Remove (netID);

		ScoreReset (killsBeforeReset);
	}

	public void AddKill (uint netID) {
		PlayerData curData = playerDatas [netID];
		curData.kills += 1;
		playerDatas [netID] = curData; 
		ScoreValueChanged (curData.kills);
//		print (curData.name + " now has " + curData.kills + " kills");
	}

	public void ResetKills (uint netID) {
		PlayerData curData = playerDatas [netID];
		int killsBeforeReset = curData.kills;
		curData.kills = 0;
		playerDatas [netID] = curData;
		ScoreReset (killsBeforeReset);
//		print ("Reset kills for " + curData.name);
	}

	public string GetName (uint netID) {
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



	// RENDERING UI
	Transform[] slots = new Transform[3];
	int lowestKills = -1;
	public Color[] leaderboardColors;

	[SyncVar(hook="RenderScoreboard")]
	string leaderboardString = "-";

	void Start () {
		Transform scoreboard = GameObject.Find ("Canvas").transform.Find ("Scoreboard");
		for (int i = 0; i < slots.Length; i++) {
			slots [i] = scoreboard.GetChild (i);
		}
	
		if (isClient) {
			RenderScoreboard (leaderboardString);
		}
	}

	void ScoreValueChanged (int newKills) {
		if (newKills > lowestKills) {
			CalculateScoreboard ();
		}
	}

	void ScoreReset (int killsBeforeReset) {
//		print ("Player died with kills: " + killsBeforeReset + " Lowest kills: " + lowestKills);
		if (killsBeforeReset > lowestKills) {
			CalculateScoreboard ();
		}
	}

	void CalculateScoreboard () { // runs on server
		if (playerDatas.Count < 3) {
			print ("Not enough players to generate scoreboard");
			return;
		}

		// get new top players
		int firstKills = -1;
		uint? firstNetID = null;
		int secondKills = -1;
		uint? secondNetID = null;
		int thirdKills = -1;
		uint? thirdNetID = null;

		foreach (uint key in playerDatas.Keys) {
			int kills = playerDatas[key].kills;
			if (kills > firstKills) {
				firstKills = kills;
				firstNetID = key;
			} else if (kills > secondKills) {
				secondKills = kills;
				secondNetID = key;
			} else if (kills > thirdKills) {
				thirdKills = kills;
				thirdNetID = key;
			}
		}

		lowestKills = thirdKills;

		PlayerData[] topPlayerDatas = new PlayerData[3];
		if (firstNetID != null && secondNetID != null && thirdNetID != null) {
			topPlayerDatas [0] = playerDatas [firstNetID.Value];
			topPlayerDatas [1] = playerDatas [secondNetID.Value];
			topPlayerDatas [2] = playerDatas [thirdNetID.Value];
		} else {
			print ("Leaderboard players not valid");
			return;
		}

		// serialize leaderboard
		string newLeaderboardString = "";
		foreach (var data in topPlayerDatas) {
			string slotString = data.name + "." + data.kills + "|";
			newLeaderboardString += slotString;
		}
		newLeaderboardString = newLeaderboardString.Substring(0, newLeaderboardString.Length - 1);

		leaderboardString = newLeaderboardString;

		if (!isClient) {
			RenderScoreboard (leaderboardString);
		}
	}
		
	void RenderScoreboard (string newString) {
		if (newString == "-") {
			print ("Scoreboard not setup");
			return;
		}
		string[] slotStrings = newString.Split('|');
		for (int i = 0; i < slotStrings.Length; i++) {
			string[] splitSlot = slotStrings [i].Split ('.');
			string name = splitSlot [0];
			string kills = splitSlot [1];
			slots [i].Find ("Name").GetComponent<Text> ().text = name;
			slots [i].Find ("Kills").GetComponent<Text> ().text = kills;
			slots [i].Find ("Crown").GetComponent<Image> ().color = leaderboardColors[i];
		}
	}

	// NAMING

	public string[] adjectives;
	public string[] nouns;
	string GenerateName (bool bot) {
		string randomAdj = adjectives [Random.Range (0, adjectives.Length)];
		string randomNoun = (bot) ? "Bot" : nouns [Random.Range (0, nouns.Length)];
		return randomAdj + " " + randomNoun;
	}
}
