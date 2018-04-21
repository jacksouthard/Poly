using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.UI;

public class NetworkManagerScript : NetworkManager {
	public void StartupHost ()
	{
//		SetPort();
//		NetworkManager.singleton.StartHost();
		StartMatchMaker();
		NetworkManager.singleton.StartServer();
	}

//	public void JoinGame()
//	{
////		SetIPAdress();
////		SetPort();
////		NetworkManager.singleton.StartClient();
//	}

//	void SetIPAdress ()
//	{
//		string ipAddress = GameObject.Find("InputFieldIPAdress").transform.FindChild("Text").GetComponent<Text>().text;
//		NetworkManager.singleton.networkAddress = ipAddress;
//	}

//	void SetPort() 
//	{
//		NetworkManager.singleton.networkPort = 7777;
//	}

//	void OnLevelWasLoaded (int level)
//	{
//		if (level == 0) {
//			SetupMenuSceneButtons();
//		} else {
//			SetupGameSceneButtons();
//		}
//	}

////	void SetupMenuSceneButtons()
//	{
////		GameObject.Find("ButtonStartHost").GetComponent<Button>().onClick.RemoveAllListeners();
////		GameObject.Find("ButtonStartHost").GetComponent<Button>().onClick.AddListener(StartupHost);
////
////		GameObject.Find("ButtonJoinGame").GetComponent<Button>().onClick.RemoveAllListeners();
////		GameObject.Find("ButtonJoinGame").GetComponent<Button>().onClick.AddListener(JoinGame);
//	}

	void SetupGameSceneButtons()
	{
		GameObject.Find("ButtonDisconnect").GetComponent<Button>().onClick.RemoveAllListeners();
		GameObject.Find("ButtonDisconnect").GetComponent<Button>().onClick.AddListener(NetworkManager.singleton.StopHost);
	}
}
