using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeLookCam : MonoBehaviour
{
    [field: SerializeField] private Transform Head { get; set; }
    [field: SerializeField] private float LookSensitivityX { get; set; } = 60f;
    [field: SerializeField] private float LookSensitivityY { get; set; } = 33.75f;
    [field: SerializeField] private Rigidbody Rigidbody { get; set; }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            HandleRotation();
        }
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector3 velVector = new Vector3(0, Rigidbody.velocity.y, 0);

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            velVector += transform.forward;
        }
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            velVector -= transform.forward;
        }
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            velVector -= transform.right;
        }
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            velVector += transform.right;
        }

        Rigidbody.velocity = velVector;
    }

    private void HandleRotation()
    {
        transform.Rotate(new Vector3(0, InputHandler.MouseDeltaScreenPercentage.x * LookSensitivityX, 0));
        Head.transform.Rotate(new Vector3(-InputHandler.MouseDeltaScreenPercentage.y * LookSensitivityY, 0, 0));
        var signedAngle = Vector3.SignedAngle(transform.forward, Head.forward, transform.right);

        if (signedAngle > 70)
        {
            Head.transform.localEulerAngles = new Vector3(70, 0, 0);
        }

        if (signedAngle < -70)
        {
            Head.transform.localEulerAngles = new Vector3(-70, 0, 0);
        }
    }
}
