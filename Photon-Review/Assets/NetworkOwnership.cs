using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;


/// <author>�̼���</author>
/// <summary>
/// ��Ʈ��ũ ���ʽ� ������ ���� ������Ʈ Ŭ����.
/// id�� NetworkConnectionManager�� playerId�� �����ϴٸ� bIsMine�� true, �ƴ϶�� false�� ����.
/// </summary>
public class NetworkOwnership : MonoBehaviour
{
	public int id = -1;
	public bool bIsMine { get { return id == NetworkConnectionManager.instance.playerId; } private set { } }


	public static NetworkOwnership Instantiate(string resourceName, Vector3 position, Quaternion rotation)
	{
		Debug.Log("NetworkOwnership Instantiate �õ�!");
		NetworkConnectionManager.instance.SendObjectSpawnInfo(resourceName, position, rotation);
		Debug.Log("NetworkOwnership ���� ��� ���� �Ϸ�!");

		// ���θ� �ٷ� ����, �ٸ� ������� �޽����� �޾� ����. (���� �ʿ��� ���� ���ο��Դ� ���� ����� ���� ����)
		// �׷��� Ÿ�ο��Դ� [��Ʈ��ũ ������, ť�� �и� �۾� ��, ���ҽ� Load] ��ŭ�� ������尡 �����.
		// @todo ���߿� ���� �����ϴٸ� �����غ���.
		return _InternalInstantiate(resourceName, position, rotation, NetworkConnectionManager.instance.playerId);
	}

	
	public static NetworkOwnership _InternalInstantiate(string resourceName, Vector3 position, Quaternion rotation, int ownerId)
	{
		var prefab = Resources.Load<NetworkOwnership>(resourceName);

		if (prefab == null)
		{
			Debug.LogError("��Ʈ��ũ ����: ������ ���ҽ� [" + resourceName + "] �ε� ����! NetworkOwnership Ŭ������ ���� ���� �ƴұ��?");
			return null;
		}

		NetworkOwnership ownershipObject = GameObject.Instantiate<NetworkOwnership>(prefab, position, rotation);

		ownershipObject.id = ownerId;

		return ownershipObject;
	}
}
