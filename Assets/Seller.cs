using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
        MinPrize *= UnityEngine.Random.Range(0.5f, 1.0f);
        ExpectedPrize = UnityEngine.Random.Range(MinPrize, MinPrize * 2.0f);
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

        if (!referred) _deal.Buyer.MakeDeal(getDeal, setDeal, true);
        else
        {
            _deal = getDeal();
            _deal.Active = true;
            setDeal(_deal);
        }

        CurrentDeal = _deal;
        ExpectedPrize = ExpectedPrize * UnityEngine.Random.Range(1.0f, 1.25f);

        StartCoroutine(doDeal(getDeal, setDeal));
    }

    /// <summary>
    /// Does a deal.
    /// </summary>
    /// <param name="getDeal"></param>
    /// <param name="setDeal"></param>
    /// <returns></returns>
    IEnumerator doDeal(Func<Deal> getDeal, Action<Deal> setDeal)
    {
        Debug.Log($"Seller: Deal started with {getDeal().Buyer.name}");

        // Complete the deal after a small delay
        yield return new WaitForSeconds(3.0f);

        if (getDeal().Active) CancelDeal(getDeal, setDeal);
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
        Debug.Log($"Seller: Deal cancelled with {_deal.Buyer.name}");

        if (CurrentDeal != null) CurrentDeal = null;
        if (!referred) _deal.Buyer.CancelDeal(getDeal, setDeal, true);
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
        Debug.Log($"Seller: Deal completed with {_deal.Buyer.name}");

        if (CurrentDeal != null) CurrentDeal = null;
        if (!referred) _deal.Buyer.CompleteDeal(getDeal, setDeal, true);
        else
        {
            _deal = getDeal();
            _deal.Active = false;
            setDeal(_deal);
        }
    }
}
