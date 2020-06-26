using UnityEngine;
// using UnityEditor;

[System.Serializable]
public struct HexCoordinates
{
    [SerializeField]
    private int x, z;

    // Data
    public int X
    {
        get
        {
            return x;
        }
    }

    public int Y
    {
        get
        {
            return -X - Z;
        }
    }
    public int Z
    { 
        get
        {
            return z;
        }
    }
    

    // Ctor
    public HexCoordinates (int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public static HexCoordinates FromPositionToHexCoordinates(Vector3 position)
    {
        float x = position.x / (Hex.innerRadius * 2f);
        float y = -x;

        // Every two rows -> x-1, y-1, z+2
        float offset = position.z / (Hex.outerRadius * 3f); // 3 * outerRadius = z ++ 2
        x -= offset;
        y -= offset;

        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x -y);

        if(iX + iY + iZ != 0)
        {
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x - y - iZ);

            if(dX > dY && dX > dZ)
            {
                iX = -iY - iZ;
            }
            else if(dZ >dY)
            {
                iZ = -iX - iY;
            }

        }

        return new HexCoordinates(iX, iZ);
    }

    public static HexCoordinates FromOffsetToHexCoordinates (int x, int z)
    {
        return new HexCoordinates(x - z / 2, z);
    }

    public string ToStringOnSingleLine()
    {
        return "(" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
    }

    public string ToStringOnSeperateLines()
    {
        return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
    }

// Distance
    public int DistanceTo(HexCoordinates coord)
    {
        return ((x < coord.x ? coord.x - x : x - coord.x) +
            (Y < coord.Y ? coord.Y - Y : Y - coord.Y) +
            (z < coord.z ? coord.z - z : z - coord.z))  / 2;
    }
}
