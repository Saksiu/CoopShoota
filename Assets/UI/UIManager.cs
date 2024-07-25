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
        print("showing death screen on player "+player.playerName.Value);
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
}
