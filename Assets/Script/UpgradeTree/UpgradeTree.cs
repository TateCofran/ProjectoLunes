using System;

public class UpgradeTree
{
    public UpgradeNode Root;

    public UpgradeTree()
    {
        Root = new UpgradeNode(
            "Oro al matar enemigos",
            "Ganá 1 oro extra por cada enemigo eliminado.",
            1);

        var damageUpgrade = new UpgradeNode(
            "Más daño de las torretas",
            "Las torretas infligen un 10% más de daño.",
            2);

        var rangeUpgrade = new UpgradeNode(
            "Más alcance de las torretas",
            "Las torretas tienen un 10% más de alcance.",
            2);

        var globalUpgrade = new UpgradeNode(
            "Aumenta todas las estadísticas",
            "Todas las estadísticas aumentan un 5%.",
            5);

        Root.Left = damageUpgrade;
        Root.Right = rangeUpgrade;

        damageUpgrade.Right = globalUpgrade;
        rangeUpgrade.Left = globalUpgrade;
    }

    public bool Unlock(UpgradeNode node)
    {
        if (node == null) return false;

        if (node == Root)
        {
            node.Unlocked = true;
            return true;
        }
        else if (node == Root.Left || node == Root.Right)
        {
            if (Root.Unlocked)
            {
                node.Unlocked = true;
                return true;
            }
        }
        else
        {
            if (Root.Left.Unlocked && Root.Right.Unlocked)
            {
                node.Unlocked = true;
                return true;
            }
        }
        return false;
    }

    public string GetMissingRequirement(UpgradeNode node)
    {
        if (node == Root) return "";

        if (node == Root.Left || node == Root.Right)
            return Root.Unlocked ? "" : "- Desbloqueá 'Oro al matar enemigos'";

        string faltan = "";
        if (!Root.Left.Unlocked) faltan += "- Desbloqueá 'Más daño de las torretas'\n";
        if (!Root.Right.Unlocked) faltan += "- Desbloqueá 'Más alcance de las torretas'";
        return faltan;
    }
}
