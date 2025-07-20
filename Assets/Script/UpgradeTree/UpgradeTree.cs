using System;
using System.Collections.Generic;

public class UpgradeTree
{
    public UpgradeNode Root;

    public UpgradeTree()
    {
        Root = new UpgradeNode(
            10, 
            "Oro al matar enemigos",
            "Ganá 1 oro extra por cada enemigo eliminado.",
            1);

        var damageUpgrade = new UpgradeNode(
            5, 
            "Más daño de las torretas",
            "Las torretas infligen un 10% más de daño.",
            2);

        var rangeUpgrade = new UpgradeNode(
            15, 
            "Más alcance de las torretas",
            "Las torretas tienen un 10% más de alcance.",
            2);

        var globalUpgrade = new UpgradeNode(
            12, 
            "Aumenta todas las estadísticas",
            "Todas las estadísticas aumentan un 5%.",
            5);

        Insert(damageUpgrade);
        Insert(rangeUpgrade);
        Insert(globalUpgrade);
    }

    public void Insert(UpgradeNode node)
    {
        Root = InsertRec(Root, node);
    }

    private UpgradeNode InsertRec(UpgradeNode current, UpgradeNode node)
    {
        if (current == null) return node;
        if (node.Id < current.Id)
            current.Left = InsertRec(current.Left, node);
        else if (node.Id > current.Id)
            current.Right = InsertRec(current.Right, node);
        
        return current;
    }

    public UpgradeNode Search(int id)
    {
        return SearchRec(Root, id);
    }

    private UpgradeNode SearchRec(UpgradeNode current, int id)
    {
        if (current == null || current.Id == id)
            return current;
        if (id < current.Id)
            return SearchRec(current.Left, id);
        else
            return SearchRec(current.Right, id);
    }

    public UpgradeNode GetParent(int id)
    {
        return GetParentRec(Root, null, id);
    }

    private UpgradeNode GetParentRec(UpgradeNode current, UpgradeNode parent, int id)
    {
        if (current == null)
            return null;
        if (current.Id == id)
            return parent;

        if (id < current.Id)
            return GetParentRec(current.Left, current, id);
        else
            return GetParentRec(current.Right, current, id);
    }

    public bool Unlock(int id)
    {
        var node = Search(id);
        if (node == null) return false;

        if (node == Root)
        {
            node.Unlocked = true;
            return true;
        }

        var parent = GetParent(id);
        if (parent != null && parent.Unlocked)
        {
            node.Unlocked = true;
            return true;
        }

        return false;
    }

    public string GetMissingRequirement(int id)
    {
        var node = Search(id);
        if (node == null) return "- Mejora no encontrada.";
        if (node == Root) return "";

        if (!Root.Unlocked)
            return "- Desbloqueá '" + Root.Name + "' primero";

        
        return "";
    }

    public List<UpgradeNode> InOrderTraversal()
    {
        List<UpgradeNode> upgrades = new List<UpgradeNode>();
        InOrderRec(Root, upgrades);
        return upgrades;
    }

    private void InOrderRec(UpgradeNode node, List<UpgradeNode> list)
    {
        if (node == null) return;
        InOrderRec(node.Left, list);
        list.Add(node);
        InOrderRec(node.Right, list);
    }
}

