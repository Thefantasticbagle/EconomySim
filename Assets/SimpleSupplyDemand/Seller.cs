using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Seller : MonoBehaviour
{
    public float    MinPrize = 1.0f,
                    ExpectedPrize = 1.5f;

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
}
