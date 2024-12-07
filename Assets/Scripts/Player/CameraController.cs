﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public EInput inputType;

    public MenuManager menuManager;

    //if there is no specified focus, this one will be applied
    public Transform defaultFocus;

    //transform to follow around
    public Transform focus;

    //values of the current camera rotation around the focus
    public float yaw = 0.0f;
    public float pitch = 0.0f;

    public float range = 5.0f;

    //allow the editor to change this value within reason
    public float sensitivity = 0.0f;

    public KeyCode connect = KeyCode.Mouse0;
    public KeyCode release = KeyCode.Escape;

    public TouchData touchData;

    private float collisionRadius = 0.2f;
    public LayerMask blockingMask;

    public bool isThirdPerson = false;
    public bool fuzz = false;

    void Start ()
    {
		
	}

    public void Poll()
    {
        if (menuManager != null)
        {
            //confine the mouse to the middle of the screen
            if (Input.GetKeyDown(connect))
            {
                if (!menuManager.isActive)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }

            //unlock the mouse from the middle of the screen
            if (Input.GetKeyDown(release))
            {
                if (!menuManager.isActive)
                {
                    Cursor.lockState = CursorLockMode.None;
                    menuManager.ActivateMenu();
                }
            }
        }

        if (inputType == EInput.DESKTOP)
        {
            //mouse control block
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                yaw += Input.GetAxisRaw("Mouse X") * sensitivity;
                pitch += Input.GetAxisRaw("Mouse Y") * sensitivity;

                //wrap around rX
                if (yaw < Mathf.PI * -2.0f)
                {
                    yaw += Mathf.PI * 2.0f;
                }
                if (yaw > Mathf.PI * 2.0f)
                {
                    yaw -= Mathf.PI * 2.0f;
                }

                pitch = Mathf.Clamp(pitch, -Mathf.PI * 0.5f + 0.001f, Mathf.PI * 0.5f - 0.001f);
            }
        }
        else if (inputType == EInput.MOBILE)
        {
            MobileMenuManager mobileMenuManager = menuManager as MobileMenuManager;

            //find touch in top half of screen
            ManagedTouch cameraDragData = null;     

            RectTransform referenceTransform = mobileMenuManager.jumpButton.GetComponent<RectTransform>();
            float jumpButtonY = referenceTransform.position.y;
            float jumpButtonYRatio = jumpButtonY / Screen.height;

            int count = touchData.touchList.Count;

            for (int i = 0; i < count; i++)
            {
                ManagedTouch item = touchData.touchList[i];

                if (mobileMenuManager.currentOrientation == MobileMenuManager.EOrientation.VERTICAL)
                {
                    if (item.start.y >= Screen.height * jumpButtonYRatio)
                    {
                        cameraDragData = item;
                        break;
                    }
                }
                else if (mobileMenuManager.currentOrientation == MobileMenuManager.EOrientation.HORIZONTAL)
                {
                    if (item.start.x >= Screen.width * 0.5f)
                    {
                        cameraDragData = item;
                        break;
                    }
                }
            }

            if (cameraDragData != null)
            {
                float dpi = Screen.dpi;

                yaw += (cameraDragData.touch.deltaPosition.x * sensitivity) / dpi;
                pitch += (cameraDragData.touch.deltaPosition.y * sensitivity) / dpi;
            }

            //wrap around rX
            if (yaw < Mathf.PI * -2.0f)
            {
                yaw += Mathf.PI * 2.0f;
            }
            if (yaw > Mathf.PI * 2.0f)
            {
                yaw -= Mathf.PI * 2.0f;
            }

            pitch = Mathf.Clamp(pitch, -Mathf.PI * 0.5f + 0.001f, Mathf.PI * 0.5f - 0.001f);
        }

        if (fuzz)
        {
            yaw = Random.Range(-Mathf.PI, Mathf.PI);
            pitch = Random.Range(-Mathf.PI * 0.5f + 0.001f, Mathf.PI * 0.5f - 0.001f);
        }

        Vector3 f = MathExtension.DirectionFromYawPitch(yaw, pitch);

        if (focus == null)
        {
            focus = defaultFocus;
        }

        RaycastHit collInfo;

        Vector3 target = focus.position + f * range;

        //readjust the camera position if there is a collider in the way
        if (Physics.SphereCast(focus.position, collisionRadius, (target - focus.position).normalized, out collInfo, range, blockingMask.value))
        {
            transform.position = collInfo.point + collInfo.normal * collisionRadius;
        }
        else
        {
            transform.position = focus.position + f * range;
        }

        transform.rotation = Quaternion.LookRotation(-f);
    }

    public void TogglePerspective()
    {
        isThirdPerson = !isThirdPerson;

        if (menuManager.client.proxy != null)
        {
            MeshRenderer[] meshRenderers = menuManager.client.proxy.animator.GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                if (isThirdPerson)
                {
                    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                }
                else
                {
                    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
            }
        }

        if (isThirdPerson)
        {
            if (menuManager.perspectiveButtonText != null)
            {
                menuManager.perspectiveButtonText.text = "First-Person";
            }

            range = 2.0f;
        }
        else
        {
            if (menuManager.perspectiveButtonText != null)
            {
                menuManager.perspectiveButtonText.text = "Third-Person";
            }

            range = 0.01f;
        }
    }
}