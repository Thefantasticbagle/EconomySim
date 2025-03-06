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

    private float   lastDealTime = -1.0f;
    private bool    alive = true;
    public  float   ImpatienceThreshold = 2.0f; // Time required since no deal til prices adjust
    public  float   HeartbeatFrequency = 0.5f;  // heartbeats per second

    // Start is called before the first frame update
    void Start()
    {
        initPrizes();
        resetDealTimer();
        startHeartbeat();
    }

    private void visualizeExpectedPrize()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null) return;

        float R = 1.0f / 2.0f * ExpectedPrize,
              G = 0.0f,
              B = 1.0f * (2.0f - ExpectedPrize);
        renderer.color = new Color(R, G, B);
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

    private void startHeartbeat()
    {
        StartCoroutine(doHeartbeat(HeartbeatFrequency));
    }

    private IEnumerator doHeartbeat(float frequency)
    {
        while(alive)
        {
            yield return new WaitForSeconds(1.0f / frequency);
            onHeartbeat();
        }
    }

    private void onHeartbeat()
    {
        // Adjust prices based on time since last deal
        float timeSinceLastDeal = MeasureDealTimer();
        if (timeSinceLastDeal > ImpatienceThreshold)
        {
            ExpectedPrize = Mathf.Max( MinPrize, ExpectedPrize / UnityEngine.Random.Range(1.0f, 1.25f) );
            resetDealTimer();
        }

        // Update color
        visualizeExpectedPrize();
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

        resetDealTimer();
        StartCoroutine(doDeal(getDeal, setDeal));
    }


    private float resetDealTimer()
    {
        // Measure time since previous deal was made
        float curTime = Time.time;
        float timeSinceLastDeal = curTime - lastDealTime;
        if (lastDealTime <= 0.0f) timeSinceLastDeal = 0.0f;
        lastDealTime = curTime;

        return timeSinceLastDeal;
    }

    public float MeasureDealTimer()
    {
        // Measure time since previous deal was made
        return Time.time - lastDealTime;
    }

    // public IEnumerator doTravel()
    // {
    //     Deal _deal = getDeal();
    //     //Debug.Log($"Seller: Traveling to {}")
    // }

    /// <summary>
    /// Does a deal.
    /// </summary>
    /// <param name="getDeal"></param>
    /// <param name="setDeal"></param>
    /// <returns></returns>
    IEnumerator doDeal(Func<Deal> getDeal, Action<Deal> setDeal)
    {
        //Debug.Log($"Seller: Deal started with {getDeal().Buyer.name}");

        // Timeout
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
        //Debug.Log($"Seller: Deal cancelled with {_deal.Buyer.name}");

        if (CurrentDeal != null) CurrentDeal = null;
        if (_deal.Seller == null || _deal.Buyer == null) return;
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
        //Debug.Log($"Seller: Deal completed with {_deal.Buyer.name}");

        if (CurrentDeal != null) CurrentDeal = null;
        if (_deal.Seller == null || _deal.Buyer == null) return;
        if (!referred) _deal.Buyer.CompleteDeal(getDeal, setDeal, true);
        else
        {
            _deal = getDeal();
            _deal.Active = false;
            setDeal(_deal);
        }
    }
}
