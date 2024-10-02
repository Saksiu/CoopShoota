using System;
using System.Collections;
using Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GunController : NetworkBehaviour
{

    [SerializeField] public string gunName;

    [Header("Shooting Config")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float ShootCooldown;
    [SerializeField] private BulletDistribution bulletDistribution;

    //[SerializeField] private uint magazineSize = 30;
    //[SerializeField] private float reloadTime = 2.0f;


    private int Internal_Ammoleft=0;
    public int AmmoLeft{
        get=>Internal_Ammoleft;
        set{
            Internal_Ammoleft=value;
            onAmmoLeftValueChanged(value);
        }
    }

    public int initialAmmo=100;

    [SerializeField] private uint bulletsPerShot = 1;

    [Tooltip("This is only applied for BurstRandom distribution type")]
    [SerializeField] private float bulletSpread = 0.1f;
    

    [Header("Effects")]
    [SerializeField] private Animator gunAnimator;

    [SerializeField] private ParticleSystem shootEffect;

    [SerializeField] private CinemachineImpulseSource cameraShakeEffect;
    
    //private PlayerController owningPlayer;

    //? could be replaced by the networkobject owning it, but works so no touchy
    public bool isControlledByPlayer = false; 

    [SerializeField] private Transform visualNozzle;
    [NonSerialized] public Transform gunNozzle;
    [NonSerialized] public Transform gunAnchor;
    
    private bool ShootInput => InputManager.PlayerInput.Player.Shoot.ReadValue<float>() > 0.0f;
    private bool canShoot = true;
    private Coroutine shootCoroutineHandle;
    
    private static readonly int Shoot = Animator.StringToHash("Shoot");
    private static readonly int ShootTrigger= Animator.StringToHash("ShootTrigger");


    /*public override void OnNetworkSpawn()
    {
        
        AmmoLeft.OnValueChanged += onAmmoLeftValueChanged;
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        AmmoLeft.OnValueChanged -= onAmmoLeftValueChanged;
        base.OnNetworkDespawn();
    }*/

    private void Update()
    {
        if(!IsOwner) return;
        if(isControlledByPlayer){ 
            transform.SetPositionAndRotation(gunAnchor.position, gunAnchor.rotation);
        } //! we shouldnt rely on framerate to determine shooting position, but I'm fine with this for now

        
        //transform.Rotate(0, 90, 0);
    }
    private void onAmmoLeftValueChanged(int newAmmo)
    {
        if (!IsOwner) return;
        print($"ammo left changed to {newAmmo} on {NetworkManager.LocalClientId} is owner? {IsOwner}");
        HUDManager.Instance.updateAmmoLeft(newAmmo);
    }
    public void resetAmmoCount(){
        GunsManager.Instance.setAmmoServerRpc(NetworkManager.LocalClientId,gunName,initialAmmo);
    }


    private void FixedUpdate()
    {
        if(!IsOwner) return;
        if(!isControlledByPlayer) return;

        
        if (ShootInput&&canShoot&&AmmoLeft>0)
        {
            canShoot = false;
            Invoke(nameof(enableShootingAfterCooldown),ShootCooldown);
            
            //dir = getDirTowardsMouse();
            Vector3 gunNozzlePos = gunNozzle.position;
            Vector3 nozzleDir=gunNozzle.up;

            RequestFireServerRpc(NetworkManager.LocalClientId, gunNozzle.up,gunNozzlePos);
            if(AmmoLeft<=0){
                return;
            }
            FireBullet(gunNozzle.up, gunNozzlePos);
            gunAnimator.SetTrigger(ShootTrigger);
            cameraShakeEffect.GenerateImpulse();
        }
    }

    /*public void onAmmoChanged(int newAmmo){
        print("onammochangedClientRpc called "+NetworkManager.LocalClientId+" is controlled by player? "+isControlledByPlayer);
        if(!isControlledByPlayer) return;
        AmmoLeft=newAmmo;
    }*/

    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        if(parentNetworkObject==null) return;
        if(parentNetworkObject.TryGetComponent(out PlayerController player)){
            isControlledByPlayer=true;
            gunAnchor=player.playerCamera.transform.GetChild(2);
            gunNozzle=player.CamNozzle;

            print("gun on parent changed: owner? "+IsOwner+" id: "+NetworkManager.LocalClientId);
            if(IsOwner){
                GunsManager.Instance.OnGunEquippedServerRpc(NetworkManager.LocalClientId,gunName);
            }
                
                
        }else if(parentNetworkObject.GetComponent<GunsManager>()!=null){
            isControlledByPlayer=false;
            gunAnchor=null;
            gunNozzle=null;
        }


        base.OnNetworkObjectParentChanged(parentNetworkObject);
    }

    private IEnumerator ShootCoroutine()
    {
        //Vector2 dir;
        Vector3 gunNozzlePos;
        while (true)
        {
            if (ShootInput)
            {
                canShoot = false;
                //dir = getDirTowardsMouse();
                gunNozzlePos = gunNozzle.position;
                
                RequestFireServerRpc(NetworkManager.LocalClientId,gunNozzle.up,gunNozzlePos);
                if(AmmoLeft<=0){
                    yield return null;
                }   
                FireBullet(gunNozzle.up, gunNozzlePos);
                yield return new WaitForSeconds(ShootCooldown);
                canShoot = true;
            }
            yield return null;
        }
    } 
    private void enableShootingAfterCooldown(){
        canShoot = true;
    }

    [ServerRpc]
    private void RequestFireServerRpc(ulong senderClientId, Vector3 dir,Vector3 initPos)
    {
        if(GunsManager.Instance.getAmmoLeft(senderClientId,gunName)<=0){
            print("requested shot with no ammo left, aborting");
            return;
        }

        GunsManager.Instance.addAmmoServerRpc(senderClientId,gunName,-1);
        FireBullet(dir,initPos);
        FireBulletClientRpc(dir,initPos);
    }

    [ClientRpc]
    private void FireBulletClientRpc(Vector3 dir,Vector3 initPos)
    {
        if(AmmoLeft<=0){
            return;
        }
        FireBullet(dir,initPos);
    }

    private void FireBullet(Vector3 dir,Vector3 initPos)
    { 
        switch (bulletDistribution)
            {
                case BulletDistribution.BurstRandom: //! this means the actual bullets fly differently on the server and each client. bad, very bad
                    for(int j = 0; j < bulletsPerShot; j++)
                    {
                        Vector3 randomizedDir = dir + new Vector3(UnityEngine.Random.Range(-bulletSpread, bulletSpread), UnityEngine.Random.Range(-bulletSpread, bulletSpread), 0);
                        Instantiate(bulletPrefab, initPos, gunNozzle.rotation).GetComponent<BulletController>().Launch(randomizedDir,visualNozzle.position);
                    }
                    break;
                case BulletDistribution.BurstEqual:
                    break;
                case BulletDistribution.Series:
                    break;
                default: case BulletDistribution.Single:
                    if(bulletsPerShot>1)
                        Debug.LogError("BulletDistribution.Single used with bulletsPerShot > 1, firing only 1");
                    Instantiate(bulletPrefab, initPos, gunNozzle.rotation).GetComponent<BulletController>().Launch(dir,visualNozzle.position);
                    break;
            }
        
        gunAnimator.SetTrigger(ShootTrigger);
        shootEffect.Play();
    }
    /*private Coroutine reloadCoroutineHandle;

    private bool isReloading=false;

    public void Reload()
    {
        //print("reload called on "+NetworkManager.LocalClientId);
        if(AmmoLeft==magazineSize){
            reloadCoroutineHandle=null;
            return;
        }
        if(isReloading) return;

        if(reloadCoroutineHandle!=null)
            StopCoroutine(reloadCoroutineHandle);
        reloadCoroutineHandle=StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        isReloading=true;
        canShoot = false;
        HUDManager.Instance.ShowReload(reloadTime);
        yield return new WaitForSeconds(reloadTime);
        AmmoLeft = magazineSize;
        canShoot = true;
        isReloading=false;
    }*/
    
}

[Serializable]
public enum BulletDistribution
{
    Single,
    BurstRandom,
    BurstEqual,
    Series
}
