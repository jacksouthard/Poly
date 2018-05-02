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

	bool frozen = false;
	public bool freezeServer;
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
		if (freezeServer && playerCount == 0 && runAs == RunAs.server) {
			FreezeServer ();
		}
		base.OnServerSceneChanged (sceneName);
	}

	public override void OnServerConnect (NetworkConnection conn)
	{
		print ("Client Con");
		playerCount++;

		if (frozen) {
			ResumeServer ();
		}

		base.OnServerConnect (conn);
	}

	public override void OnServerDisconnect (NetworkConnection conn)
	{
		print ("Client Dis");
		playerCount--;

		if (playerCount <= 0) {
			FreezeServer ();
		}

		base.OnServerDisconnect (conn);
	}

	void FreezeServer () {
		Time.timeScale = 0f;
		frozen = true;
	}

	void ResumeServer () {
		Time.timeScale = 1f;
		frozen = false;
	}
}
