using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


/// <author>�̼���</author>
/// <summary>
/// ��Ʈ��ũ ���ʽ� ������ ���� ������Ʈ Ŭ����.
/// id�� NetworkConnectionManager�� playerId�� �����ϴٸ� bIsMine�� true, �ƴ϶�� false�� ����.
/// </summary>
public class NetworkOwnership : Instantiatable
{
	public Player ownerPlayer;

	public int ownerIdForDebug = -1;

	public bool bIsMine { get { return ownerPlayer != null && ownerPlayer.id == NetworkConnectionManager.instance.playerId; } private set { } }

	private void SetOwner(Player NewOwner) { ownerPlayer = NewOwner; }


	public static Instantiatable RequestInstantiate(string resourceName, Vector3 position, Quaternion rotation, params object[] args)
	{
		//if ((int)args[0] == NetworkConnectionManager.instance.playerId)
		{
			NetworkConnectionManager.instance.SendObjectSpawnInfo(resourceName, position, rotation);
			Debug.Log("���� ��� ���� �Ϸ�!");
		}

		return _InternalInstantiate(resourceName, position, rotation, args);
	}

	
	public static Instantiatable _InternalInstantiate(string resourceName, Vector3 position, Quaternion rotation, params object[] args)
	{
		var prefab = Resources.Load<Instantiatable>(resourceName);

		if (prefab == null)
		{
			Debug.LogError("��Ʈ��ũ ����: ������ ���ҽ� [" + resourceName + "] �ε� ����! NetworkOwnership Ŭ������ ���� ���� �ƴұ��?");
			return null;
		}

		Instantiatable ownershipObject = Instantiate<Instantiatable>(prefab, position, rotation);
		ownershipObject.OnCreated(args);

		return ownershipObject;
	}


	public override void OnCreated(params object[] args)
	{
		Player owner = null;
		if (NetworkConnectionManager.instance.playerMap.TryGetValue((int)args[0], out owner))
		{
			SetOwner(owner);
			ownerIdForDebug = owner.id;
			Debug.Log("NetworkOwnership OnCreated: ���� ���� �Ϸ�! " + owner);
		}
		else
		{
			Debug.LogWarning("��Ʈ��ũ ����: Owner�� �������� �ʰų� NetworkOwnership�� ������ ���� �ʾƿ�!");
		}
	}
}
