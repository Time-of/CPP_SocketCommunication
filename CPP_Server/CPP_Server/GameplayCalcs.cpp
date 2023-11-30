#include "GameplayCalcs.h"
// include�� �� "CPP_Server/GameplayCalcs.h" �� �ؾ� ��

#include <cassert>


GameplayCalcs::GameplayCalcs() noexcept : functionMap(
	{
		{"RPCTakeDamageServer", std::bind(&GameplayCalcs::RPCTakeDamageServer, this, std::placeholders::_1)}
	})
{
	
}


GameplayCalcs& GameplayCalcs::GetInstance()
{
	static GameplayCalcs instance;

	return instance;
}


const CalcResult& GameplayCalcs::InvokeFunction(const std::string& functionName, const std::vector<void*>& args)
{
	CalcResult result = functionMap[functionName](args);

	return result;
}


const CalcResult& GameplayCalcs::RPCTakeDamageServer(const std::vector<void*>& args)
{
	// �� ���¿� �´� �ּҷ� ����ȯ �� �� ��������...
	// ������ȭ�� �Ǿ��ٰ� �����ϰ� ����

	assert(sizeof(args[0]) == sizeof(float));
	assert(sizeof(args[1]) == sizeof(int));

	float damage;
	int defense;

	memcpy(&damage, args[0], sizeof(float));
	memcpy(&defense, args[1], sizeof(int));

	float result = damage - defense;
	CalcResult returnValue;
	returnValue.result = &result;
	returnValue.broadcastFunctionName = std::move(std::string("RPCTakeDamageAll"));

	return returnValue;
}