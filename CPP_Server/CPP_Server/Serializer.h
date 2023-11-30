#pragma once

#include <vector>
#include "../CVSP.h"


class Serializer
{
public:
	// ����ȭ�� ��ü �迭, ����ȭ ��ü Ÿ�� �迭, ����ȭ �ƿ� �迭
	// C#���� �ߴ� �Ŷ� �����ϰ� �����Ϸ� ������, C++�� Ÿ�� ���÷����� �������� ����
	// ���� Ÿ�� ������ �Բ� �־���� ��...
	// Ÿ�� ������ �־��ֱ� ������ type output�� �ʿ� ���� ��
	// ��Ʋ �ε�� ���!
	// @param objectTypesNotOut objects�� Ÿ�� ������ �Է�! objects.size() == objectsTypesNotOut.size() �̰� objects.size() < 8�̸� objectsTypesNotOut.push_back(RPCValueTypes::UNDEFINED);
	static bool Serialize(const std::vector<void*>& objects, std::vector<byte>& objectTypesNotOut, std::vector<byte>& outSerializedBytes);

	// @param serializedObjects ����Ƽ���� �޾ƿ��� �� �ֱ� ���� ���¶� byte[] ä��
	// @param typeInfos �ؼ��ϱ� ���� Ÿ�� ����
	static std::vector<void*>& Deserialize(byte bytesToDeserialize[], byte typeInfo[8]);


private:
	static int ByteToInt(const std::vector<byte>& bytes, int startIndex);
	static float ByteToFloat(const std::vector<byte>& bytes, int startIndex);
	static uint16 ByteToUint16(const std::vector<byte>& bytes, int startIndex);
	static struct Vector3 ByteToVector3(const std::vector<byte>& bytes, int startIndex);
	static struct Quaternion ByteToQuaternion(const std::vector<byte>& bytes, int startIndex);


public:
	// ���ͳ� �ڵ�
	static std::vector<byte> StringToByte(const std::string& str);
	// ���ͳ� �ڵ� �����ͼ� ����
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