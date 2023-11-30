#pragma once

#include <vector>
#include "../CVSP.h"


class Serializer
{
public:
	// 직렬화할 객체 배열, 직렬화 객체 타입 배열, 직렬화 아웃 배열
	// C#으로 했던 거랑 동일하게 수행하려 했으나, C++은 타입 리플렉션을 지원하지 않음
	// 따라서 타입 정보를 함께 넣어줘야 함...
	// 타입 정보를 넣어주기 때문에 type output은 필요 없을 듯
	// 리틀 인디언 방식!
	// @param objectTypesNotOut objects의 타입 정보를 입력! objects.size() == objectsTypesNotOut.size() 이고 objects.size() < 8이면 objectsTypesNotOut.push_back(RPCValueTypes::UNDEFINED);
	static bool Serialize(const std::vector<void*>& objects, std::vector<byte>& objectTypesNotOut, std::vector<byte>& outSerializedBytes);

	// @param serializedObjects 유니티에서 받아왔을 때 넣기 편한 형태라 byte[] 채택
	// @param typeInfos 해석하기 위한 타입 정보
	static std::vector<void*>& Deserialize(byte bytesToDeserialize[], byte typeInfo[8]);


private:
	static int ByteToInt(const std::vector<byte>& bytes, int startIndex);
	static float ByteToFloat(const std::vector<byte>& bytes, int startIndex);
	static uint16 ByteToUint16(const std::vector<byte>& bytes, int startIndex);
	static struct Vector3 ByteToVector3(const std::vector<byte>& bytes, int startIndex);
	static struct Quaternion ByteToQuaternion(const std::vector<byte>& bytes, int startIndex);


public:
	// 인터넷 코드
	static std::vector<byte> StringToByte(const std::string& str);
	// 인터넷 코드 가져와서 수정
	static std::string ByteToString(const std::vector<byte>& bytes, int startIndex, int length);
};

struct Vector3
{
	float x, y, z;
};

struct Quaternion
{
	float x, y, z, w;
};