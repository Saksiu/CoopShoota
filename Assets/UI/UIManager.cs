using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : SingletonLocal<UIManager>
{
    [SerializeField] private TextMeshProUGUI HPText;
    public void updateDisplayedHP(int newHP)
    {
        HPText.text = "HP: " + newHP;
    }
}
