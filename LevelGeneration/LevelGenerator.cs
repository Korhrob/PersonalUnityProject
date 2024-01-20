using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : Singleton<LevelGenerator> {

    private Transform t;

    public bool debug;
    public bool GenerationFinished = false;
    public int GenerationStage = 0;
    private int maxGenerationStages = 5; // grid, tiles, prefabs, path, x

    private GameObject portal; // Reference to portal - might not need
    public GameObject debugPrefab;
    public GameObject exitPortal, pathBlocker, minimapLayout;
    public GameObject defaultPlatform; // Default playable area
    public GameObject lowQualityPlatform; // Fill outer grid
    public GameObject defaultTree; // Replace with array
    public GameObject[] treePrefab, bushPrefab, rockPrefab, doodadPrefab, pathDetails, fencePrefab, wallPrefab; // Replace rockPrefab with rockFormation prefab
    public GameObject[] orientedWallPrefab;

    public GameObject treasurePrefab, secretRoomTrigger;
    public GameObject mapEdgeDecoration; // Replace with array

    public bool useWind = true;
    public GameObject[] windParticles;
    private GameObject[] sceneParticles;

    public GameObject[] orePrefabs;

    public int mapSize; // 8 Tiles x 8 Tiles
    public int tileSize; // 4 Units
    private float halfTileSize, thirdTileSize;
    public int maxTiles; // Max playable area
    public int seed;

    public MapTile startingTile, finalTile; // rename to first and last tile
    public Dictionary<Vector2, MapTile> levelGrid = new Dictionary<Vector2, MapTile>();
    public List<MapTile> allTiles = new List<MapTile>(); // Entire grid
    public List<MapTile> edgeTiles = new List<MapTile>(); // Edge tiles
    public List<MapTile> innerTiles = new List<MapTile>(); // Inner tiles
    public List<MapTile> mapTiles = new List<MapTile>(); // Generated playable area
    public List<MapTile> wallTiles = new List<MapTile>();

    private readonly Vector2[] directions = new Vector2[] { // Direction array
        new Vector2(-1, 0),
        new Vector2(1, 0),
        new Vector2(0, -1),
        new Vector2(0, 1),
        new Vector2(0, 0) // Blank direction
    };

    private readonly int[] rotations = new int[] { // Fixed y rotation based on direction
        90,
        -90,
        0,
        180,
        0 // Blank rotation
    }; // Inverted

    // 3x3 Sockets
    private Vector2[] directions3x3 = new Vector2[] {
            new Vector2(-1, -1), new Vector2(0, -1), new Vector2(1, -1),
            new Vector2(-1, 0),/*new Vector2(0, 0),*/ new Vector2(1, 0),
            new Vector2(-1, 1), new Vector2(0, 1), new Vector2(1, 1),
    };

    // 2x2 Sockets (Used for trees)
    private readonly Vector2[] sockets = new Vector2[] {
            new Vector2(-1, -1), new Vector2(1, -1),
            new Vector2(-1, 1), new Vector2(1, 1)
    };


    // Might not need to serialize, just instantiate new copy
    [SerializeField] private SplatMapGenerator splatMapGenerator; 
    [SerializeField] private SplatMapPainter pathPainter;

    public class MapTile {

        public Vector2 pos; // GridPosition
        public GameObject tile; // Could move to gameObjects
        public List<GameObject> gameObjects = new List<GameObject>();

        public List<MapTile> neighbours = new List<MapTile>(); // Connected tiles
        public List<MapTile> openNeighbours = new List<MapTile>(); // Connected tiles that are "empty"

        public bool[] socketInUse; // = new bool[sockets.Length]; treeSockets

        public Dictionary<int, bool[]> edgeSockets; // 0, 1, 2, 3 direction, sockets, Might remove (Used to prevent overlapping prefabs)
        public bool[] edgeObjectSocket; // 0, 1, 2

        public bool hasTrees; // Removed
        public bool isPath; // Should replace with tags, ex. List<TileTags> = Path, Ambush or Stone, Ores
        public bool isWall;
        public bool isRoom; // Should move to tags
        public bool spawnOre;

        public enum TileType { // Base Type
            empty,
            floor,
            wall
        }
        public TileType tileType;

        public enum TerrainType {
            plains,
            forest,
            mountains,
            beach,
            water
        }
        public TerrainType terrainType; // Move to Tags?

        public enum WallOrientation {
            None,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }
        public WallOrientation wallOrientation;

        /// <summary>
        /// Resets MapTile to default values
        /// </summary>
        /// <param name="numSockets">Number of tree sockets (max 5)</param>
        public void Initialize(int numSockets) {

            Clear();

            openNeighbours = new List<MapTile>(neighbours);

            /*
            foreach (MapTile mapTile in neighbours)
                openNeighbours.Add(mapTile);
            */

            tileType = TileType.empty;
            socketInUse = new bool[numSockets];
            spawnOre = false;

        }

        private void Clear() {

            if (tile != null) {

                Destroy(tile);
                tile = null;

            }

            foreach (GameObject go in gameObjects)
                Destroy(go);

            gameObjects = new List<GameObject>();

            hasTrees = false;
            isPath = false;
            isRoom = false;

        }

        /// <summary>
        /// Removes tile from neighbouring open tiles
        /// </summary>
        public void DisconnectTile() {

            foreach (MapTile neighbour in neighbours)
                neighbour.openNeighbours.Remove(this);

        }

        public void SetCornerOrientation() {

            bool hasTopWall = instance.MapTileAt(pos + Vector2.up)?.isWall ?? false;
            bool hasBottomWall = instance.MapTileAt(pos + Vector2.down)?.isWall ?? false;
            bool hasLeftWall = instance.MapTileAt(pos + Vector2.left)?.isWall ?? false;
            bool hasRightWall = instance.MapTileAt(pos + Vector2.right)?.isWall ?? false;

            if (hasTopWall && hasLeftWall) {
                wallOrientation = WallOrientation.TopLeft;
            } else if (hasTopWall && hasRightWall) {
                wallOrientation = WallOrientation.TopRight;
            } else if (hasBottomWall && hasLeftWall) {
                wallOrientation = WallOrientation.BottomLeft;
            } else if (hasBottomWall && hasRightWall) {
                wallOrientation = WallOrientation.BottomRight;
            }

        }

    }

    protected override void CustomAwake() { // Initialize Grid

        GenerateSceneParticles();

        t = new GameObject("Level").transform; // Could pass this to to use in pathfinding generation

        levelGrid = new Dictionary<Vector2, MapTile>();
        allTiles = new List<MapTile>();
        edgeTiles = new List<MapTile>();
        innerTiles = new List<MapTile>();

        halfTileSize = tileSize * 0.5f;
        thirdTileSize = tileSize * 0.333f;

        int x, y;

        for (x = -mapSize; x < mapSize; x++) {

            for (y = -mapSize; y < mapSize; y++) {

                Vector2 curPos = new Vector2(x, y);
                MapTile newTile = new MapTile();

                newTile.pos = curPos;
                levelGrid.Add(curPos, newTile);

                if (Mathf.Abs(x - 1) == mapSize || Mathf.Abs(y - 1) == mapSize)
                    edgeTiles.Add(newTile);
                else
                    innerTiles.Add(newTile);

                allTiles.Add(newTile);

            }

        }

        // Initialize neighbours, must loop a second time unless neighbours are switched from MapTile references to coordinate values

        for (x = -mapSize; x < mapSize; x++) {

            for (y = -mapSize; y < mapSize; y++) {

                Vector2 curPos = new Vector2(x, y);

                for (int i = 0; i < directions.Length; i++) {

                    if (TileAt(curPos + directions[i], out MapTile neighbour)) // Chech for adjacent tile
                        levelGrid[curPos].neighbours.Add(neighbour);

                }

            }

        }

        Generate();

    }

    /// <summary>
    /// Initialize all MapTiles
    /// </summary>
    private void ResetGrid() {

        //foreach (MapTile mapTile in allTiles) {}

        for (int x = -mapSize; x < mapSize; x++) {

            for (int y = -mapSize; y < mapSize; y++) {

                Vector2 curPos = new Vector2(x, y);
                MapTile mapTile = levelGrid[curPos];

                mapTile.Initialize(sockets.Length);

            }

        }

    }

    private void SetSeed() {

        seed = 1;

        // Only get session seed if not in debug mode
        if (debug == false && GameSession.instance != null)
            seed = GameSession.instance.seed;

        Random.InitState(seed);

    }

    private void GenerateSceneParticles() {

        // Probably move this to GlobalEnvironmentManager or something
        // to manage wind direction, lighting etc

        sceneParticles = new GameObject[4];

        if (!useWind)
            return;

        for (int i = 0; i < windParticles.Length; i++)
            sceneParticles[i] = Instantiate(windParticles[i], LocalPlayer.Transform);

        // Parent to player or make wind effects follow player?
        // Add global wind direction too

    }

    private void UpdateWindDirection() {

        if (!useWind)
            return;

        Vector3 direction = new Vector3(0, Random.Range(0, 360), 0);
        Quaternion windDirection = Quaternion.Euler(direction);

        for (int i = 0; i < windParticles.Length; i++)
            sceneParticles[i].transform.rotation = windDirection;

    }

    public void RemoveSceneParticles() { // Clean up scene

        if (!useWind)
            return;

        for (int i = 0; i < sceneParticles.Length; i++) {

            if (sceneParticles[i] != null)
                Destroy(sceneParticles[i]);

        }

    }

    [ContextMenu("Generate")]
    public void Generate() {

        StopAllCoroutines();

        // Initialize

        GenerationStage = 0;
        GenerationFinished = false;

        if (MeshInstanceManager.instance != null) { // Clear previous grass instances

            MeshInstanceManager.instance.Disable();
            MeshInstanceManager.instance.ClearMeshInstances();

        }

        SetSeed();
        ResetGrid();

        GeneratePlayableArea();

        StartCoroutine(Finalize());

    }

    private void GeneratePlayableArea() {
        
        UpdateGenerationStage("Generate Layout"); // 1

        // TODO: dont let generation reach outer tiles

        // Pick random inner tile for starting point, can also use edgeTiles to force level generation from edges
        int r = Random.Range(0, innerTiles.Count);
        startingTile = innerTiles[r];
        startingTile.DisconnectTile();

        mapTiles = new List<MapTile>();
        wallTiles = new List<MapTile>();

        MapTile prevTile;
        MapTile curTile = startingTile;
        mapTiles.Add(curTile);

        int loop = maxTiles;

        while (loop > 0) {

            prevTile = curTile;

            if (curTile.openNeighbours.Count > 0) { // Grow

                r = Random.Range(0, curTile.openNeighbours.Count);
                curTile = curTile.openNeighbours[r];

                curTile.openNeighbours.Remove(prevTile);

                curTile.DisconnectTile();

                //foreach (MapTile neighbour in curTile.neighbours)
                //    neighbour.openNeighbours.Remove(curTile);

            } else { // Branch

                r = Random.Range(0, mapTiles.Count);
                curTile = mapTiles[r];

            }

            if (mapTiles.Contains(curTile) == false)
                mapTiles.Add(curTile);

            loop--;

        }

        finalTile = curTile;

    }

    private IEnumerator Finalize() { // Rename function

        UpdateGenerationStage("Generate Tiles"); // 2

        GenerateTiles();
        RoomDetection();

        GenerateSecretRoom();

        FillEmptyTiles();

        UpdateGenerationStage("Generate Paths"); // 3

        // Possible to generate pathfinding here
        //GenerationStage++;

        splatMapGenerator.GenerateSplatMap();
        yield return StartCoroutine(PathWalker());

        UpdateGenerationStage("Generate Details"); // 4

        // Possible to generate minimap snapshot here

        GenerateDetails();
        UpdateWindDirection();

        UpdateGenerationStage("Finalize"); // 5

        GenerationFinished = true;

    }

    private void GenerateTiles() {

        // Fill playable area

        foreach (MapTile mapTile in mapTiles) {

            mapTile.tileType = MapTile.TileType.floor;

            if (mapTile.tile == null) { // Prevent duplicate tiles

                mapTile.tile = Instantiate(defaultPlatform, WorldPosition(mapTile.pos), Quaternion.Euler(90, 0, 0), t); // Use defaultPlatform <- grassPlatform

                // Generate minimap cover
                GameObject minimapLayoutPrefab = Instantiate(minimapLayout, WorldPosition(mapTile.pos), Quaternion.identity, t);
                mapTile.gameObjects.Add(minimapLayoutPrefab);

            }

            GenerateEdgeBlocker(mapTile);

        }

        // Exit Portal
        portal = Instantiate(exitPortal, WorldPosition(finalTile.pos), Quaternion.identity);
        finalTile.gameObjects.Add(portal);

    }

    private void FillEmptyTiles() {

        if (lowQualityPlatform == null)
            return;

        // Fill empty tiles -> use allTiles

        for (int x = -mapSize; x < mapSize; x++) {

            for (int y = -mapSize; y < mapSize; y++) {

                Vector2 curPos = new Vector2(x, y);
                MapTile mapTile = levelGrid[curPos];

                if (mapTile.tileType != MapTile.TileType.empty)
                    continue;

                // pathBlocker
                mapTile.tile = Instantiate(lowQualityPlatform, WorldPosition(curPos), Quaternion.Euler(90, 0, 90), t); // Use lower res tiles

                bool useEdgeDecoration = false;

                foreach (MapTile neighbour in mapTile.openNeighbours) {

                    if (neighbour.tileType != MapTile.TileType.empty) {

                        useEdgeDecoration = true;
                        break;

                    }

                }

                if (useEdgeDecoration) {

                    GameObject decoration = Instantiate(mapEdgeDecoration, mapTile.tile.transform);
                    mapTile.gameObjects.Add(decoration);

                }

            }

        }

    }

    private void GenerateEdgeBlocker(MapTile mapTile) {

        GameObject edgePrefab = pathBlocker; // Default

        // In bounds

        foreach (MapTile openNeighbour in mapTile.openNeighbours) {

            openNeighbour.isWall = true; // New
            openNeighbour.tileType = MapTile.TileType.wall;

            edgePrefab = GetEdgeTileType(openNeighbour);

            if (openNeighbour.tile == null) { // Prevent duplicate tiles

                // Default edge tile
                openNeighbour.tile = Instantiate(edgePrefab, WorldPosition(openNeighbour.pos), Quaternion.Euler(90, 0, 0), t);

                wallTiles.Add(openNeighbour);

            }

        }

        // Out of bounds

        for (int i = 0; i < directions.Length; i++) {

            if (TileAt(mapTile.pos + directions[i], out MapTile openTile) == false) {

                edgePrefab = GetEdgeTileType(mapTile);

                // Edge tile outside of playable grid
                mapTile.gameObjects.Add(Instantiate(edgePrefab, WorldPosition(mapTile.pos + directions[i]), Quaternion.Euler(90, 0, 0), t));

            }

        }

    }

    private GameObject GetEdgeTileType(MapTile mapTile) {

        GameObject edgePrefab = pathBlocker;

        //if (mapTile.hasTrees == false) // TODO: Set terrain tile type and use that
        //    edgePrefab = edgeTilePrefabs[1];

        return edgePrefab;

    }

    private void GenerateSecretRoom() {

        List<MapTile> possibleRooms = new List<MapTile>();
        List<Vector2> pathPos = new List<Vector2>();

        foreach (MapTile mapTile in mapTiles) {

            if (mapTile == startingTile || mapTile == finalTile)
                continue;

            if (mapTile.openNeighbours.Count < 2)
                continue;

            for (int i = 0; i < directions.Length; i++) {

                if (TileAt(mapTile.pos + (directions[i] * 2), out MapTile tempRoom)) { // If there is a tile here

                    if (tempRoom.tile != null)
                        continue;

                    if (TileAt(mapTile.pos + directions[i], out MapTile t_inbetweenTile)) {

                        // Optional conditions for connecting tile
                        if (t_inbetweenTile.openNeighbours.Count < 4)
                            continue;

                    }

                    // Add to list, pair secret room, tile connected
                    possibleRooms.Add(tempRoom);
                    pathPos.Add(mapTile.pos + directions[i]);

                }

            }

        }

        if (possibleRooms.Count == 0) {

            Debug.Log("Secret room not possible");
            return;

        }

        MapTile secretRoom = null;
        MapTile inbetweenTile = null;

        while (secretRoom == null) { // Probaly dont need while loop

            int i = Random.Range(0, possibleRooms.Count);
            MapTile tempRoom = possibleRooms[i];

            // Can condense this function to generate treasure room

            tempRoom.tileType = MapTile.TileType.floor;

            mapTiles.Add(tempRoom);

            tempRoom.DisconnectTile();

            // Replace prefab with secretroom prefab
            tempRoom.tile = Instantiate(defaultPlatform, WorldPosition(tempRoom.pos), Quaternion.Euler(90, 0, 0));

            PathfindPlatform pathfindPlatform = tempRoom.tile.GetComponent<PathfindPlatform>();
            pathfindPlatform.NoPatrolZone = true;

            // Generate path / remove path blocker to secret room
            if (TileAt(pathPos[i], out inbetweenTile)) {

                // Can condense this function to generate treasure room connector

                if (inbetweenTile.tileType == MapTile.TileType.wall) {

                    Destroy(inbetweenTile.tile);

                    inbetweenTile.tileType = MapTile.TileType.floor;

                    mapTiles.Add(inbetweenTile);

                    inbetweenTile.DisconnectTile();

                    // Create a path tile to secret room here
                    inbetweenTile.tile = Instantiate(defaultPlatform, WorldPosition(inbetweenTile.pos), Quaternion.Euler(90, 0, 0));

                    PathfindPlatform t_pathfindPlatform = inbetweenTile.tile.GetComponent<PathfindPlatform>();
                    t_pathfindPlatform.NoPatrolZone = true;

                    GenerateTreesOnTile(inbetweenTile, 4, chance: 1f);

                }

            }

            secretRoom = tempRoom;

        }

        GenerateEdgeBlocker(secretRoom);
        GenerateEdgeBlocker(inbetweenTile);

        GameObject treasure = Instantiate(treasurePrefab, WorldPosition(secretRoom.pos), Quaternion.Euler(0, 180, 0));
        secretRoom.gameObjects.Add(treasure);

        GameObject trigger = Instantiate(secretRoomTrigger, WorldPosition(secretRoom.pos), Quaternion.identity);
        secretRoom.gameObjects.Add(trigger);

        GenerateDetail(inbetweenTile, null, bushPrefab, 0f, 0.3f, centerClamp: 0.75f, entireTile: true);

    }

    private void GenerateDetailWithOffset(MapTile mapTile, int i, GameObject[] prefabs) { // Add scale variance

        if (prefabs.Length == 0)
            return;

        Vector3 finalPosition = WorldPosition(mapTile.pos);
        finalPosition += new Vector3(directions[i].x, 0, directions[i].y) * (halfTileSize + Random.Range(1f, 1.5f));

        Vector3 randomPosition = new Vector3(Random.Range(-halfTileSize, halfTileSize), 0, Random.Range(-halfTileSize, halfTileSize)) * .75f; // * centerClamp
        randomPosition += new Vector3(directions[i].x, 0, directions[i].y) * halfTileSize; // * edgeClamp

        if (i == 0 || i == 1) {

            randomPosition.x *= 0.1f;

        } else {

            randomPosition.z *= 0.1f;

        }

        finalPosition += randomPosition;

        Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

        // SCALE

        float scaleModifier = 1 + Random.Range(-0.1f, 0.1f);

        // OBJECT

        int r = Random.Range(0, prefabs.Length);

        GameObject newDetail = Instantiate(prefabs[r], finalPosition, randomRotation, t);
        newDetail.transform.localScale *= scaleModifier;

        mapTile.gameObjects.Add(newDetail);

    }

    private void GenerateDetailsOnTile(MapTile mapTile) {

        //Instantiate(debugPrefab, WorldPosition(mapTile.pos), Quaternion.identity); // Tile position debug

        // Stuff that only generates next to / inside a wall

        List<int> wallSockets = DetectWallSockets(mapTile, false);

        for (int i = 0; i < directions.Length; i++) { // For every direction

            // Edge position debug

            Vector3 edgePosition = WorldPosition(mapTile.pos);
            edgePosition += new Vector3(directions[i].x, 0, directions[i].y) * halfTileSize;

            // Instantiate(debugPrefab, edgePosition, Quaternion.identity);

            //Instantiate(debugPrefab, WorldPosition(mapTile.pos + directions[i]), Quaternion.identity);
            // Could define edge tiles here so we dont have to calculate them multiple times per time

            if (TileAt(mapTile.pos + directions[i], out MapTile openTile)) { // Inside Grid

                if (openTile.tileType == MapTile.TileType.wall) {

                    //Instantiate(debugPrefab, edgePosition, Quaternion.identity); Edge position debug

                    GenerateTreesOnTile(openTile, -1); // i

                    GenerateDetailWithOffset(mapTile, i, bushPrefab);

                    GenerateWallsOnTile(mapTile, i);

                }

            } else { // Outside Grid

                GenerateTreesOnTile(mapTile, i);

                GenerateDetailWithOffset(mapTile, i, bushPrefab);

                GenerateWallsOnTile(mapTile, i);

            }

        }

        if (mapTile.isPath) { // Path details

            GenerateDetail(mapTile, wallSockets, pathDetails, -0.4f, -0.2f, centerClamp: 0.5f, chance: 0.2f, entireTile: true);
            GenerateDetail(mapTile, wallSockets, fencePrefab, 0f, 0f, centerClamp: 0.6f, edgeClamp: 0.9f, maxDetail: 1, entireTile: false, fixedOrientation: true);

        }

        // Stuff that can generate anywhere inside the tile

        //GenerateDetail(mapTile, wallSockets, bushPrefab, 0f, 0.3f, edgeClamp: 0.25f, entireTile: !mapTile.isPath);
        GenerateDetail(mapTile, wallSockets, doodadPrefab, -0.4f, 0f, 1, edgeClamp: 0.9f, entireTile: !mapTile.isPath);
        //GenerateDetail(mapTile, wallSockets, rockPrefab, -0.25f, 0f, edgeClamp: 0.75f, entireTile: false); // entireTile false mean this will only generate on edges with a wall

        if (mapTile.spawnOre)
            GenerateDetail(mapTile, wallSockets, orePrefabs, -0.1f, 0.1f, 3, edgeClamp: 0.8f, entireTile: false, chance: 0.1f);

    }

    private void GenerateDetails() {

        foreach (MapTile wallTile in wallTiles) {

            if (wallTile.openNeighbours.Count >= 2) {

                wallTile.SetCornerOrientation();

            }

        }

        foreach (MapTile mapTile in mapTiles)
            GenerateDetailsOnTile(mapTile);

    }

    private void GenerateTreesOnTile(MapTile mapTile, int i, float chance = 0.6f) { // i is edge direction, -1 means we dont use it

        if (defaultTree == null)
            return;

        // Prevent generating trees on a tile multiple times, might not need!

        if (mapTile.hasTrees == true)
            return;

        mapTile.hasTrees = true;

        List<int> openSockets = new List<int>();

        for (int b = 0; b < sockets.Length; b++)
            if (mapTile.socketInUse[b] == false)
                openSockets.Add(b);

        float quarterTileSize = (halfTileSize * 0.5f);
        // Socket offset size should just be a constant value set based on number of sockets
        // ex. 2x2 = quarter of a tile, 3x3 = third of a tile

        if (openSockets.Count <= 0)
            return;

        // Lower index prefabs have much higher chance to appear

        int[] weightedRandom = new int[treePrefab.Length * 2];
        int indx = 0;
        int len = weightedRandom.Length;

        for (int b = 0; b < weightedRandom.Length; b++) {

            weightedRandom[b] = indx;

            if (b >= len / 2) {

                if (indx < treePrefab.Length)
                    indx++;

                len = len - b;

            }

        }

        int x = 0;

        do {

            x++;

            int socket = openSockets[Random.Range(0, openSockets.Count)];

            Vector3 betweenTiles = WorldPosition(mapTile.pos); // Inside Tile

            if (i > -1)
                betweenTiles = WorldPosition(mapTile.pos + directions[i]); // Inside edge tile

            Vector3 offsetPosition = new Vector3(sockets[socket].x, 0, sockets[socket].y) * quarterTileSize; // Socket position
            Vector3 randomPosition = new Vector3(Random.Range(-quarterTileSize, quarterTileSize), 0, Random.Range(-quarterTileSize, quarterTileSize)) * 0.25f; // Wiggle

            Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            float scaleModifier = 1 + Random.Range(-0.4f, -0.1f);

            int r = weightedRandom[Random.Range(0, weightedRandom.Length)];

            GameObject newTree = Instantiate(treePrefab[r], betweenTiles + offsetPosition + randomPosition, randomRotation, t);
            newTree.transform.localScale *= scaleModifier;

            mapTile.gameObjects.Add(newTree);

            mapTile.socketInUse[socket] = true;
            openSockets.Remove(socket);

            if (openSockets.Count <= 0)
                return;

        } while (Dice.Roll(chance));

        if (x > 3) // Error check
            Debug.LogError("Generated more than 4 trees on this tile");

    }

    private void GenerateWallsOnTile(MapTile mapTile, int i) {

        if (wallPrefab.Length == 0)
            return;

        Vector3 tilePosition = WorldPosition(mapTile.pos);
        Vector3 finalPosition = tilePosition;
        finalPosition += new Vector3(directions[i].x, 0, directions[i].y) * halfTileSize;

        float y = rotations[i];
        Quaternion rotation = Quaternion.Euler(0, y, 0);

        int r = Random.Range(0, wallPrefab.Length);
        GameObject prefab = wallPrefab[r];

        GameObject newObject = Instantiate(prefab, finalPosition, rotation, t);
        newObject.transform.parent = mapTile.tile.transform;
        mapTile.gameObjects.Add(newObject);

        if (r == 0) { // DEBUG ORE SPAWN, TODO MOVE ELSEWHERE!!!

            // Tag as ore spawn
            mapTile.spawnOre = true;

        }

    }

    private List<int> DetectWallSockets(MapTile mapTile, bool flipOrientation = false) {

        List<int> wallSockets = new List<int>();

        for (int b = 0; b < directions.Length; b++) { // For every direction

            if (TileAt(mapTile.pos + directions[b], out MapTile openTile)) { // Inside Grid

                if (openTile.tileType == MapTile.TileType.wall) {

                    wallSockets.Add(b);

                } else if (flipOrientation) {

                    wallSockets.Add(b);

                }
            }

        }

        return wallSockets;

    }

    private void GenerateDetail(MapTile mapTile, List<int> wallSockets, GameObject[] prefabs, float minVariation = -0.25f, float maxVariation = 0.25f, int maxDetail = 2, float centerClamp = 1f, float edgeClamp = 1f, float chance = 0.2f, bool entireTile = false, bool flipOrientation = false, bool fixedOrientation = false) { // i = orientation

        if (prefabs.Length == 0)
            return;

        while (Dice.Roll(chance) && maxDetail > 0) {

            // POSITION

            Vector3 finalPosition = WorldPosition(mapTile.pos);
            Vector3 randomPosition = new Vector3(Random.Range(-halfTileSize, halfTileSize), 0, Random.Range(-halfTileSize, halfTileSize)) * centerClamp; //Vector3.zero;

            int savedOrientation = 0;

            if (entireTile == false) {

                if (wallSockets == null) // No wall sockets defined
                    wallSockets = DetectWallSockets(mapTile, flipOrientation);

                if (wallSockets.Count == 0) // No potential wall sockets on this tile
                    return; // Probably a path with no edge tiles

                int i = wallSockets[Random.Range(0, wallSockets.Count)]; // Choose random wall socket
                savedOrientation = i; // Used for rotation later

                finalPosition += new Vector3(directions[i].x, 0, directions[i].y) * halfTileSize * edgeClamp;

                // Decrease random offset to keep object between tiles

                if (i == 0 || i == 1) {

                    randomPosition.x *= 0.1f;

                } else {

                    randomPosition.z *= 0.1f;

                }

            }

            finalPosition += randomPosition;

            // ROTATION

            Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

            if (fixedOrientation) { // Fixed rotation based on edge direction, TODO: corners

                //float y = savedOrientation * 90;
                float y = savedOrientation == 0 || savedOrientation == 1 ? 90 : 0;
                randomRotation = Quaternion.Euler(0, y, 0);

            }

            // SCALE

            float scaleModifier = 1 + Random.Range(minVariation, maxVariation);

            // OBJECT

            int r = Random.Range(0, prefabs.Length);

            GameObject newDetail = Instantiate(prefabs[r], finalPosition, randomRotation, t);
            newDetail.transform.localScale *= scaleModifier;

            mapTile.gameObjects.Add(newDetail);
            maxDetail--;

        }

    }

    public Vector3 WorldPosition(Vector2 gridPosition) {

        return new Vector3(gridPosition.x * tileSize, 0, gridPosition.y * tileSize);

    }

    public bool TileAt(Vector2 gridPosition, out MapTile mapTile) {

        if (levelGrid.ContainsKey(gridPosition)) {

            mapTile = levelGrid[gridPosition];
            return true;

        }

        mapTile = null;
        return false;

    }

    public MapTile MapTileAt(Vector2 gridPosition) {

        MapTile tile;
        if (levelGrid.TryGetValue(gridPosition, out tile))
            return tile;

        return null;

    }

    private void GenerateMonsters() {

        // Move function to dungeon manager, just request maptile positions from here

        if (NetworkManager.instance != null)
            if (NetworkManager.instance.NetworkState == NetworkState.client)
                return;

        if (GameSession.instance == null)
            return;

        float monsterChance = 0.05f + (GameSession.instance.curDifficulty * 0.01f);

        // 3 + 1 for every 5 difficulty + 1 for every stage
        // difficulty 1 = 3 + 4 + 5
        // difficulty 5 = 4 + 5 + 6
        int monsterPoints = 3 + Mathf.FloorToInt(GameSession.instance.curDifficulty / 5) + GameSession.instance.curStage;

        while (monsterPoints > 0) {

            foreach (MapTile mapTile in mapTiles) {

                if (mapTile == startingTile)
                    continue;

                // Exclude non patrollable areas

                PathfindPlatform platform;

                if (mapTile.tile.TryGetComponent(out platform))
                    if (platform.NoPatrolZone)
                        continue;

                // Monster spawning
                if (Dice.Roll(monsterChance)) {

                    // Spawn cluster of monsters
                    if (DungeonManager.instance)
                        DungeonManager.instance.SpawnMonsters(WorldPosition(mapTile.pos));

                    monsterPoints--;
                    monsterChance = 0.0f;

                } else {

                    monsterChance += 0.1f;

                }

                if (monsterPoints <= 0)
                    break;

            }

        }

    }

    private IEnumerator PathWalker() {

        // TO-DO size variation, random offset, curved paths and possible speed up painting

        int curTile = 0;

        Vector3 nextPosition = mapTiles[0].tile.transform.position;
        pathPainter.transform.position = nextPosition;
        float walkSpeed = 20f;

        while (curTile < mapTiles.Count - 2) {

            Vector3 direction = nextPosition - pathPainter.transform.position; // Should beging to curve towards next tile before reaching it
            pathPainter.transform.position += direction * walkSpeed * Time.deltaTime;

            if (Vector3.Distance(pathPainter.transform.position, nextPosition) < 0.25f || Vector3.Distance(pathPainter.transform.position, nextPosition) > 8f) {

                curTile++;

                // Shortcut

                for (int i = curTile; i < mapTiles.Count; i++) {

                    float distanceToShortcut = Vector3.Distance(pathPainter.transform.position, mapTiles[i].tile.transform.position); // CurTile - ShortCutTile

                    if (distanceToShortcut - 0.5f > tileSize * 2)
                        continue;

                    float distanceToCurTile = Vector3.Distance(pathPainter.transform.position, mapTiles[curTile].tile.transform.position);

                    if (distanceToShortcut - 0.5f <= distanceToCurTile) {

                        Vector3 dir = mapTiles[i].tile.transform.position - mapTiles[curTile].tile.transform.position;

                        if (Physics.Raycast(mapTiles[curTile].tile.transform.position + (Vector3.up * 2f), dir, 4f, StaticData.WORLD_LAYER) == false) // Make sure there's a straight line
                            curTile = i;

                    }

                    //curTile = i;

                }

                if (curTile < mapTiles.Count) {

                    nextPosition = mapTiles[curTile].tile.transform.position;
                    mapTiles[curTile].isPath = true; // Change to tag

                }

            }

            yield return null;

        }

        pathPainter.PaintPixels();

    }

    private void RoomDetection() {

        // Should create a list of possible room combinations and select them randomly

        foreach (MapTile tile in mapTiles)
            RoomDetectionFromTile(tile);

    }

    private void RoomDetectionFromTile(MapTile mapTile) {

        if (mapTile.isRoom) // No need to run this calculation
            return;

        List<MapTile> roomTiles = new List<MapTile>();
        roomTiles.Add(mapTile);

        for (int i = 0; i < directions3x3.Length; i++) {

            if (TileAt(mapTile.pos + directions3x3[i], out MapTile adjacent)) {

                if (adjacent.tileType != MapTile.TileType.floor)
                    continue;

                // Avoid overlap
                if (adjacent.isRoom)
                    continue;

                // Continue
                roomTiles.Add(adjacent);

            }

        }

        // If num connected tiles == 4, small room (2x2)
        // if num connected tiles == 9, large room (3x3)

        if (roomTiles.Count < 8) // Reduced from 9 to 8 
            return;

        foreach (MapTile tempTile in roomTiles) {

            // Mark as part of room, create debug object
            tempTile.isRoom = true;

            //GameObject a = Instantiate(debugPrefab, tempTile.tile.transform.position, Quaternion.identity);
            //tempTile.gameObjects.Add(a);
            //a.transform.parent = tempTile.tile.transform;

        }

    }

    [ContextMenu("PlayLevel")]
    public void PlayLevel() { // Debug function, Move to dungeon manager ?

        if (MinimapCamera.instance) {

            MinimapCamera.instance.Refresh(true); // Could do this right after generating tiles

        }

        if (MeshInstanceManager.instance != null)
            MeshInstanceManager.instance.Enable();

        if (NewGameManager.instance != null)
            NewGameManager.instance.GeneratePathfinding(); // Generate pathfinding directly here

        GenerateMonsters();

        if (LocalPlayer.instance) {

            LocalPlayer.instance.Warp(WorldPosition(startingTile.pos) + Vector3.up * 0.5f);
            LocalPlayer.instance.SetRespawnPoint(WorldPosition(startingTile.pos) + Vector3.up * 0.5f);

        }

        DungeonManager.instance.Initialize(portal);
        NewGameManager.instance.EnterScene(GameSceneManager.activeMapData); // Asd

        string updatedMapName = $"({1 + GameSession.instance.curDifficulty}-{1 + DungeonManager.instance.StageDifficulty})";

        if (UI.instance)
            UI.instance.SetDifficultyText(updatedMapName);

    }

    private void UpdateGenerationStage(string taskName = "Debug") {

        GenerationStage++;

        if (GameSceneManager.instance)
            GameSceneManager.instance.CustomLoadingProgress(taskName, (float)GenerationStage / (float)maxGenerationStages);

    }

}
