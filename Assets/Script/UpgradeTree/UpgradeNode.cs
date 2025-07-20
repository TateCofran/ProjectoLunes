public class UpgradeNode
{
    public int Id;
    public string Name;
    public string Description;
    public int Cost;
    public bool Unlocked;

    public UpgradeNode Left;
    public UpgradeNode Right;

    public UpgradeNode(int id, string name, string description, int cost)
    {
        Id = id;
        Name = name;
        Description = description;
        Cost = cost;
        Unlocked = false;
        Left = null;
        Right = null;
    }
}
