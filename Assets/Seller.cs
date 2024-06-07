using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Seller : MonoBehaviour
{
    public float    MinPrize = 1.0f,
                    ExpectedPrize = 1.5f;
    public Deal?    CurrentDeal = null;

    // Start is called before the first frame update
    void Start()
    {
        initPrizes();
    }

    /// <summary>
    /// Initializes the min- and preferred prize of the seller.
    /// For now, is random.
    /// </summary>
    void initPrizes()
    {
        MinPrize *= Random.Range(0.5f, 1.0f);
        ExpectedPrize = Random.Range(MinPrize, MinPrize * 2.0f);
    }

    /// <summary>
    /// Makes a deal.
    /// </summary>
    /// <param name="deal"></param>
    /// <param name="referred"></param>
    public void MakeDeal(Deal deal, bool referred = false)
    {
        if (!referred) deal.Buyer.MakeDeal(deal, true);

        CurrentDeal = deal;
        ExpectedPrize = ExpectedPrize * UnityEngine.Random.Range(1.0f, 1.25f);

        StartCoroutine(doDeal(deal));
    }

    /// <summary>
    /// Does a deal.
    /// </summary>
    /// <param name="deal"></param>
    /// <returns></returns>
    IEnumerator doDeal(Deal deal)
    {
        Debug.Log($"Seller: Deal started with {deal.Buyer.name}");

        // After a given amount of time, time out the deal
        yield return new WaitForSeconds(10.0f);
        CancelDeal(deal);
    }

    /// <summary>
    /// Cancels a given deal.
    /// </summary>
    /// <param name="deal"></param>
    public void CancelDeal(Deal deal, bool referred = false)
    {
        Debug.Log($"Seller: Deal calcelled with {deal.Buyer.name}");

        if (CurrentDeal != null) CurrentDeal = null;
        if (!referred) deal.Buyer.CancelDeal(deal, true);
    }

    /// <summary>
    /// Completes a given deal.
    /// </summary>
    /// <param name="deal"></param>
    public void CompleteDeal(Deal deal, bool referred = false)
    {
        Debug.Log($"Seller: Deal completed with {deal.Buyer.name}");

        if (CurrentDeal != null) CurrentDeal = null;
        if (!referred) deal.Buyer.CompleteDeal(deal, true);
    }
}
