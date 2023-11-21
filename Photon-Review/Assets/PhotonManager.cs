using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;



public class PhotonManager : MonoBehaviourPunCallbacks
{
	[SerializeField]
	private string TestRoomName = "HELLOWORLD";

	[SerializeField]
	private GameManager gameManager;

	public string localNickname { get; private set; }

	public static PhotonManager instance;

	[SerializeField]
	private TMP_Text chattingBox;

	[SerializeField]
	private GameObject chattingBoxPanel;


	private void Awake()
	{
		if (instance == null) instance = this;
		PhotonNetwork.AutomaticallySyncScene = true;

		chattingBoxPanel.SetActive(false);
	}


	private void Start()
	{
		PhotonNetwork.GameVersion = "0.01";
		PhotonNetwork.NickName = "MyName";

		PhotonNetwork.ConnectUsingSettings();
	}


	public override void OnConnectedToMaster()
	{
		Debug.Log("Connected to Master");
		//CreateRoom();

		
		//PhotonNetwork.JoinRoom(TestRoomName);
	}


	public bool ConfirmNicknameAndJoinRandomRoom(string newNickname)
	{
		if (!PhotonNetwork.IsConnected) return false;

		localNickname = newNickname;
		PhotonNetwork.JoinRandomRoom();

		chattingBoxPanel.SetActive(true);

		return true;
	}


	public override void OnJoinedRoom()
	{
		Debug.Log("<color=yellow>방 입장 성공. 방 이름:</color> " + PhotonNetwork.CurrentRoom.Name);

		gameManager.SpawnPlayer();
	}


	public override void OnJoinRoomFailed(short returnCode, string message)
	{
		PhotonNetwork.CreateRoom(TestRoomName, new RoomOptions { MaxPlayers = 4 });
	}


	public override void OnJoinRandomFailed(short returnCode, string message)
	{
		Debug.Log("<color=orange>랜덤 방 입장 실패!</color>");

		PhotonNetwork.CreateRoom(TestRoomName, new RoomOptions { MaxPlayers = 4 });
	}


	public override void OnCreatedRoom()
	{
		Debug.Log("<color=yellow>방을 생성했습니다. 방 이름:</color> " + PhotonNetwork.CurrentRoom.Name);
	}


	public void SendChat(string message)
	{
		if (photonView == null)
		{
			Debug.LogWarning("playerPhotonViewRef가 null입니다!!");
			return;
		}

		string chattingText = localNickname + ": " + message + "\n";
		photonView.RPC("RPCSendChatAll", RpcTarget.All, chattingText);
	}


	[PunRPC]
	public void RPCSendChatAll(string message)
	{
		chattingBox.text += message;
	}
}
