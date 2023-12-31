#pragma once

#include <cassert>
#include <cstdlib>
#include <fcntl.h>
#include <iostream>
#include <malloc.h>
#include <string>


#define WIN32 1

#ifdef WIN32
#pragma comment(lib, "ws2_32.lib")
#include <winsock.h>
#include <io.h>
#endif

#ifndef WIN32
#include <sys/socket.h>
#include <unistd.h>
#endif

#define CVSP_MONITORING_MESSAGE 700
#define CVSP_MONITORING_LOAD 701

// 버전 및 포트
#define CVSP_VER 0x01
#define CVSP_PORT 9000

// 페이로드 크기
#define CVSP_STANDARD_PAYLOAD_LENGTH 4096

// 커맨드
#define CVSP_JOINREQ 0x01
#define CVSP_JOINRES 0x02
#define CVSP_CHATTINGREQ 0x03
#define CVSP_CHATTINGRES 0x04
#define CVSP_OPERATIONREQ 0x05
#define CVSP_MONITORINGMSG 0x06
#define CVSP_LEAVEREQ 0x07

// 옵션
#define CVSP_SUCCESS 0x01
#define CVSP_FAIL 0x02


#ifdef WIN32
typedef unsigned char u_char;
typedef unsigned short u_short;
typedef unsigned int u_int;
typedef unsigned long u_long;
#endif

using uint8 = u_char;
using uint16 = u_short;
using uint32 = u_int;
using ulong = u_long;

// 4바이트 단위로 맞춰주기
struct CVSPHeader
{
	uint8 cmd;
	uint8 option;
	uint16 packetLength;
};

int SendCVSP(uint32 socketfd, uint8 cmd, uint8 option, void* payload, uint16 len);
int RecvCVSP(uint32 socketfd, uint8* cmd, uint8* option, void* payload, uint16 len);