using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Camera Camera;
    [SerializeField] private AudioListener AudioListener;
    
    public float cameraSensitivity=10;

    [Tooltip("More=more rotation freedom")]
    [SerializeField] private float verticalCameraClamp = 40;
    
    public void Init(bool isOwner)
    {
        if(isOwner) return;
        
        Camera.enabled = false;
        AudioListener.enabled = false;
        enabled = false;
    }

    private float verticalAngle=0.0f;

    private void Update()
    {
        // Camera rotation on the X axis (vertical)
        verticalAngle -= Input.GetAxis("Mouse Y") * cameraSensitivity; // Subtract to invert the vertical input
        verticalAngle = Mathf.Clamp(verticalAngle, -verticalCameraClamp, verticalCameraClamp); // Clamp the vertical angle within the limits

        // Apply rotation to the camera using Quaternion to avoid gimbal lock issues
        Camera.transform.localRotation = Quaternion.Euler(verticalAngle, 0, 0);
    }
}
