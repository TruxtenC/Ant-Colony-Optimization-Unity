using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class AntColonyOptimizer : MonoBehaviour {
    /// <summary>
    /// Implementation of the Ant Colony Optimization algorithm For the Travelling Salesman Problem
    /// https://en.wikipedia.org/wiki/Ant_colony_optimization_algorithms
    /// This is in desperate need of a refactor
    /// </summary>
    // TODO:
    // Move the point spawning events into this class, we can regenerate when a point is removed
    [SerializeField] private GameObject AntPrefab;
    [SerializeField] private float antSpeed;
    [SerializeField] private int NumberAnts;
    private GameObject Ant;
    private int antPoint;
    private bool animateAnt;
    private PointManager PointManager;
    private LineRenderer LineRenderer;
    private InputManager controls;
    private float[,] distanceField;
    private float[,] pheromones;
    private float distanceEpsilon;
    [SerializeField] private float pheromoneEvaporateRate;
    [SerializeField] int numIterations = 30;
    [SerializeField] float desirePower;
    [SerializeField] float cycleUpdateRate = .5f;
    private LineRenderer[] antLines;
    private float[,] deltaPheromones;
    // Start is called before the first frame update

    private void Awake() {
        controls = new InputManager();
        antPoint = 1;
        distanceEpsilon = .1f;
        controls.AntColonyPathing.FindPath.performed += _ => StartCoroutine(AntSystemPath());
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
    }

    // private void FindPath() {
    //     Destroy(Ant);
    //     antPoint = 1;
    //
    //     GenerateDistanceField();
    //
    //     int num_circles = PointManager.circles.Count, cur_circle = 0;
    //     int numAnts = NumberAnts;
    //     if (NumberAnts < num_circles) {
    //         numAnts = num_circles;
    //     }
    //
    //     Vector3[] circles = new Vector3[num_circles];
    //     for (int i = 0; i < num_circles; i++) {
    //         circles[i] = PointManager.circles[i].transform.position;
    //     }
    //
    //     Vector3[][] paths = new Vector3[numAnts][];
    //     Vector3[] best_path = new Vector3[0];
    //     float best_path_cost = Mathf.Infinity;
    //
    //     for (int i = 0; i < numAnts; i++) {
    //         int path_length = 1;
    //         HashSet<Vector3> closed_set = new HashSet<Vector3>();
    //
    //         paths[i] = new Vector3[num_circles];
    //         paths[i][0] = circles[i];
    //         closed_set.Add(circles[i]);
    //
    //         cur_circle = i;
    //         while (path_length < num_circles) {
    //             cur_circle = getBestCircle(cur_circle, closed_set, circles);
    //             closed_set.Add(circles[cur_circle]);
    //             paths[i][path_length] = circles[cur_circle];
    //             path_length += 1;
    //         }
    //
    //         float path_cost = GetPathCost(paths[i], circles);
    //         if (path_cost < best_path_cost) {
    //             best_path = paths[i];
    //             best_path_cost = path_cost;
    //         }
    //     }
    //
    //     if (best_path.Length == 0)
    //         return;
    //
    //     DrawConnections(best_path, 0);
    //
    //     Ant = Instantiate(AntPrefab, best_path[0], Quaternion.identity);
    //     animateAnt = true;
    // }

    IEnumerator AntSystemPath() {
        // http://www.cs.unibo.it/babaoglu/courses/cas05-06/tutorials/Ant_Colony_Optimization.pdf
        Destroy(Ant);
        antPoint = 1;
        int num_circles = PointManager.circles.Count;
        GenerateDistanceField();
        // Init ALl the LineRenders
        antLines = new LineRenderer[NumberAnts];
        for (int i = 0; i < NumberAnts; i++) {
            antLines[i] = Instantiate(LineRenderer);
            antLines[i].positionCount = num_circles + 1;
        }
        
        // Init circles array
        Vector3[] circles = new Vector3[num_circles];
        for (int i = 0; i < num_circles; i++) {
            circles[i] = PointManager.circles[i].transform.position;
        }
        
        // Initialize pheromones
        pheromones = new float[num_circles, num_circles];
        deltaPheromones = new float[num_circles, num_circles];
        for (int i = 0; i < num_circles; i++) {
            for (int j = 0; j < num_circles; j++) {
                pheromones[i, j] = 1;
                deltaPheromones[i, j] = 1;
            }
        }

        int[][] paths = new int[NumberAnts][];
        float[] path_costs = new float[NumberAnts];
        
        // Do numIterations iterations
        float best_path_cost = Mathf.Infinity;
        for (int iteration = 0; iteration < numIterations; iteration++) {
            GenerateAllPaths(NumberAnts, circles, paths, path_costs);
            PheromoneUpdate(paths, path_costs, circles);
            
            // Find best path
            int best_path_index = 0;
            float best_path_cost_iteration = path_costs[0];
            for(int i = 1; i < NumberAnts; i++) {
                if(path_costs[i] < best_path_cost_iteration) {
                    best_path_index = i;
                    best_path_cost_iteration = path_costs[i];
                }
            }
            if ( Math.Abs(best_path_cost_iteration - best_path_cost) < 0.001f) {
                // Using a break here just freezes the unity editor for some reason
                iteration = numIterations;
            }
            // Draw best path
            DrawConnections(paths[best_path_index], 0, circles);
            yield return new WaitForSeconds(cycleUpdateRate);
        }
        // Ant = Instantiate(AntPrefab, paths[0][0], Quaternion.identity);
        // animateAnt = true;
        yield return null;
    }

    private void GenerateAllPaths(int num_paths, Vector3[] circles,  int[][] paths, float[] path_costs) {
        // Function modifies all the input arrays in place
        int num_circles = circles.Length;
        for (int i = 0; i < num_paths; i++) {
            int cur_circle = Random.Range(0, num_circles-1);

            HashSet<Vector3> closed_set = new HashSet<Vector3>();
            closed_set.Add(circles[cur_circle]);
            
            paths[i] = new int[num_circles];
            paths[i][0] = cur_circle;

            int path_length = 1;
            while (path_length < num_circles) {
                cur_circle = ChooseNextPointP(cur_circle, closed_set, circles);
                closed_set.Add(circles[cur_circle]);
                paths[i][path_length] = cur_circle;
                path_length += 1;
            }

            float path_cost = GetPathCost(paths[i], circles);
            path_costs[i] = path_cost;
        }
    }
    private float GetPathCost(int[] path, Vector3[] circles) {
        float totalCost = 0;
        for (int i = 1; i < path.Length; i++) {
            totalCost += Vector3.Distance(circles[path[i]], circles[path[i-1]]);
        }
        return totalCost;
    }

    private int getBestCircle(int cur_circle, HashSet<Vector3> closed_set, Vector3[] circles) {
        // Given a circle index, find the most desirable unexplored circle
        int num_circles = circles.Length, best_circle = cur_circle;
        float distance, current_desirability;
        float best_desirability = 0;
        for (int i = 0; i < num_circles; i++) {
            if (closed_set.Contains(circles[i])) {
                continue;
            }

            current_desirability = AttractivenessHeuristic(cur_circle, i);
            if (best_desirability < current_desirability) {
                best_desirability = current_desirability;
                best_circle = i;
            }
        }

        return best_circle;
    }

    private int ChooseNextPointP(int cur_circle, HashSet<Vector3> closed_set, Vector3[] circles) {
        int num_circles = circles.Length, best_circle = cur_circle;
        float distance, a, ph;
        float[] probs = new float[num_circles];
        for (int i = 0; i < num_circles; i++) {
            if (closed_set.Contains(circles[i])) {
                probs[i] = 0;
                continue;
            }
            a = AttractivenessHeuristic(cur_circle, i);
            ph = pheromones[cur_circle, i];
            // alpha is set to 1, beta is handled in AttractivenessHeuristic
            probs[i] = a * ph;
        }
        // return the ith circle with probability props[i]
        return PickRandomPointGivenProbability(circles, probs);
    }

    private float AttractivenessHeuristic(int p1, int p2) {
        // Simple distance 
        float distance = distanceField[p1, p2];
        return Mathf.Pow(1 / distance, desirePower);
    }
    
    private void UpdateDeltaPhenomone(int[] path, int p1, int p2, Vector3[] circles, float path_cost) {
        // Returns 0 if there is no link between p1 and p2 in path
        // else return  1 / path_cost
        int num_circles = circles.Length;
        // Special case for when p1 connects to p2 in the last link
        if (path[num_circles - 1] == p1 && path[0] == p2) {
            deltaPheromones[p1, p2] += 1 / path_cost;
            return;
        }
        for (int i = 0; i < num_circles - 1; i++) {
            if (path[i] == p1 && path[i + 1] == p2) {
                deltaPheromones[p1, p2] += 1 / path_cost;
                return;
            }
        }
    }
    
    private void PheromoneUpdate(int[][] paths, float[] path_costs, Vector3[] circles) {
        int num_circles = circles.Length;
        // Update the pheromones by the following:
        // Txy = (1 - rho) * Txy + sum of all pheromones from x to y
        for (int x = 0; x < num_circles; x++) {
            for (int y = 0; y < num_circles; y++) {
                pheromones[x, y] = pheromoneEvaporateRate * pheromones[x, y];
                for(int i = 0; i < paths.Length; i++) {
                    UpdateDeltaPhenomone(paths[i], x, y, circles, path_costs[i]);
                    pheromones[x, y] += deltaPheromones[x, y];
                }
            }
        }
    }
    
    private void DrawConnections(int[] points, int lineRendererIndex, Vector3[] circles) {
        Vector3[] points_array = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++) {
            points_array[i] = circles[points[i]];
        }
        antLines[lineRendererIndex].SetPositions(points_array);
        antLines[lineRendererIndex].SetPosition(points_array.Length, points_array[0]);
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
    void Update() {
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
        Debug.Log("Starting ClearPath");
        StopAllCoroutines();
        Debug.Log("Clearing path");
        if(antLines != null) {
            Vector3[] points = new Vector3[1];
            for(int i = 0; i < antLines.Length; i++) {
                Debug.Log("Clearing line " + i);
                if(antLines[i] != null) {
                    antLines[i].positionCount = 0;
                    antLines[i].SetPositions(points);
                }
            }
        }
        Debug.Log("Cleared path");
        PointManager.ClearPoints();
        Debug.Log("Cleared points");
        Destroy(Ant);
        animateAnt = false;
        antPoint = 1;
    }

    // Given a list of points, pick a random point i with probability probability[i]
    private int PickRandomPointGivenProbability(Vector3[] points, float[] probabilities) {
        // sum probabilities
        float prob_sum = probabilities.Sum();
        float r = Random.Range(0, prob_sum);
        float sum = 0;
        for (int i = 0; i < points.Length; i++) {
            sum += probabilities[i];
            if (sum > r) {
                return i;
            }
        }
        Debug.Log("Error! probabilities array: ");
        foreach (float p in probabilities) {
            Debug.Log(p);
        }
        Debug.Log("Pheromone array: ");
        foreach (float p in pheromones) {
            Debug.Log(p);
        }
        // If we get here, something went wrong
        return -1;
    }

    private void OnEnable() {
        controls.Enable();
    }

    private void OnDisable() {
        controls.Disable();
    }
}