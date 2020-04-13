using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour
{
    public HexCoordinates hexCoordinates;
    public Color color;
    public RectTransform uiRect;
    
    private int elevation;

    [SerializeField]
    HexCell[] neighbors;

 

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
            elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * Hex.elevationStep;

            // Perturb
            position.y += (Hex.SampleNoise(position).y * 2f - 1f) * Hex.elevationPerturbStrength;
            transform.localPosition = position;

            Vector3 textPosition = uiRect.localPosition;
            textPosition.z = -position.y;
            uiRect.localPosition = textPosition;
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