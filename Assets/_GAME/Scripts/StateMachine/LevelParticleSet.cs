using UnityEngine;

[System.Serializable]
public class LevelParticleSet
{
    [Tooltip("Level này (1, 2, 3, ...)")]
    public int level;
    
    [Tooltip("Particle system cho level này")]
    public ParticleSystem levelParticle;
    
    [Tooltip("Particle khi có 10 kiếm")]
    public ParticleSystem sword10Particle;
    
    [Tooltip("Particle khi có 20 kiếm")]
    public ParticleSystem sword20Particle;
}
