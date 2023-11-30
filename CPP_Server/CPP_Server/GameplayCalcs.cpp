#include "GameplayCalcs.h"
// include할 때 "CPP_Server/GameplayCalcs.h" 로 해야 함


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


const CalcResult GameplayCalcs::InvokeFunction(const std::string& functionName, const std::vector<void*>& args)
{
	CalcResult result = functionMap[functionName](args);

	return result;
}


const CalcResult GameplayCalcs::RPCTakeDamageServer(const std::vector<void*>& args)
{
	// 각 형태에 맞는 주소로 형변환 후 값 가져오기...
	// 역직렬화가 되었다고 가정하고 수행

	assert(sizeof(args[0]) == sizeof(float));
	assert(sizeof(args[1]) == sizeof(int));

	float damage = *(float*)args[0];
	int defense = *(int*)args[1];

	//memcpy(&damage, args[0], sizeof(float));
	//memcpy(&defense, args[1], sizeof(int));

	float result = damage - defense;
	CalcResult returnValue;
	returnValue.result.push_back(&result);
	returnValue.broadcastFunctionName = std::move(std::string("RPCTakeDamageAll"));
	returnValue.typeInfos.push_back(RPCValueType::FLOAT);

	return returnValue;
}
