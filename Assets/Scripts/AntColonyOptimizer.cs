using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AntColonyOptimizer : MonoBehaviour {
    // TODO:
    // Move the point spawning events into this class, we can regenerate when a point is removed
    [SerializeField] private GameObject AntPrefab;
    [SerializeField] private float antSpeed;
    private GameObject Ant;
    private int antPoint;
    private bool animateAnt;
    private PointManager PointManager;
    private LineRenderer LineRenderer;
    private InputManager controls;
    private float[,] distanceField;
    private float distanceEpsilon;
    [SerializeField] float desirePower;
    // Start is called before the first frame update

    private void Awake() {
        controls = new InputManager();
        antPoint = 1;
        distanceEpsilon = .1f;
        controls.AntColonyPathing.FindPath.performed += _ => FindPath();
        controls.AntColonyPathing.ClearPoints.performed += _ => ClearPath();
        controls.AntColonyPathing.SpawnPoints.performed += _ => GetNewPoints();
    }

    private void GetNewPoints() {
        ClearPath();
        PointManager.RandomlyGeneratePoints();
    }

    void Start() {
        PointManager = FindObjectOfType<PointManager>();
        LineRenderer = FindObjectOfType<LineRenderer>();
        desirePower = 1;
    }

    private void FindPath() {
        
        GenerateDistanceField();
        
        int num_circles = PointManager.circles.Count, cur_circle = 0, path_length = 1;;
        List<GameObject> circles = PointManager.circles;
        HashSet<GameObject> closed_set = new HashSet<GameObject>();
        GameObject[] path = new GameObject[PointManager.circles.Count];

        path[0] = circles[cur_circle];
        closed_set.Add(path[cur_circle]);
        
        while(path_length < num_circles) {
            cur_circle = getBestCircle(cur_circle, closed_set, circles);
            closed_set.Add(circles[cur_circle]);
            path[path_length] = circles[cur_circle];
            path_length += 1;
        }
        
        DrawConnections(path);
        
        Ant = Instantiate(AntPrefab, path[0].transform.position, Quaternion.identity);
        animateAnt = true;
    }

    private int getBestCircle(int cur_circle, HashSet<GameObject> closed_set, List<GameObject> circles) {
        // Given a circle index, find the most desirable unexplored circle
        int num_circles = circles.Count, best_circle = 0;
        float distance, current_desirability;
        float best_desirability = 0;
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
        return best_circle;
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
            distanceField[i, i] = -1;
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (animateAnt) {
            Vector3 nextPoint = LineRenderer.GetPosition(antPoint);
            Vector3 antPos = Ant.transform.position;
            if (Vector3.Distance(nextPoint, antPos) < Single.Epsilon) {
                antPoint += 1;
                if (antPoint == LineRenderer.positionCount)
                    // The end of the array is the same as the first, so skip it 
                    antPoint = 1;
                nextPoint = LineRenderer.GetPosition(antPoint);
            }
            Ant.transform.position = Vector3.MoveTowards(
                antPos,
                nextPoint,
                Time.deltaTime * antSpeed);
        }
    }

    void ClearPath() {
        LineRenderer.positionCount = 0;
        LineRenderer.SetPositions(new Vector3[0]);
        PointManager.ClearPoints();
        Destroy(Ant);
        animateAnt = false;
        antPoint = 1;
    }
    private void OnEnable() {
        controls.Enable();
    }

    private void OnDisable() {
        controls.Disable();
    }
    
}
