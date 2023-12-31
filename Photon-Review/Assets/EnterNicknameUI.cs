using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;
using UnityEngine.UI;

public class EnterNicknameUI : MonoBehaviour
{
	[SerializeField]
	private TMP_InputField inputField;

	[SerializeField]
	private Button inputNicknameButton;


	private bool bWasNicknameEntered = false;

	private bool bInputFieldActivated = false;



	private void Awake()
	{
		inputNicknameButton.onClick.AddListener(EnterNickname);
		inputField.ActivateInputField();
	}



	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Return))
		{
			if (bWasNicknameEntered)
				EnterChat();
			else
				EnterNickname();
		}
	}



	private void EnterNickname()
	{
		if (inputField.text == string.Empty)
		{
			return;
		}

		if (!NetworkConnectionManager.instance.ConfirmNicknameAndJoinRandomRoom(inputField.text))
		{
			Debug.LogWarning("서버와 연결 실패!");
			return;
		}

		//inputNicknameButton.gameObject.SetActive(false);
		//inputField.gameObject.SetActive(false);

		inputNicknameButton.onClick.RemoveListener(EnterNickname);
		GameObject.Find("Placeholder").GetComponent<TMP_Text>().SetText("채팅 입력");
		inputNicknameButton.onClick.AddListener(EnterChat);

		bWasNicknameEntered = true;
		inputField.text = string.Empty;
		inputField.DeactivateInputField();
		inputField.gameObject.SetActive(false);
		bInputFieldActivated = false;
	}



	private void EnterChat()
	{
		if (bInputFieldActivated)
		{
			inputField.DeactivateInputField();
			inputField.gameObject.SetActive(false);
			bInputFieldActivated = false;
		}
		else
		{
			inputField.gameObject.SetActive(true);
			inputField.ActivateInputField();
			bInputFieldActivated = true;
		}

		if (inputField.text == string.Empty)
		{
			return;
		}

		inputField.text += " ";
		Debug.Log("채팅 보내기: " + inputField.text);

		NetworkConnectionManager.instance.SendChat(inputField.text);
		inputField.text = string.Empty;
	}
}
