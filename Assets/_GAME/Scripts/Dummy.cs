using UnityEngine;

public class Dummy : MonoBehaviour
{
    [SerializeField] private SwordOrbit swordOrbit;

    public SwordOrbit GetSwordOrbit() => swordOrbit;
}
