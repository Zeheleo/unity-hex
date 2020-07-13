using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;         // IEnumerator
using System.Collections.Generic; // Queue

public class HexGrid : MonoBehaviour
{
    // Larger Map
    public int chunkCountX = 4, chunkCountZ = 3;
    int cellCountX, cellCountZ;
    public HexGridChunk chunkPrefab;
    HexGridChunk[] chunks;

    public HexCell cellPrefab;
    public Text cellTextPrafab;
    public Texture2D noiseSource;

    HexCell[] hexCells;

    public Color touchedColor = Color.gray;

    HexCellPriorityQueue searchFrontier; // Search Priority Queue
    int searchFrontierPhase;             // A* skip

    private void OnEnable()
    {
        if (!Hex.noiseSource)
        {
            Hex.noiseSource = noiseSource;
            Hex.InitializeHashGrid(seed);
            Hex.colors = colors;
        }
    }

    private void Awake()
    {
        Hex.noiseSource = noiseSource;
        Hex.InitializeHashGrid(seed);
        Hex.colors = colors;

        cellCountX = chunkCountX * Hex.chunkSizeX;
        cellCountZ = chunkCountZ * Hex.chunkSizeZ;

        CreateChunks();
        CreateCells();
    }

    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];
        for (int z = 0, count = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[count++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    void CreateCells()
    {
        hexCells = new HexCell[cellCountX * cellCountZ];
        for (int z = 0, count = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                CreateCell(x, z, count++);
            }
        }
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            // HandleInput();
        }
    }

    private void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            // ColorCell(hit.point);
        }
    }

    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPositionToHexCoordinates(position);
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;

        return hexCells[index];
    }

    public HexCell GetCell(HexCoordinates coordinate)
    {
        int z = coordinate.Z;
        if (z < 0 || z >= cellCountZ)
        {
            return null;
        }
        int x = coordinate.X + z / 2;
        if (x < 0 || x >= cellCountX)
        {
            return null;
        }

        return hexCells[x + z * cellCountX];
    }

    void CreateCell(int x, int z, int count)
    {
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (Hex.innerRadius * 2f);
        position.y = 0f;
        position.z = z * (Hex.outerRadius * 1.5f);

        HexCell cell = hexCells[count] = Instantiate<HexCell>(cellPrefab);
        cell.name = "Cell" + x.ToString() + "||" + z.ToString();
        // cell.transform.SetParent(transform, false);
        cell.transform.localPosition = position;
        cell.hexCoordinates = HexCoordinates.FromOffsetToHexCoordinates(x, z);

        // Neighboring
        if (x > 0)
        {
            // Connecting from right to left, vice versa
            cell.SetNeighbor(HexDirection.Left, hexCells[count - 1]);
        }
        if (z > 0)
        {
            if ((z & 1) == 0) // even number - masking
            {
                // Connecting from top-left to down-right, vice versa
                cell.SetNeighbor(HexDirection.DownRight, hexCells[count - cellCountX]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.DownLeft, hexCells[count - cellCountX - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.DownLeft, hexCells[count - cellCountX]);
                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.DownRight, hexCells[count - cellCountX + 1]);
                }
            }
        }

        Text cellText = Instantiate<Text>(cellTextPrafab);
        cellText.name = "Text " + x.ToString() + "||" + z.ToString();
        // cellText.rectTransform.SetParent(hexCanvas.transform, false);
        cellText.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        // cellText.text = cell.hexCoordinates.ToStringOnSeperateLines(); CHANGE INTO DISTANCE SYSTEM

        cell.uiRect = cellText.rectTransform;
        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }

    void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / Hex.chunkSizeX;
        int chunkZ = z / Hex.chunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        int localX = x - chunkX * Hex.chunkSizeX;
        int localZ = z - chunkZ * Hex.chunkSizeZ;
        chunk.AddCell(localX + localZ * Hex.chunkSizeX, cell);
    }

    public void ShowUI(bool visible)
    {
        for (int count = 0; count < chunks.Length; count++)
        {
            chunks[count].ShowUI(visible);
        }
    }

    // Features
    public int seed;

    public Color[] colors;

    // Saves
    public void Save(BinaryWriter writer)
    {
        for (int count = 0; count < hexCells.Length; count++)
        {
            hexCells[count].Save(writer);
        }
    }

    public void Load(BinaryReader reader)
    {
        // StopAllCoroutines();
        ClearPath();

        for (int count = 0; count < hexCells.Length; count++)
        {
            hexCells[count].Load(reader);
        }

        for (int count = 0; count < chunks.Length; count++)
        {
            chunks[count].Refresh();
        }
    }

    // Distance
    /*IEnumerator*/
    bool Search(HexCell fromCell, HexCell toCell, int speed)
    {
        searchFrontierPhase += 2;

        if(searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }
        else
        {
            searchFrontier.Clear();
        }


        //for (int count = 0; count < hexCells.Length; count++)
        //{
        //    hexCells[count].SetLabel(null);
        //    hexCells[count].DisableOutline();
        //}
        //fromCell.EnableOutline(Color.blue);
        // -----------------------------------
        //toCell.EnableOutline(Color.red); 
        // WaitForSeconds delay = new WaitForSeconds(1 / 360f);
        // List<HexCell> frontier = new List<HexCell>();
        // ListPool<HexCell>.Add(frontier);
        // frontier.Add(fromCell);
        // frontier.RemoveAt(0);

        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);

        while (searchFrontier.Count > 0)
        {
            // yield return delay;
            HexCell current = searchFrontier.Dequeue();    

            if (current == toCell)
            {
                // current = current.PathFrom;

                //while(current != fromCell)
                //{
                //    int turn = current.Distance / speed;
                //    current.SetLabel(turn.ToString());

                //    current.EnableOutline(Color.white);
                //    current = current.PathFrom;
                //}
                //toCell.EnableOutline(Color.red);
                //break;

                return true;
            }

            int currentTurn = current.Distance / speed;

            for (HexDirection dir = HexDirection.TopRight; dir <= HexDirection.TopLeft; dir++)
            {
                HexCell neighbor = current.GetNeighbor(dir);

                if (neighbor == null || neighbor.SearchPhase > searchFrontierPhase)
                    continue;

                if (neighbor.IsUnderwater)
                    continue;


                HexEdgeType edgeType = current.GetEdgeType(neighbor);
                if (edgeType == HexEdgeType.Cliff)
                    continue;

                // int distance = current.Distance;
                int moveCost;
                if (current.HasRoadThroughEdge(dir))
                {
                    moveCost = 1;
                }
                else if (current.HasWallThroughEdge(dir))
                {
                    continue;
                }
                else
                {
                    moveCost = (edgeType == HexEdgeType.Flat ? 2 : 4); // Non-Road
                    // moveCost += // Objective
                }

                int distance = current.Distance + moveCost;
                int turn = distance / speed;
                if (turn > currentTurn)
                    distance = turn * speed + moveCost;

                if(neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    // neighbor.SetLabel(turn.ToString());
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic = neighbor.hexCoordinates.DistanceTo(toCell.hexCoordinates);
                    searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    int pastPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    // neighbor.SetLabel(turn.ToString());
                    neighbor.PathFrom = current;
                    searchFrontier.Change(neighbor, pastPriority);
                }
            }
        }

        return false;
    }

    public void FindPath(HexCell fromCell, HexCell toCell, int speed)
    {
        // StopAllCoroutines();
        // StartCoroutine(Search(fromCell, toCell, speed));

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        ClearPath();
        currentPathFrom = fromCell;
        currentPathTo = toCell;
        currentPathExists = Search(fromCell, toCell, speed);
        ShowPath(speed);

        sw.Stop();
        Debug.Log(sw.ElapsedMilliseconds);
    }

    HexCell currentPathFrom, currentPathTo;
    bool currentPathExists;

    void ShowPath(int speed)
    {
        if(currentPathExists)
        {
            HexCell current = currentPathTo;
            while(current != currentPathFrom)
            {
                int turn = current.Distance / speed;
                current.SetLabel(turn.ToString());
                current.EnableOutline(Color.white);
                current = currentPathFrom;
            }
        }

        currentPathFrom.EnableOutline(Color.blue);
        currentPathTo.EnableOutline(Color.red);
    }

    void ClearPath()
    {
        if(currentPathExists)
        {
            HexCell current = currentPathTo;
            while(current != currentPathFrom)
            {
                current.SetLabel(null);
                // current.EnableOutline(Color.white);
                current.DisableOutline();
                current = current.PathFrom;
            }

            current.DisableOutline();
            currentPathExists = false;
        }
        else if(currentPathFrom)
        {
            currentPathFrom.DisableOutline();
            currentPathTo.DisableOutline();
        }

        currentPathFrom = currentPathTo = null;
    }
}

