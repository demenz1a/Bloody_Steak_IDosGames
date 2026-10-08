using UnityEngine;

public class ParticleEffectEvent : MonoBehaviour
{
    [Header("Effects")]
    [SerializeField] private ParticleSystem cookingParticles;

    public void PlayCookingParticles()
    {
        if (cookingParticles != null)
        {
            cookingParticles.Play();
        }
    }
}
