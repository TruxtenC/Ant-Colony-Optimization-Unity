using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AntColonyOptimizer : MonoBehaviour {
    // TODO:
    // Move the point spawning events into this class, we can regenerate we a point is removed
    private PointManager PointManager;
    private LineRenderer LineRenderer;
    private InputManager controls;
    private float[,] distanceField;

    [SerializeField] float desirePower;
    // Start is called before the first frame update

    private void Awake() {
        controls = new InputManager();
        controls.AntColonyPathing.FindPath.performed += _ => FindPath();
        controls.AntColonyPathing.ClearPoints.performed += _ => ClearPath();
    }

    void Start() {
        PointManager = FindObjectOfType<PointManager>();
        LineRenderer = FindObjectOfType<LineRenderer>();
        desirePower = 1;
    }

    private void FindPath() {
        int num_circles = PointManager.circles.Count;
        List<GameObject> circles = PointManager.circles;
        HashSet<GameObject> closed_set = new HashSet<GameObject>();
        GenerateDistanceField();
        GameObject[] path = new GameObject[PointManager.circles.Count];
        // Starting from the first circle, iterate through, and find the most desirable
        // path
        float distance, current_desirability, best_desirability;
        int best_circle = 0, cur_circle = 0;
        path[0] = circles[cur_circle];
        closed_set.Add(path[cur_circle]);
        int path_length = 1;
        // n - 1 connections in acyclic path
        while(path_length < num_circles) {
            best_desirability = 0;
            for (int i = 0; i < num_circles; i++) {
                if (closed_set.Contains(circles[i])) {
                    continue;
                }
                distance = distanceField[cur_circle, i];
                current_desirability = Mathf.Pow(1 / distance, desirePower);
                if (best_desirability < current_desirability) {
                    best_desirability = current_desirability;
                    best_circle = i;
                }
            }
            path[path_length] = circles[best_circle];
            closed_set.Add(circles[best_circle]);
            cur_circle = best_circle;
            path_length += 1;
        }
        DrawConnections(path);
    }
    
    private void DrawConnections(GameObject[] objects) {
        int num_circles = objects.Length;
        // +1 for connecting the end to the start
        LineRenderer.positionCount = num_circles+1;
        Vector3[] points = new Vector3[num_circles+1];
        for (int i = 0; i < num_circles; i++) {
            points[i] = objects[i].transform.position;
        }
        points[num_circles] = points[0];
        LineRenderer.SetPositions(points);
    }
    private void GenerateDistanceField() {
        int numCircles = PointManager.circles.Count;
        distanceField = new float[numCircles, numCircles];
        // distanceField[i, j] = Distance from circle i to circle j
        for (int i = 0; i < numCircles; i++) {
            for (int j = 0; j < numCircles; j++) {
                distanceField[i, j] = Vector3.Distance(
                    PointManager.circles[i].transform.position,
                    PointManager.circles[j].transform.position
                );
            }
            // The distance between a circle and itself is 'undefined'
            distanceField[i, i] = Mathf.Infinity;
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    void ClearPath() {
        LineRenderer.positionCount = 0;
        PointManager.ClearPoints();
    }
    private void OnEnable() {
        controls.Enable();
    }

    private void OnDisable() {
        controls.Disable();
    }
}
