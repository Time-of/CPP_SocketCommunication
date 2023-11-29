#include "GameServer.h"

#include <algorithm>

#include <sys/types.h>
#include <sys/stat.h>
#include <mmsystem.h>
#include <thread>

#include "CVSP.h"
using namespace std;


GameServer::GameServer()
{
	portNum = 0;
	bIsRunning = true;

	InitializeSocketLayer();

	clientArray = vector<ClientInfo>(100, ClientInfo());

	for (auto iter = clientArray.begin(); iter != clientArray.end(); ++iter)
	{
		clientPools.push(iter);
	}
}


GameServer::~GameServer()
{
	bIsRunning = false;

	// ���� �ʿ��Ѱ�?
	//while (!clientPools.empty()) clientPools.pop();

	// ���⼱ ���� �ݺ��ڸ� ������� �ʾƵ� �� ��?
	for (auto& info : clientArray)
	{
		if (info.bIsConnected)
		{
			WaitForSingleObject(info.clientHandle, INFINITE);
			CloseHandle(info.clientHandle);
			closesocket(info.socket);
			info.bIsConnected = false;
		}
	}

	closesocket(serverSocket);
	CloseSocketLayer();
}


void GameServer::Listen(int port)
{
	portNum = port;

	thread listenThread(&GameServer::ListenThread, this);
	listenThread.join();

	/*
	listenHandle = (HANDLE)_beginthreadex(nullptr, 0, GameServer::ListenThread, this, 0, nullptr);

	if (listenHandle == nullptr)
	{
		cout << "���� ������ ����!\n";
		return;
	}
	*/
}


UINT __stdcall GameServer::ControlThread(LPVOID p)
{
	GameServer* server = (GameServer*)p;

	SOCKET connectSocket = server->lastSocket;
	assert(connectSocket != SOCKET_ERROR);
	vector<ClientInfo>& clientArray = server->clientArray;

	auto iter = find_if(clientArray.begin(), clientArray.end(), [&connectSocket](ClientInfo& info) -> bool
		{
			return info.socket == connectSocket;
		});

	assert(iter != clientArray.end());

	int id = iter - clientArray.begin() + 1;
	id = 100 - id;

	cout << "Ŭ���̾�Ʈ " << id << " ����!\n";

	int recvLen = 0;


	// select ������
	fd_set fdReadSet, fdErrorSet, fdMaster;
	struct timeval tvs;

	FD_ZERO(&fdMaster);
	FD_SET(connectSocket, &fdMaster);
	tvs.tv_sec = 0;
	tvs.tv_usec = 100;


	// CVSP
	uint8 cmd;
	uint8 option;

	char extraPacket[CVSP_STANDARD_PAYLOAD_LENGTH - sizeof(CVSPHeader)];


	while (iter->bIsConnected)
	{
		// select�� �̿��� �񵿱� ���
		fdReadSet = fdMaster;
		fdErrorSet = fdMaster;

		select((int)connectSocket + 1, &fdReadSet, nullptr, &fdErrorSet, &tvs);

		// read ���� �� recv�� ȣ���ϵ��� �����Ͽ�, Blocking ���� ȿ��
		if (FD_ISSET(connectSocket, &fdReadSet))
		{
			ZeroMemory(extraPacket, sizeof(extraPacket));

			recvLen = RecvCVSP((uint32)connectSocket, &cmd, &option, extraPacket, sizeof(extraPacket));

			if (recvLen == SOCKET_ERROR)
			{
				cout << "recv ����: " << GetLastError() << "\n";
				break;
			}

			switch (cmd)
			{

			// ä�� ��û
			case CVSP_CHATTINGREQ:
			{
				//messageBuffer[recvLen] = '\0';

				char clientInfoText[CVSP_STANDARD_PAYLOAD_LENGTH - sizeof(CVSPHeader)] = "[";
				strcat_s(clientInfoText, sizeof(clientInfoText), to_string(id).c_str());
				strcat_s(clientInfoText, sizeof(clientInfoText), "]: ");
				strcat_s(clientInfoText, sizeof(clientInfoText), extraPacket);
				clientInfoText[strlen(clientInfoText)] = '\0';

				cmd = CVSP_CHATTINGRES;
				option = CVSP_SUCCESS;

				for (auto infoIter = clientArray.begin(); infoIter != clientArray.end(); ++infoIter)
				{
					if (!infoIter->bIsConnected) continue;

					int clientId = 100 - (infoIter - clientArray.begin() + 1);
					cout << clientId << "���� �޽��� ����\n";

					int sendResult = SendCVSP((uint32)infoIter->socket, cmd, option, clientInfoText, static_cast<uint16>(strlen(clientInfoText)));
					if (sendResult < 0) cout << "Send ����!\n";
				}

				cout << "Ŭ���̾�Ʈ" << clientInfoText << "\n";
				break;
			}


			// �̵� ���� ��û (TransformInfo)
			case CVSP_OPERATIONREQ:
			{
				// ���� ���� �ٸ� Ŭ���̾�Ʈ ��ο��� �Ѹ���
				for (auto infoIter = clientArray.begin(); infoIter != clientArray.end(); ++infoIter)
				{
					if (!infoIter->bIsConnected) continue;
					int clientId = 100 - (infoIter - clientArray.begin() + 1);
					if (clientId == id) continue;

					int sendResult = SendCVSP((uint32)infoIter->socket, CVSP_MONITORINGMSG, CVSP_SUCCESS, extraPacket, static_cast<uint16>(sizeof(TransformInfo)));
					//cout << "TransformInfo�� " << id << " ���Լ� " << clientId << " ���� ���� " << ((sendResult >= 0) ? "����!\n" : "����!\n");
				}
				break;
			}


			// RPC ��û
			case CVSP_RPC_REQ:
			{

				switch (option)
				{
				case CVSP_RPCTARGET_ALL:
				{
					for (auto infoIter = clientArray.begin(); infoIter != clientArray.end(); ++infoIter)
					{
						if (!infoIter->bIsConnected) continue;

						int sendResult = SendCVSP((uint32)infoIter->socket, CVSP_RPC_RES, CVSP_SUCCESS, extraPacket, static_cast<uint16>(sizeof(RPCInfo)));
						if (sendResult < 0) cout << "RPC ���� ����!\n";
					}
					cout << "RPC ��û ��ο��� ���� �Ϸ�!\n";
					break;
				}
				default:
				{
					cout << "RPC ��û�� option�� ��ȿ���� ����!\n";
					break;
				}
				}

				break;
			}


			// Join ��û
			case CVSP_JOINREQ:
			{
				cout << "Ŭ���̾�Ʈ " << id << "���� Join ��û! �г���: " << extraPacket << "\n";

				// ��û�� ���ο��� int Ÿ������ id ����
				if (SendCVSP((uint32)iter->socket, CVSP_JOINRES, CVSP_SUCCESS, (int*)&id, static_cast<uint16>(sizeof(int))) < 0)
				{
					cout << "Ŭ���̾�Ʈ " << id << "���� Join ��û �����ϱ� ���� Send�ϴ� �� ���� �߻�!\n";
				}

				// ��� ������ �������, ���� �������� ������ �˷��ֱ�
				for (auto infoIter = clientArray.begin(); infoIter != clientArray.end(); ++infoIter)
				{
					if (!infoIter->bIsConnected) continue;
					int clientId = 100 - (infoIter - clientArray.begin() + 1);
					if (clientId == id) continue;

					int sendResult = SendCVSP((uint32)iter->socket, CVSP_JOINRES, CVSP_NEW_USER, (int*)&clientId, static_cast<uint16>(sizeof(int)));
					cout << "Ŭ���̾�Ʈ " << id << "���� " << clientId << "�� Join ���� ���� " << ((sendResult >= 0) ? "����!\n" : "����...\n");
				}
				break;
			}


			// ������Ʈ ���� ��û
			case CVSP_SPAWN_OBJECT_REQ:
			{
				cout << "Ŭ���̾�Ʈ " << id << "���� Object Spawn ��û!\n";

				// **��û�ڸ� �����ϰ�** Ŭ���̾�Ʈ�� ���� �Ѹ���
				// ��û�ڴ� ������ �Ｎ�ؼ� ���ÿ� �����ϴ� ���. (Ŭ���̾�Ʈ ��)
				for (auto infoIter = clientArray.begin(); infoIter != clientArray.end(); ++infoIter)
				{
					if (!infoIter->bIsConnected) continue;
					int clientId = 100 - (infoIter - clientArray.begin() + 1);
					if (clientId == id) continue;

					int sendResult = SendCVSP((uint32)infoIter->socket, CVSP_SPAWN_OBJECT_RES, CVSP_SUCCESS, extraPacket, static_cast<uint16>(sizeof(ObjectSpawnInfo)));
					cout << "Ŭ���̾�Ʈ " << clientId << "���� Spawn ��û ����" << ((sendResult >= 0) ? " ����!\n" : "�ϱ� ���� Send�ϴ� �� ���� �߻�!\n");
				}
				break;
			}


			// ���� ��û
			case CVSP_LEAVEREQ:
			{
				cout << "���� ������ �����մϴ�!\n";
				iter->bIsConnected = false;
				break;
			}
			}
		}
	}

	iter->bIsConnected = false;
	closesocket(connectSocket);
	server->clientPools.push(iter);
	cout << "Ŭ���̾�Ʈ " << id << " ���� �����!!: " << GetLastError() << "\n";

	return 0;
}


UINT __stdcall GameServer::ListenThread(LPVOID p)
{
	GameServer* server = (GameServer*)p;
	SOCKET& serverSocket = server->serverSocket;
	SOCKADDR_IN service;

	// ���� ����
	serverSocket = socket(PF_INET, SOCK_STREAM, 0);
	if (serverSocket == INVALID_SOCKET)
	{
		cout << "���� ���� ���� ����\n";
		WSACleanup();

		return -1;
	}


	{
		ZeroMemory(&service, sizeof(service));
		service.sin_family = AF_INET;
		service.sin_addr.s_addr = htonl(INADDR_ANY);
		service.sin_port = htons(server->portNum);
	}


	if (bind(serverSocket, (SOCKADDR*)&service, sizeof(service)) == SOCKET_ERROR)
	{
		cout << "���ε� ����\n";
		closesocket(serverSocket);
		return -1;
	}


	// ��αװ� 5
	if (listen(serverSocket, 5) == SOCKET_ERROR)
	{
		cout << "���� ����\n";
		closesocket(serverSocket);
		return -1;
	}


	cout << "���� ����: ���� ��� ��\n";


	while (server->bIsRunning)
	{
		SOCKET connectSocket;
		connectSocket = accept(serverSocket, nullptr, nullptr);

		if (connectSocket > 0)
		{
			// Ŀ�ؼ� Ǯ���� üũ
			if (server->clientPools.empty())
			{
				closesocket(connectSocket);

				continue;
			}

			auto iter = server->clientPools.top();
			server->clientPools.pop();

			iter->socket = connectSocket;
			server->lastSocket = connectSocket;

			iter->bIsConnected = true;
			iter->clientHandle = (HANDLE)_beginthreadex(nullptr, 0, GameServer::ControlThread, server, 0, nullptr);
			//iter->clientThread = thread([&iter, &server]() -> UINT { return GameServer::ControlThread(server); } );
			//iter->clientThread.join();
		}

		Sleep(50); // �����ս��� ���� �κ�
	}


	return 0;
}


bool GameServer::InitializeSocketLayer()
{
	WORD version = MAKEWORD(2, 2);
	
	WSADATA wsaData;

	// WinSock ���� ���� ���̺귯��(DLL)�� �ʱ�ȭ�ϰ� WinSock ������ �� �䱸������ ������Ű���� Ȯ��
	if (WSAStartup(version, &wsaData) != 0)
	{
		cout << "WSAStartup���� ���� �߻�!\n";
		return false;
	}

	return true;
}


void GameServer::CloseSocketLayer()
{
	// ���� ��� ��� ������
	if (WSACleanup() == SOCKET_ERROR)
	{
		cout << "WSACleanup ����, ����: " << WSAGetLastError();
	}
}
