using Unity.Netcode;
using UnityEngine;

public class GunController : NetworkBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform gunNozzle;
    
    private PlayerController owningPlayer;
    private void FixedUpdate()
    {
        if(!IsOwner) return;

        Vector2 dir = getDirTowardsMouse();
        rotateGunTowards(dir);
        
        if (Input.GetMouseButton(0))
        {
            RequestFireServerRpc(dir);
            FireBullet(dir);
        }
            
    }

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
    private void RequestFireServerRpc(Vector2 dir)
    {
        FireBulletClientRpc(dir);
    }

    [ClientRpc]
    private void FireBulletClientRpc(Vector2 dir)
    {
        if(!IsOwner) FireBullet(dir);
    }

    private void FireBullet(Vector2 dir)
    { 
        //print(bulletPrefab==null);
        //Quaternion bulletDir = Quaternion.LookRotation(Vector3.forward, dir);
        Instantiate(bulletPrefab, gunNozzle.position, gunNozzle.rotation).GetComponent<BulletController>().Launch(dir);
    }
    
    private Vector2 getDirTowardsMouse()
    {
        Vector2 mousePos = CameraController.Instance.Camera.ScreenToWorldPoint(Input.mousePosition);
        return (mousePos - (Vector2)transform.position).normalized;
    }
}
