using System.Collections.Generic;
using UnityEngine;

public sealed class ArenaGrid : MonoBehaviour
{
    private const float GridSpacing = 2f;
    private const float GridLineWidth = 0.035f;
    private const float GridOverscan = 12f;
    private const float MinimumVisualSize = 32f;

    [SerializeField] private SpriteRenderer floorRenderer;
    [SerializeField] private MeshFilter gridMeshFilter;
    [SerializeField] private MeshRenderer gridMeshRenderer;
    [SerializeField] private Camera arenaCamera;

    private Transform followTarget;
    private Material gridMaterial;
    private float currentVisualWidth;
    private float currentVisualHeight;

    private void Awake()
    {
        EnsureGridMaterial();
        RefreshVisualLayout(forceRebuild: true);
    }

    public void SetFollowTarget(Transform cameraTarget)
    {
        followTarget = cameraTarget;
        if (followTarget == null)
        {
            return;
        }

        RefreshVisualLayout(forceRebuild: true);
        RecenterVisuals();
    }

    private void LateUpdate()
    {
        if (followTarget == null)
        {
            return;
        }

        RefreshVisualLayout(forceRebuild: false);
        RecenterVisuals();
    }

    private void EnsureGridMaterial()
    {
        if (gridMeshRenderer == null || gridMaterial != null)
        {
            return;
        }

        gridMaterial = CreateGridMaterial();
        gridMeshRenderer.sharedMaterial = gridMaterial;
        gridMeshRenderer.sortingOrder = -10;
    }

    private Material CreateGridMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new(shader);
        material.color = new Color(0.16f, 0.19f, 0.2f, 0.55f);
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", 0f);
        }

        return material;
    }

    private void RefreshVisualLayout(bool forceRebuild)
    {
        if (arenaCamera == null || floorRenderer == null || gridMeshFilter == null)
        {
            return;
        }

        float viewportHeight = arenaCamera.orthographicSize * 2f;
        float viewportWidth = viewportHeight * arenaCamera.aspect;
        float visualWidth = Mathf.Max(MinimumVisualSize, SnapSize(viewportWidth + GridOverscan));
        float visualHeight = Mathf.Max(MinimumVisualSize, SnapSize(viewportHeight + GridOverscan));

        if (!forceRebuild &&
            Mathf.Approximately(currentVisualWidth, visualWidth) &&
            Mathf.Approximately(currentVisualHeight, visualHeight))
        {
            return;
        }

        currentVisualWidth = visualWidth;
        currentVisualHeight = visualHeight;
        floorRenderer.transform.localScale = new Vector3(currentVisualWidth, currentVisualHeight, 1f);

        if (gridMeshFilter.sharedMesh != null)
        {
            Destroy(gridMeshFilter.sharedMesh);
        }

        gridMeshFilter.sharedMesh = BuildGridMesh(currentVisualWidth, currentVisualHeight);
    }

    private void RecenterVisuals()
    {
        if (followTarget == null)
        {
            return;
        }

        Vector3 targetPosition = followTarget.position;
        transform.position = new Vector3(targetPosition.x, targetPosition.y, 0f);

        if (gridMeshFilter != null)
        {
            gridMeshFilter.transform.localPosition = new Vector3(
                -GetGridOffset(targetPosition.x),
                -GetGridOffset(targetPosition.y),
                0f);
        }
    }

    private Mesh BuildGridMesh(float visualWidth, float visualHeight)
    {
        int verticalLineCount = Mathf.FloorToInt(visualWidth / GridSpacing) + 1;
        int horizontalLineCount = Mathf.FloorToInt(visualHeight / GridSpacing) + 1;
        int quadCount = verticalLineCount + horizontalLineCount;

        List<Vector3> vertices = new(quadCount * 4);
        List<int> triangles = new(quadCount * 6);

        float halfWidth = visualWidth * 0.5f;
        float halfHeight = visualHeight * 0.5f;

        for (int i = 0; i < verticalLineCount; i++)
        {
            float offset = -halfWidth + i * GridSpacing;
            AddQuad(vertices, triangles, new Vector2(offset, 0f), new Vector2(GridLineWidth, visualHeight));
        }

        for (int i = 0; i < horizontalLineCount; i++)
        {
            float offset = -halfHeight + i * GridSpacing;
            AddQuad(vertices, triangles, new Vector2(0f, offset), new Vector2(visualWidth, GridLineWidth));
        }

        Mesh mesh = new();
        mesh.name = "Prototype Arena Grid";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static float SnapSize(float size)
    {
        return Mathf.Ceil(size / GridSpacing) * GridSpacing;
    }

    private static float GetGridOffset(float position)
    {
        return Mathf.Repeat(position, GridSpacing);
    }

    private void AddQuad(List<Vector3> vertices, List<int> triangles, Vector2 center, Vector2 size)
    {
        int start = vertices.Count;
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;

        vertices.Add(new Vector3(center.x - halfWidth, center.y - halfHeight, 0f));
        vertices.Add(new Vector3(center.x - halfWidth, center.y + halfHeight, 0f));
        vertices.Add(new Vector3(center.x + halfWidth, center.y + halfHeight, 0f));
        vertices.Add(new Vector3(center.x + halfWidth, center.y - halfHeight, 0f));

        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 3);
    }

    private void OnDestroy()
    {
        if (gridMeshFilter != null && gridMeshFilter.sharedMesh != null)
        {
            Destroy(gridMeshFilter.sharedMesh);
        }

        if (gridMaterial != null)
        {
            Destroy(gridMaterial);
        }
    }
}
