using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HexDirection
{
    TopRight, Right, DownRight, DownLeft, Left, TopLeft
}

public enum HexEdgeType
{
    Flat, Slope, Cliff
}

public static class HexDirectionExtensions
{
    public static HexDirection Opposite (this HexDirection dir)
    {
        return (int)dir < 3 ? (dir + 3) : (dir - 3);
    }

    public static HexDirection Previous (this HexDirection dir)
    {
        return dir == HexDirection.TopRight ? HexDirection.TopLeft : (dir - 1);
    }

    public static HexDirection Next (this HexDirection dir)
    {
        return dir == HexDirection.TopLeft ? HexDirection.TopRight : (dir + 1);
    }
}

public static class Hex
{
    // Hex
    public const float outerToInner = 0.866025f;
    public const float innerToOuter = 1f / outerToInner;

    public const float outerRadius = 10f;
    public const float innerRadius = outerRadius * innerToOuter;

    public const float solidFactor = 0.75f;
    public const float blendFactor = 1f - solidFactor;
    public const float elevationStep = 3f;

    public static Texture2D noiseSource;

    public static Vector3[] points =
    {
        new Vector3(0f, 0f, outerRadius), // p1, top
	    new Vector3(innerRadius, 0f, 0.5f * outerRadius), // p2, top right
	    new Vector3(innerRadius, 0f, -0.5f * outerRadius), // p3, bot right
	    new Vector3(0f, 0f, -outerRadius), // p4, bot
	    new Vector3(-innerRadius, 0f, -0.5f * outerRadius), // p5 bot left
	    new Vector3(-innerRadius, 0f, 0.5f * outerRadius), // p6 top left
        new Vector3(0f, 0f, outerRadius), // p1=7, top
    };

    public static Vector3 GetFirstPoint(HexDirection dir)
    {
        return points[(int)dir];
    }

    public static Vector3 GetSecondPoint(HexDirection dir)
    {
        return points[(int)dir + 1];
    }

    public static Vector3 GetFirstSolidPoint(HexDirection dir)
    {
        return points[(int)dir] * solidFactor;
    }

    public static Vector3 GetSecondSolidPoint(HexDirection dir)
    {
        return points[(int)dir + 1] * solidFactor;
    }

    public static Vector3 GetBridge(HexDirection dir)
    {
        return (points[(int)dir] + points[(int)dir + 1]) * blendFactor;
    }

    // Elevation
    public const int terracesPerSlope = 2;
    public const int terracesSteps = terracesPerSlope * 2 + 1;

    public const float horizontalTerraceStepSize = 1f / (terracesSteps);
    public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        float h = step * horizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;

        float v = ((step + 1) / 2) * Hex.verticalTerraceStepSize;
        a.y += (b.y - a.y) * v;

        return a;
    }

    public static Color TerraceColorLerp(Color a, Color b, int step)
    {
        float h = step * Hex.horizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
    {
        if (elevation1 == elevation2)
            return HexEdgeType.Flat;

        else if (Mathf.Abs(elevation1 - elevation2) == 1)
            return HexEdgeType.Slope;

        else
            return HexEdgeType.Cliff;
    }

    // Noise Sample 
    public static Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * perturbStrength;
        position.z += (sample.z * 2f - 1f) * perturbStrength;
        return position;
    }

    public const float perturbStrength = 0f; // 5f;
    public const float noiseScale = 0.0015f;
    public const float elevationPerturbStrength = 3f;

    public static Vector4 SampleNoise(Vector3 position)
    {
        return noiseSource.GetPixelBilinear(
            position.x * noiseScale,
            position.z * noiseScale);
    }

    // Larger Map
    public static int chunkSizeX = 5;
    public static int chunkSizeZ = 5;

    // River
    public const float streamBedElevationOffSet = -1f;

    public static Vector3 GetSolidEdgeMiddle(HexDirection dir)
    {
        return (points[(int)dir] + points[(int)dir + 1]) * (0.5f * solidFactor);
    }
}
