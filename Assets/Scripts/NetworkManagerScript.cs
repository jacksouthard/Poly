using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.UI;

public class NetworkManagerScript : NetworkManager {
	public static NetworkManagerScript instance;
	public bool master = false;

	public Text playText;

	public int playerCount = 0;

	public bool freezeServer;

	public bool resetServer;

	public enum RunAs
	{
		menu,
		client,
		host,
		server
	};
	public RunAs runAs;

	void Awake () {
		instance = this;
//		useWebSockets = true;
	}

	void Start () {
		if (runAs == RunAs.server) {
			StartServer ();
		} else if (runAs == RunAs.host) {
			StartHost ();
		} else if (runAs == RunAs.client) {
			StartClient ();
		} 
	}

	public void JoinGame () {
		// TODO test if server is active
		StartClient();
		playText.text = "Loading";
	}

	public override void OnServerSceneChanged (string sceneName) {
		if (sceneName == "Game") {
			if (freezeServer && playerCount == 0 && runAs == RunAs.server) {
				FreezeServer ();
			}
		}
		base.OnServerSceneChanged (sceneName);
	}

	public override void OnServerConnect (NetworkConnection conn)
	{
		print ("Client Connect");
		playerCount++;

		if (Time.timeScale == 0) {
			ResumeServer ();
		}

		base.OnServerConnect (conn);
	}

	public override void OnServerDisconnect (NetworkConnection conn)
	{
		print ("Client Disconnect");
		playerCount--;

		if (playerCount <= 0) {
			if (resetServer) {
				ResetServer ();
			}
			if (freezeServer) {
				FreezeServer ();
			}
		} else {
			// remove from leaderboard
			ScoreManager.instance.RemovePlayerData (conn.playerControllers[0].unetView.netId.Value);
		}

		base.OnServerDisconnect (conn);
	}

	void ResetServer () {
		print ("Reseting Server");
		StopServer ();
	}

	void FreezeServer () {
		print ("Freezing Server");
		Time.timeScale = 0f;
	}

	void ResumeServer () {
		print ("Resuming Server");
		Time.timeScale = 1f;
	}
}
