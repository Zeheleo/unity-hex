using System;
using System.Collections.Generic;
using UnityEngine;

public struct EdgeVertices
{
    public Vector3 v1, v2, v3, v4, v5;

    public EdgeVertices(Vector3 p1, Vector3 p2)
    {
        v1 = p1;
        v2 = Vector3.Lerp(p1, p2, 0.25f);
        v3 = Vector3.Lerp(p1, p2, 0.5f);
        v4 = Vector3.Lerp(p1, p2, 0.75f);
        v5 = p2;
    }

    public EdgeVertices(Vector3 p1, Vector3 p2, float step)
    {
        v1 = p1;
        v2 = Vector3.Lerp(p1, p2, step);
        v3 = Vector3.Lerp(p1, p2, 0.5f);
        v4 = Vector3.Lerp(p1, p2, 1f - step);
        v5 = p2;
    }

    public static EdgeVertices TerraceLerp(EdgeVertices a, EdgeVertices b, int step)
    {
        EdgeVertices result;
        result.v1 = Hex.TerraceLerp(a.v1, b.v1, step);
        result.v2 = Hex.TerraceLerp(a.v2, b.v2, step);
        result.v3 = Hex.TerraceLerp(a.v3, b.v3, step);
        result.v4 = Hex.TerraceLerp(a.v4, b.v4, step);
        result.v5 = Hex.TerraceLerp(a.v5, b.v5, step);
        return result;
    }
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    Mesh hexMesh;
    MeshCollider hexMeshCollider;
    public bool useCollider, useColors, useUVCoordinates, useUV2Coordinates;

    [NonSerialized] List<Vector3> vertices;
    [NonSerialized] List<Color> colors;
    [NonSerialized] List<int> triangles;
    [NonSerialized] List<Vector2> uvs, uv2s;


    private void Awake()
    {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();

        if(useCollider)
            hexMeshCollider = gameObject.AddComponent<MeshCollider>();

        hexMesh.name = "Hex Mesh";
        vertices = new List<Vector3>();
        colors = new List<Color>();
        triangles = new List<int>();
    }

    public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v4)
    {
        int vertextIndex = vertices.Count;
        vertices.Add(Hex.Perturb(v1));
        vertices.Add(Hex.Perturb(v2));
        vertices.Add(Hex.Perturb(v4));
        triangles.Add(vertextIndex);
        triangles.Add(vertextIndex + 1);
        triangles.Add(vertextIndex + 2);
    }

    public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v4);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    public void AddTriangleColor(Color color)
    {
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }

    public void AddTriangleColor (Color c1, Color c2, Color c3)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
    }

    public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector3 uv3)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
    }

    public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector3 uv3)
    {
        uv2s.Add(uv1);
        uv2s.Add(uv2);
        uv2s.Add(uv3);
    }

    public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v4, Vector3 v5)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(Hex.Perturb(v1));
        vertices.Add(Hex.Perturb(v2));
        vertices.Add(Hex.Perturb(v4));
        vertices.Add(Hex.Perturb(v5));

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex +2);
        triangles.Add(vertexIndex +1);
        triangles.Add(vertexIndex +1);
        triangles.Add(vertexIndex +2);
        triangles.Add(vertexIndex +3);
    }

    public void AddQuadColor (Color c1, Color c2)
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c2);
    }

    public void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
        colors.Add(c4);
    }

    public void AddQuadColor(Color color)
    {
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }

    public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector3 uv3, Vector3 uv4)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
        uvs.Add(uv4);
    }

    public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
    {
        uvs.Add(new Vector2(uMin, vMin));
        uvs.Add(new Vector2(uMax, vMin));
        uvs.Add(new Vector2(uMin, vMax));
        uvs.Add(new Vector2(uMax, vMax));
    }

    public void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector3 uv3, Vector3 uv4)
    {
        uv2s.Add(uv1);
        uv2s.Add(uv2);
        uv2s.Add(uv3);
        uv2s.Add(uv4);
    }

    public void AddQuadUV2(float uMin, float uMax, float vMin, float vMax)
    {
        uv2s.Add(new Vector2(uMin, vMin));
        uv2s.Add(new Vector2(uMax, vMin));
        uv2s.Add(new Vector2(uMin, vMax));
        uv2s.Add(new Vector2(uMax, vMax));
    }

    public void AddQuadUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = vertices.Count;

        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        vertices.Add(v4);

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }

    public void Clear()
    {
        hexMesh.Clear();
        vertices = ListPool<Vector3>.Get();

        if (useColors)
            colors = ListPool<Color>.Get();

        if (useUVCoordinates)
            uvs = ListPool<Vector2>.Get();

        if (useUV2Coordinates)
            uv2s = ListPool<Vector2>.Get();

        

        triangles = ListPool<int>.Get();
    }

    public void Apply()
    {
        hexMesh.SetVertices(vertices);
        ListPool<Vector3>.Add(vertices);

        if (useColors)
        {
            hexMesh.SetColors(colors);
            ListPool<Color>.Add(colors);
        }

        if(useUVCoordinates)
        {
            hexMesh.SetUVs(0, uvs);
            ListPool<Vector2>.Add(uvs);
        }
            
        if(useUV2Coordinates)
        {
            hexMesh.SetUVs(1, uv2s);
            ListPool<Vector2>.Add(uv2s);
        }

        hexMesh.SetTriangles(triangles, 0);
        ListPool<int>.Add(triangles);

        hexMesh.RecalculateNormals();

        // Drawing exactly same Mesh Twice -> cleaning the mesh failed error occurs
        if (useCollider)
            hexMeshCollider.sharedMesh = hexMesh;

        /*
        hexMesh.vertices = vertices.ToArray();
        hexMesh.colors = colors.ToArray();
        hexMesh.triangles = triangles.ToArray();
        hexMesh.RecalculateNormals();
        */
    }
}

