public class UpgradeNode
{
    public string Name;
    public string Description;
    public int Cost;
    public bool Unlocked;
    public UpgradeNode Left;
    public UpgradeNode Right;

    public UpgradeNode(string name, string description, int cost)
    {
        Name = name;
        Description = description;
        Cost = cost;
        Unlocked = false;
        Left = null;
        Right = null;
    }
}
