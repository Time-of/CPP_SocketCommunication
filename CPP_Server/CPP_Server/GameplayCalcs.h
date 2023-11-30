#pragma once

#include "../CVSP.h"
#include <functional>
#include <map>
#include <string>
#include <vector>

struct CalcResult;

/**
* @author �̼���
* @brief �����÷��̿� �ʿ��� ����� ������ Ŭ����
* @details �ַ�� Ž�� â���� ��Ŭ���ϰ� Ŭ���� ���� �������� ���� �ϳ� �� ���� ���������...
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
	// ��� ����
	std::vector<void*> result;
	// ������ �Լ� �̸�
	std::string broadcastFunctionName;
	// ��� Ÿ�� ����
	std::vector<byte> typeInfos;
};
