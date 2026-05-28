using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class StartSession : MonoBehaviour
{
    [SerializeField] private StackGameMode gameMode = StackGameMode.PhysicsMode;
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
    [SerializeField] private float squashAmount = 0.14f;
    [SerializeField] private float impactPulseDuration = 0.08f;
    [SerializeField] private float cameraImpactStrength = 0.08f;
    [SerializeField] private float perfectCameraImpactStrength = 0.14f;
    [SerializeField] private float missCameraImpactStrength = 0.22f;
    [SerializeField] private float perfectDistance = 0.06f;
    [SerializeField] private float perfectHitStopDuration = 0.045f;
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
    [SerializeField] private StackMainMenu mainMenu;

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
    private bool hasStarted;
    private bool isRestarting;
    private bool isExitConfirmOpen;

    private void Start()
    {
        Time.timeScale = 1f;
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

        if (mainMenu == null)
        {
            mainMenu = FindFirstObjectByType<StackMainMenu>();
        }

        if (mainMenu == null)
        {
            mainMenu = StackMainMenu.Create();
        }

        placementGuide = PlacementGuide.Create(placementTolerance);
        placementGuide.SetVisible(false);
        hud.SetMeta(progression);
        mainMenu.Show(StartSelectedMode);
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!hasStarted || isRestarting || currentBlock == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            OpenExitConfirmation();
            return;
        }

        if (isExitConfirmOpen)
        {
            return;
        }

        UpdatePlacementGuide();
        CheckTowerStability();

        if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
        {
            TryPlaceCurrentBlock();
        }
    }

    private void FixedUpdate()
    {
        if (!hasStarted || isRestarting || isExitConfirmOpen || stackedRigidbodies.Count == 0)
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

    private void StartSelectedMode(StackGameMode selectedMode)
    {
        gameMode = selectedMode;
        hasStarted = true;
        isExitConfirmOpen = false;
        StartNewGame();
    }

    private void StartNewGame()
    {
        Time.timeScale = 1f;
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

    private void OpenExitConfirmation()
    {
        isExitConfirmOpen = true;
        Time.timeScale = 0f;
        mainMenu.ShowExitConfirmation(ConfirmExitToModeMenu, CancelExitToModeMenu);
    }

    private void ConfirmExitToModeMenu()
    {
        Time.timeScale = 1f;
        isExitConfirmOpen = false;
        hasStarted = false;
        EndRun();
        ClearSpawnedBlocks();
        hud.ShowReady();
        mainMenu.Show(StartSelectedMode);
    }

    private void CancelExitToModeMenu()
    {
        isExitConfirmOpen = false;
        Time.timeScale = 1f;
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
            if (stackCamera != null)
            {
                stackCamera.AddImpact(missCameraImpactStrength);
            }
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

        if (placedRigidbody != null)
        {
            stackedRigidbodies.Add(placedRigidbody);
        }

        lastStackedBlock = currentBlock.transform;
        score++;
        float placementDistance = new Vector2(placementOffset.x, placementOffset.z).magnitude;
        float accuracy = 1f - Mathf.Clamp01(placementDistance / currentPlacementTolerance);
        bool isPerfect = placementDistance <= perfectDistance;
        int gainedScore = RegisterScore(accuracy, isPerfect);
        nextStabilityCheckTime = Time.time + stabilityGraceAfterPlacement;
        StartCoroutine(PulseBlock(currentBlock.transform));
        currentBiomeName = biomeController != null ? biomeController.UpdateForBlocks(score) : currentBiomeName;
        feedback.PlayPlacement(currentBlock.transform.position, comboStreak, isPerfect, accuracy);

        if (isPerfect)
        {
            StartCoroutine(HitStop(perfectHitStopDuration));
        }

        if (stackCamera != null)
        {
            stackCamera.SetTargetHeight(targetPosition.y);
            stackCamera.AddImpact(isPerfect ? perfectCameraImpactStrength : cameraImpactStrength);
        }

        UpdateHud();
        hud.ShowPlacement(comboStreak > 1, gainedScore, isPerfect, GetPrecisionLabel(accuracy));

        Debug.Log($"Blocks: {score} | Score: {totalScore} | Combo: x{comboStreak}");
    }

    private Rigidbody AddPlacedBlockPhysics(GameObject block, Vector3 placementOffset)
    {
        if (gameMode == StackGameMode.Relax)
        {
            block.transform.position -= new Vector3(placementOffset.x, 0f, placementOffset.z);
            return null;
        }

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

    private int RegisterScore(float accuracy, bool isPerfect)
    {
        float timeSinceLastPlacement = Time.time - lastPlacementTime;
        comboStreak = timeSinceLastPlacement <= comboWindow ? comboStreak + 1 : 1;
        lastPlacementTime = Time.time;

        int comboBonus = Mathf.Max(0, comboStreak - 1) * 25;
        int accuracyBonus = Mathf.RoundToInt(baseScorePerBlock * accuracy);
        int perfectBonus = isPerfect ? baseScorePerBlock : 0;
        int gainedScore = baseScorePerBlock + comboBonus + accuracyBonus + perfectBonus;

        totalScore += gainedScore;

        return gainedScore;
    }

    private IEnumerator PulseBlock(Transform blockTransform)
    {
        Vector3 startScale = blockTransform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < impactPulseDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / impactPulseDuration;
            Vector3 squashScale = new(
                startScale.x * (1f + squashAmount),
                startScale.y * (1f - squashAmount),
                startScale.z * (1f + squashAmount)
            );

            blockTransform.localScale = Vector3.Lerp(startScale, squashScale, progress);
            yield return null;
        }

        elapsedTime = 0f;
        Vector3 stretchScale = new(
            startScale.x * (1f - squashAmount * 0.45f),
            startScale.y * (1f + squashAmount * 0.65f),
            startScale.z * (1f - squashAmount * 0.45f)
        );

        while (elapsedTime < impactPulseDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / impactPulseDuration;
            blockTransform.localScale = Vector3.Lerp(blockTransform.localScale, stretchScale, progress);
            yield return null;
        }

        elapsedTime = 0f;

        while (elapsedTime < impactPulseDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / impactPulseDuration;
            blockTransform.localScale = Vector3.Lerp(stretchScale, startScale, progress);
            yield return null;
        }

        blockTransform.localScale = startScale;
    }

    private IEnumerator HitStop(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    private static string GetPrecisionLabel(float accuracy)
    {
        if (accuracy >= 0.9f)
        {
            return "CENTERED";
        }

        if (accuracy >= 0.65f)
        {
            return "GOOD";
        }

        return "OK";
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
        if (gameMode == StackGameMode.Relax)
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
        if (gameMode == StackGameMode.Relax)
        {
            hud.ShowDanger($"{reason} Try again");
            EndRun();
            StartCoroutine(RestartCleanAfterDelay());
            return;
        }

        EndRun();
        StartCoroutine(RestartAfterFall());
    }

    private IEnumerator RestartCleanAfterDelay()
    {
        isRestarting = true;
        yield return new WaitForSeconds(restartDelay);
        StartNewGame();
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
