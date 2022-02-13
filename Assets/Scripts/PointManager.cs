using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class PointManager : MonoBehaviour {
    
    // Point Varaibles -- Set in Editor
    [SerializeField] private GameObject Point;
    [SerializeField] private LayerMask PointLayerMask;
    [SerializeField] private int numPoints;
    [SerializeField] private float boundsOffset = .5f;
    private InputManager controls;
    public List<GameObject> circles;
    



    // Start is called before the first frame update
    void Start()
    {
   
    }

    // Update is called once per frame
    void Update()
    {
    }

    public bool SpawnPointOnMouse() {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(
            Mouse.current.position.ReadValue());
        return TrySpawnPoint(mousePos, remove: true);
    }
    
    private bool TrySpawnPoint(Vector3 pos, bool remove=false) {
        RaycastHit hitPoint;
        if (!Physics.Raycast(pos, Vector3.forward, out hitPoint, Mathf.Infinity, PointLayerMask)) {
            pos.z = 0;
            circles.Add(Instantiate(Point, pos, Quaternion.identity));
            return true;
        }
        else if (remove) {
            circles.Remove(hitPoint.transform.gameObject);
            Destroy(hitPoint.transform.gameObject);
        }

        return false;
    }
    public void ClearPoints() {
        foreach(GameObject circle in circles) {
            Destroy(circle);
        }
        circles = new List<GameObject>();
    }

    public void RandomlyGeneratePoints() {
        Vector3 potentialPos;
        for(int i = 0; i < numPoints; i++) {
            potentialPos = getRandomPosInCamera();
            while (!TrySpawnPoint(potentialPos)) {
                potentialPos = getRandomPosInCamera();
            }
        }
    }

    Vector3 getRandomPosInCamera() {
        return Camera.main.ScreenToWorldPoint(new Vector3(
                Random.Range(boundsOffset, Screen.width - boundsOffset),
                Random.Range(boundsOffset, Screen.height - boundsOffset),
                0)
        );
    }
}
