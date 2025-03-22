using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// --- Structs and interfaces used by TransactionManager
public interface IAuctionable
{
    float GetValueAssessment( Buyer buyer );
}

/// <summary>
/// Contains details about an outbid event when a bidder is surpassed by a higher bid in an auction.
/// </summary>
public struct OutbidDetails
{
    public Auction  auction; 
    public Buyer    bidder;
    public float    bid;
    public float    diff;
    public float    remainingAuctionTime;
}

public enum DealState
{
    Unassigned,
    Active,
    Closed,
}

/// <summary>
/// Direct exchange agreement from Seller to Buyer, including delivery terms and conditions for closing the Deal.
/// Could be as simple as "come close -> receive goods", or as complicated as "kill these bandits -> receive new Deal"
/// </summary>
[System.Serializable]
public class Deal : IAuctionable
{
    public DealState  state;
    public Seller     seller;
    public Buyer      buyer;
    public float      sellerExpected,
                      buyerExpected;             

    public float GetValueAssessment( Buyer buyer )
    {
        return 10.0f;
    }

    public bool TryCloseDeal()
    {
        // TODO: Make closing deal distance depend on not just buyer interact range
        if ( Vector3.Distance( seller.transform.position, buyer.transform.position ) > buyer.InteractRange ) { /*Debug.LogWarning("Buyer attempted to complete deal, but was too far away!");*/ return false; }

        // TODO: Transfer some stuff from Seller to Buyer at some abstract requirement
        state = DealState.Closed;
        return true;
    }
}

[System.Serializable]
/// <summary>
/// The right - not obligation - to acquire a Deal for a given price (strike) within a set duration.
/// </summary>
public class Option : IAuctionable
{
    public Deal     deal;
    public float    strike;
    public float    duration;
    public float    purchasedAtTime;
    public bool     beenExchanged;

    public float GetValueAssessment( Buyer buyer )
    {
        return (deal.GetValueAssessment( buyer ) - strike) * duration;
    }

    public void BeginTimer()
    {
        purchasedAtTime = Time.time;
        deal.state = DealState.Active;
    }

    public bool IsExchangable()
    {
        if ( Time.time - purchasedAtTime < duration ) return true;
        return false;
    }

    public bool TryExchange( Buyer buyer )
    {
        if ( !IsExchangable() ){ Debug.LogWarning("Buyer tried to exchange option but it was not exchangable!"); return false; }
        if ( !buyer.SubtractOption( this ) ) { Debug.LogWarning("Buyer tried to exchange option but did not have it in their inventory!"); return false; }
        if ( !buyer.SubtractMoney( strike ) ) { Debug.LogWarning("Buyer tried to strike with missing funds!"); return false; }

        buyer.ReceiveDeal( deal );
        beenExchanged = true;
        return true;
    }
}

[System.Serializable]
public class Offer : IAuctionable
{
    public Option option;
    public Buyer  recipient;
    public float  premium;
    public float  decisionTimeSeconds;

    public float GetValueAssessment( Buyer buyer )
    {
        return option.GetValueAssessment( buyer ) - premium;
    }
}

public enum AuctionState
{
    Unassigned,
    Bidding,
    Resolving,
    Cancelled,
}

[System.Serializable]
public class Auction
{
    public AuctionState state;

    public Seller seller;
    public Option option;
    public Dictionary<Buyer, float> bids { private set; get; } = new();
    public List<KeyValuePair<Buyer, float>> bidsOrdered { private set; get;} = new();

    public float startTime;
    public float expectedResolveTime;
    public GameObject cancelledBy;

    public void PlaceBid( Buyer bidder, float bid )
    {
        // If the bidder already has a bid in place, replace previous entry from bidsOrdered
        if ( bids.ContainsKey(bidder) )
        {
            for ( int i = 0; i < bidsOrdered.Count; i++ )
            {
                KeyValuePair<Buyer, float> curBid = bidsOrdered[ i ];
                // TODO: Bubble up more optimal than complete re-sort
                if ( curBid.Key == bidder )
                {
                    bidsOrdered[i] = new KeyValuePair<Buyer, float>( bidder, bid );
                    break;
                }
            }
        }
        else bidsOrdered.Add( new KeyValuePair<Buyer, float>( bidder, bid ) );

        // Set new bid in lookup table and re-order list
        bids[ bidder ] = bid;
        bidsOrdered = bidsOrdered.OrderByDescending( (kvp) => kvp.Value ).ToList(); 
    }
}

// --- TransactionManager
public class TransactionManager : MonoBehaviour
{
    // Fake singleton pattern
    private static TransactionManager _instance;
    public static TransactionManager Instance
    {
        get
        {
            if ( _instance == null ) 
            {
                _instance = FindObjectOfType<TransactionManager>();
                if ( _instance == null )
                {
                    GameObject obj = new GameObject("TransactionManager (Runtime Created)");
                    _instance = obj.AddComponent<TransactionManager>();
                    Debug.LogWarning("TransactionManager created at runtime.");
                }
            }

            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static List<Auction> ActiveAuctions { get; private set; } = new();
    public static Dictionary<Option, bool> OptionInAuction { private get; set; } = new(); 
    public static Dictionary<Deal, bool> DealInAuction { private get; set; } = new(); 

    public static int FindActiveAuction( Option option ){ return OptionInAuction.ContainsKey( option ) ? ActiveAuctions.FindIndex((x) => object.Equals(x.option, option) ) : -1; }
    public static int FindActiveAuction( Deal deal ){ return DealInAuction.ContainsKey( deal ) ? ActiveAuctions.FindIndex((x) => object.Equals(x.option.deal, deal)) : -1; }
    public static bool CancelActiveAuction( Deal _deal, Option _option, Auction auction, GameObject source )
    {
        // Try to find the auction with either the direct reference, via option, or via deal
        int auctionIndex = -1;
        if ( _option != null ) auctionIndex = FindActiveAuction( (Option)_option );
        else auctionIndex = FindActiveAuction( (Deal)_deal );
        if ( auctionIndex >= 0 ) auction = ActiveAuctions[ auctionIndex ];

        // If auction was not found, return an error. Otherwise, cancel it.
        if ( auction == null )
        {
            Debug.LogError( "Error: Attempted to cancel nonexisting auction (auction not found)" );
            return false;
        }

        DeregisterAuction( auction );
        auction.state = AuctionState.Cancelled;
        auction.cancelledBy = source;
        return true;
    }

    private static void RegisterAuction( Auction auction, float resolveAfterSeconds )
    {
        auction.startTime = Time.time;
        auction.expectedResolveTime = auction.startTime + resolveAfterSeconds;

        ActiveAuctions.Add( auction );
        OptionInAuction[ auction.option ] = true;
        DealInAuction[ auction.option.deal ] = true;
    }

    private static void DeregisterAuction( Auction auction )
    {
        ActiveAuctions.Remove( auction );
        OptionInAuction[ auction.option ] = false;
        DealInAuction[ auction.option.deal ] = false;
    }

    public static void CreateAuction( Seller seller, Option option, float resolveAfterSeconds )
    {
        Auction auction = new Auction();
        auction.state = AuctionState.Unassigned;
        auction.seller = seller;
        auction.option = option;

        RegisterAuction( auction, resolveAfterSeconds ); // Register right away rather than in the async function
        Instance.StartCoroutine( Instance.AuctionRoutine(auction, resolveAfterSeconds) );
    }

    private IEnumerator AuctionRoutine( Auction auction, float resolveAfterSeconds )
    {
        // Put the auction up for bidding
        auction.state = AuctionState.Bidding;
        yield return new WaitForSeconds( resolveAfterSeconds );
        
        // Outliers
        if ( auction.state == AuctionState.Cancelled )
        {
            //cancelled
            Debug.LogWarning("Auction cancelled during bidding!");
            DeregisterAuction( auction );
            yield break;
        }
        else if ( auction.bids.Count <= 0 )
        {
            //no bidders
            Debug.LogWarning("Auction has no bidders!");
            DeregisterAuction( auction );
            yield break;
        }

        // Resolve the auction
        auction.state = AuctionState.Resolving;

        List<KeyValuePair<Buyer, float>> bidsOrdered = auction.bidsOrdered;

        int i = 0;
        bool bought = false;
        KeyValuePair<Buyer, float> bid;

        Action<Offer> makeExchange = (offer) => {
            // Transfer ownership
            if ( !offer.recipient.SubtractMoney( offer.premium ) ) { Debug.LogWarning("Buyer tried to purchase with missing funds!"); return; }
            if ( !auction.seller.SubtractDeal( offer.option.deal ) ) { Debug.LogError("Error: Seller does NOT have Deal at moment of exchange!"); return; }

            offer.recipient.ReceiveOption( offer.option );
            auction.seller.ReceiveMoney( offer.premium );

            auction.option.BeginTimer();
            auction.option.deal.buyer = offer.recipient;
            bought = true;
        };

        while ( i < bidsOrdered.Count && !bought )
        {
            bid = bidsOrdered[i++];

            Offer offer = new();
            offer.recipient = bid.Key;
            offer.premium = bid.Value;
            offer.option = auction.option;
            offer.decisionTimeSeconds = 0.15f;

            StartCoroutine( bid.Key.ReceiveOffer( offer, makeExchange ) );
            yield return new WaitForSeconds(offer.decisionTimeSeconds);
        }

        if ( bought )
        {

        }

        DeregisterAuction( auction );
    }

    public static bool BuyerCanBid( Buyer bidder, Auction auction )
    {
        return true;
    }

    public static bool PlaceBid( Buyer bidder, float bid, Auction auction )
    {
        // Check if the auction is up for bidding
        if ( !ActiveAuctions.Contains( auction ) || auction.state != AuctionState.Bidding ) return false;

        // Check if the bidder is allowed to bid on this object
        if ( !BuyerCanBid( bidder, auction ) ) return false;

        // Check if bid is lower than previously placed bid
        if ( auction.bids.ContainsKey( bidder ) && bid <= auction.bids[bidder] ) return false;

        // Place bid
        auction.PlaceBid( bidder, bid );

        // Notify those that were just out-bid
        List<KeyValuePair<Buyer, float>> bidsOrdered = auction.bidsOrdered;
        int curBidIdx = bidsOrdered.Count - 1;
        KeyValuePair<Buyer, float> curBid = bidsOrdered[ curBidIdx ];

        OutbidDetails curOutbidDetails;
        curOutbidDetails.bidder = bidder;
        curOutbidDetails.bid = bid;
        curOutbidDetails.auction = auction;
        curOutbidDetails.remainingAuctionTime = auction.expectedResolveTime - Time.time;

        while ( curBid.Value < bid && curBidIdx > 0 )
        {
            curOutbidDetails.diff = bid - curBid.Value;

            Buyer curBidder = curBid.Key;
            curBidder.NotifyOutbid( curOutbidDetails );

            curBid = bidsOrdered[ --curBidIdx ];
        }

        // Visualize
        // VisualizeBid( auction, bid, bidder );
        return true;
    }

    public static bool VisualizeBid( Auction auction, float bid, Buyer bidder )
    {
        if (auction == null || auction.seller == null)
            return false;
            
        // Calculate projectile size based on bid amount
        float size = bid / 700.0f;
        
        // Create the projectile
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.transform.localScale = new Vector3(size, size, size);
        
        // Set material/color to indicate it's a bid
        Renderer renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color( 0.1f, 0.9f, 0.5f );
        }
        
        // Position at bidder location (assuming auction has a bidder property)
        Vector3 startPosition = bidder.transform.position;
        projectile.transform.position = startPosition;
        
        // Launch towards seller
        Vector3 targetPosition = auction.seller.transform.position;
        
        Instance.StartCoroutine(Instance.AnimateProjectile(projectile, startPosition, targetPosition));
        
        return true;
    }
    
    public IEnumerator AnimateProjectile(GameObject projectile, Vector3 start, Vector3 target)
    {
        float duration = 1.0f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            projectile.transform.position = Vector3.Lerp(start, target, elapsed / duration);
            
            // Add a slight arc to the projectile path
            float arcHeight = 0.0f;
            float arcFactor = Mathf.Sin(Mathf.PI * (elapsed / duration));
            Vector3 currentPos = projectile.transform.position;
            currentPos.y += arcHeight * arcFactor;
            projectile.transform.position = currentPos;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Destroy the projectile or trigger an effect at the end
        GameObject.Destroy(projectile);
    }
}
