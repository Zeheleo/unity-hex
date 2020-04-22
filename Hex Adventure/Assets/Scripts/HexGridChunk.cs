using UnityEngine;
using UnityEngine.UI;

public class HexGridChunk : MonoBehaviour
{
    HexCell[] hexCells;
    Canvas gridCanvas;
    public HexMesh terrain, rivers, roads;

    private void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();
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
        Triangulate();
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

//Triangulate functions
    public void Triangulate()
    {
        terrain.Clear();
        rivers.Clear();
        roads.Clear();

        for (int count = 0; count < hexCells.Length; count++)
        {
            Triangulate(hexCells[count]);
        }

        terrain.Apply();
        rivers.Apply();
        roads.Apply();
    }

    void Triangulate(HexCell hexCell)
    {
        for (HexDirection dir = HexDirection.TopRight; dir <= HexDirection.TopLeft; dir++)
        {
            Triangulate(dir, hexCell);
        }
    }

    void Triangulate(HexDirection dir, HexCell hexCell)
    {
        Vector3 center = hexCell.Position;

        EdgeVertices e = new EdgeVertices(
            center + Hex.GetFirstSolidPoint(dir),
            center + Hex.GetSecondSolidPoint(dir));

        if (hexCell.HasRiver)
        {
            if (hexCell.HasRiverThroughEdge(dir))
            {
                e.v3.y = hexCell.StreamBedY;
                if (hexCell.HasRiverBeginOrEnd)
                {
                    TriangulateWithRiverBeginOrEnd(dir, hexCell, center, e);
                }
                else
                {
                    TriangulateWithRiver(dir, hexCell, center, e);
                }
            }
            else
            {
                TriangulateAdjacentToRiver(dir, hexCell, center, e);
            }
        }
        else
        {
            // TriangulateEdgeFan(center, e, hexCell.color);
            TriangulateWithoutRiver(dir, hexCell, center, e);
        }

        if (dir == HexDirection.TopRight)
        {
            TriangluateConnection(dir, hexCell, e);
        }
        else if (dir <= HexDirection.DownRight)
        {
            TriangluateConnection(dir, hexCell, e);
        }
    }

    void TriangluateConnection(HexDirection dir, HexCell hexCell, EdgeVertices e1)
    {
        HexCell neighbor = hexCell.GetNeighbor(dir);
        if (neighbor == null)
            return;

        Vector3 bridge = Hex.GetBridge(dir);
        bridge.y = neighbor.Position.y - hexCell.Position.y;
        EdgeVertices e2 = new EdgeVertices(
            e1.v1 + bridge,
            e1.v5 + bridge);

        if (hexCell.HasRiverThroughEdge(dir))
        {
            e2.v3.y = neighbor.StreamBedY;
            TriangulateRiverQuad(
                    e1.v2, e1.v4, e2.v2, e2.v4,
                    hexCell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
                    hexCell.HasIncomingRiver & hexCell.IncomingRiver == dir
                );
        }

        if (hexCell.GetEdgeType(dir) == HexEdgeType.Slope)
        {
            TriangulateEdgeTerraces(e1, hexCell, e2, neighbor, hexCell.HasRoadThroughEdge(dir));
        }
        else
        {
            TriangulateEdgeStrip(e1, hexCell.color, e2, neighbor.color, hexCell.HasRoadThroughEdge(dir));
        }

        HexCell nextNeighbor = hexCell.GetNeighbor(dir.Next());
        if (dir <= HexDirection.Left && nextNeighbor != null)
        {
            Vector3 elevationVector = e1.v5 + Hex.GetBridge(dir.Next());
            elevationVector.y = nextNeighbor.Position.y;

            // hexCell < Neighbor
            if (hexCell.Elevation <= neighbor.Elevation)
            {
                // hexCell < Neighbor, nextNeighbor(clockwise)
                if (hexCell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulatePoint(e1.v5, hexCell, e2.v5, neighbor, elevationVector, nextNeighbor);
                }
                else
                {
                    TriangulatePoint(elevationVector, nextNeighbor, e1.v5, hexCell, e2.v5, neighbor);
                }

            }

            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulatePoint(e2.v5, neighbor, elevationVector, nextNeighbor, e1.v5, hexCell);
            }

            else
            {
                TriangulatePoint(elevationVector, nextNeighbor, e1.v5, hexCell, e2.v5, neighbor);
            }
        }
    }

    void TriangulatePoint(Vector3 bot, HexCell botCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        HexEdgeType leftEdgeType = botCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = botCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.Slope)
        {
            if (rightEdgeType == HexEdgeType.Slope)
            {
                // B<L && B<R && L=R
                TriangulatePointTerraces(bot, botCell, left, leftCell, right, rightCell);
                return;
            }


            else if (rightEdgeType == HexEdgeType.Flat)
            {
                // B<L && B=R && L>R
                TriangulatePointTerraces(left, leftCell, right, rightCell, bot, botCell);
                return;
            }

            // B<L && B<<R && L<R
            TriangulatePointTerracesCliff(bot, botCell, left, leftCell, right, rightCell);
            return;
        }

        else if (rightEdgeType == HexEdgeType.Slope)
        {
            if (leftEdgeType == HexEdgeType.Flat)
            {
                // B=L && B<R && L<R
                TriangulatePointTerraces(right, rightCell, bot, botCell, left, leftCell);
                return;
            }

            // B<<L && B<R && L>R
            TriangulatePointCliffTerraces(bot, botCell, left, leftCell, right, rightCell);
            return;
        }

        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            if (leftCell.Elevation < rightCell.Elevation)
            {
                TriangulatePointCliffTerraces(right, rightCell, bot, botCell, left, leftCell);
            }
            else
            {
                TriangulatePointTerracesCliff(left, leftCell, right, rightCell, bot, botCell);
            }

            return;
        }

        // B=L && R=L && L=R, ALL FLAT
        terrain.AddTriangle(bot, left, right);
        terrain.AddTriangleColor(botCell.color, leftCell.color, rightCell.color);
    }

    void TriangulateEdgeTerraces(EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell, bool hasRoad)
    {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color midColor = Hex.TerraceColorLerp(beginCell.color, endCell.color, 1);

        TriangulateEdgeStrip(begin, beginCell.color, e2, midColor, hasRoad);

        // Intermediate Steps
        for (int count = 2; count < Hex.terracesSteps; count++)
        {
            EdgeVertices e1 = e2;
            Color frontColor = midColor;

            e2 = EdgeVertices.TerraceLerp(begin, end, count);
            midColor = Hex.TerraceColorLerp(beginCell.color, endCell.color, count);

            TriangulateEdgeStrip(e1, frontColor, e2, midColor, hasRoad);
        }

        TriangulateEdgeStrip(e2, midColor, end, endCell.color, hasRoad);
    }

    void TriangulatePointTerraces(Vector3 begin, HexCell beginCell, Vector3 left,
        HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        Vector3 v4 = Hex.TerraceLerp(begin, left, 1);
        Vector3 v5 = Hex.TerraceLerp(begin, right, 1);
        Color c3 = Hex.TerraceColorLerp(beginCell.color, leftCell.color, 1);
        Color c4 = Hex.TerraceColorLerp(beginCell.color, rightCell.color, 1);

        terrain.AddTriangle(begin, v4, v5);
        terrain.AddTriangleColor(beginCell.color, c3, c4);

        for (int count = 2; count < Hex.terracesSteps; count++)
        {
            Vector3 v1 = v4;
            Vector3 v2 = v5;
            Color c1 = c3;
            Color c2 = c4;

            v4 = Hex.TerraceLerp(begin, left, count);
            v5 = Hex.TerraceLerp(begin, right, count);
            c3 = Hex.TerraceColorLerp(beginCell.color, leftCell.color, count);
            c4 = Hex.TerraceColorLerp(beginCell.color, rightCell.color, count);

            terrain.AddQuad(v1, v2, v4, v5);
            terrain.AddQuadColor(c1, c2, c3, c4);
        }

        terrain.AddQuad(v4, v5, left, right);
        terrain.AddQuadColor(c3, c4, leftCell.color, rightCell.color);
    }

    void TriangulatePointTerracesCliff(Vector3 begin, HexCell beginCell, Vector3 left,
        HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);
        if (b < 0)
            b = -b;

        Vector3 boundary = Vector3.Lerp(Hex.Perturb(begin), Hex.Perturb(right), b);
        Color boundaryColor = Color.Lerp(beginCell.color, rightCell.color, b);

        // Bot Boundary (this function only runs in slope case)
        TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);

        // Top Boundary is slope-slope
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }

        // Top Boundary is slope-cliff
        else
        {
            terrain.AddTriangleUnperturbed(Hex.Perturb(left), Hex.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }

    }

    void TriangulatePointCliffTerraces(Vector3 begin, HexCell beginCell, Vector3 left,
    HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float b = 1f / (leftCell.Elevation - beginCell.Elevation);

        if (b < 0)
            b = -b;

        Vector3 boundary = Vector3.Lerp(Hex.Perturb(begin), Hex.Perturb(left), b);
        Color boundaryColor = Color.Lerp(beginCell.color, leftCell.color, b);

        // Bot Boundary (this function only runs in slope case)
        TriangulateBoundaryTriangle(right, rightCell, begin, beginCell, boundary, boundaryColor);

        // Top Boundary is slope-slope
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }

        // Top Boundary is slope-cliff
        else
        {
            terrain.AddTriangleUnperturbed(Hex.Perturb(left), Hex.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }

    }

    void TriangulateBoundaryTriangle(Vector3 begin, HexCell beginCell, Vector3 left,
        HexCell leftCell, Vector3 boundary, Color boundaryColor)
    {
        Vector3 v2 = Hex.Perturb(Hex.TerraceLerp(begin, left, 1));
        Color c2 = Hex.TerraceColorLerp(beginCell.color, leftCell.color, 1);

        terrain.AddTriangleUnperturbed(Hex.Perturb(begin), v2, boundary);
        terrain.AddTriangleColor(beginCell.color, c2, boundaryColor);

        for (int count = 2; count < Hex.terracesSteps; count++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;

            v2 = Hex.Perturb(Hex.TerraceLerp(begin, left, count));
            c2 = Hex.TerraceColorLerp(beginCell.color, leftCell.color, count);

            terrain.AddTriangleUnperturbed(v1, v2, boundary);
            terrain.AddTriangleColor(c1, c2, boundaryColor);
        }

        terrain.AddTriangleUnperturbed(v2, Hex.Perturb(left), boundary);
        terrain.AddTriangleColor(c2, leftCell.color, boundaryColor);
    }

    void TriangulateEdgeFan(Vector3 point, EdgeVertices edge, Color color)
    {
        terrain.AddTriangle(point, edge.v1, edge.v2);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(point, edge.v2, edge.v3);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(point, edge.v3, edge.v4);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(point, edge.v4, edge.v5);
        terrain.AddTriangleColor(color);
    }

    void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2, bool hasRoad = false)
    {
        terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
        terrain.AddQuadColor(c1, c2);

        if (hasRoad)
            TriangulateRoadSegment(e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4);
    }

    void TriangulateWithRiver(HexDirection dir, HexCell hexCell, Vector3 center, EdgeVertices e)
    {
        Vector3 centerL, centerR;

        if (hexCell.HasRiverThroughEdge(dir.Opposite()))
        {
            centerL = center + Hex.GetFirstSolidPoint(dir.Previous()) * 0.25f;
            centerR = center + Hex.GetSecondSolidPoint(dir.Next()) * 0.25f;
        }
        else if (hexCell.HasRiverThroughEdge(dir.Next()))
        {
            centerL = center;
            centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
        }
        else if (hexCell.HasRiverThroughEdge(dir.Previous()))
        {
            centerR = center;
            centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
        }
        else if (hexCell.HasRiverThroughEdge(dir.Next().Next()))
        {
            centerL = center;
            centerR = center + Hex.GetSolidEdgeMiddle(dir.Next()) * 0.5f * Hex.innerToOuter;
        }
        else
        {
            centerL = center + Hex.GetSolidEdgeMiddle(dir.Previous()) * 0.5f * Hex.innerToOuter;
            centerR = center;
        }

        center = Vector3.Lerp(centerL, centerR, 0.5f);

        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(centerL, e.v1, 0.5f),
            Vector3.Lerp(centerR, e.v5, 0.5f), 1f / 6f
            );

        m.v3.y = center.y = e.v3.y;

        TriangulateEdgeStrip(m, hexCell.color, e, hexCell.color);

        terrain.AddTriangle(centerL, m.v1, m.v2);
        terrain.AddTriangleColor(hexCell.color);
        terrain.AddQuad(centerL, center, m.v2, m.v3);
        terrain.AddQuadColor(hexCell.color);
        terrain.AddQuad(center, centerR, m.v3, m.v4);
        terrain.AddQuadColor(hexCell.color);
        terrain.AddTriangle(centerR, m.v4, m.v5);
        terrain.AddTriangleColor(hexCell.color);

        bool reversed = hexCell.IncomingRiver == dir;

        TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, hexCell.RiverSurfaceY, 0.4f, reversed);
        TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, hexCell.RiverSurfaceY, 0.6f, reversed);
    }

    void TriangulateWithRiverBeginOrEnd(HexDirection dir, HexCell hexCell, Vector3 center, EdgeVertices e)
    {
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f), 1f / 6f
            );

        m.v3.y = e.v3.y;

        TriangulateEdgeStrip(m, hexCell.Color, e, hexCell.Color);
        TriangulateEdgeFan(center, m, hexCell.Color);

        bool reversed = hexCell.HasIncomingRiver;
        TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, hexCell.RiverSurfaceY, 0.6f, reversed);

        center.y = m.v2.y = m.v4.y = hexCell.RiverSurfaceY;
        rivers.AddTriangle(center, m.v2, m.v4);
        if(reversed)
        {
            rivers.AddTriangleUV(
                    new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f)
                );
        }
        else
        {
            rivers.AddTriangleUV(
                    new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f)
                );
        }
    }

    void TriangulateAdjacentToRiver(HexDirection dir, HexCell hexCell, Vector3 center, EdgeVertices e)
    {
        // 
        if(hexCell.HasRoads)
        {
            TriangulateRoadAdjacentToRiver(dir, hexCell, center, e);
        }

        // Overlapping
        if (hexCell.HasRiverThroughEdge(dir.Next()))
        {
            if (hexCell.HasRiverThroughEdge(dir.Previous()))
            {
                center += Hex.GetSolidEdgeMiddle(dir) * Hex.innerToOuter * 0.5f;
            }
            else if (hexCell.HasRiverThroughEdge(dir.Previous().Previous()))
            {
                center += Hex.GetFirstSolidPoint(dir) * 0.25f;
            }
        }
        else if (hexCell.HasRiverThroughEdge(dir.Previous()) &&
            hexCell.HasRiverThroughEdge(dir.Next().Next()))
        {
            center += Hex.GetSecondSolidPoint(dir) * 0.25f;
        }


        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f)
            );

        TriangulateEdgeStrip(m, hexCell.Color, e, hexCell.Color);
        TriangulateEdgeFan(center, m, hexCell.Color);
    }

    void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float v, bool reversed)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
        rivers.AddQuad(v1, v2, v3, v4);
        if (reversed)
        {
            rivers.AddQuadUV(1f, 0f, 0.8f - v, 0.6f -v );
        }
        else
        {
            rivers.AddQuadUV(0f, 1f, v, v + 0.2f);
        }
    }

    void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y, float v, bool reversed)
    {
        TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed);
    }

// Road
    void TriangulateRoad(Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices e, bool hasRoadThroughCellEdge)
    {
        if (hasRoadThroughCellEdge)
        {
            Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);
            TriangulateRoadSegment(mL, mC, mR, e.v2, e.v3, e.v4);
            roads.AddTriangle(center, mL, mC);
            roads.AddTriangle(center, mC, mR);
            roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
        }
        else
        {
            TriangulateRoadEdge(center, mL, mR);
        }
    }

    void TriangulateRoadSegment(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5, Vector3 v6)
    {
        roads.AddQuad(v1, v2, v4, v5);
        roads.AddQuad(v2, v3, v5, v6);
        roads.AddQuadUV(0f, 1f, 0f, 0f);
        roads.AddQuadUV(1f, 0f, 0f, 0f);
    }

    void TriangulateWithoutRiver(HexDirection dir, HexCell hexCell, Vector3 center, EdgeVertices e)
    {
        TriangulateEdgeFan(center, e, hexCell.color);

        if(hexCell.HasRoads)
        {
            Vector2 interpolators = GetRoadInterpolators(dir, hexCell);
            TriangulateRoad(
                center,
                Vector3.Lerp(center, e.v1, interpolators.x),
                Vector3.Lerp(center, e.v5, interpolators.y),
                e,
                hexCell.HasRoadThroughEdge(dir)
                );
        }
    }

    void TriangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR)
    {
        roads.AddTriangle(center, mL, mR);
        roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
    }

    Vector2 GetRoadInterpolators(HexDirection dir, HexCell hexCell)
    {
        Vector2 interpolators;

        if(hexCell.HasRoadThroughEdge(dir))
        {
            interpolators.x = interpolators.y = 0.5f;
        }
        else
        {
            interpolators.x = hexCell.HasRoadThroughEdge(dir.Previous()) ? 0.5f : 0.25f;
            interpolators.y = hexCell.HasRoadThroughEdge(dir.Next()) ? 0.5f : 0.25f;

        }

        return interpolators;
    }

    void TriangulateRoadAdjacentToRiver(HexDirection dir, HexCell hexCell, Vector3 center, EdgeVertices e)
    {
        bool hasRoadThroughEdge = hexCell.HasRoadThroughEdge(dir);
        Vector2 interpolators = GetRoadInterpolators(dir, hexCell);
        Vector3 roadCenter = center;
        
        if(hexCell.HasRiverBeginOrEnd)
        {
            roadCenter += Hex.GetSolidEdgeMiddle(hexCell.RiverBeginOrEndDirection.Opposite()) * (1f / 3f);
        }

        Vector3 mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
        Vector3 mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);
        TriangulateRoad(roadCenter, mL, mR, e, hasRoadThroughEdge);

        if(hexCell.HasRiverThroughEdge(dir.Previous()))
        {
            TriangulateRoadEdge(roadCenter, center, mL);
        }

        if(hexCell.HasRiverThroughEdge(dir.Next()))
        {
            TriangulateRoadEdge(roadCenter, mR, center);
        }
    }
}