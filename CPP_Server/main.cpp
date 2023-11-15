
// 소켓을 사용하기 위해 라이브러리를 참조
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