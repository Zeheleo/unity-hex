using UnityEngine;
using UnityEngine.UI;

public class HexGridChunk : MonoBehaviour
{
    HexCell[] hexCells;
    HexMesh hexMesh;
    Canvas gridCanvas;

    private void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();
        hexMesh = GetComponentInChildren<HexMesh>();
        hexCells = new HexCell[Hex.chunkSizeX * Hex.chunkSizeZ];

        ShowUI(false);
    }

    public void Refresh()
    {
        // hexMesh.Triangulate(hexCells);
        enabled = true;
    }

    public void LateUpdate()
    {
        hexMesh.Triangulate(hexCells);
        enabled = false;
    }

    public void AddCell(int index, HexCell cell)
    {
        hexCells[index] = cell;
        cell.parentChunk = this;
        cell.transform.SetParent(transform, false);
        cell.uiRect.SetParent(gridCanvas.transform, false);
    }

    public void ShowUI(bool visible)
    {
        gridCanvas.gameObject.SetActive(visible);
    }
}