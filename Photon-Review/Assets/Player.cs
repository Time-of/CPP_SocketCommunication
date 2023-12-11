using CVSP;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class Instantiatable : MonoBehaviour
{
	public abstract void OnCreated(params object[] args);
}



public class Player : Instantiatable
{
	public string nickname = "default";
	public int id = -1;
	public PlayerController character = null;
	public bool bIsMine { get { return id == NetworkConnectionManager.instance.playerId; } private set { } }

	
	public void SpawnPlayerCharacter()
	{
		Debug.Log("플레이어 " + id + " 에서 캐릭터 생성!");
		//character = NetworkOwnership.RequestInstantiate("Player", Vector3.zero, Quaternion.identity, id) as PlayerController;
		character = NetworkOwnership._InternalInstantiate("Player", Vector3.zero, Quaternion.identity, id) as PlayerController;
	}


	public void OnDisconnected()
	{
		
	}


	private void OnDestroy()
	{
		// @todo
		//NetworkConnectionManager.instance.SendRPCToAll(id, "OnDisconnected");
	}


	public static Player Instantiate(PlayerInfo info)
	{
		var prefab = Resources.Load<Player>("PlayerObject");

		if (prefab == null)
		{
			Debug.LogError("네트워크 스폰: 프리팹 리소스 [PlayerObject] 로드 실패!");
			return null;
		}

		Player player = GameObject.Instantiate<Player>(prefab, Vector3.zero, Quaternion.identity);
		player.InitInfo(info);

		return player;
	}


	public void InitInfo(PlayerInfo info)
	{
		id = info.id;
		nickname = info.nickname;

		NetworkConnectionManager.instance.playerMap.Add(id, this);

		SpawnPlayerCharacter();
	}


	// 폐기
	public override void OnCreated(params object[] args)
	{
		InitInfo((PlayerInfo)args[0]);
	}
}
