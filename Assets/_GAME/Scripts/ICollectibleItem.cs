using UnityEngine;

public enum CollectibleItemType
{
    Sword = 0,
    HealthPack = 1,
    SpeedBoost = 2
}

public interface ICollectibleItem
{
    Vector3 Position { get; }
    bool IsActive { get; }
    GameObject GameObject { get; }
    CollectibleItemType ItemType { get; }
    void OnSpawn(Vector3 position);
    void OnDespawn();
}
