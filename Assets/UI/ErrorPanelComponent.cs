using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ErrorPanelComponent : MonoBehaviour
{
    [SerializeField] private CanvasGroup dialogPanelCanvasGroup;
    [SerializeField] private TextMeshProUGUI errorText;

    public void DisplayError(string error){
        errorText.text = error;
        dialogPanelCanvasGroup.alpha = 1;
        dialogPanelCanvasGroup.interactable = true;
        dialogPanelCanvasGroup.blocksRaycasts = true;
        dialogPanelCanvasGroup.ignoreParentGroups = true;
        MainMenuManager.Instance.GetMainMenuCanvasGroup().interactable = false;
    }

    public void handleOkButtonPressed(){
        dialogPanelCanvasGroup.alpha = 0;
        dialogPanelCanvasGroup.interactable = false;
        dialogPanelCanvasGroup.blocksRaycasts = false;
        dialogPanelCanvasGroup.ignoreParentGroups = false;
        MainMenuManager.Instance.GetMainMenuCanvasGroup().interactable = true;
    }
}
