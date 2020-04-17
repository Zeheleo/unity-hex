using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour
{
    public HexCoordinates hexCoordinates;
    public Color color;
    public RectTransform uiRect;
    
    private int elevation = int.MinValue;

    [SerializeField]
    HexCell[] neighbors;

    public HexGridChunk parentChunk;

    void Refresh()
    {
        if (parentChunk)
        {
            parentChunk.Refresh();
            for(int i = 0; i <neighbors.Length; i++)
            {
                HexCell neighbor = neighbors[i];
                if(neighbor != null && neighbor.parentChunk != parentChunk)
                {
                    neighbor.parentChunk.Refresh();
                }
            }
        }
    }

    public HexCell GetNeighbor (HexDirection direction)
    {
        return neighbors[(int)direction];
    }

    public void SetNeighbor (HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    public Vector3 Position
    {
        get
        {
            return transform.localPosition;
        }
    }

    public int Elevation
    {
        get
        {
            return elevation;
        }

        set
        {
            if (elevation == value)
                return;

            elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * Hex.elevationStep;

            // Perturb
            position.y += (Hex.SampleNoise(position).y * 2f - 1f) * Hex.elevationPerturbStrength;
            transform.localPosition = position;

            Vector3 textPosition = uiRect.localPosition;
            textPosition.z = -position.y;
            uiRect.localPosition = textPosition;

            Refresh();
        }
    }

    public Color Color
    {
        get
        {
            return color;
        }
        set
        {
            if(color == value)
            {
                return;
            }
            color = value;
            Refresh();
        }
    }


    public HexEdgeType GetEdgeType(HexDirection dir)
    {
        return Hex.GetEdgeType(elevation, neighbors[(int)dir].elevation);
    }

    public HexEdgeType GetEdgeType (HexCell target)
    {
        return Hex.GetEdgeType(elevation, target.elevation);
    }
};