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
/// Struct for describing a potential deal.
/// </summary>
[System.Serializable]
public struct Deal : IAuctionable
{
    public Seller   Seller;
    public Buyer    Buyer;

    public float    SellerExpected,
                    BuyerExpected;

    public float GetValueAssessment( Buyer buyer )
    {
        return 10.0f;
    }
}

[System.Serializable]
public struct Option : IAuctionable
{
    public Deal     deal;
    public float    strike;
    public float    duration;

    public float GetValueAssessment( Buyer buyer )
    {
        return (deal.GetValueAssessment( buyer ) - strike) * duration;
    }
}

[System.Serializable]
public struct Offer : IAuctionable
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
    public Dictionary<Buyer, float> bids = new();

    public GameObject cancelledBy;
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
    public static bool CancelActiveAuction( Deal? _deal, Option? _option, Auction auction, GameObject source )
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

    private static void RegisterAuction( Auction auction )
    {
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

        RegisterAuction( auction ); // Register right away rather than in the async function
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

        List<KeyValuePair<Buyer, float>> bidsOrdered = auction.bids.ToList().OrderByDescending( (kvp) => kvp.Value ).ToList(); // TODO: Use Linq

        int i = 0;
        bool bought = false;
        KeyValuePair<Buyer, float> bid;

        Action<Offer> makeExchange = (offer) => {
            // Transfer ownership
            if ( !offer.recipient.SubtractMoney( offer.premium ) ) { Debug.LogWarning("Buyer tried to purchase with missing funds!"); return; }
            if ( !auction.seller.SubtractDeal( offer.option.deal ) ) { Debug.LogError("Error: Seller does NOT have Deal at moment of exchange!"); return; }

            offer.recipient.ReceiveOption( offer.option );
            auction.seller.ReceiveMoney( offer.premium );

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
        if ( auction.bids.ContainsKey( bidder ) && auction.bids[bidder] <= bid ) return false;

        // Place bid and return success
        auction.bids[bidder] = bid;

        VisualizeBid( auction, bid, bidder );
        return true;
    }

    public static bool VisualizeBid( Auction auction, float bid, Buyer bidder )
    {
        if (auction == null || auction.seller == null)
            return false;
            
        // Calculate projectile size based on bid amount
        float size = bid / 200.0f;
        
        // Create the projectile
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.transform.localScale = new Vector3(size, size, size);
        
        // Set material/color to indicate it's a bid
        Renderer renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.9f, 0.7f, 0.1f);
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
        float duration = 3.0f;
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
