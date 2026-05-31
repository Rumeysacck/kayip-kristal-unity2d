using System.Collections.Generic;
using UnityEngine;

public class EnemySpeedBoostDirector : MonoBehaviour
{
    private class EnemyCluster
    {
        public float MinX;
        public float MaxX;
        public float SumY;
        public float MinY;
        public int Count;
        public int ApproachDirection = 1;

        public float CenterY => SumY / Mathf.Max(1, Count);
        public float Width => MaxX - MinX;
    }

    private readonly List<EnemyCluster> _clusters = new();
    private Platformer2D _player;
    private EnemyCluster _activeCluster;
    private float _boostMovementSpeed;
    private float _boostMaxVelocityX;
    private float _activeUntil;
    private Sprite _speedIconSprite;

    private const float ClusterXGap = 13f;
    private const float ClusterYGap = 4.2f;
    private const float PickupOffset = 2.2f;
    private const float ExitPadding = 4f;
    private const float MinimumBoostSeconds = 1.2f;
    private const float FallbackBoostSeconds = 9f;

    public void Initialize()
    {
        GameObject playerObject = PlayerLocator.FindMainPlayer();
        _player = playerObject != null ? playerObject.GetComponent<Platformer2D>() : null;
        if (_player == null) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies == null || enemies.Length == 0) return;

        _boostMovementSpeed = _player.MovementSpeed;
        _boostMaxVelocityX = _player.MaxVelocity.x;
        List<Vector2> enemyPositions = new();
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null || !enemy.activeInHierarchy) continue;

            Platformer2D enemyPlatformer = enemy.GetComponent<Platformer2D>();
            if (enemyPlatformer != null)
            {
                _boostMovementSpeed = Mathf.Max(_boostMovementSpeed, enemyPlatformer.MovementSpeed);
                _boostMaxVelocityX = Mathf.Max(_boostMaxVelocityX, enemyPlatformer.MaxVelocity.x);
            }
            enemyPositions.Add(enemy.transform.position);
        }

        if (enemyPositions.Count == 0) return;
        BuildClusters(enemyPositions);
        SpawnPickups();
    }

    private void Update()
    {
        if (_activeCluster == null || _player == null) return;
        if (Time.time < _activeUntil) return;

        float playerX = _player.transform.position.x;
        bool passedCluster = _activeCluster.ApproachDirection > 0
            ? playerX > _activeCluster.MaxX + ExitPadding
            : playerX < _activeCluster.MinX - ExitPadding;

        if (passedCluster || Time.time > _activeUntil + FallbackBoostSeconds)
            ClearBoost();
    }

    public void ActivateBoost(int clusterIndex)
    {
        if (_player == null || clusterIndex < 0 || clusterIndex >= _clusters.Count) return;

        _activeCluster = _clusters[clusterIndex];
        _activeUntil = Time.time + MinimumBoostSeconds;
        _player.ApplyTemporarySpeedBoost(_boostMovementSpeed, _boostMaxVelocityX);

        MissionHUD hud = FindObjectOfType<MissionHUD>();
        if (hud != null)
            hud.ShowToast("Hız ikonu alındı. Düşman bölgesinde hızın eşitlendi.", 2.2f);
    }

    private void ClearBoost()
    {
        _player?.ClearTemporarySpeedBoost();
        _activeCluster = null;

        MissionHUD hud = FindObjectOfType<MissionHUD>();
        if (hud != null)
            hud.ShowToast("Hız normale döndü.", 1.8f);
    }

    private void BuildClusters(List<Vector2> positions)
    {
        positions.Sort((a, b) => a.x.CompareTo(b.x));
        Vector2 playerPosition = _player.transform.position;

        foreach (Vector2 position in positions)
        {
            EnemyCluster cluster = _clusters.Count > 0 ? _clusters[^1] : null;
            if (cluster == null || position.x - cluster.MaxX > ClusterXGap || Mathf.Abs(position.y - cluster.CenterY) > ClusterYGap)
            {
                cluster = new EnemyCluster
                {
                    MinX = position.x,
                    MaxX = position.x,
                    SumY = position.y,
                    MinY = position.y,
                    Count = 1
                };
                _clusters.Add(cluster);
                continue;
            }

            cluster.MinX = Mathf.Min(cluster.MinX, position.x);
            cluster.MaxX = Mathf.Max(cluster.MaxX, position.x);
            cluster.SumY += position.y;
            cluster.MinY = Mathf.Min(cluster.MinY, position.y);
            cluster.Count++;
        }

        foreach (EnemyCluster cluster in _clusters)
            cluster.ApproachDirection = (cluster.MinX + cluster.Width * 0.5f) >= playerPosition.x ? 1 : -1;
    }

    private void SpawnPickups()
    {
        _speedIconSprite ??= CreateSpeedIconSprite();
        for (int i = 0; i < _clusters.Count; i++)
        {
            EnemyCluster cluster = _clusters[i];
            float pickupX = cluster.ApproachDirection > 0 ? cluster.MinX - PickupOffset : cluster.MaxX + PickupOffset;
            Vector2 pickupPosition = new(pickupX, cluster.MinY + 1.15f);

            GameObject pickup = new($"SpeedBoostIcon_{i}");
            pickup.transform.position = pickupPosition;

            SpriteRenderer renderer = pickup.AddComponent<SpriteRenderer>();
            renderer.sprite = _speedIconSprite;
            renderer.sortingOrder = 80;

            CircleCollider2D collider = pickup.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.82f;

            SpeedBoostPickup boostPickup = pickup.AddComponent<SpeedBoostPickup>();
            boostPickup.Configure(this, i);
        }
    }

    private static Sprite CreateSpeedIconSprite()
    {
        const int size = 48;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        Color clear = new(0f, 0f, 0f, 0f);
        Color fill = new(0.22f, 0.96f, 1f, 1f);
        Color edge = new(0.05f, 0.26f, 0.34f, 1f);
        Color bolt = new(1f, 0.94f, 0.36f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new(x - size * 0.5f, y - size * 0.5f);
                float distance = p.magnitude;
                Color color = clear;
                if (distance < 21f) color = fill;
                if (distance > 18f && distance < 22f) color = edge;
                if (IsBoltPixel(x, y)) color = bolt;
                texture.SetPixel(x, y, color);
            }
        }

        texture.filterMode = FilterMode.Point;
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 48f);
    }

    private static bool IsBoltPixel(int x, int y)
    {
        return x > 21 && x < 31 && y > 10 && y < 28
            || x > 15 && x < 26 && y > 20 && y < 36
            || x > 20 && x < 28 && y > 30 && y < 42;
    }
}
