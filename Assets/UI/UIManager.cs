using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : SingletonLocal<UIManager>
{
    [SerializeField] private TextMeshProUGUI HPText;
    [SerializeField] private TextMeshProUGUI promptText;

    private GameObject promptPanel;
    
    
    private void Start()
    {
        promptPanel = promptText.transform.parent.gameObject;
        promptPanel.SetActive(false);
    }
    public void updateDisplayedHP(int newHP)
    {
        print("UIManager received new HP: "+newHP+"!");
        HPText.text = "HP: " + newHP;
    }
    
    public void showPrompt(string prompt)
    {
        promptText.text = "[E] "+prompt;
        promptPanel.SetActive(true);
    }
    public void hidePrompt()
    {
        promptPanel.SetActive(false);
    }
}
