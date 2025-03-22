using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Seller : MonoBehaviour
{
    public float    MinPrize = 1.0f,
                    ExpectedPrize = 1.5f;
    public Deal     CurrentDeal = null;

    private float   lastDealTime = -1.0f;
    private bool    alive = true;
    public  float   ImpatienceThreshold = 2.0f; // Time required since no deal til prices adjust
    public  float   HeartbeatFrequency = 0.5f;  // heartbeats per second

    public List<Deal> deals = new List<Deal>();

    // Start is called before the first frame update
    void Start()
    {
        initPrizes();
        // resetDealTimer();
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
    
    public void FixedUpdate()
    {
        lastDealTime -= Time.deltaTime;
    }

    private void onHeartbeat()
    {
        // If no Deals, make one out of thin air
        if ( deals.Count <= 0 && lastDealTime <= 0.0f)
        {
            lastDealTime = 1.0f;
            Deal deal = new Deal();
            deal.state = DealState.Unassigned;
            deal.seller = this;

            deals.Add( deal );
        }

        // If any deals not auctioned, auction them
        for (int i = 0; i < deals.Count; i++)
        {
            if (TransactionManager.FindActiveAuction(deals[i]) >= 0) continue;

            // Make option
            Option option = new Option();
            option.strike = UnityEngine.Random.Range(0.5f, 1.0f);
            option.deal = deals[i];
            option.duration = 10.0f;

            // Put it up for auction
            TransactionManager.CreateAuction( this, option, 3.0f );
        }

        // Update color
        visualizeExpectedPrize();
    }

    public bool SubtractDeal( Deal deal )
    {
        if (!deals.Contains(deal)) return false;
        deals.Remove( deal );
        return true;
    }

    public void ReceiveMoney( float amount )
    {
        // TODO: Add money.
    }
}
