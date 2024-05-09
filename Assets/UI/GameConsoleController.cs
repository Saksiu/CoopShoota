using System.Collections;
using System.Collections.Generic;
using UnityEngine;



//controller for basic game console to set some game and room generation settings
public class GameConsoleController : SingletonLocal<GameConsoleController>
{
    [SerializeField] private GameObject gameConsoleRootPanel;
    
    public void Open()
    {
        gameConsoleRootPanel.SetActive(true);
    }

    public void Close()
    {
        gameConsoleRootPanel.SetActive(false);
    }

    public void Toggle()
    {
        if(gameConsoleRootPanel.activeSelf)
            Close();
        else
            Open();
    }
}
