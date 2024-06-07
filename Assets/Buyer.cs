using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Struct for describing a potential deal.
/// </summary>
public struct Deal
{
    public Seller   Seller;
    public Buyer    Buyer;

    public float    Profit;
    public float    Risk;
}

public class Buyer : MonoBehaviour
{
    public float    MaxPrize = 2.0f,
                    ExpectedPrize = 1.5f;
    public Deal?    CurrentDeal = null;

    // Start is called before the first frame update
    void Start()
    {
        initPrizes();
    }

    private void FixedUpdate()
    {
        // If the buyer does not have any deals, find one
        if (CurrentDeal == null)
        {
            List<Deal>  potentialDeals = getDeals().OrderBy(x => -x.Profit).ToList();
            Deal? deal = pickDeal(potentialDeals);
            if (deal != null) MakeDeal((Deal)deal);
        }
    }

    /// <summary>
    /// Initializes the max- and preferred prize of the buyer.
    /// For now, is random.
    /// </summary>
    void initPrizes()
    {
        MaxPrize *= UnityEngine.Random.Range(1.0f, 2.0f);
        ExpectedPrize = UnityEngine.Random.Range(MaxPrize * 0.5f, MaxPrize);
    }

    /// <summary>
    /// Gets all sellers.
    /// </summary>
    /// <returns>A list of all sellers.</returns>
    Seller[] getSellers()
    {
        // TODO: Make a static list of sellers instead of using unity built-in to fetch every time
        return FindObjectsOfType<Seller>();
    }

    /// <summary>
    /// Gets a list of potential deals the buyer may make.
    /// For now, naive approach: Each seller has a deal with profit inversely proportional to seller expectations.
    /// </summary>
    /// <returns></returns>
    List<Deal> getDeals()
    {
        // Get all sellers with available contracts
        var sellers = getSellers();

        // Get potential deals
        List<Deal> deals = new List<Deal>();
        foreach (var seller in sellers)
        {
            // Check if it is possible to make a deal at all
            if ( seller.CurrentDeal != null ||
                 seller.MinPrize > ExpectedPrize
            ) continue;

            // Calculate the potential deal
            Deal deal = new Deal();
            deal.Seller = seller;
            deal.Buyer  = this;
            deal.Profit = ExpectedPrize - seller.ExpectedPrize;
            deal.Risk   = 1.0f;

            // (Deals which aren't profitable for the seller are not concidered)
            if (deal.Profit > 0.0f) deals.Add(deal);
        }

        // Return
        return deals;
    }

    /// <summary>
    /// Picks a deal out of a list of potential deals.
    /// For now, naive approach: Pick the first available deal
    /// </summary>
    /// <param name="deals"></param>
    /// <returns></returns>
    Deal? pickDeal(List<Deal> deals)
    {
        if (!deals.Any()) return null;
        return deals.Last();
    }

    /// <summary>
    /// Makes a deal.
    /// </summary>
    /// <param name="deal"></param>
    /// <param name="referred"></param>
    public void MakeDeal(Deal deal, bool referred = false)
    {
        if (!referred) deal.Seller.MakeDeal(deal, true);

        CurrentDeal = deal;
        ExpectedPrize = ExpectedPrize / UnityEngine.Random.Range(1.0f, 1.25f);
        
        StartCoroutine( doDeal(deal) );
    }

    /// <summary>
    /// Does a deal.
    /// </summary>
    /// <returns></returns>
    IEnumerator doDeal(Deal deal)
    {
        Debug.Log($"Buyer: Deal started with {deal.Seller.name}");
        // Complete the deal after a small delay
        yield return new WaitForSeconds(2.0f);
        CompleteDeal(deal);
    }

    /// <summary>
    /// Cancels a given deal.
    /// </summary>
    /// <param name="deal"></param>
    public void CancelDeal(Deal deal, bool referred = false)
    {
        Debug.Log($"Buyer: Deal canceled with {deal.Seller.name}");
        if (CurrentDeal != null) CurrentDeal = null;
        if (!referred) deal.Seller.CancelDeal(deal, true);
    }

    /// <summary>
    /// Completes a given deal.
    /// </summary>
    /// <param name="deal"></param>
    public void CompleteDeal(Deal deal, bool referred = false)
    {
        Debug.Log($"Buyer: Deal completed with {deal.Seller.name}");
        if (CurrentDeal != null) CurrentDeal = null;
        if (!referred) deal.Seller.CompleteDeal(deal, true);
    }
}
