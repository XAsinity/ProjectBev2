using UnityEngine;

public class InputManager : MonoBehaviour
{
    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            // Handle forward movement
            Debug.Log("Move Forward");
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            // Handle backward movement
            Debug.Log("Move Backward");
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            // Handle left movement
            Debug.Log("Move Left");
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            // Handle right movement
            Debug.Log("Move Right");
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Handle jump
            Debug.Log("Jump");
        }
    }
}