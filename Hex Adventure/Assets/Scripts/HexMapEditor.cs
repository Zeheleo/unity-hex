using UnityEngine;

public class HexMapEditor : MonoBehaviour
{
    public Color[] colors;

    public HexGrid hexGrid;

    private Color activeColor;
    private int activeElevation;

    private bool applyColor;
    private bool applyElevation = true;
    private int brushSize;

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
        if (Input.GetMouseButton(0)) // On DOWN
        {
            HandleInput();
        }
    }

    private void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            EditCells(hexGrid.GetCell(hit.point));
        }
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
}
