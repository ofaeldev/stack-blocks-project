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
    [SerializeField] private float maxMoveSpeed = 7f;
    [SerializeField] private float speedIncreasePerBlock = 0.18f;
    [SerializeField] private float moveDistance = 2.5f;
    [SerializeField] private float placementTolerance = 0.45f;
    [SerializeField] private float minPlacementTolerance = 0.18f;
    [SerializeField] private float toleranceShrinkPerBlock = 0.015f;
    [SerializeField] private float restartDelay = 1.5f;
    [SerializeField] private float comboWindow = 0.8f;
    [SerializeField] private int baseScorePerBlock = 100;
    [SerializeField] private float impactPulseScale = 1.12f;
    [SerializeField] private float impactPulseDuration = 0.12f;
    [SerializeField] private float cameraImpactStrength = 0.08f;
    [SerializeField] private float placedBlockMass = 2f;
    [SerializeField] private float placementTorqueImpulse = 2.5f;
    [SerializeField] private float windStrength = 0.08f;
    [SerializeField] private float windGrowthPerBlock = 0.012f;
    [SerializeField] private float windFrequency = 0.75f;
    [SerializeField] private StackCameraController stackCamera;

    private GameObject currentBlock;
    private Transform lastStackedBlock;
    private readonly List<GameObject> spawnedBlocks = new();
    private readonly List<Rigidbody> stackedRigidbodies = new();
    private PlacementGuide placementGuide;
    private Vector3 currentTargetPosition;
    private float currentPlacementTolerance;
    private float lastPlacementTime = -999f;
    private int score;
    private int totalScore;
    private int comboStreak;
    private bool isRestarting;

    private void Start()
    {
        if (stackCamera == null)
        {
            stackCamera = FindFirstObjectByType<StackCameraController>();
        }

        placementGuide = PlacementGuide.Create(placementTolerance);
        StartNewGame();
    }

    private void Update()
    {
        if (isRestarting || currentBlock == null)
        {
            return;
        }

        UpdatePlacementGuide();

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
        {
            TryPlaceCurrentBlock();
        }
    }

    private void FixedUpdate()
    {
        if (isRestarting || stackedRigidbodies.Count == 0)
        {
            return;
        }

        float wind = Mathf.Sin(Time.time * windFrequency) * (windStrength + score * windGrowthPerBlock);
        Vector3 windForce = Vector3.right * wind;

        foreach (Rigidbody blockRigidbody in stackedRigidbodies)
        {
            if (blockRigidbody != null)
            {
                blockRigidbody.AddForce(windForce, ForceMode.Force);
            }
        }
    }

    private void StartNewGame()
    {
        ClearSpawnedBlocks();

        score = 0;
        totalScore = 0;
        comboStreak = 0;
        lastPlacementTime = -999f;
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
        currentTargetPosition = GetNextTargetPosition();
        currentPlacementTolerance = GetCurrentPlacementTolerance();
        Vector3 movementAxis = score % 2 == 0 ? Vector3.right : Vector3.forward;
        Vector3 spawnPosition = currentTargetPosition - movementAxis * moveDistance;

        currentBlock = Instantiate(blockPrefab, spawnPosition, spawnPoint.rotation);
        spawnedBlocks.Add(currentBlock);

        MovingBlock movingBlock = currentBlock.GetComponent<MovingBlock>();
        movingBlock.Initialize(movementAxis, GetCurrentMoveSpeed(), moveDistance * 2f);

        placementGuide.SetTarget(currentTargetPosition, currentPlacementTolerance, blockHeight);
        placementGuide.SetVisible(true);
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

        if (IsInsidePlacementTolerance(currentBlock.transform.position, currentTargetPosition))
        {
            PlaceBlock(currentTargetPosition);
            SpawnNextBlock();
        }
        else
        {
            placementGuide.SetVisible(false);
            StartCoroutine(RestartAfterFall());
        }
    }

    private bool IsInsidePlacementTolerance(Vector3 blockPosition, Vector3 targetPosition)
    {
        Vector2 blockXZ = new(blockPosition.x, blockPosition.z);
        Vector2 targetXZ = new(targetPosition.x, targetPosition.z);

        return Vector2.Distance(blockXZ, targetXZ) <= currentPlacementTolerance;
    }

    private void PlaceBlock(Vector3 targetPosition)
    {
        Vector3 placementOffset = currentBlock.transform.position - targetPosition;
        currentBlock.transform.position = new Vector3(
            currentBlock.transform.position.x,
            targetPosition.y,
            currentBlock.transform.position.z
        );

        Rigidbody placedRigidbody = AddPlacedBlockPhysics(currentBlock, placementOffset);
        stackedRigidbodies.Add(placedRigidbody);

        lastStackedBlock = currentBlock.transform;
        score++;
        RegisterScore(placementOffset);
        StartCoroutine(PulseBlock(currentBlock.transform));

        if (stackCamera != null)
        {
            stackCamera.SetTargetHeight(targetPosition.y);
            stackCamera.AddImpact(cameraImpactStrength);
        }

        Debug.Log($"Blocks: {score} | Score: {totalScore} | Combo: x{comboStreak}");
    }

    private Rigidbody AddPlacedBlockPhysics(GameObject block, Vector3 placementOffset)
    {
        Rigidbody blockRigidbody = block.GetComponent<Rigidbody>();

        if (blockRigidbody == null)
        {
            blockRigidbody = block.AddComponent<Rigidbody>();
        }

        blockRigidbody.mass = placedBlockMass;
        blockRigidbody.linearDamping = 1.2f;
        blockRigidbody.angularDamping = 2.5f;

        Vector3 torque = new(placementOffset.z, 0f, -placementOffset.x);
        blockRigidbody.AddTorque(torque * placementTorqueImpulse, ForceMode.Impulse);

        return blockRigidbody;
    }

    private void RegisterScore(Vector3 placementOffset)
    {
        float timeSinceLastPlacement = Time.time - lastPlacementTime;
        comboStreak = timeSinceLastPlacement <= comboWindow ? comboStreak + 1 : 1;
        lastPlacementTime = Time.time;

        float accuracy = 1f - Mathf.Clamp01(new Vector2(placementOffset.x, placementOffset.z).magnitude / currentPlacementTolerance);
        int comboBonus = Mathf.Max(0, comboStreak - 1) * 25;
        int accuracyBonus = Mathf.RoundToInt(baseScorePerBlock * accuracy);

        totalScore += baseScorePerBlock + comboBonus + accuracyBonus;
    }

    private IEnumerator PulseBlock(Transform blockTransform)
    {
        Vector3 startScale = blockTransform.localScale;
        Vector3 pulseScale = startScale * impactPulseScale;
        float elapsedTime = 0f;

        while (elapsedTime < impactPulseDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / impactPulseDuration;
            blockTransform.localScale = Vector3.Lerp(startScale, pulseScale, progress);
            yield return null;
        }

        elapsedTime = 0f;

        while (elapsedTime < impactPulseDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / impactPulseDuration;
            blockTransform.localScale = Vector3.Lerp(pulseScale, startScale, progress);
            yield return null;
        }

        blockTransform.localScale = startScale;
    }

    private void UpdatePlacementGuide()
    {
        bool isInsideTolerance = IsInsidePlacementTolerance(currentBlock.transform.position, currentTargetPosition);
        placementGuide.SetState(isInsideTolerance);
    }

    private float GetCurrentMoveSpeed()
    {
        return Mathf.Min(maxMoveSpeed, moveSpeed + score * speedIncreasePerBlock);
    }

    private float GetCurrentPlacementTolerance()
    {
        return Mathf.Max(minPlacementTolerance, placementTolerance - score * toleranceShrinkPerBlock);
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
        stackedRigidbodies.Clear();
        currentBlock = null;

        if (placementGuide != null)
        {
            placementGuide.SetVisible(false);
        }
    }
}
