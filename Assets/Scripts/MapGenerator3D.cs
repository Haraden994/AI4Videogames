using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using Random = System.Random;

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
    
    [Header("Map smoothing values")]
    [SerializeField] private int deathThreshold;
    [SerializeField] private int birthThreshold;
    [SerializeField] private int smoothness;

    [Header("Map processing values")] 
    [SerializeField] private bool doMapProcessing;
    [SerializeField] private int wallThresholdSize;
    [SerializeField] private int roomThresholdSize;
    [SerializeField] private int passagewayRadius;
    //[SerializeField] private int maxPassagewaySlopeValue;
    [Space]
    [SerializeField] private GameObject player;
    //[SerializeField] private GameObject whiteCube;
    //[SerializeField] private GameObject blackCube;
    
    private bool[,,] map;
    
    void Start()
    {
        Generate();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            /*GameObject[] blocks = GameObject.FindGameObjectsWithTag("block");
            foreach (var block in blocks)
            {
                Destroy(block);
            }*/
            foreach (Transform child in transform) {
                Destroy(child.gameObject);
            }
            Destroy(GameObject.FindGameObjectWithTag("Player"));
            Generate();
        }
    }

    void Generate()
    {
        map = new bool[width, height, depth];

        RandomFillMap();

        for (int i = 0; i < smoothness; i++)
        {
            SmoothMap();
        }
        if(doMapProcessing)
            ProcessMap();
        //DrawMap();
        MeshGenerator3D meshGenerator = GetComponent<MeshGenerator3D>();
        meshGenerator.GenerateMesh(map, width, height, depth);
    }

    void RandomFillMap()
    {
        if (useRandomSeed)
            seed = Time.time.ToString();
        
        Random pseudoRandom = new Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (x == 0 || x == width-1 || y == 0 || y == height -1 || z == 0 || z == depth - 1) {
                        map[x,y,z] = true;
                    }
                    else {
                        map[x,y,z] = pseudoRandom.Next(0,100) < randomFillPercent;
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
                    int neighbourWallTiles = GetNeighbourCount(x, y, z, true);

                    if (neighbourWallTiles > birthThreshold)
                        map[x, y, z] = true;
                    else if (neighbourWallTiles < deathThreshold)
                        map[x, y, z] = false;
                }
            }
        }
    }

    void ProcessMap()
    {
        //Get rid of wall regions under a certain threshold size.
        List<List<Coord>> wallRegions = GetRegions(true);

        foreach (List<Coord> wallRegion in wallRegions)
        {
            if (wallRegion.Count < wallThresholdSize)
            {
                foreach (Coord tile in wallRegion)
                {
                    map[tile.tileX, tile.tileY, tile.tileZ] = false;
                }
            }
        }
        
        //Get rid of room regions under a certain threshold size.
        List<List<Coord>> roomRegions = GetRegions(false);
        List<Room> survivingRooms = new List<Room>();

        foreach (List<Coord> roomRegion in roomRegions)
        {
            if (roomRegion.Count < roomThresholdSize)
            {
                foreach (Coord tile in roomRegion)
                {
                    map[tile.tileX, tile.tileY, tile.tileZ] = true;
                }
            }
            else
            {
                survivingRooms.Add(new Room(roomRegion, map));
            }
        }

        if (survivingRooms.Count != 0)
        {
            survivingRooms.Sort();
            survivingRooms[0].isMainRoom = true;
            survivingRooms[0].isAccessibleFromMainRoom = true;
            ConnectClosestRooms(survivingRooms);
        }
        
        SpawnPlayer(survivingRooms[0]);
    }

    void ConnectClosestRooms(List<Room> rooms, bool forceAccessibilityFromMainRoom = false)
    {
        List<Room> roomListA = new List<Room>();
        List<Room> roomListB = new List<Room>();

        if (forceAccessibilityFromMainRoom)
        {
            foreach (var room in rooms)
            {
                if (room.isAccessibleFromMainRoom)
                {
                    roomListB.Add(room);
                }
                else
                {
                    roomListA.Add(room);
                }
            }
        }
        else
        {
            roomListA = rooms;
            roomListB = rooms;
        }
        
        int bestDistance = 0;
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach (Room roomA in roomListA)
        {
            if (!forceAccessibilityFromMainRoom)
            {
                possibleConnectionFound = false;
                if (roomA.connectedRooms.Count > 0)
                {
                    continue;
                }
            }
            
            foreach (Room roomB in roomListB)
            {
                if (roomA == roomB || roomA.IsConnected(roomB))
                {
                    continue;
                }

                for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
                {
                    for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
                    {
                        Coord tileA = roomA.edgeTiles[tileIndexA];
                        Coord tileB = roomB.edgeTiles[tileIndexB];
                        
                        
                        //int distanceBetweenRooms = (int) (Mathf.Pow(tileA.tileX - tileB.tileX, 2) + Mathf.Pow(tileA.tileZ - tileB.tileZ, 2));
                        //int heightBetweenRooms = (int) Mathf.Pow(tileA.tileY - tileB.tileY, 2);
                         
                        int distanceBetweenRooms = (int) (Mathf.Pow(tileA.tileX - tileB.tileX, 2) + 
                                                          Mathf.Pow(tileA.tileY - tileB.tileY, 2) + 
                                                          Mathf.Pow(tileA.tileZ - tileB.tileZ, 2));
                        
                        //TODO: Implementation of maximum slope value cannot be applied since some rooms could be disconnected from the rest of the map.
                        //if(heightBetweenRooms < maxPassagewaySlopeValue)
                            if (distanceBetweenRooms < bestDistance || !possibleConnectionFound)
                            {
                                bestDistance = distanceBetweenRooms;
                                possibleConnectionFound = true;
                                bestTileA = tileA;
                                bestTileB = tileB;
                                bestRoomA = roomA;
                                bestRoomB = roomB;
                            }
                    }
                }
                
                if (possibleConnectionFound && !forceAccessibilityFromMainRoom)
                {
                    CreatePassageway(bestRoomA, bestRoomB, bestTileA, bestTileB);
                }
            }

            if (possibleConnectionFound && forceAccessibilityFromMainRoom)
            {
                CreatePassageway(bestRoomA, bestRoomB, bestTileA, bestTileB);
                ConnectClosestRooms(rooms, true);
            }
            
            if (!forceAccessibilityFromMainRoom)
            {
                ConnectClosestRooms(rooms, true);
            }
        }
    }

    void CreatePassageway(Room roomA, Room roomB, Coord tileA, Coord tileB)
    {
        Room.ConnectRooms(roomA, roomB);
        //Debug.DrawLine(CoordToWorldPoint(tileA), CoordToWorldPoint(tileB), Color.red, 50);
        List<Coord> passageway = GetPassageway(tileA, tileB);
        foreach (Coord coord in passageway)
        {
            DrawSphere(coord, passagewayRadius);
        }
    }
    
    // Bresenham's 3D line algorithm to find the line between two rooms.
    List<Coord> GetPassageway(Coord from, Coord to)
    {
        int x = from.tileX;
        int y = from.tileY;
        int z = from.tileZ;
        
        // Direction values
        int xdir, ydir, zdir;
        // Slope error values
        int sle1, sle2;
        
        List<Coord> passageway = new List<Coord>();
        passageway.Add(from);
        
        // Distance between coordinates 
        int dx = Math.Abs(to.tileX - from.tileX);
        int dy = Math.Abs(to.tileY - from.tileY);
        int dz = Math.Abs(to.tileZ - from.tileZ);

        if (to.tileX > from.tileX)
            xdir = 1;
        else
            xdir = -1;
        if (to.tileY > from.tileY)
            ydir = 1;
        else
            ydir = -1;
        if (to.tileZ > from.tileZ)
            zdir = 1;
        else
            zdir = -1;
        
        // X is the driving axis
        if (dx >= dy && dx >= dz)
        {
            sle1 = 2 * dy - dx;
            sle2 = 2 * dz - dx;
            while (x != to.tileX)
            {
                x += xdir;
                if (sle1 >= 0)
                {
                    y += ydir;
                    sle1 -= 2 * dx;
                }
                if (sle2 >= 0)
                {
                    z += zdir;
                    sle2 -= 2 * dx;
                }
                sle1 += 2 * dy;
                sle2 += 2 * dz;
                passageway.Add(new Coord(x,y,z));
            }
        }
        
        // Y is the driving axis
        else if (dy >= dx && dy >= dz)
        {
            sle1 = 2 * dx - dy;
            sle2 = 2 * dz - dy;
            while (y != to.tileY)
            {
                y += ydir;
                if (sle1 >= 0)
                {
                    x += xdir;
                    sle1 -= 2 * dy;
                }
                if (sle2 >= 0)
                {
                    z += zdir;
                    sle2 -= 2 * dy;
                }
                sle1 += 2 * dx;
                sle2 += 2 * dz;
                passageway.Add(new Coord(x,y,z));
            }
        }
        
        // Z is the driving axis
        else if (dz >= dx && dz >= dy)
        {
            sle1 = 2 * dy - dz;
            sle2 = 2 * dx - dz;
            while (z != to.tileZ)
            {
                z += zdir;
                if (sle1 >= 0)
                {
                    y += ydir;
                    sle1 -= 2 * dz;
                }
                if (sle2 >= 0)
                {
                    x += xdir;
                    sle2 -= 2 * dz;
                }
                sle1 += 2 * dy;
                sle2 += 2 * dx;
                passageway.Add(new Coord(x,y,z));
            }
        }
        return passageway;
    }

    // Draw the passageway as a sequence of spheres
    void DrawSphere(Coord c, int r)
    {
        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                for (int z = -r; z <= r; z++)
                {
                    if (x*x + y*y + z*z <= r*r)
                    {
                        int drawX = c.tileX + x;
                        int drawY = c.tileY + y;
                        int drawZ = c.tileZ + z;
                        if (IsInMapRange(drawX, drawY, drawZ))
                        {
                            map[drawX, drawY, drawZ] = false;
                        }
                    }
                }
            }
        }
    }
    
    Vector3 CoordToWorldPoint(Coord tile)
    {
        return new Vector3(-width/2 + .5f + tile.tileX, -height/2 + .5f + tile.tileY, -depth/2 + .5f + tile.tileZ);
    }
    
    // Method used to get the neighbors (of the given type, true or false) of a cell.
    int GetNeighbourCount(int gridX, int gridY, int gridZ, bool type)
    {
        int neighbourCount = 0;

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
                            if(map[neighbourX, neighbourY, neighbourZ] == type)
                                neighbourCount++;
                        }
                    }
                    else
                        neighbourCount++;
                }
            }
        }
        return neighbourCount;
    }
    
    // Check if passed coordinates are inside of the map range.
    bool IsInMapRange(int x, int y, int z)
    {
        return x >= 0 && x < width && y >= 0 && y < height && z >= 0 && z < depth;
    }

    List<List<Coord>> GetRegions(bool tileType)
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
        bool[,,] mapFlags = new bool[width, height, depth];
        bool tileType = map[startX, startY, startZ];
        
        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY, startZ));
        mapFlags[startX, startY, startZ] = true;
        
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
                            if (!mapFlags[x, y, z] && map[x, y, z] == tileType)
                            {
                                mapFlags[x, y, z] = true;
                                queue.Enqueue(new Coord(x, y, z));
                            }
                        }
                    }
                }
            }
        }
        return tiles;
    }

    // Spawn the player in a random position inside a room.
    void SpawnPlayer(Room room)
    {
        Random rnd = new Random();
        int r = rnd.Next(room.tiles.Count);
        Instantiate(player, CoordToWorldPoint(room.tiles[r]), player.transform.rotation);
    }
    
    // Struct used for storing map coordinates of a cell.
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
    
    /*void DrawMap()
    {
        GameObject cube;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    Vector3 position = CoordToWorldPoint(new Coord(x, y, z));
                    if (!map[x, y, z])
                    {
                        if (GetNeighbourCount(x, y, z, false) >= 26)
                        {
                            cube = Instantiate(whiteCube, position, whiteCube.transform.rotation);
                            cube.transform.localScale.Set(2, 2, 2);
                        }
                        else
                        {
                            cube = Instantiate(whiteCube, position, whiteCube.transform.rotation);
                            cube.transform.localScale.Set(1, 1, 1);
                        }
                    }

                    //if (map[x, y, z])
                    //    Instantiate(blackCube, position, whiteCube.transform.rotation);
                }
            }
        }
    }*/
    
    // This class is used to track each Room region of the map.
    class Room : IComparable<Room>
    {
        public List<Coord> tiles;
        public List<Coord> edgeTiles;
        public List<Room> connectedRooms;
        public int roomSize;
        public bool isAccessibleFromMainRoom;
        public bool isMainRoom;
        
        public Room(){}
        
        public Room(List<Coord> roomTiles, bool[,,] map)
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
                                if (map[x, y, z])
                                {
                                    edgeTiles.Add(tile);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void SetAccessibleFromMainRoom()
        {
            if (!isAccessibleFromMainRoom)
            {
                isAccessibleFromMainRoom = true;
                foreach (Room connectedRoom in connectedRooms)
                {
                    connectedRoom.SetAccessibleFromMainRoom();
                }
            }
        }
        
        public static void ConnectRooms(Room roomA, Room roomB)
        {
            if (roomA.isAccessibleFromMainRoom)
            {
                roomB.SetAccessibleFromMainRoom();
            }
            else if (roomB.isAccessibleFromMainRoom)
            {
                roomA.SetAccessibleFromMainRoom();
            }
            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomA);
        }

        public bool IsConnected(Room otherRoom)
        {
            return connectedRooms.Contains(otherRoom);
        }
        
        public int CompareTo(Room otherRoom)
        {
            return otherRoom.roomSize.CompareTo(roomSize);
        }
    }
}
