using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Netcode;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;



//controller for basic game console to set some game and room generation settings
public class GameConsoleController : SingletonNetwork<GameConsoleController>
{
    [SerializeField] private GameObject gameConsoleRootPanel;
    [SerializeField] private GameObject diffTogglesSubPanel;
    //[SerializeField] private EventInteractable consoleInteractableTrigger;

    [SerializeField] private GameObject diffTogglePrefab;
    [SerializeField] List<Difficulty> difficulties;
    
    public NetworkVariable<uint> selectedDifficulty;

    private Dictionary<uint, Toggle> difficultyToggles = new Dictionary<uint, Toggle>();

    public override void OnNetworkSpawn()
    { 
        Toggle[] previewToggles = diffTogglesSubPanel.GetComponentsInChildren<Toggle>();
        foreach (var toggle in previewToggles)
            Destroy(toggle.gameObject);

        Assert.IsTrue(difficulties.Count>0);


        foreach (var difficulty in difficulties){
            Toggle newToggle = Instantiate(diffTogglePrefab, diffTogglesSubPanel.transform).GetComponent<Toggle>();

            difficultyToggles.Add(difficulty.diffID,newToggle);

            newToggle.GetComponentInChildren<Text>().text = difficulty.diffID+". "+difficulty.diffName;
            newToggle.group = diffTogglesSubPanel.GetComponent<ToggleGroup>();

            newToggle.onValueChanged.AddListener(delegate { OnDifficultyToggleSelected(newToggle); });
            newToggle.interactable = NetworkManager.IsHost;
        }
            

        selectedDifficulty.OnValueChanged += setDifficulty;


        //! TEMP
        if(IsHost)
            selectedDifficulty.Value=difficulties[0].diffID;

        base.OnNetworkSpawn();
    }


    /*private void OnPlayerSpawned(PlayerController player) //onplayerspawned is only called on server only, so checking
    {
        isSpawnedOnHost = player.IsHost; 
        print("isSpawnedOnHost: "+isSpawnedOnHost);
        foreach (var difftoggle in difficultyToggles)
            difftoggle.interactable = isSpawnedOnHost;
    }*/

    public override void OnDestroy()
    {
        //PlayerController.OnPlayerSpawned -= OnPlayerSpawned;

        foreach (var difftoggle in difficultyToggles.Values)
            difftoggle.onValueChanged.RemoveAllListeners();

        selectedDifficulty.OnValueChanged -= setDifficulty;

        base.OnDestroy();
    }

    private void OnDifficultyToggleSelected(Toggle changedToggle){
        if(!IsHost) return;
        if(!changedToggle.isOn) return;

        //print("difficulty selected: "+changedToggle.transform.GetComponentInChildren<Text>().text);

        uint toggleDiffId=getDifficultyIdFromToggle(changedToggle);
        
        OnDifficultyChangedServerRpc(toggleDiffId);
    }

    private uint getDifficultyIdFromToggle(Toggle toggle){
        //print("getDifficultyIdFromToggle: "+toggle.transform.GetComponentInChildren<Text>().text.Split(' ')[0].Substring(0,1));
        return uint.Parse(toggle.transform.GetComponentInChildren<Text>().text.Split(' ')[0].Substring(0,1));
    }
    
    [ServerRpc]
    private void OnDifficultyChangedServerRpc(uint difficulty)
    {
        selectedDifficulty.Value = difficulty;

    }

    private void setDifficulty(uint prev, uint curr)
    {
        // if(IsHost) return;
        if(prev==curr) return; //prevent infinite loops

        print("setting diff on "+NetworkManager.LocalClientId+" prev: "+prev+" curr: "+curr);
        difficultyToggles[curr].isOn = true;
    }

    public void Open()
    {
        //print("game console opening");
        gameConsoleRootPanel.SetActive(true);

        //need to update this only after the panel is active, known unity bug
        //! HOW can you fuck up toggles?
        difficultyToggles[selectedDifficulty.Value].isOn = true; 

        PlayerController.localPlayer.switchToUIInput();
        //print("DISABLE INPUT");
        PlayerController.localPlayer.disableMovement();
        Cursor.lockState = CursorLockMode.None;
    }

    public void Close()
    {
       // print("close game console panel called");
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

[Serializable]
public struct Difficulty{
    public uint diffID;
    public string diffName;
}