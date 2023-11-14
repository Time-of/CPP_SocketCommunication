
#pragma comment(lib, "ws2_32")

#pragma warning(disable:4996)

#include <iostream>
#include <cassert>
#include <string>
#include <vector>
#include <process.h>

#include <Winsock2.h>

#include "CVSP.h"

using namespace std;


bool bIsRunning = false;
SOCKET* socketPtr = nullptr;


unsigned __stdcall Receive(void* p)
{
	//SOCKET serverSocket = (SOCKET)p;
	SOCKET serverSocket = *socketPtr;
	assert(socketPtr != nullptr and serverSocket != SOCKET_ERROR);
	// messageBuffer[120];
	cout << "서버로부터 메시지를 받기 시작합니다!\n";


	// CVSP
	uint8 cmd;
	uint8 option;
	int length;
	char extraPacket[CVSP_STANDARD_PAYLOAD_LENGTH - sizeof(CVSPHeader)];


	// 리시브 루프 (스레드로 실행)
	while (bIsRunning)
	{
		ZeroMemory(extraPacket, sizeof(extraPacket));
		//int length = recv(serverSocket, messageBuffer, sizeof(messageBuffer) - 1, 0);
		length = RecvCVSP((uint32)serverSocket, &cmd, &option, extraPacket, sizeof(extraPacket));

		if (length == SOCKET_ERROR)
		{
			cout << "recv 오류" << GetLastError() << "\n";
			bIsRunning = false;
			break;
		}

		//messageBuffer[length] = '\0';


		switch (cmd)
		{
			case CVSP_CHATTINGRES:
			{
				cout << extraPacket << "\n";
				break;
			}
		}
	}

	return 0;
}


int main(int argc, char* argv)
{
	system("mode con:cols=70 lines=20");

	WSADATA wsaData;

	SOCKET serverSocket;
	SOCKADDR_IN serverAddr;
	int portNum = 5004;

	if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
	{
		cout << "WSAStartup에서 오류 발생!\n";
		return -1;
	}


	serverSocket = socket(PF_INET, SOCK_STREAM, 0);
	if (serverSocket == INVALID_SOCKET)
	{
		cout << "서버 소켓 생성 실패\n";
		return -1;
	}


	{
		ZeroMemory(&serverAddr, sizeof(serverAddr));
		serverAddr.sin_family = AF_INET;
		serverAddr.sin_port = htons(portNum);
		serverAddr.sin_addr.s_addr = inet_addr("127.0.0.1");
	}


	if (connect(serverSocket, (SOCKADDR*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR)
	{
		cout << "연결 오류\n";
		return -1;
	}


	cout << "연결 성공\n";
	bIsRunning = true;


	char messageBuffer[100];
	socketPtr = &serverSocket;


	uint8 cmd;
	uint8 option;
	int len;

	
	HANDLE recvThreadHandle = (HANDLE)_beginthreadex(nullptr, 0, Receive, &serverSocket, 0, nullptr);
	assert(recvThreadHandle != nullptr);

	//ResumeThread(recvThreadHandle);
	cout << "메세지를 입력하세요: \n";

	// 메인 루프
	while (bIsRunning)
	{
		ZeroMemory(messageBuffer, sizeof(messageBuffer));
		
		gets_s(messageBuffer, sizeof(messageBuffer));

		//send(serverSocket, messageBuffer, strlen(messageBuffer), 0);

		if (strcmp(messageBuffer, "exit") == 0)
		{
			bIsRunning = false;
			cmd = CVSP_LEAVEREQ;
			option = CVSP_SUCCESS;
		}
		else
		{
			cmd = CVSP_CHATTINGREQ;
			option = CVSP_SUCCESS;
		}

		int sendResult = SendCVSP((uint32)serverSocket, cmd, option, messageBuffer, strlen(messageBuffer));
		if (sendResult < 0) cout << "Send 오류!\n";
	}


	cout << "소켓 연결 종료\n";
	WaitForSingleObject(recvThreadHandle, INFINITE);
	CloseHandle(recvThreadHandle);
	closesocket(serverSocket);


	// 윈속 사용 통신 끝내기
	if (WSACleanup() == SOCKET_ERROR)
	{
		cout << "WSACleanup 실패, 원인: " << WSAGetLastError();
		return -1;
	}

	return 0;
}