
// ������ ����ϱ� ���� ���̺귯���� ����
#pragma comment(lib, "ws2_32")

#include "GameServer.h"
#include "CVSP.h"

int main(int argc, char* argv)
{
	system("mode con:cols=70 lines=20");

	GameServer server;
	server.Listen(5004);
	server.Wait();

	return 0;
}