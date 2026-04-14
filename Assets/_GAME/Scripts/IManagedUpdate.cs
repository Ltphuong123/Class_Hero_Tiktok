using UnityEngine;

public interface IManagedUpdate
{
    void ManagedUpdate(float deltaTime);
    Vector3 Position { get; }
}
