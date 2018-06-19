using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour {

    public Enemy enemy;
    public float timeBetweenSpawns = 2f;
    public float timeAcceleration = 15f;

    MapGenerator map;
    float nextSpawnTime;
    float newSpawnTime;
    float nextAccelerationTime;
    LivingEntity player;
    float timeBetweenCampingChecks = 2f;
    float campThresholdDistance = 1.5f;
    float nextCampCheckTime;
    Vector3 campPositionOld;
    bool isCamping;
    bool playerIsDeath;
    readonly List<Enemy> enemyList = new List<Enemy>();

    void Start() {
        nextAccelerationTime = timeAcceleration;
        map = FindObjectOfType<MapGenerator>();
        player = FindObjectOfType<Player>();
        ResetMap();
    }

    // reset the game
    void ResetMap() {
        RemoveEnemies();
        map.GenerateMap();
        ResetPlayerPosition();
        nextCampCheckTime = timeBetweenCampingChecks + Time.time;
        campPositionOld = player.transform.position;
        player.OnDeath += OnPlayerDeath;
        newSpawnTime = timeBetweenSpawns;
        playerIsDeath = false;
    }

    void Update() {
        if (!playerIsDeath) {
            // check if the player is camping
            if (Time.time > nextCampCheckTime) {
                nextCampCheckTime = Time.time + timeBetweenCampingChecks;

                isCamping = (Vector3.Distance(player.transform.position, campPositionOld) < campThresholdDistance);
                campPositionOld = player.transform.position;
            }
            // accelerate spawn time
            if (Time.time > nextAccelerationTime) {
                nextAccelerationTime = Time.time + timeAcceleration;
                // until 0.5 seconds
                if (newSpawnTime > 0.5f) {
                    newSpawnTime -= 0.25f;
                }
            }
            // spawn enemies
            if (Time.time > nextSpawnTime) {
                nextSpawnTime = Time.time + newSpawnTime;
                StartCoroutine("SpawnEnemy");
            }
        }
    }

    // if player die reset position
    void ResetPlayerPosition() {
        player.transform.position = map.GetTileFromPosition(Vector3.zero).position + Vector3.up * 3f;
    }

    void OnPlayerDeath() {
        playerIsDeath = true;
        player.OnDeath -= OnPlayerDeath;
        StopCoroutine("SpawnEnemy");
        RemoveEnemies();
        ResetMap();
    }

    // destroy all the element
    void RemoveEnemies() {
        for (int i = 0; i < enemyList.Count; i++) {
            Destroy(enemyList[i].gameObject);
        }
        enemyList.Clear();
    }

    void RemoveEnemy(Enemy e) {
        enemyList.Remove(e);
    }


    IEnumerator SpawnEnemy() {
        float tileFlashSpeed = 4f;
        float spawnDelay = 1;

        Transform spawnTile = map.GetRandomOpenTile();
        if (isCamping) {
            spawnTile = map.GetTileFromPosition(player.transform.position);
        }
        Material tileMat = spawnTile.GetComponent<Renderer>().material;
        Color initialColour = tileMat.color;
        Color flashColour = Color.red;
        float spawnTimer = 0;

        while (spawnTimer < spawnDelay) {

            tileMat.color = Color.Lerp(initialColour, flashColour, Mathf.PingPong(spawnTimer * tileFlashSpeed, 1f));

            spawnTimer += Time.deltaTime;
            yield return null;
        }

        Enemy spawnedEnemy = Instantiate(enemy, spawnTile.position + Vector3.up, Quaternion.identity);
        enemyList.Add(spawnedEnemy);
        spawnedEnemy.OnDeathEnemy += RemoveEnemy;
    }
}