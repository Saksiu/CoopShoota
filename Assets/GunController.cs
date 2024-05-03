using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GunController : NetworkBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float ShootCooldown;
    [SerializeField] private Transform gunNozzle;
    
    private PlayerController owningPlayer;
    
    private bool ShootInput => Input.GetMouseButton(0);
    private bool canShoot = true;
    private Coroutine shootCoroutineHandle;
    
    private void FixedUpdate()
    {
        if(!IsOwner) return;

        Vector2 dir = getDirTowardsMouse();
        rotateGunTowards(dir);


        if (ShootInput&&canShoot)
        {
            canShoot = false;
            Invoke(nameof(enableShootingAfterCooldown),ShootCooldown);
            
            dir = getDirTowardsMouse();
            Vector2 gunNozzlePos = gunNozzle.position;
            
            RequestFireServerRpc(getDirTowardsMouse(),gunNozzlePos);
            FireBullet(getDirTowardsMouse(), gunNozzlePos);
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
        Vector2 dir;
        Vector2 gunNozzlePos;
        while (true)
        {
            if (ShootInput)
            {
                canShoot = false;
                dir = getDirTowardsMouse();
                gunNozzlePos = gunNozzle.position;
                
                RequestFireServerRpc(getDirTowardsMouse(),gunNozzlePos);
                FireBullet(getDirTowardsMouse(), gunNozzlePos);
                yield return new WaitForSeconds(ShootCooldown);
                canShoot = true;
            }
            yield return null;
        }
    } 
    private void enableShootingAfterCooldown()=>canShoot = true;

    private void Start()
    {
        owningPlayer = GetComponentInParent<PlayerController>();
    }

    private void rotateGunTowards(Vector2 dir)
    {
        transform.rotation = Quaternion.LookRotation(Vector3.forward, dir);
        //transform.Rotate(0, 0, 90);
    }

    [ServerRpc]
    private void RequestFireServerRpc(Vector2 dir,Vector2 initPos)
    {
        FireBullet(dir,initPos);
        FireBulletClientRpc(dir,initPos);
    }

    [ClientRpc]
    private void FireBulletClientRpc(Vector2 dir,Vector2 initPos)
    {
        if(!IsOwner) FireBullet(dir,initPos);
    }

    private void FireBullet(Vector2 dir,Vector2 initPos)
    { 
        //print(bulletPrefab==null);
        //Quaternion bulletDir = Quaternion.LookRotation(Vector3.forward, dir);
        Instantiate(bulletPrefab, initPos, gunNozzle.rotation).GetComponent<BulletController>().Launch(dir);
    }
    
    private Vector2 getDirTowardsMouse()
    {
        Vector2 mousePos = CameraController.Instance.Camera.ScreenToWorldPoint(Input.mousePosition);
        return (mousePos - (Vector2)transform.position).normalized;
    }
}
