using System.Collections;
using Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class GunController : NetworkBehaviour
{

    [SerializeField] public string gunName;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float ShootCooldown;
    public Transform gunNozzle;
    public Transform gunAnchor;
    
    [SerializeField] private Animator gunAnimator;

    [SerializeField] private ParticleSystem shootEffect;

    [SerializeField] private CinemachineImpulseSource cameraShakeEffect;
    
    //private PlayerController owningPlayer;
    public bool isControlledByPlayer = false;
    
    private bool ShootInput => InputManager.PlayerInput.Player.Shoot.ReadValue<float>() > 0.0f;
    private bool canShoot = true;
    private Coroutine shootCoroutineHandle;
    
    private static readonly int Shoot = Animator.StringToHash("Shoot");
    private static readonly int ShootTrigger= Animator.StringToHash("ShootTrigger");

    private void Update()
    {
        if(!IsOwner) return;
        if(isControlledByPlayer){
            
        }

        
        //transform.Rotate(0, 90, 0);
    }


    private void FixedUpdate()
    {
        if(!IsOwner) return;
        if(!isControlledByPlayer) return;

        //Vector2 dir = getDirTowardsMouse();
        //rotateGunTowards(dir);

        transform.SetPositionAndRotation(gunAnchor.position, gunAnchor.rotation);

        print("gun update called, gunanchor pos: "+gunAnchor.position+" gun pos: "+transform.position);
        
        if (ShootInput&&canShoot)
        {
            canShoot = false;
            Invoke(nameof(enableShootingAfterCooldown),ShootCooldown);
            
            //dir = getDirTowardsMouse();
            Vector3 gunNozzlePos = gunNozzle.position;
            
            RequestFireServerRpc(gunNozzle.up,gunNozzlePos);
            FireBullet(gunNozzle.up, gunNozzlePos);
            gunAnimator.SetTrigger(ShootTrigger);
            cameraShakeEffect.GenerateImpulse();
        }

        /*if (ShootInput&&shootCoroutineHandle==null)
        {
            shootCoroutineHandle = StartCoroutine(ShootCoroutine());
        }
        else if (!ShootInput&&shootCoroutineHandle!=null)
        {
            StopCoroutine(shootCoroutineHandle);
            Invoke(nameof(enableShootingAfterCooldown),ShootCooldown);
            shootCoroutineHandle = null;
        }*/
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

    /*private void Start()
    {
        owningPlayer = GetComponentInParent<PlayerController>();
    }*/

    private void rotateGunTowards(Vector3 dir)
    {
        transform.rotation = Quaternion.LookRotation(Vector3.forward, dir);
        //transform.Rotate(0, 0, 90);
    }

    [ServerRpc]
    private void RequestFireServerRpc(Vector3 dir,Vector3 initPos)
    {
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
        //print(bulletPrefab==null);
        //Quaternion bulletDir = Quaternion.LookRotation(Vector3.forward, dir);
        Instantiate(bulletPrefab, initPos, gunNozzle.rotation).GetComponent<BulletController>().Launch(dir);
        gunAnimator.SetTrigger(ShootTrigger);
        shootEffect.Play();
    }
    
    /*private Vector2 getDirTowardsMouse()
    {
        //Vector2 mousePos = CameraController.Instance.Camera.ScreenToWorldPoint(Input.mousePosition);
        //return (mousePos - (Vector2)transform.position).normalized;
        return Vector2.zero;
    }*/
}
