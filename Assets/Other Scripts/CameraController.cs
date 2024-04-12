using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class CameraController : MonoBehaviour
{
    public float transitionSpeed = 1.0f;
    public LevelGenerator levelGenerator;

    private int currentTargetIndex = -1;
    private Vector3 initialPosition;
    private bool transitioning = false;
    private List<GameObject> targets;
    private Camera _camera;
    private float initialCamSize;

    void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    void Start()
    {
        initialPosition = transform.position;
        targets = levelGenerator.GetGameObjects();
        initialCamSize = _camera.orthographicSize;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            targets = levelGenerator.GetGameObjects();
            SwitchTarget(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            targets = levelGenerator.GetGameObjects();
            SwitchTarget(1);
        }
        if (!transitioning && currentTargetIndex != -1 && targets[currentTargetIndex] != null)
        {
            transform.position = new Vector3(targets[currentTargetIndex].transform.position.x, targets[currentTargetIndex].transform.position.y, initialPosition.z);
        }
    }
    void SwitchTarget(int amount)
    {
        currentTargetIndex += amount;

        if (targets.Count == 0) return;

        if (currentTargetIndex < -1) currentTargetIndex = targets.Count - 1;

        if (currentTargetIndex == -1 || currentTargetIndex >= targets.Count)
        {
            ResetCamera();
        }
        else
        {
            FocusCameraOnTarget(targets[currentTargetIndex]);
        }
    }

    void FocusCameraOnTarget(GameObject target)
    {
        transitioning = true;
        StartCoroutine(TransitionCamera(new Vector3(target.transform.position.x, target.transform.position.y, initialPosition.z), initialCamSize / 4));
    }

    IEnumerator TransitionCamera(Vector3 targetPosition, float camSize)
    {
        float elapsedTime = 0.0f;
        float initialSize = _camera.orthographicSize;
        Vector3 initialPosition = transform.position;

        while (elapsedTime < transitionSpeed)
        {
            float t = elapsedTime / transitionSpeed;
            transform.position = Vector3.Lerp(initialPosition, targetPosition, t);

            _camera.orthographicSize = initialSize + (camSize - initialSize) * t;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        _camera.orthographicSize = camSize;

        transitioning = false;
    }

    void ResetCamera()
    {
        currentTargetIndex = -1;
        StartCoroutine(TransitionCamera(initialPosition, initialCamSize));
    }
}
