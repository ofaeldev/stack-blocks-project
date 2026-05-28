using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class StartSession : MonoBehaviour
{
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float blockHeight = 1f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float moveDistance = 2.5f;
    [SerializeField] private float placementTolerance = 0.45f;
    [SerializeField] private float restartDelay = 1.5f;
    [SerializeField] private StackCameraController stackCamera;

    private GameObject currentBlock;
    private Transform lastStackedBlock;
    private readonly List<GameObject> spawnedBlocks = new();
    private int score;
    private bool isRestarting;

    private void Start()
    {
        if (stackCamera == null)
        {
            stackCamera = FindFirstObjectByType<StackCameraController>();
        }

        StartNewGame();
    }

    private void Update()
    {
        if (isRestarting || currentBlock == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
        {
            TryPlaceCurrentBlock();
        }
    }

    private void StartNewGame()
    {
        ClearSpawnedBlocks();

        score = 0;
        lastStackedBlock = null;
        isRestarting = false;

        if (stackCamera != null)
        {
            stackCamera.ResetCamera();
        }

        SpawnNextBlock();
    }

    private void SpawnNextBlock()
    {
        Vector3 targetPosition = GetNextTargetPosition();
        Vector3 movementAxis = score % 2 == 0 ? Vector3.right : Vector3.forward;
        Vector3 spawnPosition = targetPosition - movementAxis * moveDistance;

        currentBlock = Instantiate(blockPrefab, spawnPosition, spawnPoint.rotation);
        spawnedBlocks.Add(currentBlock);

        MovingBlock movingBlock = currentBlock.GetComponent<MovingBlock>();
        movingBlock.Initialize(movementAxis, moveSpeed, moveDistance * 2f);
    }

    private Vector3 GetNextTargetPosition()
    {
        if (lastStackedBlock == null)
        {
            return spawnPoint.position;
        }

        return lastStackedBlock.position + Vector3.up * blockHeight;
    }

    private void TryPlaceCurrentBlock()
    {
        MovingBlock movingBlock = currentBlock.GetComponent<MovingBlock>();
        movingBlock.Stop();

        Vector3 targetPosition = GetNextTargetPosition();

        if (IsInsidePlacementTolerance(currentBlock.transform.position, targetPosition))
        {
            PlaceBlock(targetPosition);
            SpawnNextBlock();
        }
        else
        {
            StartCoroutine(RestartAfterFall());
        }
    }

    private bool IsInsidePlacementTolerance(Vector3 blockPosition, Vector3 targetPosition)
    {
        Vector2 blockXZ = new(blockPosition.x, blockPosition.z);
        Vector2 targetXZ = new(targetPosition.x, targetPosition.z);

        return Vector2.Distance(blockXZ, targetXZ) <= placementTolerance;
    }

    private void PlaceBlock(Vector3 targetPosition)
    {
        currentBlock.transform.position = targetPosition;
        lastStackedBlock = currentBlock.transform;
        score++;

        if (stackCamera != null)
        {
            stackCamera.SetTargetHeight(targetPosition.y);
        }

        Debug.Log($"Score: {score}");
    }

    private IEnumerator RestartAfterFall()
    {
        isRestarting = true;

        foreach (GameObject block in spawnedBlocks)
        {
            if (block != null && block.GetComponent<Rigidbody>() == null)
            {
                block.AddComponent<Rigidbody>();
            }
        }

        yield return new WaitForSeconds(restartDelay);

        StartNewGame();
    }

    private void ClearSpawnedBlocks()
    {
        foreach (GameObject block in spawnedBlocks)
        {
            if (block != null)
            {
                Destroy(block);
            }
        }

        spawnedBlocks.Clear();
        currentBlock = null;
    }
}
