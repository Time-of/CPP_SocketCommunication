#pragma once

#include "../CVSP.h"
#include <functional>
#include <map>
#include <string>
#include <vector>

struct CalcResult;

/**
* @author 이성수
* @brief 게임플레이에 필요한 계산을 수행할 클래스
* @details 솔루션 탐색 창에서 우클릭하고 클래스 생성 눌렀더니 폴더 하나 더 들어가서 만들어졌네...
* @since 2023-11-30
*/
class GameplayCalcs
{
private:
	explicit GameplayCalcs() noexcept;
	explicit GameplayCalcs(const GameplayCalcs&) = delete;
	GameplayCalcs operator=(const GameplayCalcs&) = delete;


public:
	~GameplayCalcs() = default;

	static GameplayCalcs& GetInstance();

	const CalcResult InvokeFunction(const std::string& functionName, const std::vector<void*>& args);


private:
	const CalcResult RPCTakeDamageServer(const std::vector<void*>& args);


	std::map<std::string, std::function<const CalcResult&(const std::vector<void*>&)>> functionMap;
};



struct CalcResult
{
	// 결과 벡터
	std::vector<void*> result;
	// 전파할 함수 이름
	std::string broadcastFunctionName;
	// 결과 타입 정보
	std::vector<byte> typeInfos;
};
