using UnityEngine;
using UnityEngine.EventSystems; // EventSystem Entry

public class HexMapEditor : MonoBehaviour
{
    public Color[] colors;

    public HexGrid hexGrid;

    private Color activeColor;
    private int activeElevation;
    private bool applyColor;
    private bool applyElevation = true;
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
        SelectColor(0);
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
            else if(isDrag && riverMode == OptionalToggle.Yes)
            {
                HexCell otherCell = hexCell.GetNeighbor(dragDirection.Opposite());
                if (otherCell)
                {
                    previousCell.SetOutgoingRiver(dragDirection);
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
}
