using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public class MapGenerator3D : MonoBehaviour
{
    [Header("Map dimensions")]
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int depth;
    
    [Header("Map random values")]
    [SerializeField] private String seed;
    [SerializeField] private bool useRandomSeed;
    [SerializeField] [Range(0,100)] private int randomFillPercent;
    
    [Header("Map smoothing and processing values")]
    [SerializeField] private int deathThreshold;
    [SerializeField] private int liveThreshold;
    [SerializeField] private int smoothness;
    [SerializeField] private int wallThresholdSize;
    [SerializeField] private int roomThresholdSize;
    
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
            GameObject[] blocks = GameObject.FindGameObjectsWithTag("block");
            foreach (var block in blocks)
            {
                Destroy(block);
            }
            Generate();
        }
    }

    void Generate()
    {
        map = new int[width, height, depth];

        RandomFillMap();

        for (int i = 0; i < smoothness; i++)
        {
            SmoothMap();
        }

        ProcessMap();
        DrawMap();
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
                    }
                    else {
                        map[x,y,z] = pseudoRandom.Next(0,100) < randomFillPercent? 1: 0;
                    }
                }
            }
        }
    }

    void SmoothMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int neighbourWallTiles = GetSurroundingWallCount(x, y, z);

                    if (neighbourWallTiles > liveThreshold)
                        map[x, y, z] = 1;
                    else if (neighbourWallTiles < deathThreshold)
                        map[x, y, z] = 0;
                }
            }
        }
    }

    void ProcessMap()
    {
        //Get rid of wall regions under a certain threshold size.
        List<List<Coord>> wallRegions = GetRegions(1);

        foreach (List<Coord> wallRegion in wallRegions)
        {
            if (wallRegion.Count < wallThresholdSize)
            {
                foreach (Coord tile in wallRegion)
                {
                    map[tile.tileX, tile.tileY, tile.tileZ] = 0;
                }
            }
        }
        
        //Get rid of room regions under a certain threshold size.
        List<List<Coord>> roomRegions = GetRegions(0);
        List<Room> survivingRooms = new List<Room>();

        foreach (List<Coord> roomRegion in roomRegions)
        {
            if (roomRegion.Count < roomThresholdSize)
            {
                foreach (Coord tile in roomRegion)
                {
                    map[tile.tileX, tile.tileY, tile.tileZ] = 1;
                }
            }
            else
            {
                survivingRooms.Add(new Room(roomRegion, map));
            }
        }
    }
    
    int GetSurroundingWallCount(int gridX, int gridY, int gridZ)
    {
        int wallCount = 0;

        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                for (int neighbourZ = gridZ - 1; neighbourZ <= gridZ + 1; neighbourZ++)
                {
                    if (IsInMapRange(neighbourX, neighbourY, neighbourZ))
                    {
                        if (neighbourX != gridX || neighbourY != gridY || neighbourZ != gridZ)
                        {
                            wallCount += map[neighbourX, neighbourY, neighbourZ];
                        }
                    }
                    else
                        wallCount++;
                }
            }
        }
        //Debug.Log("WallCount: " + wallCount);
        return wallCount;
    }
    
    bool IsInMapRange(int x, int y, int z)
    {
        return x >= 0 && x < width && y >= 0 && y < height && z >= 0 && z < depth;
    }

    List<List<Coord>> GetRegions(int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,,] mapFlags = new int[width, height, depth];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (mapFlags[x, y, z] == 0 && map[x, y, z] == tileType)
                    {
                        List<Coord> newRegion = GetRegionTiles(x, y, z);
                        regions.Add(newRegion);

                        foreach (Coord tile in newRegion)
                        {
                            mapFlags[tile.tileX, tile.tileY, tile.tileZ] = 1;
                        }
                    }
                }
            }
        }
        //Debug.Log(regions.Count);
        return regions;
    }
    
    //Flood fill from start point to find tiles making a region, the flood stops when there are no more tiles of the same type.
    List<Coord> GetRegionTiles(int startX, int startY, int startZ)
    {
        List<Coord> tiles = new List<Coord>();
        int[,,] mapFlags = new int[width, height, depth];
        int tileType = map[startX, startY, startZ];
        
        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY, startZ));
        mapFlags[startX, startY, startZ] = 1;
        
        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
            {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    for (int z = tile.tileZ - 1; z <= tile.tileZ + 1; z++)
                    {
                        if (IsInMapRange(x, y, z) && (x == tile.tileX || y == tile.tileY || z == tile.tileZ))
                        {
                            if (mapFlags[x, y, z] == 0 && map[x, y, z] == tileType)
                            {
                                mapFlags[x, y, z] = 1;
                                queue.Enqueue(new Coord(x, y, z));
                            }
                        }
                    }
                }
            }
        }
        //Debug.Log("RegionTiles: " + tiles.Count);
        return tiles;
    }
    
    struct Coord
    {
        public int tileX;
        public int tileY;
        public int tileZ;
        
        public Coord(int x, int y, int z)
        {
            tileX = x;
            tileY = y;
            tileZ = z;
        }
    }
    
    void DrawMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    Vector3 position = new Vector3(-width/2 + x + .5f,-height/2 + y + .5f, -depth/2 + z + .5f);
                    if (map[x, y, z] == 0)
                        Instantiate(whiteCube, position, whiteCube.transform.rotation);
                    //if (map[x, y, z] == 1)
                    //    Instantiate(blackCube, position, whiteCube.transform.rotation);
                }
            }
        }
    }
    
    class Room : IComparable<Room>
    {
        public List<Coord> tiles;
        public List<Coord> edgeTiles;
        public List<Room> connectedRooms;
        public int roomSize;
        public bool isAccessibleFromMainRoom;
        public bool isMainRoom;
        
        public Room(){}
        
        public Room(List<Coord> roomTiles, int[,,] map)
        {
            tiles = roomTiles;
            roomSize = tiles.Count;
            connectedRooms = new List<Room>();

            edgeTiles = new List<Coord>();
            foreach (Coord tile in tiles)
            {
                for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
                {
                    for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                    {
                        for (int z = tile.tileZ - 1; z <= tile.tileZ + 1; z++)
                        {
                            if (x == tile.tileX || y == tile.tileY || z == tile.tileZ)
                            {
                                if (map[x, y, z] == 1)
                                {
                                    edgeTiles.Add(tile);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        public int CompareTo(Room otherRoom)
        {
            return otherRoom.roomSize.CompareTo(roomSize);
        }
    }
}
