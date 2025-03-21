using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Buyer : MonoBehaviour
{
    public float    MaxPrize = 2.0f,
                    ExpectedPrizeModifier = 1.0f;
    public Deal?    CurrentDeal = null;

    private bool    unableToDeal = false;
    public float    MinDealTimeout = 1.0f;
    public float    MaxDealTimeout = 3.0f;
    public float    InteractRange = 0.1f;
    public float    TravelSpeed = 5.0f;

    public List<Option> options = new();
    public List<Offer>  offers = new();

    public Dictionary<Auction, List<OutbidDetails>> auctionOutbidDetails = new();

    // Start is called before the first frame update
    void Start()
    {
        initPrizes();
    }

    private void FixedUpdate()
    {
        // If the Buyer doesn't have any active deal, try to buy one
        if ( CurrentDeal == null && !unableToDeal )
        {
            if ( options.Count > 0 )
            {
                // Look among owned options first

            }
            else
            {
                // If no owned options, bid
                List<Auction> activeAuctions = TransactionManager.ActiveAuctions;
                for ( int i = 0; i < activeAuctions.Count; i++ )
                {
                    Auction auction = activeAuctions[i];
                    float bid = auction.option.GetValueAssessment( this ) * ExpectedPrizeModifier;
                    if ( bid > 0 ) TransactionManager.PlaceBid( this, bid, auction );
                }
            }

            StartCoroutine( dealTimeout() );
        }
    }

    /// <summary>
    /// Sets the buyer on a timeout before they begin looking for deals again.
    /// </summary>
    /// <returns></returns>
    IEnumerator dealTimeout()
    {
        unableToDeal = true;
        yield return new WaitForSeconds(UnityEngine.Random.Range(MinDealTimeout, MaxDealTimeout));
        unableToDeal = false;
    }

    /// <summary>
    /// Initializes the max- and preferred prize of the buyer.
    /// For now, is random.
    /// </summary>
    void initPrizes()
    {
        // MaxPrize *= UnityEngine.Random.Range(1.0f, 2.0f);
        // ExpectedPrizeModifier = UnityEngine.Random.Range(MaxPrize * 0.5f, MaxPrize);
        ExpectedPrizeModifier = UnityEngine.Random.Range(0.5f, 0.9f);
    }

    public IEnumerator ReceiveOffer( Offer offer, Action<Offer> accept )
    {
        // Poll offers
        offers.Add( offer );
        yield return new WaitForSeconds( UnityEngine.Random.Range(0.8f, 0.9f) * offer.decisionTimeSeconds );

        // If the Buyer has enough Options, deny
        if ( options.Count > 0 )
        {
            offers.Remove( offer );
            yield break;
        }

        // Right before an offer expires, decide to buy it or not based on whether there's better offers forwards in the queue
        // (This also re-calculates the value of all offers in case they have changed)
        float estimateValue = offer.GetValueAssessment( this );
        if ( estimateValue <= 0 )
        {
            offers.Remove( offer );
            yield break;
        }

        for ( int i = 0; i < offers.Count; i++ )
        {
            Offer nextOffer = offers[i];
            if ( nextOffer.GetValueAssessment(this) > estimateValue ) // (this should avoid the case of nextOffer == offer)
            {
                // If another offer in the queue is better, swap its position in the list and don't take this offer
                // This should hopefully partially sort the list as-we-go...
                int offerIndex = offers.IndexOf( offer );
                if ( i > offerIndex )
                {
                    offers[offerIndex] = nextOffer;
                    offers.RemoveAt(i);
                }
                else offers.RemoveAt( offerIndex );

                yield break;
            }
        }

        // Reaching this place in code means the current offer is up and the best offer known
        // TransactionManager handles the exchange of money and assets.
        accept( offer );
        offers.Remove( offer );
        StartCoroutine( doTravel( offer.option.deal.Seller ) );
    }

    IEnumerator doTravel( Seller seller )
    {
        if ( seller == null ) yield break;

        // Begin traveling towards the seller
        GameObject buyer = gameObject;
        float distance = 1000.0f;
        float dt = Time.deltaTime;
        while (distance > InteractRange + TravelSpeed * dt && seller != null && buyer != null)
        {
            Vector3 to = seller.transform.position - buyer.transform.position,
                    toN = to.normalized;
            distance = to.magnitude;
            // For now, simply move in a straight line at a constant speed 
            // Here we're assuming the seller doesn't move during the travel.
            buyer.transform.position += toN * TravelSpeed * dt;
            dt = Time.deltaTime;
            yield return false;
        }
    }

    public bool SubtractMoney( float amount )
    {
        return true; // TODO: Add money.
    }

    public void ReceiveOption( Option option )
    {
        options.Add( option );
    }

    public void NotifyOutbid( OutbidDetails details )
    {
        Auction auction = details.auction;

        // If we're already considering what our new bid should be for this auction,
        // add the details to the existing entry and return.
        if ( auctionOutbidDetails.ContainsKey( auction ) )
        {
            auctionOutbidDetails[auction].Add( details );
            return;
        }

        auctionOutbidDetails[auction] = new List<OutbidDetails>{ details };
        StartCoroutine( outbidRoutine( details.auction ) );
    }

    private IEnumerator outbidRoutine( Auction auction )
    {
        float expectedSecondsToResolve = auctionOutbidDetails[auction][0].remainingAuctionTime;

        // Measure Auction Velocity
        // ( wait half the remaining time, down to minimum of 1 second or the remaining time depending on which is greater )
        float waitForSeconds = Mathf.Max(expectedSecondsToResolve / 2.0f, Mathf.Min( expectedSecondsToResolve - 0.1f, 1.0f ) );
        yield return new WaitForSeconds( waitForSeconds );
        float velocity = auctionOutbidDetails[auction].Count / waitForSeconds;

        // Determine rest of variables. Since this happens in a single frame we can assume auctionOutbidDetails is constant.
        List<OutbidDetails> allDetails = auctionOutbidDetails[ auction ];
        
        // Measure Auction volatility
        // ( highest increase in bid )
        float volatility = 0.0f;
        float prevBid = allDetails[0].bid;
        for ( int i = 1; i < allDetails.Count; i++ )
        {
            OutbidDetails curDetails = allDetails[i];
            
            float curBidDiff = curDetails.bid - prevBid;
            if ( curBidDiff > volatility ) volatility = curBidDiff;
            
            prevBid = curDetails.bid;
        }

        // Determine the new bid, if any
        float currentHighestBid = allDetails[allDetails.Count - 1].bid;
        float myValueAssessment = auction.option.GetValueAssessment( this ); //* ExpectedPrizeModifier;
        
        // float bidIncrement = volatility * velocity;
        float bidIncrement = Mathf.Min( volatility * velocity, currentHighestBid * 0.05f );
        float newBid = currentHighestBid + bidIncrement;
        
        // Only bid if the new amount is still below our maximum valuation
        if ( newBid <= myValueAssessment )
        {
            TransactionManager.PlaceBid( this, newBid, auction );
        }
        
        // Clean up outbid details as we've processed them
        auctionOutbidDetails.Remove( auction );
    }
}
