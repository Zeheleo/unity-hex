using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct HexPropCollection
{
    public Transform[] prefabs;
    public Transform Pick(float choice)
    {
        return prefabs[(int)(choice * prefabs.Length)];
    }
}

public class HexFeatureManager : MonoBehaviour
{
    // public Transform[][] propPrefabs;
    public HexPropCollection[] treeCollections, stoneCollection;
    public Transform walls;

    private Transform container;

    public void Clear()
    {
        if(container)
        {
            Destroy(container.gameObject);
        }

        container = new GameObject("Features Containers").transform;
        container.SetParent(transform, false);
    }

    public void Apply()
    {

    }

    public void AddFeature(HexCell hexCell, Vector3 position)
    {        
        HexHash hash = Hex.SampleHashGrid(position);
        Transform prefab = PickPrefab(treeCollections, hexCell.TreeLevel, hash.a, hash.d);
        Transform otherPrefab = PickPrefab(stoneCollection, hexCell.StoneLevel, hash.b, hash.d);

        if(prefab)
        {
            if(otherPrefab && hash.b < hash.a)
            {
                prefab = otherPrefab;
            }
        }
        else if(otherPrefab)
        {
            prefab = otherPrefab;
        }
        else
        {
            return; // third option
        }

        Transform instance = Instantiate(prefab);

        Vector3 result = Hex.Perturb(position);        

        /*
         *if(!isStone)
            result.y += 3f;
            */
        //float offset = 0.5f;
        //result.x += offset;
        //result.z += offset;

        instance.localPosition = result;
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
        instance.SetParent(container, false);
    }

    Transform PickPrefab (HexPropCollection[] collection, int level, float hash, float choice)
    {
        if (level > 0)
        {
            float[] thresholds = Hex.GetPropThresholds(level - 1);
            for(int count = 0; count < thresholds.Length; count++)
            {
                if(hash < thresholds[count])
                {
                    return collection[count].Pick(choice);// propPrefabs[count][(int)(choice * propPrefabs[count].Length)];
                }
            }
        }

        return null;
    }

    public void AddWall(EdgeVertices near, HexCell nearCell, EdgeVertices far, HexCell farCell, HexDirection dir)
    {
        // In-out doesnt matter, only their state is diff
        if (Mathf.Abs(nearCell.Elevation - farCell.Elevation) <= 1)
        {
            // nearLeft, farLeft, nearRight, farRight
            // near.v1; far.v1; near.v5; far.v5;

            Vector3 left = Vector3.Lerp(near.v1, far.v1, 0.5f);
            // Vector3 right = Vector3.Lerp(near.v5, far.v5, 0.5f);

            Transform instance = Instantiate(walls);

            instance.localPosition = left;

            if (dir == HexDirection.TopRight || dir == HexDirection.DownLeft)
            {
                instance.localRotation = Quaternion.Euler(0f, 25f, 0f);
            }
            else if (dir == HexDirection.Right || dir == HexDirection.Left)
            {
                instance.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }
            else // DownRight - DownLeft
            {
                instance.localRotation = Quaternion.Euler(0f, 155f, 0f);
            }

            instance.SetParent(container, false);
            // nearCell.UpdateWalls(dir);
            // instance.SetParent();
            // instance.position;
            // instance.rotation;

        }
        else
        {
            // Make it false
            // nearCell.SetWall()
        }
    }
}