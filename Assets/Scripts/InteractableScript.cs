using UnityEngine;
using System.Collections.Generic;

public class InteractableObject : MonoBehaviour
{
    public enum InteractableType
    {
        Fountain,
        Sign,
        NPC
    }
    public enum SignType
    {
        None,
        FountainSign,
        BlacksmithSign,
        BountyBoardSign
    }
    public SignType signType = SignType.None;
    public InteractableType objectType;

    [TextArea(2, 5)] // For nicer editing in Inspector
    public string additionalMessage = "";
    public bool overrideMessage = false; // Override default message

    public string GetSignMessage()
    {
        string baseMessage = "";

        switch (signType)
        {
            case SignType.FountainSign:
                int fountainCost = GetFountainCost();
                baseMessage = $"Respawns all monsters. ({fountainCost} GP)";
                break;

            case SignType.BlacksmithSign:
                int atkCost = GetShopCost(ShopManager.ShopType.ATK);
                int defCost = GetShopCost(ShopManager.ShopType.DEF);
                baseMessage = $"+1 ATK: {atkCost} GP.\n+5 DEF: {defCost} GP.";
                break;

            case SignType.BountyBoardSign:
                baseMessage = GetBountyBoardMessage();
                break;

            default:
                baseMessage = "Nothing interesting here.";
                break;
        }

        if (overrideMessage && !string.IsNullOrWhiteSpace(additionalMessage))
            return additionalMessage;

        if (!string.IsNullOrWhiteSpace(additionalMessage))
            return baseMessage + "\n" + additionalMessage;

        return baseMessage;
    }
    private int GetFountainCost()
    {
        GameManager gm = GameManager.Instance;
        List<int> costs = gm.fountainCosts;

        // Use reflection to access private fountainUseCount
        var field = typeof(GameManager).GetField("fountainUseCount",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        int useCount = (int)field.GetValue(gm);

        return (useCount < costs.Count) ? costs[useCount] : costs[costs.Count - 1];
    }

    private int GetShopCost(ShopManager.ShopType type)
    {
        return ShopManager.Instance.GetCurrentCost(type);
    }
    
    private string GetBountyBoardMessage()
    {
        return
            "Bounty Board\n" +
            "-----------------------------\n" +
            "Name     ATK  DEF  Bounty\n" +
            "Rat       1    1    1 GP\n" +
            "Spider    2    2    3 GP\n" +
            "Rat2      2    5    6 GP\n" +
            "Crab      1    5    5 GP\n" +
            "Ghost     4    2    8 GP\n" +
            "Cyclops  10   20   50 GP\n" ;
    }
}
