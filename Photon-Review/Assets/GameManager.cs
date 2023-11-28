//using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
	private void Awake()
	{
		//SceneManager.sceneLoaded += OnSceneLoaded;
	}


	private void Start()
	{
		NetworkConnectionManager.instance.OnJoinSuccessedDelegate += SpawnPlayer;
	}


	void OnSceneLoaded(Scene loadedScene, LoadSceneMode mode)
	{
		//if (!PhotonNetwork.IsConnected) return;

		SpawnPlayer();
	}



	public void SpawnPlayer()
	{
		Debug.Log("게임 매니저에서 플레이어 스폰을 시도합니다...");
		StartCoroutine(SpawnPlayerCoroutine());
	}



	private IEnumerator SpawnPlayerCoroutine()
	{
		Debug.Log("게임 매니저에서 플레이어 스폰 시작");
		yield return new WaitForSeconds(1.0f);

		//PhotonNetwork.Instantiate("Player", Vector3.zero, Quaternion.identity);

		var result = NetworkOwnership.Instantiate("Player", Vector3.zero, Quaternion.identity);
		
		Debug.Log("플레이어 스폰 결과: " + (result != null));
	}
}
