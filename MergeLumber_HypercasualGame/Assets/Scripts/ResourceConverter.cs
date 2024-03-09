using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class ResourceConverter : MonoBehaviour
{
    [Header("Converter")]
    public bool free = false;
    public bool hidden = true;
    public bool unlocked = false;

    public int productivityUpgradeMultiplier = 5;
    public int clicksToUpgrade = 50;

    public ResourceManager.Resources resourceProduce;
    public int initialProduceAmount;
    private long currentProduceAmount;
    
    public ResourceManager.Resources resourceConsume;
    public int initialConsumeAmount;
    private long currentConsumeAmount;

    public Sprite resourceSprite;

    public int level = 1;
    private long currentUpgradePrice = 50;

    [Header("UI")] 
    public TextMeshProUGUI resourceText;
    public TextMeshProUGUI productionSpeedText;
    public TextMeshProUGUI upgradePriceText;

    [Header("Interaction")]
    public KeyCode interactKey;

    [Header("Feedbacks")] 
    public bool feedback;
    public List<AudioClip> soundFX;
    public ParticleSystem particles;
    public GameObject textFeedback;
    public bool shake;

    [Header("Art")]
    public SpriteRenderer conveyor1;
    public SpriteRenderer conveyor2;
    
    private SpriteRenderer spriteRenderer;
    private bool isNear;
    private Vector3 originalPos;

    void Start()
    {
        originalPos = transform.localPosition;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    void Update()
    {
        transform.GetChild(0).gameObject.SetActive(isNear);
        currentProduceAmount = initialProduceAmount * Convert.ToInt64(Mathf.Pow(productivityUpgradeMultiplier, level - 1));
        currentConsumeAmount = currentProduceAmount * 100;
        currentUpgradePrice = currentProduceAmount * clicksToUpgrade;

        
        //UI
        if (hidden)
        {
            spriteRenderer.color = Color.clear;
            if (conveyor1) conveyor1.color = Color.clear;
            if (conveyor2) conveyor2.color = Color.clear;
            productionSpeedText.enabled = false;
            upgradePriceText.gameObject.SetActive(false);
            resourceText.enabled = false;
            if (ResourceManager.instance.resourceDict[resourceConsume] > initialConsumeAmount) hidden = false;
        }

        if (!hidden && !unlocked)
        {
            spriteRenderer.color = new Color(0, 0, 0, 0.25f);
            if (conveyor1) conveyor1.color = new Color(0, 0, 0, 0.25f);
            if (conveyor2) conveyor2.color = new Color(0, 0, 0, 0.25f);
            transform.GetChild(0).GetComponent<TextMeshPro>().text = $"100 {resourceConsume} to unlock\n[SPACE]";
            if (isNear && Input.GetKeyDown(interactKey))
            {
                if (ResourceManager.instance.resourceDict[resourceConsume] < currentConsumeAmount)
                {
                    UIManager.instance.PlayNotEnoughAnimation(resourceConsume);
                    print("Not Enough Resource");
                    return;
                }
                
                ResourceManager.instance.resourceDict[resourceConsume] -= currentConsumeAmount;
                unlocked = true;
            };
        }
        
        if (unlocked)
        {
            if (isNear && Input.GetKeyDown(interactKey)) ConvertResource();
            spriteRenderer.color = Color.white;
            if (conveyor1) conveyor1.color = Color.white;
            if (conveyor2) conveyor2.color = Color.white;
            productionSpeedText.enabled = true;
            upgradePriceText.gameObject.SetActive(true);
            resourceText.enabled = true;
            productionSpeedText.text = $"{resourceProduce}: {currentProduceAmount}";
            upgradePriceText.text = $"{currentUpgradePrice} {resourceProduce}";
            transform.GetChild(0).GetComponent<TextMeshPro>().text = $"Level {level}\n[SPACE]";
        }
    }

    void ConvertResource()
    {
        if (free) ResourceManager.instance.resourceDict[resourceProduce] += currentProduceAmount;
        else
        {
            if (ResourceManager.instance.resourceDict[resourceConsume] < currentConsumeAmount)
            {
                UIManager.instance.PlayNotEnoughAnimation(resourceConsume);
                print("Not Enough Resource");
                return;
            }
        
            ResourceManager.instance.resourceDict[resourceConsume] -= currentConsumeAmount;
            ResourceManager.instance.resourceDict[resourceProduce] += currentProduceAmount;
        }
        
        PlayFeedbackAnimation();
        
        if (feedback)
        {
            AudioManager.instance.PlaySound(soundFX[Random.Range(0, soundFX.Count)]);
            if (particles != null)
            {
                switch (level)
                {
                    case 1:
                        particles.emission.SetBurst(0, new ParticleSystem.Burst(0.0f, 10));
                        break;
                    case 2:
                        particles.emission.SetBurst(0, new ParticleSystem.Burst(0.0f, 20));
                        break;
                    case 3:
                        particles.emission.SetBurst(0, new ParticleSystem.Burst(0.0f, 30));
                        break;
                    case 4:
                        particles.emission.SetBurst(0, new ParticleSystem.Burst(0.0f, 40));
                        break;
                }
                Instantiate(particles, transform.position, Quaternion.identity);
            }
            if (shake)
                transform.DOShakePosition(0.1f, Vector3.one * 0.07f, 300).OnComplete((() =>
                {
                    transform.localPosition = originalPos;
                }));
        }
    }

    public void Upgrade()
    {
        if (ResourceManager.instance.resourceDict[resourceProduce] < currentUpgradePrice)
        {
            UIManager.instance.PlayNotEnoughAnimation(resourceProduce);
            print("Not Enough Resource");
            return;
        }
        
        ResourceManager.instance.resourceDict[resourceProduce] -= currentUpgradePrice;
        level++;
    }

    
    private void OnTriggerEnter2D(Collider2D col)
    {
        if(hidden) return;
        isNear = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if(hidden) return;
        isNear = false;
    }
    
    private void PlayFeedbackAnimation()
    {
        GameObject feedback = Instantiate(textFeedback, transform.position + new Vector3(Random.Range(-0.1f,0.1f),0,0), Quaternion.identity);
        TextMeshPro feedbackText = feedback.GetComponentInChildren<TextMeshPro>();
        feedbackText.color = resourceText.color;
        Sequence feedbackSequence = DOTween.Sequence();
        
        feedbackText.text = $"+ {currentProduceAmount}";
        
        feedbackSequence
            .Append(feedbackText.transform.DOScale(Vector3.zero, 0))
            .Append(feedbackText.transform.DOScale(Vector3.one, 0.5f))
            .Insert(0, feedbackText.transform.DOMoveY(transform.position.y + 1, 2f))
            .Insert(1, feedbackText.DOFade(0, 1f))
            .OnComplete((() => { Destroy(feedback); }));
        feedbackSequence.Play();
    }
}