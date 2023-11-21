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

		if (!PhotonManager.instance.ConfirmNicknameAndJoinRandomRoom(inputField.text))
		{
			return;
		}

		//inputNicknameButton.gameObject.SetActive(false);
		//inputField.gameObject.SetActive(false);

		inputNicknameButton.onClick.RemoveListener(EnterNickname);

		inputNicknameButton.onClick.AddListener(EnterChat);
		bWasNicknameEntered = true;
		inputField.text = string.Empty;
		inputField.DeactivateInputField();
	}



	private void EnterChat()
	{
		if (bInputFieldActivated)
		{
			inputField.DeactivateInputField();
			bInputFieldActivated = false;
		}
		else
		{
			inputField.ActivateInputField();
			bInputFieldActivated = true;
		}

		if (inputField.text == string.Empty)
		{
			return;
		}

		PhotonManager.instance.SendChat(inputField.text);
		inputField.text = string.Empty;
	}
}
