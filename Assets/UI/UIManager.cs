using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : SingletonLocal<UIManager>, PlayerInputGenerated.IUIActions
{
    [SerializeField] private TextMeshProUGUI HPText;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI promptText;


    private GameObject promptPanel;

    private Coroutine promptCoroutine;
    
    
    private void Start()
    {
        promptPanel = promptText.transform.parent.gameObject;
        promptPanel.SetActive(false);

    }

    public void onPlayerSpawn(PlayerController spawnedPlayer)
    {
        print("subscribing to player death event on "+spawnedPlayer.playerName.Value);
        spawnedPlayer.healthComponent.OnDeathAction += DisplayDeathScreen;
    }

    private void DisplayDeathScreen(PlayerController player){
        showPromptFor("You died! You will respawn in "+GameMaster.Instance.respawnTime+" seconds.",GameMaster.Instance.respawnTime);
        //print("showing death screen on player "+player.playerName.Value);
    }
    public void updateAmmoLeft(uint newAmmo)
    {
        ammoText.text = "Ammo: " + newAmmo;
    }

    private void OnDestroy()
    {
        PlayerController.localPlayer.healthComponent.OnDeathAction -= DisplayDeathScreen;
    }
    public void updateDisplayedHP(int newHP)
    {
        //print("UIManager received new HP: "+newHP+"!");
        HPText.text = "HP: " + newHP;
    }

    public void showPromptFor(string prompt,float duration)
    {
        if(promptCoroutine!=null){
            StopCoroutine(promptCoroutine);
            hidePrompt();
            promptCoroutine=null;
        }
            
        if(promptPanel.activeSelf)
            hidePrompt();

        promptCoroutine=StartCoroutine(showPromptForCoroutine(duration, prompt));
    }

    private IEnumerator showPromptForCoroutine(float duration, string prompt)
    {
        showPrompt(prompt);
        yield return new WaitForSeconds(duration);
        if(prompt==promptText.text)
            hidePrompt();
    }    
    public void showPrompt(string prompt)
    {
        promptText.text = prompt;
        promptPanel.SetActive(true);
    }
    public void hidePrompt()
    {
        promptPanel.SetActive(false);
        promptText.text = "";
    }

    public void OnExitInteractableMenu(InputAction.CallbackContext context)
    {
        print("exit interactable menu called from "+context.action.actionMap.name);
        if(!context.performed) return;
        
        GameConsoleController.Instance.Close();

    }  

    #region unused input events
    public void OnNavigate(InputAction.CallbackContext context)
    {
        //throw new NotImplementedException();
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
        //throw new NotImplementedException();
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        //throw new NotImplementedException();
    }

    public void OnPoint(InputAction.CallbackContext context)
    {
        //throw new NotImplementedException();
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        //throw new NotImplementedException();
    }

    public void OnScrollWheel(InputAction.CallbackContext context)
    {
        //throw new NotImplementedException();
    }

    public void OnMiddleClick(InputAction.CallbackContext context)
    {
        //throw new NotImplementedException();
    }

    public void OnRightClick(InputAction.CallbackContext context)
    {
        //throw new NotImplementedException();
    }

    public void OnTrackedDevicePosition(InputAction.CallbackContext context)
    {
        //throw new NotImplementedException();
    }

    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context)
    {
        //throw new NotImplementedException();
    }
    #endregion
}
