using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GunController : NetworkBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float ShootCooldown;
    [SerializeField] private Transform gunNozzle;
    
    [SerializeField] private Animator gunAnimator;

    [SerializeField] private ParticleSystem shootEffect;
    
    private PlayerController owningPlayer;
    
    private bool ShootInput => Input.GetMouseButton(0);
    private bool canShoot = true;
    private Coroutine shootCoroutineHandle;
    
    private static readonly int Shoot = Animator.StringToHash("Shoot");
    private static readonly int ShootTrigger= Animator.StringToHash("ShootTrigger");
    

    private void FixedUpdate()
    {
        if(!IsOwner) return;

        //Vector2 dir = getDirTowardsMouse();
        //rotateGunTowards(dir);


        if (ShootInput&&canShoot)
        {
            canShoot = false;
            Invoke(nameof(enableShootingAfterCooldown),ShootCooldown);
            
            //dir = getDirTowardsMouse();
            Vector3 gunNozzlePos = gunNozzle.position;
            
            RequestFireServerRpc(gunNozzle.up,gunNozzlePos);
            FireBullet(gunNozzle.up, gunNozzlePos);
            gunAnimator.SetTrigger(ShootTrigger);
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

    private void Start()
    {
        owningPlayer = GetComponentInParent<PlayerController>();
    }

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
        shootEffect.Play();
    }
    
    /*private Vector2 getDirTowardsMouse()
    {
        //Vector2 mousePos = CameraController.Instance.Camera.ScreenToWorldPoint(Input.mousePosition);
        //return (mousePos - (Vector2)transform.position).normalized;
        return Vector2.zero;
    }*/
}
