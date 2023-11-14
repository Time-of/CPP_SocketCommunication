#pragma once

#include <cassert>
#include <iostream>
#include <process.h>
#include <stack>
#include <string>
#include <vector>

// 가장 최신 WinSock 버전
#include <Winsock2.h>
using namespace std;




struct ClientInfo
{
public:
	SOCKET socket;
	bool bIsConnected;
	char id[50];
	HANDLE clientHandle;
	HANDLE listenHandle;

	ClientInfo()
	{
		socket = NULL;
		bIsConnected = false;
		ZeroMemory(&id, sizeof(id));
		clientHandle = NULL;
		listenHandle = NULL;
	}
};

typedef vector<ClientInfo>::iterator ClientInfoIter;


class GameServer
{
public:
	GameServer();
	~GameServer();

	void Wait();
	void Listen(int port);

	static UINT WINAPI ListenThread(LPVOID p);
	static UINT WINAPI ControlThread(LPVOID p);
	

private:
	bool InitializeSocketLayer();
	void CloseSocketLayer();


private:
	int portNum;
	SOCKET serverSocket;
	HANDLE listenHandle;
	HANDLE mainHandle;
	bool bIsRunning;
	SOCKET lastSocket;

	vector<ClientInfo> clientArray;
	stack<ClientInfoIter, vector<ClientInfoIter>> clientPools;
};
