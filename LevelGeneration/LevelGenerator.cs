using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour {

    public GameObject defaultPlatform, exitPortal, pathBlocker;
    public int mapSize; // 8 Tiles x 8 Tiles
    public int tileSize; // 4 Units
    public int maxTiles;

    public Dictionary<Vector2, MapTile> levelGrid = new Dictionary<Vector2, MapTile>();
    public List<MapTile> edgeTiles = new List<MapTile>();

    private Vector2[] directions = new Vector2[] {
        new Vector2(-1, 0),
        new Vector2(1, 0),
        new Vector2(0, -1),
        new Vector2(0, 1)
    };

    public class MapTile {

        public Vector2 pos;
        public GameObject tile;
        public List<GameObject> gameObjects = new List<GameObject>();

        public List<MapTile> neighbours = new List<MapTile>();
        public List<MapTile> openNeighbours = new List<MapTile>();

        public void Initialize() {

            if (tile != null) {

                Destroy(tile);
                tile = null;

            }

            openNeighbours = new List<MapTile>();

            // Duplicate neighbours to openNeighoburs
            foreach (MapTile mapTile in neighbours)
                openNeighbours.Add(mapTile);

            foreach (GameObject go in gameObjects)
                Destroy(go);

        }

    }

    public void Awake() {

        levelGrid = new Dictionary<Vector2, MapTile>();
        edgeTiles = new List<MapTile>();

        // Initialize grid

        int x, y = mapSize;

        for (x = -mapSize; x < mapSize; x++) {

            for (y = -mapSize; y < mapSize; y++) {

                Vector2 curPos = new Vector2(x, y);
                MapTile newTile = new MapTile();

                newTile.pos = curPos;
                levelGrid.Add(curPos, newTile);

                if (Mathf.Abs(x) == mapSize || Mathf.Abs(y) == mapSize)
                    edgeTiles.Add(newTile);

            }

        }

        // Initialize neighbours;

        for (x = -mapSize; x < mapSize; x++) {

            for (y = -mapSize; y < mapSize; y++) {

                Vector2 curPos = new Vector2(x, y);

                for (int i = 0; i < directions.Length; i++) {

                    if (TileAt(curPos + directions[i], out MapTile neighbour))
                        levelGrid[curPos].neighbours.Add(neighbour);

                }

            }

        }

    }

    [ContextMenu("WalkerGenerate")]
    public void WalkerGenerate() {

        foreach (MapTile mapTile in levelGrid.Values)
            mapTile.Initialize();

        List<MapTile> generatedLevel = new List<MapTile>();

        int r = Random.Range(0, edgeTiles.Count);
        MapTile startingTile = edgeTiles[r];
        MapTile prevTile;
        MapTile curTile = startingTile;
        generatedLevel.Add(curTile);

        int loop = maxTiles;

        while (loop > 0) {

            prevTile = curTile;

            if (curTile.openNeighbours.Count > 0) {

                // Grow
                
                r = Random.Range(0, curTile.openNeighbours.Count);
                curTile = curTile.openNeighbours[r];
                
                curTile.openNeighbours.Remove(prevTile);

                foreach (MapTile neighbour in curTile.neighbours)
                    neighbour.openNeighbours.Remove(curTile);

            } else {

                // Branch

                r = Random.Range(0, generatedLevel.Count);
                curTile = generatedLevel[r];

            }

            if (generatedLevel.Contains(curTile) == false)
                generatedLevel.Add(curTile);

            loop--;

        }

        // Ground Tiles
        foreach (MapTile mapTile in generatedLevel) {

            if (mapTile.tile == null)
                mapTile.tile = Instantiate(defaultPlatform, WorldPosition(mapTile.pos), Quaternion.Euler(90, 0, 0));

            foreach (MapTile openNeighobur in mapTile.openNeighbours) {

                if (openNeighobur.tile == null)
                    openNeighobur.tile = Instantiate(pathBlocker, WorldPosition(openNeighobur.pos), Quaternion.identity);

            }

            // Edge walls

            for (int i = 0; i < directions.Length; i++) {

                if (TileAt(mapTile.pos + directions[i], out MapTile edgeTile) == false) {

                    mapTile.gameObjects.Add(Instantiate(pathBlocker, WorldPosition(mapTile.pos + directions[i]), Quaternion.identity));

                }

            }

        }

        // Exit Portal
        curTile.gameObjects.Add(Instantiate(exitPortal, WorldPosition(curTile.pos), Quaternion.identity));

        // Generate navmesh data

        LocalPlayer.instance.SetRespawnPoint(WorldPosition(startingTile.pos) + Vector3.up);
        LocalPlayer.instance.gameObject.SetActive(true);

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

}
