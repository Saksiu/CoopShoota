using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;



//controller for basic game console to set some game and room generation settings
public class GameConsoleController : SingletonLocal<GameConsoleController>
{
    [SerializeField] private GameObject gameConsoleRootPanel;

    private Toggle[] difficultyToggles;

    private void Start()
    {
        difficultyToggles=GetComponentsInChildren<Toggle>(true);
        Assert.IsTrue(difficultyToggles.Length>0);

        foreach (var difftoggle in difficultyToggles)
        {
            difftoggle.onValueChanged.AddListener(delegate { OnDifficultySelected(difftoggle); });
        }

    }

    void OnDestroy()
    {
        foreach (var difftoggle in difficultyToggles)
        {
            difftoggle.onValueChanged.RemoveAllListeners();
        }
    }

    private void OnDifficultySelected(Toggle changedToggle){
        if(!changedToggle.isOn)
            return;
        print("difficulty selected: "+changedToggle.transform.GetComponentInChildren<Text>().text);
    }
    
    public void Open()
    {
        print("game console opening");
        gameConsoleRootPanel.SetActive(true);
        print("SET GAME CONSOLE ACTIVE");
        //print("IS IT FUCKING OPENED THOUGH?: "+gameConsoleRootPanel.activeSelf);
        PlayerController.localPlayer.switchToUIInput();
        print("DISABLE INPUT");
        PlayerController.localPlayer.disableMovement();
        Cursor.lockState = CursorLockMode.None;
    }

    public void Close()
    {
        print("close game console panel called");
        gameConsoleRootPanel.SetActive(false);
        PlayerController.localPlayer.switchToPlayerInput();
        PlayerController.localPlayer.enableMovement();
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void Toggle()
    {
        if(gameConsoleRootPanel.activeSelf)
            Close();
        else
            Open();
    }
}
