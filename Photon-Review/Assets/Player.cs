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
		Debug.Log("�÷��̾� " + id + " ���� ĳ���� ����!");
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
			Debug.LogError("��Ʈ��ũ ����: ������ ���ҽ� [PlayerObject] �ε� ����!");
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


	// ���
	public override void OnCreated(params object[] args)
	{
		InitInfo((PlayerInfo)args[0]);
	}
}
