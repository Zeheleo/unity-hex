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
    public Transform wallDoors;
    public Transform bridge;
    public Transform[] specObj;

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
        if (Mathf.Abs(nearCell.Elevation - farCell.Elevation) <= 1 &&
            !nearCell.IsUnderwater && !farCell.IsUnderwater && !nearCell.HasRoadThroughEdge(dir))
        {
            // nearLeft, farLeft, nearRight, farRight
            // near.v1; far.v1; near.v5; far.v5;

            Vector3 left = Vector3.Lerp(near.v1, far.v1, 0.5f);
            // Vector3 right = Vector3.Lerp(near.v5, far.v5, 0.5f);

            Transform instance = Instantiate(walls);

            instance.localPosition = left;

            //if (dir == HexDirection.TopRight || dir == HexDirection.DownLeft)
            //{
            //    instance.localRotation = Quaternion.Euler(0f, -66f, 0f);
            //}
            //else if (dir == HexDirection.Right || dir == HexDirection.Left)
            //{
            //    // instance.localRotation = Quaternion.Euler(0f, 90f, 0f);
            //}
            //else // DownRight - DownLeft
            //{
            //    instance.localRotation = Quaternion.Euler(0f, 66f, 0f);
            //}

            // Rotation / Position Fix
            Vector3 localFix = Vector3.zero;
            localFix.y = 4.72f;

            if(dir == HexDirection.TopRight)
            {
                instance.localPosition = new Vector3(
                    instance.localPosition.x + localFix.x + 4.116f,
                    instance.localPosition.y + localFix.y,
                    instance.localPosition.z + localFix.z - 2.275f);

                instance.localRotation = Quaternion.Euler(0f, -66f, 0f);
            }
            else if (dir == HexDirection.Right)
            {
                instance.localPosition = new Vector3(
                   instance.localPosition.x + localFix.x,
                   instance.localPosition.y + localFix.y,
                   instance.localPosition.z + localFix.z - 3.75f);
            }
            else if (dir == HexDirection.DownRight)
            {
                instance.localPosition = new Vector3(
                   instance.localPosition.x + localFix.x - 4.79f,
                   instance.localPosition.y + localFix.y,
                   instance.localPosition.z + localFix.z - 2.075f);

                instance.localRotation = Quaternion.Euler(0f, 66f, 0f);
            }
            else if (dir == HexDirection.DownLeft)
            {
                instance.localPosition = new Vector3(
                    instance.localPosition.x + localFix.x + 4.116f + 0.8f,
                    instance.localPosition.y + localFix.y,
                    instance.localPosition.z + localFix.z - 2.275f);

                instance.localRotation = Quaternion.Euler(0f, -66f + 180f, 0f);
            }
            else if (dir == HexDirection.Left)
            {
                instance.localPosition = new Vector3(
                    instance.localPosition.x + localFix.x,
                    instance.localPosition.y + localFix.y,
                    instance.localPosition.z + localFix.z - 3.75f);

                instance.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
            else // TopLeft
            {
                instance.localPosition = new Vector3(
                   instance.localPosition.x + localFix.x - 4.79f+ 0.605f,
                   instance.localPosition.y + localFix.y,
                   instance.localPosition.z + localFix.z - 2.075f);

                instance.localRotation = Quaternion.Euler(0f, 66f + 180f, 0f);
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

        if(nearCell.HasRoadThroughEdge(dir))
        {
            /*
            Vector3 left = Vector3.Lerp(near.v1, far.v1, 0.5f);
            Vector3 right = Vector3.Lerp(near.v5, far.v5, 0.5f);

            Transform instanceLeft = Instantiate(wallDoors);
            Transform instanceRight = Instantiate(wallDoors);
            instanceLeft.localPosition = left;
            instanceRight.localPosition = right;

            if (dir == HexDirection.TopRight || dir == HexDirection.DownLeft)
            {
                instanceLeft.localRotation = Quaternion.Euler(0f, 25f, 0f);
                instanceRight.localRotation = Quaternion.Euler(0f, 205f, 0f);
            }
            else if (dir == HexDirection.Right || dir == HexDirection.Left)
            {
                instanceLeft.localRotation = Quaternion.Euler(0f, 90f, 0f);
                instanceRight.localRotation = Quaternion.Euler(0f, 270f, 0f);
            }
            else // DownRight - DownLeft
            {
                instanceLeft.localRotation = Quaternion.Euler(0f, 155f, 0f);
                instanceRight.localRotation = Quaternion.Euler(0f, 340f, 0f);
            }

            instanceLeft.SetParent(container, false);
            instanceRight.SetParent(container, false);
            */
        }
    }

    public void AddBridge(Vector3 roadCenter1, Vector3 roadCenter2)
    {
        roadCenter1 = Hex.Perturb(roadCenter1);
        roadCenter2 = Hex.Perturb(roadCenter2);

        roadCenter1.y += 1f;
        roadCenter2.y += 1f;

        Transform instance = Instantiate(bridge);
        instance.localPosition = (roadCenter1 + roadCenter2) * 0.5f;

        // Vector3 fix = instance.localPosition.y + 0.8f;
        // instance.localPosition = fix;

        instance.forward = roadCenter2 - roadCenter1;

        float length = Vector3.Distance(roadCenter1, roadCenter2);
        instance.localScale = new Vector3(1f, 1f, length * (1f / Hex.bridgeLengthStep));

        instance.SetParent(container, false);
    }

    public void AddBridgeRotate(Vector3 roadCenter1, Vector3 roadCenter2)
    {
        // Considerable Option
    }

    public void AddSpecialFeature(HexCell hexCell, Vector3 position)
    {
        HexHash hash = Hex.SampleHashGrid(position);

        Transform instance = Instantiate(specObj[hexCell.SpecIndex - 1]);
        instance.localPosition = Hex.Perturb(position);
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
        // HexHash hash = 
        instance.SetParent(container, false);
    }
}