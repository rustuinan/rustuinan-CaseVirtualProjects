using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum FormationType
{
    Line,
    Wedge,
    Square,
    Arc
}

public class BattleSetupUI : MonoBehaviour
{
    [Header("Referanslar")]
    public UnitPoolManager poolManager;
    public CameraManager cameraManager;

    [Header("UI Root")]
    public GameObject rootPanel;

    [Header("Cube Inputları")]
    public TMP_InputField cubeMeleeInput;
    public TMP_InputField cubeArcherInput;
    public TMP_InputField cubeCommanderInput;

    [Header("Sphere Inputları")]
    public TMP_InputField sphereMeleeInput;
    public TMP_InputField sphereArcherInput;
    public TMP_InputField sphereCommanderInput;

    [Header("Formasyon Seçimi")]
    public TMP_Dropdown cubeFormationDropdown;
    public TMP_Dropdown sphereFormationDropdown;

    [Header("Spawn Ayarları")]
    public Transform cubeSpawnOrigin;
    public Transform sphereSpawnOrigin;

    [Tooltip("Yan yana birim aralığı")]
    public float spacing = 1.5f;

    [Tooltip("Arkadaki sıra kaç 'grid birimi' geride olsun?")]
    public float rowOffset = 2f;

    [Tooltip("Tek formasyonda max birim sayısı")]
    public int maxUnitsPerType = 200;

    private bool battleStarted = false;

    private void Start()
    {
        if (rootPanel != null)
            rootPanel.SetActive(true);

        if (poolManager == null)
            poolManager = UnitPoolManager.Instance;

        SetupDropdown(cubeFormationDropdown);
        SetupDropdown(sphereFormationDropdown);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetupDropdown(TMP_Dropdown dd)
    {
        if (dd == null) return;

        dd.ClearOptions();
        var options = new List<string>
        {
            "Çizgi",
            "Üçgen",
            "Kare",
            "Yay"
        };
        dd.AddOptions(options);
        dd.value = 0;
    }

    private FormationType GetFormation(TMP_Dropdown dd)
    {
        if (dd == null) return FormationType.Line;

        switch (dd.value)
        {
            case 0: return FormationType.Line;
            case 1: return FormationType.Wedge;
            case 2: return FormationType.Square;
            case 3: return FormationType.Arc;
            default: return FormationType.Line;
        }
    }

    public void OnClickStartBattle()
    {
        Debug.Log("[BattleSetupUI] Start butonuna basıldı");

        if (battleStarted)
            return;

        battleStarted = true;

        Time.timeScale = 1f;

        if (poolManager == null)
            poolManager = UnitPoolManager.Instance;

        int cubeMelee = ReadInput(cubeMeleeInput);
        int cubeArcher = ReadInput(cubeArcherInput);
        int cubeCommander = ReadInput(cubeCommanderInput);

        int sphereMelee = ReadInput(sphereMeleeInput);
        int sphereArcher = ReadInput(sphereArcherInput);
        int sphereCommander = ReadInput(sphereCommanderInput);

        cubeMelee = Mathf.Clamp(cubeMelee, 0, maxUnitsPerType);
        cubeArcher = Mathf.Clamp(cubeArcher, 0, maxUnitsPerType);
        cubeCommander = Mathf.Clamp(cubeCommander, 0, maxUnitsPerType);

        sphereMelee = Mathf.Clamp(sphereMelee, 0, maxUnitsPerType);
        sphereArcher = Mathf.Clamp(sphereArcher, 0, maxUnitsPerType);
        sphereCommander = Mathf.Clamp(sphereCommander, 0, maxUnitsPerType);

        FormationType cubeForm = GetFormation(cubeFormationDropdown);
        FormationType sphereForm = GetFormation(sphereFormationDropdown);

        SpawnTeam(
            isCubeTeam: true,
            formation: cubeForm,
            meleeCount: cubeMelee,
            archerCount: cubeArcher,
            commanderCount: cubeCommander,
            origin: cubeSpawnOrigin
        );

        SpawnTeam(
            isCubeTeam: false,
            formation: sphereForm,
            meleeCount: sphereMelee,
            archerCount: sphereArcher,
            commanderCount: sphereCommander,
            origin: sphereSpawnOrigin
        );

        if (rootPanel != null)
            rootPanel.SetActive(false);

        if (cameraManager != null)
        {
            cameraManager.StartBattleWithSpectator();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private int ReadInput(TMP_InputField field)
    {
        if (field == null || string.IsNullOrWhiteSpace(field.text))
            return 0;

        int value;
        if (!int.TryParse(field.text, out value))
            value = 0;

        return value;
    }


    private void SpawnTeam(bool isCubeTeam, FormationType formation,
                           int meleeCount, int archerCount, int commanderCount,
                           Transform origin)
    {
        if (origin == null || poolManager == null)
            return;

        List<Vector2> meleeOffsets = GenerateFormationPositions(formation, meleeCount);

        List<Vector2> archerOffsets = GenerateFormationPositions(formation, archerCount);

        List<Vector2> commanderOffsets = GenerateFormationPositions(FormationType.Line, commanderCount);

        float meleeRowShift = 0f;
        float archerRowShift = -rowOffset;
        float commanderRowShift = rowOffset;

        UnitKind meleeKind = isCubeTeam ? UnitKind.CubeMelee : UnitKind.SphereMelee;
        SpawnFromOffsets(meleeKind, meleeOffsets, origin, meleeRowShift);

        UnitKind archerKind = isCubeTeam ? UnitKind.CubeArcher : UnitKind.SphereArcher;
        SpawnFromOffsets(archerKind, archerOffsets, origin, archerRowShift);

        UnitKind commanderKind = isCubeTeam ? UnitKind.CubeCommander : UnitKind.SphereCommander;
        SpawnFromOffsets(commanderKind, commanderOffsets, origin, commanderRowShift);
    }

    private void SpawnFromOffsets(UnitKind kind, List<Vector2> offsets, Transform origin, float rowShift)
    {
        if (offsets == null || offsets.Count == 0)
            return;

        for (int i = 0; i < offsets.Count; i++)
        {
            GameObject obj = poolManager.Get(kind);
            if (obj == null)
                continue;

            Vector2 off = offsets[i];

            float x = off.x * spacing;
            float z = (off.y + rowShift) * spacing;

            Vector3 worldPos =
                origin.position +
                origin.right * x +
                origin.forward * z;

            obj.transform.position = worldPos;
            obj.transform.rotation = origin.rotation;
            obj.SetActive(true);
        }
    }


    private List<Vector2> GenerateFormationPositions(FormationType formation, int count)
    {
        List<Vector2> list = new List<Vector2>(count);
        if (count <= 0)
            return list;

        switch (formation)
        {
            case FormationType.Line:
                GenerateLine(list, count);
                break;

            case FormationType.Wedge:
                GenerateWedge(list, count);
                break;

            case FormationType.Square:
                GenerateSquare(list, count);
                break;

            case FormationType.Arc:
                GenerateArc(list, count);
                break;
        }

        return list;
    }

    private void GenerateLine(List<Vector2> list, int count)
    {
        float half = (count - 1) * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float x = i - half;
            float z = 0f;
            list.Add(new Vector2(x, z));
        }
    }

    private void GenerateWedge(List<Vector2> list, int count)
    {
        int remaining = count;
        int row = 0;
        float zStep = -1f;

        while (remaining > 0)
        {
            int rowSize = Mathf.Min(remaining, row + 1);
            float half = (rowSize - 1) * 0.5f;
            float z = row * zStep;

            for (int i = 0; i < rowSize; i++)
            {
                float x = i - half;
                list.Add(new Vector2(x, z));
            }

            remaining -= rowSize;
            row++;
        }
    }

    private void GenerateSquare(List<Vector2> list, int count)
    {
        int rowCount = Mathf.CeilToInt(Mathf.Sqrt(count));
        int colCount = Mathf.CeilToInt((float)count / rowCount);

        float halfCols = (colCount - 1) * 0.5f;
        float halfRows = (rowCount - 1) * 0.5f;

        int index = 0;
        for (int r = 0; r < rowCount; r++)
        {
            for (int c = 0; c < colCount; c++)
            {
                if (index >= count)
                    return;

                float x = c - halfCols;
                float z = -(r - halfRows);
                list.Add(new Vector2(x, z));
                index++;
            }
        }
    }

    private void GenerateArc(List<Vector2> list, int count)
    {
        float radius = Mathf.Max(2f, count * 0.4f);
        float totalAngle = Mathf.Deg2Rad * 120f;
        float halfAngle = totalAngle * 0.5f;

        if (count == 1)
        {
            list.Add(new Vector2(0f, 0f));
            return;
        }

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);

            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius * -0.4f;

            x /= spacing;
            z /= spacing;

            list.Add(new Vector2(x, z));
        }
    }
}
