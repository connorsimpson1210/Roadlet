﻿using UnityEngine;

public class EnterExitCar : MonoBehaviour
{
    [Header("Runtime References")]
    public PlayerController player;      // set by PlayerController when you press E
    public CameraScript cameraFollow; // your camera follow script
    public Transform carTransform; // the root of your car (Prometeo)

    private GameObject currentCar;
    private bool isInCar = false;

    void Awake()
    {
        // // find the car's driving/controller script once
        
        // if (carController == null)
        //     Debug.LogError("EnterExitCar: no CarPhysicsController found on " + carTransform.name);

        // // start with car _not_ drivable
        // carController.enabled = false;
    }

    void Update()
    {
        // only listen for “F” while you're in the car
        if (isInCar && Input.GetKeyDown(KeyCode.F))
        {
            ExitCar();
        }
    }

    /// <summary>
    /// Called by your PlayerController when you press E near the car.
    /// </summary>
    public void EnterCar(PlayerController p, GameObject car)
    {
        // hide the player
        player = p;
        player.SetActive(false);

        // switch camera to follow the car
        cameraFollow.SetTarget(car.transform);
        print(car.transform);
        

        currentCar = car;
        isInCar = true;

        
        // Enable all custom scripts attached to the car
        MonoBehaviour[] scripts = car.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            script.enabled = true;
        }
        
    }

    /// <summary>
    /// Called when you press F inside the car.
    /// </summary>
    public void ExitCar()
    {
        print("EXIT EXIT EXIT");
        if (player == null) return;

        // put the player next to the car
        player.transform.position = currentCar.transform.position + currentCar.transform.transform.right * -1f;
        player.SetActive(true);

        isInCar = false;

        // switch camera back to player
        cameraFollow.SetTarget(player.transform);

        // Disable all custom scripts attached to the car
        MonoBehaviour[] scripts = currentCar.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            script.enabled = false;
        }


        player = null;
        
    }
}
