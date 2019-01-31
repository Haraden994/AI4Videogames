using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public class CellularAutomata3D : MonoBehaviour
{
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int depth;

    [SerializeField] private String seed;
    [SerializeField] private bool useRandomSeed;
    
    [SerializeField] [Range(0,100)] private int randomFillPercent;

    [SerializeField] private GameObject whiteCube;
    [SerializeField] private GameObject blackCube;
    
    private int[,,] map;
    
    void Start()
    {
        Generate();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Generate();
        }
    }

    void Generate()
    {
        map = new int[width, height, depth];

        //StartCoroutine(RandomFillMap());
        RandomFillMap();
    }

    void RandomFillMap()
    {
        if (useRandomSeed)
            seed = Time.time.ToString();
        
        System.Random pseudoRandom = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (x == 0 || x == width-1 || y == 0 || y == height -1 || z == 0 || z == depth - 1) {
                        map[x,y,z] = 1;
                        DrawCube(1, x, y, z);
                    }
                    else {
                        map[x,y,z] = pseudoRandom.Next(0,100) < randomFillPercent? 1: 0;
                        DrawCube(map[x,y,z], x, y, z);
                    }
                    //yield return new WaitForSeconds(0);
                }
            }
        }
    }

    /*void OnDrawGizmos() {
        if (map != null) {
            for (int x = 0; x < width; x ++) {
                for (int y = 0; y < height; y ++) {
                    for (int z = 0; z < depth; z++)
                    {
                        Gizmos.color = (map[x, y, z] == 1) ? Color.black : Color.white;
                        Vector3 pos = new Vector3(-width / 2 + x + 3, -height / 2 + y + 3, -depth / 2 + z + 3);
                        Gizmos.DrawCube(pos, Vector3.one);
                    }
                }
            }
        }
    }*/
    
    void DrawCube(int color, int x, int y, int z)
    {
        Vector3 pos = new Vector3(-width/2 + x + .5f,-height/2 + y + .5f, -depth/2 + z + .5f);
        if(color == 0)
            Instantiate(whiteCube, pos, whiteCube.transform.rotation);
        if(color == 1)
            Instantiate(blackCube, pos, whiteCube.transform.rotation);
    }
}
