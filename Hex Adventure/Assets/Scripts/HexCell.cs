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

            if (hasOutgoingRiver &&
                elevation < GetNeighbor(outgoingRiver).elevation)
                RemoveOutgoingRiver();

            if (hasIncomingRiver &&
                elevation < GetNeighbor(incomingRiver).elevation)
                RemoveIncomingRiver();

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

// River Flow 
    private HexDirection incomingRiver, outgoingRiver;
    private bool hasIncomingRiver, hasOutgoingRiver;

    public bool HasIncomingRiver
    {
        get
        {
            return hasIncomingRiver;
        }
    }

    public bool HasOutgoingRiver
    {
        get
        {
            return HasOutgoingRiver;
        }
    }

    public HexDirection IncomingRiver
    {
        get
        {
            return incomingRiver;
        }
    }

    public HexDirection OutgoingRiver
    {
        get
        {
            return outgoingRiver;
        }
    }

    public bool HasRiver
    {
        get
        {
            return hasIncomingRiver || hasOutgoingRiver;
        }
    }

    public bool HasRiverBeginOrEnd
    {
        get
        {
            return hasIncomingRiver != hasOutgoingRiver;
        }
    }

    public bool HasRiverThroughEdge(HexDirection dir)
    {
        return hasIncomingRiver && incomingRiver == dir ||
            hasOutgoingRiver && outgoingRiver == dir;
    }

    public void RemoveOutgoingRiver()
    {
        if(!hasOutgoingRiver)
        {
            return;
        }

        hasOutgoingRiver = false;
        RefreshSelf();

        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelf();
    }

    public void RemoveIncomingRiver ()
    {
        if(!hasIncomingRiver)
        {
            return;
        }

        hasIncomingRiver = false;
        RefreshSelf();

        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.hasOutgoingRiver = false;
        neighbor.RefreshSelf();
    }

    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    private void RefreshSelf()
    {
        parentChunk.Refresh();
    }

    public void SetOutgoingRiver (HexDirection dir)
    {
// river already exists
        if(hasOutgoingRiver && OutgoingRiver == dir)
        {
            return;
        }

        HexCell neighbor = GetNeighbor(dir);

// No neighbor cases + uphill flow
        if(!neighbor || elevation < neighbor.elevation)
        {
            return;
        }

// Clear up existing rivers
        RemoveOutgoingRiver();
        if(hasIncomingRiver && incomingRiver == dir)
        {
            RemoveIncomingRiver();
        }

        hasOutgoingRiver = true;
        outgoingRiver = dir;
        RefreshSelf();

        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = dir.Opposite();
        neighbor.RefreshSelf();
    }

    public float StreamBedY
    {
        get
        {
            return (elevation + Hex.streamBedElevationOffSet) * Hex.elevationStep;
        }
    }

    public float RiverSurfaceY
    {
        get
        {
            return
                (elevation + Hex.riverSurfaceElevationOffset) * Hex.elevationStep;
        }
    }
};