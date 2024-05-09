using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera POVCamera;
    [SerializeField] private AudioListener AudioListener;
    
    public float cameraSensitivity=10;
    public float cameraLerpSpeed=50;

    [Tooltip("More=more rotation freedom")]
    [SerializeField] private float verticalCameraClamp = 40;
    
    public void Init(bool isOwner)
    {
        if(isOwner) return;
        
        POVCamera.enabled = false;
        AudioListener.enabled = false;
        enabled = false;
    }

    private float verticalAngle=0.0f;
    private float targetVerticalAngle=0.0f;
    
    public void moveCamera(float vertical)
    {
        targetVerticalAngle-= vertical * cameraSensitivity;
        targetVerticalAngle= Mathf.Clamp(targetVerticalAngle, -verticalCameraClamp, verticalCameraClamp); // Clamp the vertical angle within the limits
        
        verticalAngle=Mathf.Lerp(verticalAngle,targetVerticalAngle,Time.deltaTime*cameraLerpSpeed);
        //verticalAngle -= vertical * cameraSensitivity; // Subtract to invert the vertical input
        //verticalAngle = Mathf.Clamp(verticalAngle, -verticalCameraClamp, verticalCameraClamp); // Clamp the vertical angle within the limits

        // Apply rotation to the camera using Quaternion to avoid gimbal lock issues
        transform.localRotation = Quaternion.Euler(verticalAngle, 0, 0);
    }
}
