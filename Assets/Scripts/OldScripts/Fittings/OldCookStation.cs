using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OldCookStation : MonoBehaviour
{
    public OldProductType inputProduct = OldProductType.RawMeat;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip fryClip;

    [Header("Result")]
    public OldProductType resultProduct;
    public bool isSteak;

    [Header("Timers")]
    public float cookTime = 3f;
    public float burnTime = 5f;

    [Header("Placement")]
    public Transform placePoint; 

    [Header("UI")]
    public Image progressImage;          
    public bool hideWhenIdle = true;

    private OldProduct currentProduct;
    private Coroutine cookCoroutine;

    void SetUI(float fill, bool visible)
    {
        if (progressImage == null) return;
        progressImage.fillAmount = Mathf.Clamp01(fill);
        if (hideWhenIdle) progressImage.enabled = visible;
    }

    void ResetUI()
    {
        if (progressImage == null) return;
        progressImage.fillAmount = 0f;
        if (hideWhenIdle) progressImage.enabled = false;
    }

    IEnumerator FillOverTime(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetUI(1f - (t / duration), true); 
            yield return null;
        }
        SetUI(0f, true);
    }

    IEnumerator Cook()
    {
        currentProduct.isCooking = true;
        ResetUI();

        PlayFrySound();

        if (!isSteak)
        {
            yield return StartCoroutine(FillOverTime(cookTime));
            currentProduct.SetType(resultProduct);
            currentProduct.isReady = true;

            yield return StartCoroutine(FillOverTime(burnTime));
            currentProduct.SetType(OldProductType.BurnedMeat);
            currentProduct.isReady = true; 
        }
        else
        {
            // Steak1
            yield return StartCoroutine(FillOverTime(cookTime));
            currentProduct.SetType(OldProductType.Steak1);
            currentProduct.isReady = true;

            // Steak2
            yield return StartCoroutine(FillOverTime(cookTime));
            currentProduct.SetType(OldProductType.Steak2);

            // Steak3
            yield return StartCoroutine(FillOverTime(cookTime));
            currentProduct.SetType(OldProductType.Steak3);

            yield return StartCoroutine(FillOverTime(burnTime));
            currentProduct.SetType(OldProductType.BurnedMeat);
        }

        StopFrySound();
        SetUI(0f, false);
    }

    public void Interact(OldPlayerPickUp player)
    {
        // Положить
        if (currentProduct == null && player.heldProduct != null)
        {
            if (player.heldProduct.productType == inputProduct)
            {
                currentProduct = player.heldProduct;
                player.DropProduct();

                currentProduct.transform.position = placePoint ? placePoint.position : transform.position;

                cookCoroutine = StartCoroutine(Cook());
            }
            return;
        }

        // Забрать (разрешаем забирать когда isReady)
        if (currentProduct != null && currentProduct.isReady && player.heldProduct == null)
        {
            player.TakeProduct(currentProduct);

            if (cookCoroutine != null) StopCoroutine(cookCoroutine);
            cookCoroutine = null;

            StopFrySound();

            currentProduct.isCooking = false;
            currentProduct = null;

            ResetUI();
        }

    }

    void PlayFrySound()
    {
        if (audioSource == null || fryClip == null) return;

        if (!audioSource.isPlaying)
        {
            audioSource.clip = fryClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void StopFrySound()
    {
        if (audioSource == null) return;

        if (audioSource.isPlaying)
            audioSource.Stop();
    }

}
