using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buyer : MonoBehaviour
{
    public float    MaxPrize = 2.0f,
                    ExpectedPrize = 1.5f;

    // Start is called before the first frame update
    void Start()
    {
        initPrizes();
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
    /// Tries to make a deal with a given seller. True on success, false otherwise.
    /// </summary>
    /// <param name="seller"></param>
    /// <returns></returns>
    public bool TryMakeDeal(Seller seller)
    {
        // If a deal was made, raise expectations
        if (ExpectedPrize >= seller.ExpectedPrize)
        {
            ExpectedPrize = ExpectedPrize / UnityEngine.Random.Range(1.0f, 1.25f);
            seller.ExpectedPrize = seller.ExpectedPrize * UnityEngine.Random.Range(1.0f, 1.25f);
            return true;
        }

        // Otherwise, lower them
        ExpectedPrize = Math.Min(ExpectedPrize * UnityEngine.Random.Range(1.0f, 1.25f), MaxPrize);
        seller.ExpectedPrize = Math.Max(seller.ExpectedPrize / UnityEngine.Random.Range(1.0f, 1.25f), seller.MinPrize);
        return false;
    }
}
