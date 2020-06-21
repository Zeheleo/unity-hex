using UnityEngine;
using UnityEngine.UI;

public class HexGridChunk : MonoBehaviour
{
    static Color color1 = new Color(1f, 0f, 0f);
    static Color color2 = new Color(0f, 1f, 0f);
    static Color color3 = new Color(0f, 0f, 1f);

    HexCell[] hexCells;
    Canvas gridCanvas;
    public HexMesh terrain, rivers, roads, water, waterShore, estuaries;
    public HexFeatureManager features;

    private void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();
        hexCells = new HexCell[Hex.chunkSizeX * Hex.chunkSizeZ];
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
        water.Clear();
        waterShore.Clear();
        estuaries.Clear();
        features.Clear();
        
        for (int count = 0; count < hexCells.Length; count++)
        {
            Triangulate(hexCells[count]);
        }

        terrain.Apply();
        rivers.Apply();
        roads.Apply();
        water.Apply();
        waterShore.Apply();
        estuaries.Apply();
        features.Apply();
    }

    void Triangulate(HexCell hexCell)
    {
        for (HexDirection dir = HexDirection.TopRight; dir <= HexDirection.TopLeft; dir++)
        {
            Triangulate(dir, hexCell);
        }

        if (!hexCell.IsUnderwater && !hexCell.HasRiver && !hexCell.HasRoads)
        {
            features.AddFeature(hexCell, hexCell.Position);
        }

        if(hexCell.IsSpecial)
        {
            features.AddSpecialFeature(hexCell, hexCell.Position);
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

            if(!hexCell.IsUnderwater && !hexCell.HasRoadThroughEdge(dir))
            {
                features.AddFeature(hexCell, (center + e.v1 + e.v5) * (1f / 3f));
            }
        }

        if (hexCell.IsUnderwater)
        {
            TriangulateWater(dir, hexCell, center);
        }

        if (dir == HexDirection.TopRight)
        {
            TriangulateConnection(dir, hexCell, e);
        }
        else if (dir <= HexDirection.DownRight)
        {
            TriangulateConnection(dir, hexCell, e);
        }
    }

    void TriangulateConnection(HexDirection dir, HexCell hexCell, EdgeVertices e1)
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

            if (!hexCell.IsUnderwater)
            {
                if (!neighbor.IsUnderwater)
                {
                    TriangulateRiverQuad(
                            e1.v2, e1.v4, e2.v2, e2.v4,
                            hexCell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
                            hexCell.HasIncomingRiver & hexCell.IncomingRiver == dir
                        );
                }
                else if(hexCell.Elevation > neighbor.WaterLevel)
                {
                    TriangulateWaterfall(e1.v2, e1.v4, e2.v2, e2.v4, hexCell.RiverSurfaceY, neighbor.RiverSurfaceY, neighbor.WaterSurfaceY);
                }
            }
            else if(!neighbor.IsUnderwater)
            {
                if (neighbor.Elevation > hexCell.WaterLevel) // Waterlevel causing SERIOUS ERROR
                {
                    TriangulateWaterfall(e2.v4, e2.v2, e1.v4, e1.v2, neighbor.RiverSurfaceY, hexCell.RiverSurfaceY, hexCell.WaterSurfaceY);
                }
            }
        }

        if (hexCell.GetEdgeType(dir) == HexEdgeType.Slope)
        {
            TriangulateEdgeTerraces(e1, hexCell, e2, neighbor, hexCell.HasRoadThroughEdge(dir));
        }
        else
        {
            TriangulateEdgeStrip(e1, color1, hexCell.TerrainTypeIndex, e2, color2, neighbor.TerrainTypeIndex, hexCell.HasRoadThroughEdge(dir));
        }

        // Wall Creation
        if (hexCell.HasWallThroughEdge(dir) == true)
        {
            features.AddWall(e1, hexCell, e2, neighbor, dir);
        }

        HexCell nextNeighbor = hexCell.GetNeighbor(dir.Next());
        // Direction Optimization
        if (dir <= HexDirection.Right && nextNeighbor != null)
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
        terrain.AddTriangleColor(color1, color2, color3/*botCell.Color, leftCell.Color, rightCell.Color*/);

        Vector3 types;
        types.x = botCell.TerrainTypeIndex;
        types.y = leftCell.TerrainTypeIndex;
        types.z = rightCell.TerrainTypeIndex;
        terrain.AddTriangleTerrainTypes(types);
    }

    void TriangulateEdgeTerraces(EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell, bool hasRoad)
    {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color midColor = Hex.TerraceColorLerp(color1, color2, 1);
        float t1 = beginCell.TerrainTypeIndex;
        float t2 = endCell.TerrainTypeIndex;

        TriangulateEdgeStrip(begin, color1, t1, e2, midColor, t2, hasRoad);

        // Intermediate Steps
        for (int count = 2; count < Hex.terracesSteps; count++)
        {
            EdgeVertices e1 = e2;
            Color frontColor = midColor;

            e2 = EdgeVertices.TerraceLerp(begin, end, count);
            midColor = Hex.TerraceColorLerp(color1, color2, count);

            TriangulateEdgeStrip(e1, frontColor, t1, e2, midColor, t2, hasRoad);
        }

        TriangulateEdgeStrip(e2, midColor, t1, end, color2, t2, hasRoad);
    }

    void TriangulatePointTerraces(Vector3 begin, HexCell beginCell, Vector3 left,
        HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        Vector3 v4 = Hex.TerraceLerp(begin, left, 1);
        Vector3 v5 = Hex.TerraceLerp(begin, right, 1);
        Color c3 = Hex.TerraceColorLerp(color1, color2, 1);
        Color c4 = Hex.TerraceColorLerp(color1, color3, 1);

        Vector3 types;
        types.x = beginCell.TerrainTypeIndex;
        types.y = leftCell.TerrainTypeIndex;
        types.z = rightCell.TerrainTypeIndex;

        terrain.AddTriangle(begin, v4, v5);
        terrain.AddTriangleColor(color1, c3, c4);
        terrain.AddTriangleTerrainTypes(types);

        for (int count = 2; count < Hex.terracesSteps; count++)
        {
            Vector3 v1 = v4;
            Vector3 v2 = v5;
            Color c1 = c3;
            Color c2 = c4;

            v4 = Hex.TerraceLerp(begin, left, count);
            v5 = Hex.TerraceLerp(begin, right, count);
            c3 = Hex.TerraceColorLerp(color1, color2, count);
            c4 = Hex.TerraceColorLerp(color1, color3, count);

            terrain.AddQuad(v1, v2, v4, v5);
            terrain.AddQuadColor(c1, c2, c3, c4);
            terrain.AddQuadTerrainTypes(types);
        }

        terrain.AddQuad(v4, v5, left, right);
        terrain.AddQuadColor(c3, c4, color2, color3);
        terrain.AddQuadTerrainTypes(types);
    }

    void TriangulatePointTerracesCliff(Vector3 begin, HexCell beginCell, Vector3 left,
        HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);
        if (b < 0)
            b = -b;

        Vector3 boundary = Vector3.Lerp(Hex.Perturb(begin), Hex.Perturb(right), b);
        Color boundaryColor = Color.Lerp(color1, color3, b);

        Vector3 types;
        types.x = beginCell.TerrainTypeIndex;
        types.y = leftCell.TerrainTypeIndex;
        types.z = rightCell.TerrainTypeIndex;

        // Bot Boundary (this function only runs in slope case)
        TriangulateBoundaryTriangle(begin, color1, left, color2, boundary, boundaryColor, types);

        // Top Boundary is slope-slope
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, color2, right, color3, boundary, boundaryColor, types);
        }

        // Top Boundary is slope-cliff
        else
        {
            terrain.AddTriangleUnperturbed(Hex.Perturb(left), Hex.Perturb(right), boundary);
            terrain.AddTriangleColor(color2, color3, boundaryColor);
            terrain.AddTriangleTerrainTypes(types);
        }

    }

    void TriangulatePointCliffTerraces(Vector3 begin, HexCell beginCell, Vector3 left,
    HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float b = 1f / (leftCell.Elevation - beginCell.Elevation);

        if (b < 0)
            b = -b;

        Vector3 boundary = Vector3.Lerp(Hex.Perturb(begin), Hex.Perturb(left), b);
        Color boundaryColor = Color.Lerp(color1, color2, b);

        Vector3 types;
        types.x = beginCell.TerrainTypeIndex;
        types.y = leftCell.TerrainTypeIndex;
        types.z = rightCell.TerrainTypeIndex;

        // Bot Boundary (this function only runs in slope case)
        TriangulateBoundaryTriangle(right, color3, begin, color1, boundary, boundaryColor, types);

        // Top Boundary is slope-slope
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, color2, right, color3, boundary, boundaryColor, types);
        }

        // Top Boundary is slope-cliff
        else
        {
            terrain.AddTriangleUnperturbed(Hex.Perturb(left), Hex.Perturb(right), boundary);
            terrain.AddTriangleColor(color2, color3, boundaryColor);
            terrain.AddTriangleTerrainTypes(types);
        }

    }

    void TriangulateBoundaryTriangle(Vector3 begin, Color beginColor, Vector3 left,
        Color leftColor, Vector3 boundary, Color boundaryColor, Vector3 types)
    {
        Vector3 v2 = Hex.Perturb(Hex.TerraceLerp(begin, left, 1));
        Color c2 = Hex.TerraceColorLerp(beginColor, leftColor, 1);

        terrain.AddTriangleUnperturbed(Hex.Perturb(begin), v2, boundary);
        terrain.AddTriangleColor(beginColor, c2, boundaryColor);
        terrain.AddTriangleTerrainTypes(types);

        for (int count = 2; count < Hex.terracesSteps; count++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;

            v2 = Hex.Perturb(Hex.TerraceLerp(begin, left, count));
            c2 = Hex.TerraceColorLerp(beginColor, leftColor, count);

            terrain.AddTriangleUnperturbed(v1, v2, boundary);
            terrain.AddTriangleColor(c1, c2, boundaryColor);
            terrain.AddTriangleTerrainTypes(types);
        }

        terrain.AddTriangleUnperturbed(v2, Hex.Perturb(left), boundary);
        terrain.AddTriangleColor(c2, leftColor, boundaryColor);
        terrain.AddTriangleTerrainTypes(types);
    }

    void TriangulateEdgeFan(Vector3 point, EdgeVertices edge, Color color, float type)
    {
        terrain.AddTriangle(point, edge.v1, edge.v2);
        terrain.AddTriangle(point, edge.v2, edge.v3);
        terrain.AddTriangle(point, edge.v3, edge.v4);
        terrain.AddTriangle(point, edge.v4, edge.v5);

        terrain.AddTriangleColor(color1);
        terrain.AddTriangleColor(color1);
        terrain.AddTriangleColor(color1);
        terrain.AddTriangleColor(color1);

        Vector3 types;
        types.x = types.y = types.z = type;
        terrain.AddTriangleTerrainTypes(types);
        terrain.AddTriangleTerrainTypes(types);
        terrain.AddTriangleTerrainTypes(types);
        terrain.AddTriangleTerrainTypes(types);
    }

    void TriangulateEdgeStrip(EdgeVertices e1, Color c1, float type1, EdgeVertices e2, Color c2, float type2, bool hasRoad = false)
    {
        terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
        terrain.AddQuadColor(c1, c2);

        Vector3 types;
        types.x = types.z = type1;
        types.y = type2;
        terrain.AddQuadTerrainTypes(types);
        terrain.AddQuadTerrainTypes(types);
        terrain.AddQuadTerrainTypes(types);
        terrain.AddQuadTerrainTypes(types);

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

        TriangulateEdgeStrip(m, color1, hexCell.TerrainTypeIndex, e, color1, hexCell.TerrainTypeIndex);

        terrain.AddTriangle(centerL, m.v1, m.v2);
        // terrain.AddTriangleColor(hexCell.Color);
        terrain.AddQuad(centerL, center, m.v2, m.v3);
        // terrain.AddQuadColor(hexCell.Color);
        terrain.AddQuad(center, centerR, m.v3, m.v4);
        // terrain.AddQuadColor(hexCell.Color);
        terrain.AddTriangle(centerR, m.v4, m.v5);
        // terrain.AddTriangleColor(hexCell.Color);

        terrain.AddTriangleColor(color1);
        terrain.AddQuadColor(color1);
        terrain.AddQuadColor(color1);
        terrain.AddTriangleColor(color1);

        Vector3 types;
        types.x = types.y = types.z = hexCell.TerrainTypeIndex;
        terrain.AddTriangleTerrainTypes(types);
        terrain.AddQuadTerrainTypes(types);
        terrain.AddQuadTerrainTypes(types);
        terrain.AddTriangleTerrainTypes(types);

        if (!hexCell.IsUnderwater)
        {
            bool reversed = hexCell.IncomingRiver == dir;
            TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, hexCell.RiverSurfaceY, 0.4f, reversed);
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, hexCell.RiverSurfaceY, 0.6f, reversed);
        }
    }

    void TriangulateWithRiverBeginOrEnd(HexDirection dir, HexCell hexCell, Vector3 center, EdgeVertices e)
    {
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f), 1f / 6f
            );

        m.v3.y = e.v3.y;

        TriangulateEdgeStrip(m, /*hexCell.Color*/ color1, hexCell.TerrainTypeIndex, e, /*hexCell.Color*/ color1, hexCell.TerrainTypeIndex);
        TriangulateEdgeFan(center, m, /*hexCell.Color*/ color1, hexCell.TerrainTypeIndex);

        if (!hexCell.IsUnderwater)
        {
            bool reversed = hexCell.HasIncomingRiver;
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, hexCell.RiverSurfaceY, 0.6f, reversed);

            center.y = m.v2.y = m.v4.y = hexCell.RiverSurfaceY;
            rivers.AddTriangle(center, m.v2, m.v4);
            if (reversed)
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
    }

    void TriangulateAdjacentToRiver(HexDirection dir, HexCell hexCell, Vector3 center, EdgeVertices e)
    {
        // // Road + River
        if (hexCell.HasRoads)
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

        TriangulateEdgeStrip(m, /*hexCell.Color*/color1, hexCell.TerrainTypeIndex, e, /*hexCell.Color*/color1, hexCell.TerrainTypeIndex);
        TriangulateEdgeFan(center, m, /*hexCell.Color*/ color1, hexCell.TerrainTypeIndex);

        if(!hexCell.IsUnderwater && !hexCell.HasRoadThroughEdge(dir))
        {
            features.AddFeature(hexCell, ((center + e.v1 + e.v5) * (1f / 3f)));
        }
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
        TriangulateEdgeFan(center, e, /*hexCell.Color*/ color1, hexCell.TerrainTypeIndex);

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

        bool previousRiver = hexCell.HasRiverThroughEdge(dir.Previous());
        bool nextRiver = hexCell.HasRiverThroughEdge(dir.Next());

        if (hexCell.HasRiverBeginOrEnd)
        {
            roadCenter += Hex.GetSolidEdgeMiddle(hexCell.RiverBeginOrEndDirection.Opposite()) * (1f / 3f);
        }
        else if(hexCell.IncomingRiver == hexCell.OutgoingRiver.Opposite())
        {
            Vector3 corner;
            if (previousRiver)
            {
                if(!hasRoadThroughEdge && hexCell.HasRoadThroughEdge(dir.Next()))
                {
                    return;
                }
                
                corner = Hex.GetSecondSolidPoint(dir);
            }
            else
            {
                if (!hasRoadThroughEdge && hexCell.HasRoadThroughEdge(dir.Next()))
                {
                    return;
                }

                corner = Hex.GetFirstSolidPoint(dir);
            }

            roadCenter += corner * 0.5f;
            if (hexCell.IncomingRiver == dir.Next() &&
                (hexCell.HasRoadThroughEdge(dir.Next().Next()) || hexCell.HasRoadThroughEdge(dir.Opposite()))
                )
            {
                features.AddBridge(roadCenter, center - corner * 0.5f);

            }
            center += corner * 0.25f;
        }
        else if(hexCell.IncomingRiver == hexCell.OutgoingRiver.Previous())
        {
            roadCenter -= Hex.GetSecondPoint(hexCell.IncomingRiver) * 0.2f;
        }
        else if(hexCell.IncomingRiver == hexCell.OutgoingRiver.Next())
        {
            roadCenter -= Hex.GetFirstPoint(hexCell.IncomingRiver) * 0.2f;
        }
        else if(previousRiver && nextRiver)
        {
            if(!hasRoadThroughEdge)
            {
                return;
            }

            Vector3 offset = Hex.GetSolidEdgeMiddle(dir) * Hex.innerToOuter;
            roadCenter += offset * 0.7f;
            center += offset * 0.5f;
        }
        else
        {
            HexDirection middle;
            if(previousRiver)
            {
                middle = dir.Next();
            }
            else if(nextRiver)
            {
                middle = dir.Previous();
            }
            else
            {
                middle = dir;
            }

            if(!hexCell.HasRoadThroughEdge(middle) &&
                !hexCell.HasRoadThroughEdge(middle.Previous()) &&
                !hexCell.HasRoadThroughEdge(middle.Next()))
            {
                return;
            }

            Vector3 offset = Hex.GetSolidEdgeMiddle(middle);
            roadCenter += offset * 0.25f;
            if (dir == middle && hexCell.HasRoadThroughEdge(dir.Opposite()))
            {
                features.AddBridge(roadCenter, center - offset * (Hex.innerToOuter * 0.7f));
            }
        }

        Vector3 mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
        Vector3 mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);
        TriangulateRoad(roadCenter, mL, mR, e, hasRoadThroughEdge);

        if(previousRiver)
        {
            TriangulateRoadEdge(roadCenter, center, mL);
        }

        if(nextRiver)
        {
            TriangulateRoadEdge(roadCenter, mR, center);
        }
    }

// Water
    void TriangulateWater(HexDirection dir, HexCell hexCell, Vector3 center)
    {
        center.y = hexCell.WaterSurfaceY;

        HexCell neighbor = hexCell.GetNeighbor(dir);
        if(neighbor != null && !neighbor.IsUnderwater)
        {
            TriangulateWaterShore(dir, hexCell, neighbor, center);
        }
        else
        {
            TriangulateOpenWater(dir, hexCell, neighbor, center);
        }
    }

    void TriangulateOpenWater(HexDirection dir, HexCell hexCell, HexCell neighbor, Vector3 center)
    {
        Vector3 c1 = center + Hex.GetFirstWaterPoint(dir);
        Vector3 c2 = center + Hex.GetSecondWaterPoint(dir);

        water.AddTriangle(center, c1, c2);

        if (dir <= HexDirection.DownRight)
        {
            // HexCell neighbor = hexCell.GetNeighbor(dir);
            if (neighbor == null || !neighbor.IsUnderwater)
                return;

            Vector3 bridge = Hex.GetWaterBridge(dir);
            Vector3 e1 = c1 + bridge;
            Vector3 e2 = c2 + bridge;

            water.AddQuad(c1, c2, e1, e2);

            if (dir <= HexDirection.Right)
            {
                HexCell nextNeighbor = hexCell.GetNeighbor(dir.Next());
                if (nextNeighbor == null || !nextNeighbor.IsUnderwater)
                    return;

                water.AddTriangle(c2, e2, c2 + Hex.GetWaterBridge(dir.Next()));
            }
        }
    }

    void TriangulateWaterShore(HexDirection dir, HexCell hexCell, HexCell neighbor, Vector3 center)
    {
        EdgeVertices e1 = new EdgeVertices(
                center + Hex.GetFirstWaterPoint(dir),
                center + Hex.GetSecondWaterPoint(dir)
            );

        water.AddTriangle(center, e1.v1, e1.v2);
        water.AddTriangle(center, e1.v2, e1.v3);
        water.AddTriangle(center, e1.v3, e1.v4);
        water.AddTriangle(center, e1.v4, e1.v5);

        // Vector3 bridge = Hex.GetWaterBridge(dir);
        Vector3 center2 = neighbor.Position;
        center2.y = center.y;
        EdgeVertices e2 = new EdgeVertices(
            center2 + Hex.GetSecondSolidPoint(dir.Opposite()),
            center2 + Hex.GetFirstSolidPoint(dir.Opposite())
            );


        if (hexCell.HasRiverThroughEdge(dir))
        {
            TriangulateEstuary(e1, e2, hexCell.IncomingRiver == dir);
        }
        else
        {
            waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
        }

        HexCell nextNeighbor = hexCell.GetNeighbor(dir.Next());
        if(nextNeighbor != null)
        {
            //vector3 center3 = nextneighbor.position;
            //center3.y = center.y;

            //watershore.addtriangle(
            //        e1.v5, e2.v5, center3 + hex.getfirstsolidpoint(dir.previous())
            //    );

            Vector3 v3 = nextNeighbor.Position + (nextNeighbor.IsUnderwater ?
                Hex.GetFirstWaterPoint(dir.Previous()) :
                Hex.GetFirstSolidPoint(dir.Previous())
                );
            v3.y = center.y;

            waterShore.AddTriangle(e1.v5, e2.v5, v3);
            waterShore.AddTriangleUV(
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f)
                );
        }
    }

    void TriangulateWaterfall(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float waterY)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;

        v1 = Hex.Perturb(v1);
        v2 = Hex.Perturb(v2);
        v3 = Hex.Perturb(v3);
        v4 = Hex.Perturb(v4);

        float t = (waterY - y2) / (y1 - y2);
        v3 = Vector3.Lerp(v3, v1, t);
        v4 = Vector3.Lerp(v4, v2, t);
            
        rivers.AddQuadUnperturbed(v1, v2, v3, v4);
        rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
    }
    
    void TriangulateEstuary(EdgeVertices e1, EdgeVertices e2, bool incomingRiver)
    {
        waterShore.AddTriangle(e2.v1, e1.v2, e1.v1);
        waterShore.AddTriangle(e2.v5, e1.v5, e1.v4);
        waterShore.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        waterShore.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));

        estuaries.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3);
        estuaries.AddTriangle(e1.v3, e2.v2, e2.v4);
        estuaries.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);

        estuaries.AddQuadUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f));
        estuaries.AddTriangleUV(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        estuaries.AddQuadUV(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));

        if (incomingRiver)
        {
            estuaries.AddQuadUV2(new Vector2(1.5f, 1.0f), new Vector2(0.7f, 1.15f), new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f));
            estuaries.AddTriangleUV2(new Vector2(0.5f, 1.1f), new Vector2(1f, 0.8f), new Vector2(0f, 0.8f));
            estuaries.AddQuadUV2(new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f), new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f));
        }
        else
        {
            estuaries.AddQuadUV2(new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f), new Vector2(0f, 0f), new Vector2(0.5f, -0.3f));
            estuaries.AddTriangleUV2(new Vector2(0.5f, -0.3f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            estuaries.AddQuadUV2(new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f), new Vector2(1f, 0f), new Vector2(1.5f, -0.2f));
        }
    }
}