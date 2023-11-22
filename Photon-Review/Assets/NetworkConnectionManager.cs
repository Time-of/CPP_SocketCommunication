using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


[RequireComponent(typeof(SocketConnector))]
public class NetworkConnectionManager : MonoBehaviour
{
	[SerializeField]
	private string TestRoomName = "HELLOWORLD";

	[SerializeField]
	private GameManager gameManager;

	public string localNickname { get; private set; }

	public static NetworkConnectionManager instance;

	[SerializeField]
	private TMP_Text chattingBox;

	[SerializeField]
	private GameObject chattingBoxPanel;


	#region 소켓 통신
	public SocketConnector socketConnector { get; private set; }
	#endregion


	private void Awake()
	{
		if (instance == null) instance = this;
		socketConnector = GetComponent<SocketConnector>();
		//PhotonNetwork.AutomaticallySyncScene = true;

		chattingBoxPanel.SetActive(false);
	}


	private void Start()
	{
		//PhotonNetwork.GameVersion = "0.01";
		//PhotonNetwork.NickName = "MyName";

		//PhotonNetwork.ConnectUsingSettings();
	}


	public void OnConnectedToMaster()
	{
		Debug.Log("Connected to Master");

	}


	public bool ConfirmNicknameAndJoinRandomRoom(string newNickname)
	{
		//if (!PhotonNetwork.IsConnected) return false;

		localNickname = newNickname;
		//PhotonNetwork.JoinRandomRoom();

		// 우선은 여기서 서버에 연결하는 것으로 구현
		bool result = socketConnector.ConnectToServer("127.0.0.1");

		chattingBoxPanel.SetActive(result);

		return result;
	}


	public void OnJoinedRoom()
	{
		//Debug.Log("<color=yellow>방 입장 성공. 방 이름:</color> " + PhotonNetwork.CurrentRoom.Name);
		Debug.Log("<color=yellow>방 입장 성공.</color>");

		gameManager.SpawnPlayer();
	}


	public void OnJoinRoomFailed(short returnCode, string message)
	{
		//PhotonNetwork.CreateRoom(TestRoomName, new RoomOptions { MaxPlayers = 4 });
	}


	public void OnJoinRandomFailed(short returnCode, string message)
	{
		Debug.Log("<color=orange>랜덤 방 입장 실패!</color>");

		//PhotonNetwork.CreateRoom(TestRoomName, new RoomOptions { MaxPlayers = 4 });
	}


	public void OnCreatedRoom()
	{
		//Debug.Log("<color=yellow>방을 생성했습니다. 방 이름:</color> " + PhotonNetwork.CurrentRoom.Name);
		Debug.Log("<color=yellow>방을 생성했습니다.");
	}


	public void SendChat(string message)
	{
		if (!socketConnector.bIsConnected)
		{
			//Debug.LogWarning("playerPhotonViewRef가 null입니다!!");
			Debug.LogWarning("통신이 연결되지 않음!!");
			return;
		}

		string chattingText = localNickname + ": " + message + "\n";
		//photonView.RPC("RPCSendChatAll", RpcTarget.All, chattingText);
	}


	//[PunRPC]
	public void RPCSendChatAll(string message)
	{
		chattingBox.text += message;
	}
}
