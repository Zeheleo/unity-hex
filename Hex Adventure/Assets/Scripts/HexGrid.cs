using UnityEngine;
using UnityEngine.UI;

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

    public Color defaultColor = Color.white;
    public Color touchedColor = Color.gray;

    private void OnEnable()
    {
        Hex.noiseSource = noiseSource;
    }

    private void Awake()
    {
        Hex.noiseSource = noiseSource;

        cellCountX = chunkCountX * Hex.chunkSizeX;
        cellCountZ = chunkCountZ * Hex.chunkSizeZ;

        CreateChunks();
        CreateCells();
    }

    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];
        for(int z =0, count =0; z < chunkCountZ; z++)
        {
            for(int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[count++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    void CreateCells()
    { 
        hexCells = new HexCell[cellCountX * cellCountZ];
		for(int z = 0, count = 0; z< cellCountZ; z++)
		{
			for(int x = 0; x < cellCountX; x++)
			{
				CreateCell(x, z, count++);
			}
		}
	}

    private void Update()
    {
       if(Input.GetMouseButton(0))
        {
            // HandleInput();
        }
    }

    private void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(inputRay, out hit))
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
        if(z < 0 || z >= cellCountZ)
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
        cell.color = defaultColor;

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
                if(x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.DownRight, hexCells[count - cellCountX + 1]);
                }
            }
        }

        Text cellText = Instantiate<Text>(cellTextPrafab);
        cellText.name = "Text " + x.ToString() + "||" + z.ToString();
        // cellText.rectTransform.SetParent(hexCanvas.transform, false);
        cellText.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        cellText.text = cell.hexCoordinates.ToStringOnSeperateLines();

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
}