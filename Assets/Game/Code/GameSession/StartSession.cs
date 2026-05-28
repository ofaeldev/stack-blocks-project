using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class StartSession : MonoBehaviour
{
    private enum GameMode
    {
        Relax,
        Hardcore,
        PhysicsMode
    }

    [SerializeField] private GameMode gameMode = GameMode.PhysicsMode;
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
    [SerializeField] private float stabilityGraceAfterPlacement = 0.55f;
    [SerializeField] private float maxSafeTiltAngle = 24f;
    [SerializeField] private float maxSafeDrift = 1.35f;
    [SerializeField] private float fallenHeight = -1f;
    [SerializeField] private StackCameraController stackCamera;
    [SerializeField] private StackHud hud;
    [SerializeField] private StackFeedbackController feedback;
    [SerializeField] private StackBiomeController biomeController;

    private GameObject currentBlock;
    private Transform lastStackedBlock;
    private readonly List<GameObject> spawnedBlocks = new();
    private readonly List<Rigidbody> stackedRigidbodies = new();
    private PlacementGuide placementGuide;
    private Vector3 currentTargetPosition;
    private float currentPlacementTolerance;
    private float lastPlacementTime = -999f;
    private float nextStabilityCheckTime;
    private int score;
    private int totalScore;
    private int comboStreak;
    private StackProgression progression;
    private string currentBiomeName = "City";
    private bool isRestarting;

    private void Start()
    {
        progression = StackProgression.Load();

        if (stackCamera == null)
        {
            stackCamera = FindFirstObjectByType<StackCameraController>();
        }

        if (hud == null)
        {
            hud = FindFirstObjectByType<StackHud>();
        }

        if (hud == null)
        {
            hud = StackHud.Create();
        }

        if (feedback == null)
        {
            feedback = FindFirstObjectByType<StackFeedbackController>();
        }

        if (feedback == null)
        {
            feedback = StackFeedbackController.Create();
        }

        if (biomeController == null)
        {
            Camera sceneCamera = stackCamera != null ? stackCamera.GetComponent<Camera>() : Camera.main;
            biomeController = StackBiomeController.Create(sceneCamera);
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
        CheckTowerStability();

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            HandleModeHotkeys(keyboard);
        }

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
        nextStabilityCheckTime = float.PositiveInfinity;
        lastStackedBlock = null;
        isRestarting = false;

        if (stackCamera != null)
        {
            stackCamera.ResetCamera();
        }

        currentBiomeName = biomeController != null ? biomeController.UpdateForBlocks(0) : "City";
        UpdateHud();
        hud.ShowReady();

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
        movingBlock.Initialize(movementAxis, currentTargetPosition, GetCurrentMoveSpeed(), moveDistance);

        placementGuide.SetTarget(currentTargetPosition, currentPlacementTolerance, blockHeight);
        placementGuide.SetVisible(true);
        UpdateHud();
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
            hud.ShowDanger("Miss!");
            feedback.PlayDanger(currentBlock.transform.position);
            HandleFailure("Miss!");
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
        int gainedScore = RegisterScore(placementOffset);
        nextStabilityCheckTime = Time.time + stabilityGraceAfterPlacement;
        StartCoroutine(PulseBlock(currentBlock.transform));
        currentBiomeName = biomeController != null ? biomeController.UpdateForBlocks(score) : currentBiomeName;
        feedback.PlayPlacement(currentBlock.transform.position, comboStreak);

        if (stackCamera != null)
        {
            stackCamera.SetTargetHeight(targetPosition.y);
            stackCamera.AddImpact(cameraImpactStrength);
        }

        UpdateHud();
        hud.ShowPlacement(comboStreak > 1, gainedScore);

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

    private int RegisterScore(Vector3 placementOffset)
    {
        float timeSinceLastPlacement = Time.time - lastPlacementTime;
        comboStreak = timeSinceLastPlacement <= comboWindow ? comboStreak + 1 : 1;
        lastPlacementTime = Time.time;

        float accuracy = 1f - Mathf.Clamp01(new Vector2(placementOffset.x, placementOffset.z).magnitude / currentPlacementTolerance);
        int comboBonus = Mathf.Max(0, comboStreak - 1) * 25;
        int accuracyBonus = Mathf.RoundToInt(baseScorePerBlock * accuracy);
        int gainedScore = baseScorePerBlock + comboBonus + accuracyBonus;

        totalScore += gainedScore;

        return gainedScore;
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

    private void CheckTowerStability()
    {
        if (gameMode == GameMode.Relax)
        {
            return;
        }

        if (Time.time < nextStabilityCheckTime || stackedRigidbodies.Count == 0)
        {
            return;
        }

        foreach (Rigidbody blockRigidbody in stackedRigidbodies)
        {
            if (blockRigidbody == null)
            {
                continue;
            }

            float tiltAngle = Vector3.Angle(blockRigidbody.transform.up, Vector3.up);
            Vector2 drift = new(blockRigidbody.position.x, blockRigidbody.position.z);

            if (tiltAngle > maxSafeTiltAngle || drift.magnitude > maxSafeDrift || blockRigidbody.position.y < fallenHeight)
            {
                placementGuide.SetVisible(false);
                hud.ShowDanger("Tower lost!");
                feedback.PlayDanger(blockRigidbody.position);
                Debug.Log($"Tower lost stability. Tilt: {tiltAngle:0.0}, Drift: {drift.magnitude:0.00}, Height: {blockRigidbody.position.y:0.00}");
                HandleFailure("Tower lost!");
                return;
            }
        }
    }

    private void HandleFailure(string reason)
    {
        if (gameMode == GameMode.Relax)
        {
            hud.ShowDanger($"{reason} Relax saved");
            RecoverRelaxMode();
            return;
        }

        EndRun();
        StartCoroutine(RestartAfterFall());
    }

    private void RecoverRelaxMode()
    {
        if (currentBlock != null)
        {
            Destroy(currentBlock);
            spawnedBlocks.Remove(currentBlock);
            currentBlock = null;
        }

        comboStreak = 0;
        SpawnNextBlock();
    }

    private void EndRun()
    {
        progression.RegisterRun(totalScore, score);
        UpdateHud();
    }

    private void UpdateHud()
    {
        hud.SetStats(score, totalScore, comboStreak, GetCurrentMoveSpeed(), GetCurrentPlacementTolerance(), gameMode.ToString(), GetBalanceRisk(), currentBiomeName);
        hud.SetMeta(progression);
    }

    private float GetBalanceRisk()
    {
        float worstRisk = 0f;

        foreach (Rigidbody blockRigidbody in stackedRigidbodies)
        {
            if (blockRigidbody == null)
            {
                continue;
            }

            float tiltRisk = Vector3.Angle(blockRigidbody.transform.up, Vector3.up) / maxSafeTiltAngle;
            float driftRisk = new Vector2(blockRigidbody.position.x, blockRigidbody.position.z).magnitude / maxSafeDrift;
            worstRisk = Mathf.Max(worstRisk, tiltRisk, driftRisk);
        }

        return Mathf.Clamp01(worstRisk);
    }

    private void HandleModeHotkeys(Keyboard keyboard)
    {
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            SetMode(GameMode.Relax);
        }
        else if (keyboard.digit2Key.wasPressedThisFrame)
        {
            SetMode(GameMode.Hardcore);
        }
        else if (keyboard.digit3Key.wasPressedThisFrame)
        {
            SetMode(GameMode.PhysicsMode);
        }
    }

    private void SetMode(GameMode mode)
    {
        if (gameMode == mode)
        {
            return;
        }

        gameMode = mode;
        hud.ShowReady();
        UpdateHud();
    }

    private IEnumerator RestartAfterFall()
    {
        if (isRestarting)
        {
            yield break;
        }

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
