using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpeedBoostPickup : MonoBehaviour
{
    private EnemySpeedBoostDirector _director;
    private int _clusterIndex;

    public void Configure(EnemySpeedBoostDirector director, int clusterIndex)
    {
        _director = director;
        _clusterIndex = clusterIndex;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _director?.ActivateBoost(_clusterIndex);
        Destroy(gameObject);
    }
}
