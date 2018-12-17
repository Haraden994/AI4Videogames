using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{

    public GameObject player;       //Public variable to store a reference to the player game object

    private Vector3 offset;         //Private variable to store the offset distance between the player and camera

    private float maxCameraDistance = 170f;
    private float minCameraDistance = 30f;
    private float currentCameraDistance = 45f;
    private float sensitivity = -100f;

    // Use this for initialization
    void Start()
    {
        //Calculate and store the offset value by getting the distance between the player's position and camera's position.
        offset = transform.position - player.transform.position;
    }

    // LateUpdate is called after Update each frame
    void LateUpdate()
    {
        // Set the position of the camera's transform to be the same as the player's, but offset by the calculated offset distance.
        currentCameraDistance += Input.GetAxis("Mouse ScrollWheel") * sensitivity;
        currentCameraDistance = Mathf.Clamp(currentCameraDistance, minCameraDistance, maxCameraDistance);
        offset.y = currentCameraDistance;
        transform.position = player.transform.position + offset;
    }
}

