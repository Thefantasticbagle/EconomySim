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

    public bool     Active;
}

public class Buyer : MonoBehaviour
{
    public float    MaxPrize = 2.0f,
                    ExpectedPrize = 1.5f;
    public Deal?    CurrentDeal = null;

    private bool    unableToDeal = false;

    // Start is called before the first frame update
    void Start()
    {
        initPrizes();
    }

    private void FixedUpdate()
    {
        // If the buyer does not have any deals, find one
        if (CurrentDeal == null && !unableToDeal)
        {
            List<Deal>  potentialDeals = getDeals().OrderBy(x => -x.Profit).ToList();
            Deal? _deal = pickDeal(potentialDeals); // do not reference 
            if (_deal != null)
            {
                Deal deal = (Deal)_deal;
                MakeDeal( () => deal, (x) => deal = x );
            }
            else
            {
                ExpectedPrize = Mathf.Min(MaxPrize, ExpectedPrize * UnityEngine.Random.Range(1.0f, 1.25f));
                StartCoroutine(dealTimeout());
            }
        }
    }

    /// <summary>
    /// Sets the buyer on a timeout before they begin looking for deals again.
    /// </summary>
    /// <returns></returns>
    IEnumerator dealTimeout()
    {
        unableToDeal = true;
        yield return new WaitForSeconds(UnityEngine.Random.Range(1.0f, 3.0f));
        unableToDeal = false;
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
            deal.Active = false;

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
    /// <param name="getDeal"></param>
    /// <param name="setDeal"></param>
    /// <param name="referred"></param>
    public void MakeDeal(Func<Deal> getDeal, Action<Deal> setDeal, bool referred = false)
    {
        Deal _deal = getDeal();

        if (!referred) _deal.Seller.MakeDeal(getDeal, setDeal, true);
        else
        {
            _deal = getDeal();
            _deal.Active = true;
            setDeal(_deal);
        } 

        CurrentDeal = _deal;
        ExpectedPrize = ExpectedPrize / UnityEngine.Random.Range(1.0f, 1.25f);
        
        StartCoroutine( doDeal(getDeal, setDeal) );
    }


    /// <summary>
    /// Does a deal.
    /// </summary>
    /// <param name="getDeal"></param>
    /// <param name="setDeal"></param>
    /// <returns></returns>
    IEnumerator doDeal(Func<Deal> getDeal, Action<Deal> setDeal)
    {
        Debug.Log($"Buyer: Deal started with {getDeal().Seller.name}");

        // Complete the deal after a small delay
        yield return new WaitForSeconds(2.0f);

        if (getDeal().Active) CompleteDeal(getDeal, setDeal);
    }

    /// <summary>
    /// Cancels a given deal.
    /// </summary>
    /// <param name="getDeal"></param>
    /// <param name="setDeal"></param>
    /// <param name="referred"></param>
    public void CancelDeal(Func<Deal> getDeal, Action<Deal> setDeal, bool referred = false)
    {
        Deal _deal = getDeal();
        Debug.Log($"Buyer: Deal cancelled with {_deal.Seller.name}");

        if (CurrentDeal != null) CurrentDeal = null;
        if (!referred) _deal.Seller.CancelDeal(getDeal, setDeal, true);
        else
        {
            _deal = getDeal();
            _deal.Active = false;
            setDeal(_deal);
        }
    }

    /// <summary>
    /// Completes a given deal.
    /// </summary>
    /// <param name="getDeal"></param>
    /// <param name="setDeal"></param>
    /// <param name="referred"></param>
    public void CompleteDeal(Func<Deal> getDeal, Action<Deal> setDeal, bool referred = false)
    {
        Deal _deal = getDeal();
        Debug.Log($"Buyer: Deal completed with {_deal.Seller.name}");

        if (CurrentDeal != null) CurrentDeal = null;
        if (!referred) _deal.Seller.CompleteDeal(getDeal, setDeal, true);
        else
        {
            _deal = getDeal();
            _deal.Active = false;
            setDeal(_deal);
        }

        unableToDeal = true;
        StartCoroutine(dealTimeout());
    }
}
