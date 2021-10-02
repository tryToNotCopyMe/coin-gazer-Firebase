using UnityEngine;
using UnityEngine.UI;

public class ResourceController : MonoBehaviour
{
    public Button ResourceBtn;
    public Image ResourceImg;
    public Text ResourceDesc;
    public Text ResourceUpCost;
    public Text ResourceUnCost;

    private ResourceConfig _config;

    private int _index;
    private int _level
    {
        set
        {
            UserDataManager.Progress.ResourceLevels[_index] = value;
            UserDataManager.Save();
        }

        get
        {
            if (!UserDataManager.HasResources(_index))
            {
                return 1;
            }
            return UserDataManager.Progress.ResourceLevels[_index];
        }
    }
    public bool IsUnlocked { get; private set; }
    
    public double GetOutput(){
        return _config.Output * _level;
    }

    public double GetUpgradeCost(){
        return _config.UpgradeCost * _level;
    }

    public double GetUnlockCost(){
        return _config.UnlockCost;
    }

    public void SetConfig(int index, ResourceConfig config){
        _index = index;
        _config = config;
        ResourceDesc.text = $"{_config.Name} Lv.{_level}\n+{GetOutput().ToString("0")}";
        ResourceUnCost.text = $"Unlock Cost\n{_config.UnlockCost}";
        ResourceUpCost.text = $"Upgrade Cost\n{GetUpgradeCost()}";

        SetUnlocked(_config.UnlockCost == 0 || UserDataManager.HasResources(_index));
    }
    
    public void UpgradeLevel()
    {        
        double upgradeCost = GetUpgradeCost();
        if (UserDataManager.Progress.Gold < upgradeCost)
        {
            return;
        }
        GameController.Instance.AddGold(-upgradeCost);
        _level++;

        ResourceUpCost.text = $"Upgrade Cost\n{GetUpgradeCost()}";
        ResourceDesc.text = $"{_config.Name} Lv.{_level}\n+{GetOutput().ToString("0")}";

        AnalyticsManager.LogUpgradeEvent(_index, _level);
    }

    public void UnlockResource()
    {
        double unlockCost = GetUnlockCost();
        if (UserDataManager.Progress.Gold < unlockCost) return;
        SetUnlocked(true);
        GameController.Instance.ShowNextResource();
        AchievementController.Instance.UnlockAchievement(AchievementType.UnlockResource, _config.Name);

        AnalyticsManager.LogUnlockEvent(_index);
    }

    public void SetUnlocked(bool unlocked)
    {
        IsUnlocked = unlocked;

        if (unlocked)
        {
            if (!UserDataManager.HasResources(_index))
            {
                UserDataManager.Progress.ResourceLevels.Add(_level);
                UserDataManager.Save();
            }
        }
        ResourceImg.color = IsUnlocked ? Color.white : Color.grey;
        ResourceUnCost.gameObject.SetActive(!unlocked);
        ResourceUpCost.gameObject.SetActive(unlocked);

    }

    void Start(){
        ResourceBtn.onClick.AddListener(()=> {            
            if (IsUnlocked) { UpgradeLevel(); }
            else { UnlockResource(); }
        });
    }
}
