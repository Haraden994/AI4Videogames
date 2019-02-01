using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisitorController : MonoBehaviour
{
    // Start is called before the first frame update
    void Update()
    {
        CharacterController cc = gameObject.GetComponent<CharacterController>();

        float rotationSpeed = 10.0f;
        float rotationY = Input.GetAxis ("Mouse X") * rotationSpeed;
        float rotationX = Input.GetAxis("Mouse Y") * rotationSpeed;
        transform.Rotate (-rotationX, rotationY , 0);
       
        float movementSpeed = 40.0f;
        float dt = Time.deltaTime;
        float dy =  0;
        if(Input.GetKey(KeyCode.Space))
        {
            dy = movementSpeed * dt;
        }
        if(Input.GetKey(KeyCode.LeftShift))
        {
            dy -= movementSpeed * dt;
        }
        float dx = Input.GetAxis("Horizontal") * dt * movementSpeed;
        float dz = Input.GetAxis("Vertical") * dt * movementSpeed;
       
        cc.Move(transform.TransformDirection(new Vector3(dx, dy,dz))  );
    }
}
