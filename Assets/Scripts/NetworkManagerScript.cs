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
}
