using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    private static GameController _instance = null;
    public static GameController Instance
    {
        get { 
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameController>();
            }
            return _instance; 
        }
    }

    [Range(0f, 1f)]
    public float AutoCollectPercentage = 0.1f;
    public ShopItem[] ShopItems;
    public ResourceConfig[] ResConfigs;
    public Sprite[] ResSprites;

    public Transform ResourcesParent;
    public Transform CoinIcon;
    public Transform ShopPanel;

    public ResourceController ResourcePrefab;
    public TapText TapTextPrefab;
    public ShopItemController ShopItemPrefab;
    
    public Text GoldInfo;
    public Text AutoCollectInfo;

    private List<ResourceController> _activeResources = new List<ResourceController>();
    private List<TapText> _tapTextPool = new List<TapText>();

    private float _collectSecond;    

    private TapText GetOrCreateTapText()
    {
        TapText tapText = _tapTextPool.Find(t => !t.gameObject.activeSelf);
        if (!tapText)
        {
            tapText = Instantiate(TapTextPrefab).GetComponent<TapText>();
            _tapTextPool.Add(tapText);
        }

        return tapText;
    }

    void Start()
    {
        AddAllResources();
        AddShopItems();

        GoldInfo.text = $"Gold: { UserDataManager.Progress.Gold.ToString("0")}";
    }
        
    void Update()
    {
        _collectSecond += Time.unscaledDeltaTime;
        if (_collectSecond > 1f){
            CollectPerSecond();
            _collectSecond = 0f;
        }

        CheckResourceCost();

        CoinIcon.transform.localScale = Vector3.LerpUnclamped(CoinIcon.transform.localScale, Vector3.one * 1.5f, 0.15f);
        CoinIcon.transform.Rotate(0f, Time.deltaTime * -100f, 0f);
    }

    void AddShopItems()
    {
        foreach (ShopItem item in ShopItems)
        {
            GameObject obj = Instantiate(ShopItemPrefab.gameObject, ShopPanel, false);
            ShopItemController shopItem = obj.GetComponent<ShopItemController>();
            shopItem.SetItem(item);            
        }
    }

    void AddAllResources()
    {
        bool showResources = true;
        int index = 0;
        foreach(ResourceConfig config in ResConfigs)
        {
            GameObject obj = Instantiate(ResourcePrefab.gameObject, ResourcesParent, false);
            ResourceController resource = obj.GetComponent<ResourceController>();

            resource.SetConfig(index, config);
            obj.gameObject.SetActive(showResources);

            if (showResources && !resource.IsUnlocked) showResources = false;

            _activeResources.Add(resource);
            index++;
        }
    }    

    void CollectPerSecond()
    {
        double output = 0;
        foreach(ResourceController resource in _activeResources)
        {
            if (resource.IsUnlocked) output += resource.GetOutput();
        }
        output *= AutoCollectPercentage;
        AutoCollectInfo.text = $"Auto Collect: {output.ToString("F1")} / second";
        AddGold(output);
    }

    void CheckResourceCost()
    {
        foreach(ResourceController resource in _activeResources)
        {
            bool isBuyable = false;
            if (resource.IsUnlocked) { isBuyable = UserDataManager.Progress.Gold >= resource.GetUpgradeCost(); } 
            else { isBuyable = UserDataManager.Progress.Gold >= resource.GetUnlockCost();}

            resource.ResourceImg.sprite = ResSprites[isBuyable ? 1 : 0];
        }
    }

    public void ShowNextResource()
    {
        foreach (ResourceController resource in _activeResources)
        {
            if (!resource.gameObject.activeSelf)
            {
                resource.gameObject.SetActive(true);
                break;
            }
        }
    }

    public void AddGold(double value)
    {
        UserDataManager.Progress.Gold += value;
        GoldInfo.text = $"Gold: {UserDataManager.Progress.Gold.ToString("0")}";
        foreach(AchievementData achievement in AchievementController.Instance.GetAchievements())
        {
            if (achievement.Type == AchievementType.GoldReach && UserDataManager.Progress.Gold >= System.Convert.ToDouble(achievement.Value))
            {
                AchievementController.Instance.UnlockAchievement(AchievementType.GoldReach, achievement.Value);
            }            
        }

        UserDataManager.Save();
    }

    public void CollectByTap(Vector3 tapPos, Transform parent)
    {
        double output = 0;
        foreach(ResourceController resource in _activeResources)
        {
            if (resource.IsUnlocked) output += resource.GetOutput();
        }

        TapText tapText = GetOrCreateTapText();
        tapText.transform.SetParent(parent, false);
        tapText.transform.position = tapPos;

        tapText.Text.text = $"+{output.ToString("0")}";
        tapText.gameObject.SetActive(true);
        CoinIcon.transform.localScale = Vector3.one * 1.75f;

        AddGold(output);
    }
}
[System.Serializable]
public struct ResourceConfig
{
    public string Name;
    public double UnlockCost;
    public double UpgradeCost;
    public double Output;
}
[System.Serializable]
public struct ShopItem
{
    public string Name;
    public double Cost;
    public Sprite sprite;
    public bool Available;
}