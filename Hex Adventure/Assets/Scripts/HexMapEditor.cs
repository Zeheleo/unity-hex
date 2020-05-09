using UnityEngine;
using UnityEngine.EventSystems; // EventSystem Entry

public class HexMapEditor : MonoBehaviour
{
    public Color[] colors;

    public HexGrid hexGrid;

    private Color activeColor;
    private int activeElevation;
    private bool applyColor;
    private bool applyElevation = false;
    private int brushSize;

    private bool isDrag;
    private HexDirection dragDirection;
    private HexCell previousCell;

    public void SetBrushSize(float size)
    {
        brushSize = (int)size;
    }

    private void Awake()
    {
        SelectColor(-1);
    }

    private void Update()
    {
        // if (Input.GetMouseButtonDown(0)) // ON CLICK
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject()) // On DOWN
        {
            HandleInput();
        }
        else
        {
            previousCell = null;
        }
    }

    private void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            HexCell currentCell = hexGrid.GetCell(hit.point);

            if(previousCell && previousCell != currentCell)
            {
                ValidateDrag(currentCell);
            }
            else
            {
                isDrag = false;
            }

            EditCells(currentCell);
            previousCell = currentCell;
        }
        else
        {
            previousCell = null;
        }
    }

    private void ValidateDrag(HexCell curCell)
    {
        for(dragDirection = HexDirection.TopRight; dragDirection <= HexDirection.TopLeft; dragDirection++)
        {
            if(previousCell.GetNeighbor(dragDirection) == curCell)
            {
                isDrag = true;
                return;
            }
        }

        isDrag = false;
    }

    private void EditCells(HexCell center)
    {
        int centerX = center.hexCoordinates.X;
        int centerZ = center.hexCoordinates.Z;

        for(int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
        {
            for(int x = centerX -r; x <= centerX + brushSize; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }

        for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++)
        {
            for (int x = centerX - brushSize; x <= centerX + r; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    private void EditCell(HexCell hexCell)
    {
        if (hexCell)
        {
            if (applyColor)
            {
                hexCell.Color = activeColor;
            }

            if (applyElevation)
            {
                hexCell.Elevation = activeElevation;
            }

            if(riverMode == OptionalToggle.No)
            {
                hexCell.RemoveRiver();
            }

            if(roadMode == OptionalToggle.No)
            {
                hexCell.RemoveRoads();
            }

            if(applyWaterLevel)
            {
                hexCell.WaterLevel = activeWaterLevel;
            }

            if(applyTreeLevel)
            {
                hexCell.TreeLevel = activeTreeLevel;
            }

            if(applyStoneLevel)
            {
                hexCell.StoneLevel = activeStoneLevel;
            }


            if (wallMode == OptionalToggle.Yes)
            {
                for(HexDirection dir = HexDirection.TopRight; dir <= HexDirection.TopLeft; dir++)
                {
                    hexCell.AddWall(dir);
                }
            }
            /*
            else if(wallMode == OptionalToggle.No)
            {
                for (HexDirection dir = HexDirection.TopRight; dir <= HexDirection.TopLeft; dir++)
                {
                    hexCell.RemoveWalls(dir);
                }
            }
            */

            if (isDrag)
            {
                HexCell otherCell = hexCell.GetNeighbor(dragDirection.Opposite());

                if (otherCell)
                {
                    if(riverMode == OptionalToggle.Yes)
                    {
                        otherCell.SetOutgoingRiver(dragDirection);
                    }

                    if(roadMode == OptionalToggle.Yes)
                    {
                        otherCell.AddRoad(dragDirection);
                    }

                    if (wallMode == OptionalToggle.No)
                    {
                        otherCell.RemoveWalls(dragDirection);
                    }

                    /*
                    if (wallMode == OptionalToggle.Yes)
                    {
                        otherCell.AddWall(dragDirection);
                    }
                    else if (wallMode == OptionalToggle.No)
                    {
                        otherCell.RemoveWalls(dragDirection);
                    }
                    */
                }
            }
        }
    }

    public void SelectColor (int index)
    {
        applyColor = index >= 0;

        if (applyColor)
        {
            activeColor = colors[index];
        }
    }

    public void SetElevation (float elevation)
    {
        activeElevation = (int)elevation;
    }

    public void SetApplyElevation(bool toggle)
    {
        applyElevation = toggle;
    }

    public void ShowUI(bool visible)
    {
        hexGrid.ShowUI(visible);
    }

// River Edit
    enum OptionalToggle
    {
        Ignore, Yes, No
    }

    OptionalToggle riverMode;

    public void SetRiverMode(int mode)
    {
        riverMode = (OptionalToggle)mode;
    }

// Road Edit
    OptionalToggle roadMode;
    
    public void SetRoadMode(int mode)
    {
        roadMode = (OptionalToggle)mode;
    }

// Water Edit
    int activeWaterLevel = 1;
    bool applyWaterLevel = false;

    public void SetApplyWaterLevel(bool toggle)
    {
        applyWaterLevel = toggle;
    }

    public void SetWaterLevel(float value)
    {
        activeWaterLevel = (int)value;
    }

    // Tree Edit
    int activeTreeLevel;
    bool applyTreeLevel;

    public void SetApplyTreeLevel(bool toggle)
    {
        applyTreeLevel = toggle;
    }

    public void SetTreeLevel(float level)
    {
        activeTreeLevel = (int)level;
    }

    // Stone Edit
    int activeStoneLevel;
    bool applyStoneLevel;

    public void SetApplyStoneLevel(bool toggle)
    {
        applyStoneLevel = toggle;
    }

    public void SetStoneLevel(float level)
    {
        activeStoneLevel = (int)level;
    }

    // Wall Edit
    OptionalToggle wallMode;

    public void SetWallMode(int mode)
    {
        wallMode = (OptionalToggle)mode;
    }
}
