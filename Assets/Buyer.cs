using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Buyer : MonoBehaviour
{
    public float    MaxPrize = 2.0f,
                    ExpectedPrizeModifier = 1.0f;

    // TODO: Move "trader"-relevant stuff like this into its own class
    // Buyers and Sellers should NOT be their own entities, but just parts of a transaction.
    public Deal?    CurrentDeal = null;
    public bool     DoingDeal = false;

    private bool    unableToDeal = false;
    public float    MinDealTimeout = 1.0f;
    public float    MaxDealTimeout = 3.0f;
    public float    InteractRange = 0.1f;
    public float    TravelSpeed = 5.0f;

    public  List<Deal>    deals    =  new();
    public  List<Option>  options  =  new();
    public  List<Offer>   offers   =  new();

    public Dictionary<Auction, List<OutbidDetails>> auctionOutbidDetails = new();

    // Start is called before the first frame update
    void Start()
    {
        initPrizes();
    }

    private void FixedUpdate()
    {
        if ( CurrentDeal == null && !unableToDeal )
        {
            // If there are valid deals, look through for an actionable one and pick it
            for ( int i = 0; i < deals.Count; i++ ) 
            {
                if (deals[i].state == DealState.Active) { CurrentDeal = deals[i]; break; }
            }

            // If we still do not have an active deal, look for one
            if ( CurrentDeal == null )
            {
                if ( options.Count > 0 )
                {
                    // ...either by picking a deal from our owned options
                    // Select most profitable option from current location
                    Option bestOption = options[ 0 ];
                    float  bestOptionProfitability = bestOption.GetValueAssessment( this );
                    for ( int i = 1; i < options.Count; i++ )
                    {
                        Option option = options[ i ];
                        // (also check if the option is expired. If so, delete it from the list of options)
                        if ( option.IsExchangable() )
                        {
                            // options.RemoveAt( i-- ); // Ehh should work but doesn't. whatever
                            continue;
                        }

                        float profitability = option.GetValueAssessment( this );
                        if ( profitability > bestOptionProfitability )
                        {
                            bestOption = option;
                            bestOptionProfitability = profitability;
                        }
                    }

                    // Purchase the most profitable Option so we have a Deal
                    bestOption.TryExchange( this );
                }
                else
                {
                    // ...or, by bidding for new options
                    List<Auction> activeAuctions = TransactionManager.ActiveAuctions;
                    for ( int i = 0; i < activeAuctions.Count; i++ )
                    {
                        Auction auction = activeAuctions[i];
                        float bid = auction.option.GetValueAssessment( this ) * ExpectedPrizeModifier;
                        if ( bid > 0 ) TransactionManager.PlaceBid( this, bid, auction );
                    }
                }
            }

            StartCoroutine( dealTimeout() ); // To avoid spammy behavior
        }
        else
        {
            if ( !DoingDeal && CurrentDeal != null )
            {
                Deal deal = (Deal)CurrentDeal;
                Debug.Log("SET DEAL");
                if ( deal.state != DealState.Active ) { CurrentDeal = null; }
                else StartCoroutine( doDeal( deal ) );
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
        // StartCoroutine( doTravel( offer.option.deal.Seller ) );
    }

    IEnumerator doDeal( Deal deal )
    {
        DoingDeal = true;

        Debug.Log("TRAVELING");
        // Travel
        var travelRoutine = StartCoroutine( doTravel( deal.seller ) );
        yield return travelRoutine;

        // Complete the Deal
        if ( !deal.TryCloseDeal() ) { Debug.LogWarning("Buyer tried to close deal but closing deal failed!"); yield break; }
Debug.Log("CLOSING");
        DoingDeal = false;

        // TODO: Add deal complete for Buyer
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

    public bool SubtractOption( Option option )
    {
        if ( options.Contains( option ) )
        {
            options.Remove( option );
            return true;
        }
        // TODO: Make subtractX functions work with database-transaction type style
        return false;
    }

    public void ReceiveOption( Option option )
    {
        options.Add( option );
    }

    public void ReceiveDeal( Deal deal )
    {
        deals.Add( deal );
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
