using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using MarchingCubesProject;
using UnityEngine;

public class MeshGenerator3D : MonoBehaviour
{
    [SerializeField] private Material meshMaterial;
    
    private List<GameObject> meshes = new List<GameObject>();
    
    public void GenerateMesh(bool[,,] map, int width, int height, int depth)
    {
        Marching marching = new MarchingCubes();
        marching.Surface = 0.9f;
        
        float[] voxels = new float[width * height * depth];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int idx = x + y * width + z * width * height;

                    voxels[idx] = map[x, y, z] ? 1 : 0;
                }
            }
        }
        
        List<Vector3> verts = new List<Vector3>();
        List<int> indices = new List<int>();

        //The mesh produced is not optimal. There is one vert for each index.
        //Would need to weld vertices for better quality mesh.
        marching.Generate(voxels, width, height, depth, verts, indices);

        //A mesh in unity can only be made up of 65000 verts.
        //Need to split the verts between multiple meshes.

        int maxVertsPerMesh = 30000; //must be divisible by 3, ie 3 verts == 1 triangle
        int numMeshes = verts.Count / maxVertsPerMesh + 1;

        for (int i = 0; i < numMeshes; i++)
        {

            List<Vector3> splitVerts = new List<Vector3>();
            List<int> splitIndices = new List<int>();

            for (int j = 0; j < maxVertsPerMesh; j++)
            {
                int idx = i * maxVertsPerMesh + j;

                if (idx < verts.Count)
                {
                    splitVerts.Add(verts[idx]);
                    splitIndices.Add(j);
                }
            }

            if (splitVerts.Count == 0) continue;

            Mesh mesh = new Mesh();
            mesh.SetVertices(splitVerts);
            mesh.SetTriangles(splitIndices, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            GameObject go = new GameObject("Mesh");
            go.transform.parent = transform;
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            go.GetComponent<Renderer>().material = meshMaterial;
            go.GetComponent<MeshFilter>().mesh = mesh;
            go.transform.localPosition = new Vector3(-width / 2, -height / 2, -depth / 2);
            go.AddComponent<MeshCollider>();
            
            meshes.Add(go);
        }
    }
}
