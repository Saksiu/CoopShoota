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

    [SerializeField] private uint magazineSize = 30;
    [SerializeField] private float reloadTime = 2.0f;

    private uint INTERNAL_ammoLeft;
    public uint AmmoLeft
    {
        get => INTERNAL_ammoLeft;
        set
        {
            INTERNAL_ammoLeft = value;
            if(IsOwner)
                UIManager.Instance.updateAmmoLeft(value);
        }
    }
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

    [NonSerialized] public Transform gunNozzle;
    [NonSerialized] public Transform gunAnchor;
    
    private bool ShootInput => InputManager.PlayerInput.Player.Shoot.ReadValue<float>() > 0.0f;
    private bool canShoot = true;
    private Coroutine shootCoroutineHandle;
    
    private static readonly int Shoot = Animator.StringToHash("Shoot");
    private static readonly int ShootTrigger= Animator.StringToHash("ShootTrigger");

    private void Update()
    {
        if(!IsOwner) return;
        if(isControlledByPlayer){ 
            transform.SetPositionAndRotation(gunAnchor.position, gunAnchor.rotation);
        } //! we shouldnt rely on framerate to determine shooting position, but I'm fine with this for now

        
        //transform.Rotate(0, 90, 0);
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

            RequestFireServerRpc(gunNozzle.up,gunNozzlePos);
            FireBullet(gunNozzle.up, gunNozzlePos);
            gunAnimator.SetTrigger(ShootTrigger);
            cameraShakeEffect.GenerateImpulse();
        }
    }
    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        if(parentNetworkObject==null) return;
        if(parentNetworkObject.GetComponent<PlayerController>()!=null){
            isControlledByPlayer=true;
            gunAnchor=parentNetworkObject.GetComponent<PlayerController>().playerCamera.transform.GetChild(2);;
            gunNozzle=parentNetworkObject.GetComponent<PlayerController>().CamNozzle;
            AmmoLeft=magazineSize; //! just for now though
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
                
                RequestFireServerRpc(gunNozzle.up,gunNozzlePos);
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

    private void rotateGunTowards(Vector3 dir)
    {
        transform.rotation = Quaternion.LookRotation(Vector3.forward, dir);
        //transform.Rotate(0, 0, 90);
    }

    [ServerRpc]
    private void RequestFireServerRpc(Vector3 dir,Vector3 initPos)
    {
        AmmoLeft--;
        FireBullet(dir,initPos);
        FireBulletClientRpc(dir,initPos);
    }

    [ClientRpc]
    private void FireBulletClientRpc(Vector3 dir,Vector3 initPos)
    {
        if(!IsOwner) FireBullet(dir,initPos);
    }

    private void FireBullet(Vector3 dir,Vector3 initPos)
    { 
        switch (bulletDistribution)
            {
                case BulletDistribution.BurstRandom:
                    for(int j = 0; j < bulletsPerShot; j++)
                    {
                        Vector3 randomizedDir = dir + new Vector3(UnityEngine.Random.Range(-bulletSpread, bulletSpread), UnityEngine.Random.Range(-bulletSpread, bulletSpread), 0);
                        Instantiate(bulletPrefab, initPos, gunNozzle.rotation).GetComponent<BulletController>().Launch(randomizedDir);
                    }
                    break;
                case BulletDistribution.BurstEqual:
                    break;
                case BulletDistribution.Series:
                    break;
                default: case BulletDistribution.Single:
                    if(bulletsPerShot>1)
                        Debug.LogError("BulletDistribution.Single used with bulletsPerShot > 1, firing only 1");
                    Instantiate(bulletPrefab, initPos, gunNozzle.rotation).GetComponent<BulletController>().Launch(dir);
                    break;
            }
        
        gunAnimator.SetTrigger(ShootTrigger);
        shootEffect.Play();
    }
    private Coroutine reloadCoroutineHandle;
    public void Reload()
    {
        if(AmmoLeft==magazineSize){
            reloadCoroutineHandle=null;
            return;
        }
        
        if(reloadCoroutineHandle!=null)
            StopCoroutine(reloadCoroutineHandle);
        reloadCoroutineHandle=StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        canShoot = false;
        yield return new WaitForSeconds(reloadTime);
        AmmoLeft = magazineSize;
        canShoot = true;
    }
    
}

[Serializable]
public enum BulletDistribution
{
    Single,
    BurstRandom,
    BurstEqual,
    Series
}
