using System.Collections.Generic;

public interface IUpgradeTree
{
    void Insert(UpgradeNode node);
    UpgradeNode Search(int id);
    bool Unlock(int id);
    string GetMissingRequirement(int id);
    List<UpgradeNode> InOrderTraversal();
}
