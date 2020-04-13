using UnityEngine;

public class HexMapEditor : MonoBehaviour
{
    public Color[] colors;

    public HexGrid hexGrid;

    private Color activeColor;
    private int activeElevation;

    private void Awake()
    {
        SelectColor(0);
    }

    private void Update()
    {

        if (Input.GetMouseButtonDown(0)) // ON CLICK
        //if (Input.GetMouseButton(0)) // On DOWN
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
            EditCell(hexGrid.GetCell(hit.point));
        }
    }

    private void EditCell(HexCell hexCell)
    {
        hexCell.color = activeColor;
        hexCell.Elevation = activeElevation;
        hexGrid.Refresh();
    }

    public void SelectColor (int index)
    {
        activeColor = colors[index];
    }

    public void SetElevation (float elevation)
    {
        activeElevation = (int)elevation;
    }
}
