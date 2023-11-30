#include "Serializer.h"

//#include <cassert>
#include <iostream>
#include <string>
#include <codecvt>


bool Serializer::Serialize(const std::vector<void*>& objects, std::vector<byte>& objectTypesNotOut, std::vector<byte>& outSerializedBytes)
{
	if (objects.empty())
	{
		std::cout << "직렬화 실패: 직렬화 할 파라미터들이 아무것도 없음!\n";
		return false;
	}
	else if (objects.size() > objectTypesNotOut.size())
	{
		std::cout << "직렬화 실패: 타입 정보가 부족함!\n";
		return false;
	}

	if (objectTypesNotOut.size() < 8) objectTypesNotOut.push_back(RPCValueType::UNDEFINED);
	
	outSerializedBytes.clear();
	outSerializedBytes.reserve(96);

	union
	{
		uint16 input;
		byte output[2];
	} serializeUint16;

	union
	{
		int input;
		byte output[4];
	} serializeInt;

	union
	{
		float input;
		byte output[4];
	} serializeFloat;

	const int size = objects.size();

	for (int i = 0; i < size; ++i)
	{
		switch (objectTypesNotOut[i])
		{
			case RPCValueType::INT:
			{
				serializeInt.input = *(int*)objects[i];
				outSerializedBytes.insert(outSerializedBytes.end(), serializeInt.output, serializeInt.output + 4);
				break;
			}

			case RPCValueType::FLOAT:
			{
				serializeFloat.input = *(float*)objects[i];
				outSerializedBytes.insert(outSerializedBytes.end(), serializeFloat.output, serializeFloat.output + 4);
				break;
			}

			case RPCValueType::STRING:
			{
				std::string str(*(std::string*)objects[i]);
				serializeUint16.input = static_cast<uint16>(str.length());
				outSerializedBytes.insert(outSerializedBytes.end(), serializeUint16.output, serializeUint16.output + 2);
				auto strBytes = StringToByte(str);
				outSerializedBytes.insert(outSerializedBytes.end(), strBytes.begin(), strBytes.end());
				break;
			}

			case RPCValueType::VEC3:
			{
				Vector3 vector3 = *(Vector3*)objects[i];
				serializeFloat.input = vector3.x;
				outSerializedBytes.insert(outSerializedBytes.end(), serializeFloat.output, serializeFloat.output + 4);
				serializeFloat.input = vector3.y;
				outSerializedBytes.insert(outSerializedBytes.end(), serializeFloat.output, serializeFloat.output + 4);
				serializeFloat.input = vector3.z;
				outSerializedBytes.insert(outSerializedBytes.end(), serializeFloat.output, serializeFloat.output + 4);
				break;
			}

			case RPCValueType::QUAT:
			{
				Quaternion quat = *(Quaternion*)objects[i];
				serializeFloat.input = quat.x;
				outSerializedBytes.insert(outSerializedBytes.end(), serializeFloat.output, serializeFloat.output + 4);
				serializeFloat.input = quat.y;
				outSerializedBytes.insert(outSerializedBytes.end(), serializeFloat.output, serializeFloat.output + 4);
				serializeFloat.input = quat.z;
				outSerializedBytes.insert(outSerializedBytes.end(), serializeFloat.output, serializeFloat.output + 4);
				serializeFloat.input = quat.w;
				outSerializedBytes.insert(outSerializedBytes.end(), serializeFloat.output, serializeFloat.output + 4);
				break;
			}

			default:
			case RPCValueType::UNDEFINED:
			{
				std::cout << "직렬화 실패: 직렬화 중 UNDEFINED나 미정의 타입을 만남!\n";
				return false;
			}
		}
	}

	std::cout << "서버에서 객체 직렬화 완료! size: " << outSerializedBytes.size() << "\n";

	return true;
}


std::vector<void*>& Serializer::Deserialize(byte bytesToDeserialize[], byte typeInfo[8])
{
	int size = sizeof(bytesToDeserialize) / sizeof(byte);
	std::vector<byte> serializedObjects(bytesToDeserialize, bytesToDeserialize + size);

	std::vector<void*> result;
	result.reserve(8);
	
	size = serializedObjects.size();
	
	std::cout << "역직렬화 시작! byte size: " << size << "\n";

	int typeIndex = 0;
	for (int head = 0; head < size and typeIndex < 8;)
	{
		std::cout << "head: " << head << ", typeIndex: " << typeIndex << "\n";
		std::cout << "typeInfo[typeIndex]: " << typeInfo[typeIndex] << "\n";

		switch (typeInfo[typeIndex])
		{
			case RPCValueType::INT:
			{
				int num = ByteToInt(serializedObjects, head);
				result.push_back(&num);
				head += 4;
				break;
			}

			case RPCValueType::FLOAT:
			{
				float num = ByteToFloat(serializedObjects, head);
				result.push_back(&num);
				head += 4;
				break;
			}

			case RPCValueType::STRING:
			{
				uint16 length = ByteToUint16(serializedObjects, head);
				head += 2;
				std::string str = ByteToString(serializedObjects, head, length);
				result.push_back(&str);
				head += length;
				break;
			}

			case RPCValueType::VEC3:
			{
				Vector3 vec3 = ByteToVector3(serializedObjects, head);
				result.push_back(&vec3);
				head += 12;
				break;
			}

			case RPCValueType::QUAT:
			{
				Quaternion quat = ByteToQuaternion(serializedObjects, head);
				result.push_back(&quat);
				head += 16;
				break;
			}

			default:
			{
				std::cout << "역직렬화 실패: 역직렬화 중 UNDEFINED나 미정의 타입을 만남!\n";
				std::vector<void*> tmp;
				return tmp;
			}

			case RPCValueType::UNDEFINED:
			{
				std::cout << "역직렬화 중 UNDEFINED를 만나 바로 return됨!\n";
				return result;
			}
		}

		++typeIndex;
	}

	return result;
}


int Serializer::ByteToInt(const std::vector<byte>& bytes, int startIndex)
{
	return bytes[startIndex] | (bytes[startIndex + 1] << 8) | (bytes[startIndex + 2] << 16) | (bytes[startIndex + 3] << 24);
}


float Serializer::ByteToFloat(const std::vector<byte>& bytes, int startIndex)
{
	int toInt = ByteToInt(bytes, startIndex);
	return *(float*)&toInt;
}


uint16 Serializer::ByteToUint16(const std::vector<byte>& bytes, int startIndex)
{
	return static_cast<uint16>(bytes[startIndex] | (bytes[startIndex + 1] << 8));
}


Vector3 Serializer::ByteToVector3(const std::vector<byte>& bytes, int startIndex)
{
	Vector3 vector3;
	vector3.x = ByteToFloat(bytes, startIndex);
	vector3.y = ByteToFloat(bytes, startIndex + 4);
	vector3.z = ByteToFloat(bytes, startIndex + 8);

	return vector3;
}


Quaternion Serializer::ByteToQuaternion(const std::vector<byte>& bytes, int startIndex)
{
	Quaternion quat;
	quat.x = ByteToFloat(bytes, startIndex);
	quat.y = ByteToFloat(bytes, startIndex + 4);
	quat.z = ByteToFloat(bytes, startIndex + 8);
	quat.w = ByteToFloat(bytes, startIndex + 12);

	return quat;
}


// 인터넷 코드
std::vector<byte> Serializer::StringToByte(const std::string& str)
{
	std::wstring_convert<std::codecvt_utf8<char32_t>, char32_t> converter;
	std::u32string u32str = converter.from_bytes(str);

	return std::vector<byte>(u32str.begin(), u32str.end());
}


// 인터넷 코드 가져와서 수정
std::string Serializer::ByteToString(const std::vector<byte>& bytes, int startIndex, int length)
{
	std::u32string u32str(bytes.begin() + startIndex, bytes.begin() + startIndex + length);
	std::wstring_convert<std::codecvt_utf8<char32_t>, char32_t> converter;

	return converter.to_bytes(u32str);
}
