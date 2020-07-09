using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI; // Text

public class HexCell : MonoBehaviour
{
    public HexCoordinates hexCoordinates;
    public RectTransform uiRect;
    int _terrainTypeIndex;

    private int elevation = -10;

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

    private void Update()
    {
        // Color Transition
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
            RefreshElevation();

            if (hasOutgoingRiver &&
                elevation < GetNeighbor(outgoingRiver).elevation)
                RemoveOutgoingRiver();

            if (hasIncomingRiver &&
                elevation < GetNeighbor(incomingRiver).elevation)
                RemoveIncomingRiver();

            for(int count =0; count < roads.Length; count++)
            {
                if(roads[count] && GetElevationDifference((HexDirection)count) > 1)
                {
                    SetRoad(count, false);
                }
            }

            Refresh();
        }
    }

    void RefreshElevation()
    {
        Vector3 position = transform.localPosition;
        position.y = elevation * Hex.elevationStep;

        // Perturb
        position.y += (Hex.SampleNoise(position).y * 2f - 1f) * Hex.elevationPerturbStrength;
        transform.localPosition = position;

        Vector3 textPosition = uiRect.localPosition;
        textPosition.z = -position.y;
        uiRect.localPosition = textPosition;
    }

    public Color Color
    {
        get
        {
            return Hex.colors[_terrainTypeIndex];
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
        // if(!neighbor || elevation < neighbor.elevation)
        if(!isValidRiverDestination(neighbor))
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
        SpecIndex = 0;

        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = dir.Opposite();
        neighbor.SpecIndex = 0;

        SetRoad((int)dir, false);   
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
                (elevation + Hex.waterElevationSurface) * Hex.elevationStep;
        }
    }

 // Roads
    [SerializeField]
    bool[] roads;   

    public bool HasRoadThroughEdge(HexDirection dir)
    {
        return roads[(int)dir];
    }

    public bool HasRoads
    {
        get
        {
            for(int count =0; count < roads.Length; count++)
            {
                if(roads[count])
                {
                    return true;
                }
            }

            return false;
        }
    }

    public void AddRoad(HexDirection dir)
    {
        if(!roads[(int)dir] && !HasRiverThroughEdge(dir) && GetElevationDifference(dir) <= 1)
        {
            SetRoad((int)dir, true);
        }
    }

    public void RemoveRoads()
    {
        for(int count = 0; count < neighbors.Length; count++)
        {
            if(roads[count])
            {
                SetRoad(count, false);
            }
        }
    }

    public void SetRoad(int index, bool state)
    {
        roads[index] = state;
        neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
        neighbors[index].RefreshSelf();
        RefreshSelf();
    }

    public int GetElevationDifference(HexDirection dir)
    {
        int diff = elevation - GetNeighbor(dir).elevation;
        return diff >= 0 ? diff : -diff;
    }

    public HexDirection RiverBeginOrEndDirection
    {
        get
        {
            return hasIncomingRiver ? incomingRiver : outgoingRiver;
        }
    }

// Water
    public int WaterLevel
    {
        get
        {
            return waterLevel;
        }

        set
        {
            if (waterLevel == value)
                return;

            waterLevel = value;

            // TODO : Enforce certain terrain
            Refresh();
        }
    }

    public bool IsUnderwater
    {
        get
        {
            return waterLevel > elevation;
        }
    }

    public float WaterSurfaceY
    {
        get
        {
            return
                (waterLevel + Hex.waterElevationSurface) * Hex.elevationStep;
        }
    }

    int waterLevel;

    bool isValidRiverDestination(HexCell neighbor)
    {
        return neighbor && (elevation >= neighbor.elevation || waterLevel == neighbor.elevation);
    }

// Prop Features
    int _TreeLevel;

    public int TreeLevel
    {
        get
        {
            return _TreeLevel;
        }

        set
        {
            if (_TreeLevel != value)
                _TreeLevel = value;

            RefreshSelf();
        }
    }

    int _StoneLevel;

    public int StoneLevel
    {
        get
        {
            return _StoneLevel;
        }

        set
        {
            if (_StoneLevel != value)
                _StoneLevel = value;

            RefreshSelf();
        }
    }

    // Wall
    [SerializeField]
    bool[] walls;

    public bool HasWallThroughEdge(HexDirection dir)
    {
        return walls[(int)dir];
    }

    public void AddWall(HexDirection dir)
    {
        if (!walls[(int)dir]) // && !HasRiverThroughEdge(dir) && GetElevationDifference(dir) <= 1)
        {
            SetWall((int)dir, true);
        }
    }

    public void SetWall(int index, bool state)
    {
        walls[index] = state;

        if(!neighbors[index])
        {
            return;
        }
        else
        {
            neighbors[index].walls[(int)((HexDirection)index).Opposite()] = state;
            neighbors[index].RefreshSelf();
        }

        RefreshSelf();
    }

    public void RemoveWalls(HexDirection dir)
    {
        //for (int count = 0; count < walls.Length; count++)
        //{
        //    if (walls[count])
        //    {
        //        SetWall(count, false);
        //    }
        //}
        if(walls[(int)dir])
        {
            SetWall((int)dir, false);
        }
    }
    /*
    public bool hasWall(HexDirection dir)
    {
        for()

        return false;
    }
    */

    /*
    bool _IsWall;

    public bool IsWall
    {
        get
        {
            return _IsWall;
        }

        set
        {
            if (_IsWall != value)
            {
                _IsWall = value;
                Refresh();
            }
        }
    }*/

    int _SpecIndex;

    public int SpecIndex
    {
        get
        {
            return _SpecIndex;
        }
        set
        {
            if(_SpecIndex != value && !HasRiver)
            {
                _SpecIndex = value;
                RefreshSelf();
            }
        }
    }

    public bool IsSpecial
    {
        get
        {
            return _SpecIndex > 0;
        }
    }

    public int TerrainTypeIndex
    {
        get
        {
            return _terrainTypeIndex;
        }

        set
        {
            if(_terrainTypeIndex != value)
            {
                _terrainTypeIndex = value;
                Refresh();
            }
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)_terrainTypeIndex);
        writer.Write((byte)elevation);
        writer.Write((byte)waterLevel);
        writer.Write((byte)_TreeLevel);
        writer.Write((byte)_StoneLevel);
        writer.Write((byte)_SpecIndex);
        
        if(hasIncomingRiver)
        {
            writer.Write((byte)(incomingRiver + 128));
        }
        else
        {
            writer.Write((byte)0);
        }
        
        if(hasOutgoingRiver)
        {
            writer.Write((byte)(outgoingRiver + 128));
        }
        else
        {
            writer.Write((byte)0);
        }

        for(int count = 0; count < walls.Length; count++)
            writer.Write(walls[count]);

        int roadFlags = 0;
        for (int count = 0; count < roads.Length; count++)
        {
            if(roads[count])
            {
                roadFlags |= 1 << count;
            }
        }

        writer.Write((byte)roadFlags);
    }

    public void Load(BinaryReader reader)
    {
        _terrainTypeIndex = reader.ReadByte();
        elevation = reader.ReadByte(); RefreshElevation();
        waterLevel = reader.ReadByte();
        _TreeLevel = reader.ReadByte();
        _StoneLevel = reader.ReadByte();
        _SpecIndex = reader.ReadByte();
        
        byte riverData = reader.ReadByte();
        if(riverData >= 128)
        {
            hasIncomingRiver = true;
            incomingRiver = (HexDirection)(riverData - 128);
        }
        else
        {
            hasIncomingRiver = false;
        }

        riverData = reader.ReadByte();
        if(riverData >= 128)
        {
            hasOutgoingRiver = true;
            outgoingRiver = (HexDirection)(riverData - 128);
        }
        else
        {
            hasOutgoingRiver = false;
        }

        for (int count = 0; count < walls.Length; count++)
            walls[count] = reader.ReadBoolean();

        int roadFlags = reader.ReadByte();
        for (int count = 0; count < roads.Length; count++)
            roads[count] = (roadFlags & (1 << count)) != 0;
    }

// Distance Feature
    int _distance;

    private void UpdateDistanceLabel()
    {
        Text label = uiRect.GetComponent<Text>();
        label.text = (_distance == int.MaxValue ? "" : _distance.ToString());
    }

    public int Distance
    {
        get
        {
            return _distance;
        }

        set
        {
            _distance = value;
            UpdateDistanceLabel();
        }
    }

// Outline feature
    public void DisableOutline()
    {
        Image outline = uiRect.GetChild(0).GetComponent<Image>();
        outline.enabled = false;
    }

    public void EnableOutline(Color color)
    {
        Image outline = uiRect.GetChild(0).GetComponent<Image>();
        outline.color = color;
        outline.enabled = true;
    }

    public HexCell PathFrom
    {
        get; set;
    }


    public int SearchHeuristic
    {
        get; set;
    }

    public int SearchPriority
    {
        get
        {
            return _distance + SearchHeuristic;
        }
    }

    public HexCell NextWithSamePriority { get; set; }
};