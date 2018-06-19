using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour {

    public Transform tilePrefab;
    public Transform obstacolPrefab;
    public Transform navmeshFloor;
    public Transform mapFloor;
    public Transform navmeshMaskPrefab;
    public float tileSize;
    // map generator can menage a lot of different map
    public Map[] map;
    public int mapIndex;
    // used for the outline color
    [Range(0, 1)] public float outlinePercent;

    // store a list fo all the coordinates
    List<Coord> allTileCoords;
    // store all the shuffled tiles coords
    Queue<Coord> shuffledTilesCoords;
    // store all the shuffled tiles coords without an obstacle
    Queue<Coord> shuffledOpenTilesCoords;
    // get a reference to the map
    Map currentMap;
    // all of the map are stored here inside
    Transform[,] tileMap;

    public void GenerateMap() {

        // set the current map
        currentMap = map[mapIndex];
        tileMap = new Transform[currentMap.mapSize.x, currentMap.mapSize.y];
        // we get a pseudorandom seed for the height of the obstacles
        System.Random prng = new System.Random(currentMap.seed);

        // initialize coords list
        allTileCoords = new List<Coord>();
        // fill the list with all the tiles coordinates
        for (int x = 0; x < currentMap.mapSize.x; x++) {
            for (int y = 0; y < currentMap.mapSize.y; y++) {
                allTileCoords.Add(new Coord(x, y));
            }
        }

        // get the shuffled tiles coords queue using the shuffled algorithm with the list of coords
        shuffledTilesCoords = new Queue<Coord>(Utility.ShuffleArray(allTileCoords.ToArray(), currentMap.seed));

        // name of the object that store all the tile
        string holderName = "Generated Map";
        // if the object exists
        if (transform.Find(holderName)) {
            // destroy before recreate it
            DestroyImmediate(transform.Find(holderName).gameObject);
        }

        // create a new object
        Transform mapHolder = new GameObject(holderName).transform;
        // set mapgenerator like parent
        mapHolder.parent = transform;

        // loop through the map
        for (int x = 0; x < currentMap.mapSize.x; x++) {
            for (int y = 0; y < currentMap.mapSize.y; y++) {
                // calulate the position wich want to spawning the tile into the gameworld
                Vector3 tilePosition = CoordToPosition(x, y);
                // create a new tile rotate on the x axes of 90 degrees
                Transform newTile = Instantiate(tilePrefab, tilePosition, Quaternion.Euler(Vector3.right * 90));
                // set the percentage of fill of the outline on the map and determinate the size of the tile
                newTile.localScale = Vector3.one * (1 - outlinePercent) * tileSize;
                // store in the mapholder object
                newTile.parent = mapHolder;
                // add tile to the tile map array
                tileMap[x, y] = newTile;
            }
        }
        // map of all the obstacles
        bool[,] obstaclesMap = new bool[(int)currentMap.mapSize.x, (int)currentMap.mapSize.y];

        // number of obstacles based on the size of the map
        int obstacleCount = (int)(currentMap.obstaclePercent * currentMap.mapSize.x * currentMap.mapSize.y);
        // total of instantiated obstacles 
        int currentObstacleCount = 0;
        // list of all the not obstacled tiles
        List<Coord> allOpenTilesCoords = new List<Coord>(allTileCoords);

        // spawn the obstacol based on the obstacle count
        for (int i = 0; i < obstacleCount; i++) {
            Coord randomCoord = GetRandomCoord();
            // assign map coordinate obstacles
            obstaclesMap[randomCoord.x, randomCoord.y] = true;
            currentObstacleCount++;
            // check if randomCoord is not on the mapCenter alis spawning point and there is no obstacles
            if (randomCoord != currentMap.MapCenter && MapIsFullyAccessible(obstaclesMap, currentObstacleCount)) {
                // get obstacle height based on the height max and min of the obstacles and a random number
                float obstacleHeight = Mathf.Lerp(currentMap.minObstacleHeight, currentMap.maxObstacleHeight, (float)prng.NextDouble());
                // get the obstacle position
                Vector3 obstaclePosition = CoordToPosition(randomCoord.x, randomCoord.y);
                // instantiate the obstacole on the map
                Transform newObstacles = Instantiate(obstacolPrefab, obstaclePosition + Vector3.up * obstacleHeight / 2, Quaternion.identity);
                // assign to the mapholder object
                newObstacles.parent = mapHolder;
                // set the size of the obstacle due to the outline percentage and the scale of the tile and the random height map
                newObstacles.localScale = new Vector3((1 - outlinePercent) * tileSize, obstacleHeight, (1 - outlinePercent) * tileSize);

                // get reference of our obstacole renderer and material component
                Renderer obstacleRenderer = newObstacles.GetComponent<Renderer>();
                Material obstacleMaterial = new Material(obstacleRenderer.sharedMaterial);
                // we want to interpolate between foreground and backgrounf material
                float colourPercent = randomCoord.y / (float)currentMap.mapSize.y;
                obstacleMaterial.color = Color.Lerp(currentMap.foreground, currentMap.background, colourPercent);
                // assign obstaclematerial created on the renderer
                obstacleRenderer.sharedMaterial = obstacleMaterial;
                // removed the coordinates set with obstacle
                allOpenTilesCoords.Remove(randomCoord);
            }
            // if i can't create a new obstacole in that position
            else {
                // remove the obstacles
                obstaclesMap[randomCoord.x, randomCoord.y] = false;
                currentObstacleCount--;
            }
        }

        // get the shuffled open tiles coords queue using the shuffled algorithm with the list of open coords
        shuffledOpenTilesCoords = new Queue<Coord>(Utility.ShuffleArray(allOpenTilesCoords.ToArray(), currentMap.seed));

        // create the four mask nav mesh for each coodinates of the map
        // they are use to fill the navmesh zone outside the map like obstacle so enemies can't move outside the map
        Transform maskLeft = Instantiate(navmeshMaskPrefab, Vector3.left * (currentMap.mapSize.x + currentMap.MaxMapSize.x) / 4f * tileSize, Quaternion.identity);
        maskLeft.parent = mapHolder;
        maskLeft.localScale = new Vector3((currentMap.MaxMapSize.x - currentMap.mapSize.x) / 2f, 5f, currentMap.mapSize.y) * tileSize;

        Transform maskRight = Instantiate(navmeshMaskPrefab, Vector3.right * (currentMap.mapSize.x + currentMap.MaxMapSize.x) / 4f * tileSize, Quaternion.identity);
        maskRight.parent = mapHolder;
        maskRight.localScale = new Vector3((currentMap.MaxMapSize.x - currentMap.mapSize.x) / 2f, 5f, currentMap.mapSize.y) * tileSize;

        Transform maskTop = Instantiate(navmeshMaskPrefab, Vector3.forward * (currentMap.mapSize.y + currentMap.MaxMapSize.y) / 4f * tileSize, Quaternion.identity);
        maskTop.parent = mapHolder;
        maskTop.localScale = new Vector3(currentMap.MaxMapSize.x, 5f, (currentMap.MaxMapSize.y - currentMap.mapSize.y) / 2f) * tileSize;

        Transform maskBottom = Instantiate(navmeshMaskPrefab, Vector3.back * (currentMap.mapSize.y + currentMap.MaxMapSize.y) / 4f * tileSize, Quaternion.identity);
        maskBottom.parent = mapHolder;
        maskBottom.localScale = new Vector3(currentMap.MaxMapSize.x, 5f, (currentMap.MaxMapSize.y - currentMap.mapSize.y) / 2f) * tileSize;

        // scale of the navmesh floor
        navmeshFloor.localScale = new Vector3(currentMap.MaxMapSize.x, currentMap.MaxMapSize.y) * tileSize;

        // scale of the map floor
        mapFloor.localScale = new Vector3(currentMap.mapSize.x, currentMap.mapSize.y) * tileSize;
    }

    // check if there are area locked for the movement of the player becaouse of obstacles
    bool MapIsFullyAccessible(bool[,] obstaclesMap, int currentObstacleCount) {
        // copy of the map obstacle
        bool[,] mapFlags = new bool[obstaclesMap.GetLength(0), obstaclesMap.GetLength(1)];
        Queue<Coord> queue = new Queue<Coord>();
        // we start by center because we are sure that it is not fill
        // add mapcenter to the queue and set it on true on the map
        queue.Enqueue(currentMap.MapCenter);
        mapFlags[currentMap.MapCenter.x, currentMap.MapCenter.y] = true;

        // keep track of all the tile visited, it start from one because the center tiles is accessible
        int accessibleTileCount = 1;

        // until there are coordinates in our queue
        while (queue.Count > 0) {
            // get the first item on the queue
            Coord tile = queue.Dequeue();
            // we loop through all the adjacent element of the tile
            for (int x = -1; x <= 1; x++) {
                for (int y = -1; y <= 1; y++) {
                    // set the adjacent tiles coord
                    int neighbourX = tile.x + x;
                    int neighbourY = tile.y + y;
                    // in that way we don't check the diagonals
                    if (x == 0 || y == 0) {
                        // if we are inside the obstacle map
                        if (neighbourX >= 0 && neighbourX < obstaclesMap.GetLength(0) && neighbourY >= 0 && neighbourY < obstaclesMap.GetLength(1)) {
                            // we have not checked this tile and is not an obstacle
                            if (!mapFlags[neighbourX, neighbourY] && !obstaclesMap[neighbourX, neighbourY]) {
                                // so is free from obstacles
                                mapFlags[neighbourX, neighbourY] = true;
                                queue.Enqueue(new Coord(neighbourX, neighbourY));
                                accessibleTileCount++;
                            }
                        }
                    }
                }
            }
        }
        // how many tiles should there be
        int targetAccessibleTileCount = (int)(currentMap.mapSize.x * currentMap.mapSize.y - currentObstacleCount);
        return targetAccessibleTileCount == accessibleTileCount;
    }

    // return the position based on the coord
    Vector3 CoordToPosition(int x, int y) {
        return new Vector3(-currentMap.mapSize.x / 2f + 0.5f + x, 0, -currentMap.mapSize.y / 2f + 0.5f + y) * tileSize;
    }

    // get coordinates of the player from position
    public Transform GetTileFromPosition(Vector3 position) {
        // we use a reverse formula based on the coordtoposition method to find the current position of player
        int x = Mathf.RoundToInt((int)(position.x / tileSize) + (currentMap.mapSize.x - 1) / 2f);
        int y = Mathf.RoundToInt((int)(position.z / tileSize) + (currentMap.mapSize.y - 1) / 2f);
        // x and y will have to be contained in the map so we have to clamp them
        x = Mathf.Clamp(x, 0, tileMap.GetLength(0) - 1);
        y = Mathf.Clamp(y, 0, tileMap.GetLength(1) - 1);
        // return the coord of the tile where player is
        return tileMap[x, y];
    }

    public Coord GetRandomCoord() {
        // we take the first element in the queue
        Coord randomCoord = shuffledTilesCoords.Dequeue();
        // we enqueue it at the end of the queue
        shuffledTilesCoords.Enqueue(randomCoord);
        // we return the random coord
        return randomCoord;
    }

    public Transform GetRandomOpenTile() {
        // we take the first element in the queue
        Coord randomCoord = shuffledOpenTilesCoords.Dequeue();
        // we enqueue it at the end of the queue
        shuffledOpenTilesCoords.Enqueue(randomCoord);
        // return the random coord on the tiles map
        return tileMap[randomCoord.x, randomCoord.y];
    }

    // coordinates for each tile
    // show up in the inspector
    [System.Serializable]
    public struct Coord {
        public int x;
        public int y;

        public Coord(int _x, int _y) {
            x = _x;
            y = _y;
        }

        // override all the function to properly work with coordinates structure
        public override bool Equals(System.Object obj) {
            return obj is Coord && this == (Coord)obj;
        }

        public bool Equals(Coord c) {
            return this == c;
        }

        public static bool operator ==(Coord c1, Coord c2) {
            return c1.x == c2.x && c1.y == c2.y;
        }

        public static bool operator !=(Coord c1, Coord c2) {
            return !(c1 == c2);
        }

        public override int GetHashCode() {
            return x ^ y;
        }
    }

    // show up in the inspector
    [System.Serializable]
    public class Map {
        public Coord mapSize;
        // number of obstacle to spawn in percentage
        [Range(0, 1)] public float obstaclePercent;
        // seed for the pseudorandom generation of the obstacles
        public int seed;
        public float minObstacleHeight;
        public float maxObstacleHeight;
        public Color foreground;
        public Color background;
        // define the center of the map where enemies spawn
        public Coord MapCenter {
            get {
                return new Coord(mapSize.x / 2, mapSize.y / 2);
            }
        }
        // create the corner navigator
        public Vector2 MaxMapSize {
            get {
                return new Vector2(mapSize.x * 1.2f, mapSize.y * 1.2f);
            }
        }
    }
}