using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject   BarGraph = null;

    private Seller[]    allSellers = null;
    private Buyer[]     allBuyers = null;

    private Coroutine dayCoroutine = null;

    private int spaceHeld = 0;

    private System.Random rng = new System.Random();

    // Start is called before the first frame update
    void Start()
    {
        // Get sellers and buyers
        allSellers = getSellers();
        if (allSellers.Length <= 0) Debug.LogError("GameManager: No sellers!");

        allBuyers = getBuyers();
        if (allBuyers.Length <= 0) Debug.LogError("GameManager: No buyers!");
    }

    // Update is called once per frame
    void Update()
    {
        // Space press
        if (Input.GetKey("space")) spaceHeld++;
        else spaceHeld = 0;

        // Go to next day
        if (true)//spaceHeld == 1 || spaceHeld >= 100)
        {
            startNextDay();
        }
    }

    private Seller[] getSellers()
    {
        return FindObjectsOfType<Seller>();
    }
    
    private Buyer[] getBuyers()
    {
        return FindObjectsOfType<Buyer>();
    }

    /// <summary>
    /// Starts the next day. If the current day is not finished, returns false and skips current day.
    /// </summary>
    /// <returns></returns>
    bool startNextDay()
    {
        // Start next day
        if (dayCoroutine == null)
        {
            dayCoroutine = StartCoroutine(runDay());
            return true;
        }

        // If day is already in action, skip it and return false
        //StopCoroutine(dayCoroutine);
        //dayCoroutine = null;
        return false;
    }

    /// <summary>
    /// Runs the current day.
    /// </summary>
    /// <returns></returns>
    IEnumerator runDay()
    {
        // Filter out buyers/sellers with inactive gameobjects
        List<Seller>    sellers = new List<Seller> ();
        List<Buyer>     buyers = new List<Buyer> ();

        foreach (var seller in allSellers) if (seller.gameObject.activeSelf) sellers.Add(seller);
        foreach (var buyer in allBuyers) if (buyer.gameObject.activeSelf) buyers.Add(buyer);

        // Pit buyers against random sellers until everyone has made a deal or are unwilling to take any deals
        // Buyers and sellers adjust their prices based on the success of their sale

        //store a complete history of attempted deals indexed by buyers, and which sellers are unavailable (have made a deal)
        var dealHistory = new Dictionary<Buyer, List<Tuple<Seller, bool>>>();
        var sellersUnavailable = new Dictionary<Seller, bool>();

        //function for checking if a deal can potentially be made between a given buyer and seller
        Func<Buyer, Seller, bool> canMakeDeal = (buyer, seller) =>
        {
            if (sellersUnavailable.ContainsKey(seller)) return false;  // If the seller is unavailable, no deal can be made
            if (!dealHistory.ContainsKey(buyer)) return true;           // If the buyer has not made any deal yet, a deal can be made
            if (!dealHistory[buyer].Last().Item2) return true;          // If the buyer's previous attempt at a deal failed, a deal can be made
            return false;
        };

        //function for pushing an attempted deal (successful or not)
        Action<Buyer, Seller, bool> pushDeal = (buyer, seller, dealSuccessful) =>
        {
            if (!dealHistory.ContainsKey(buyer)) dealHistory[buyer] = new List<Tuple<Seller, bool>> { Tuple.Create(seller, dealSuccessful) };
            else dealHistory[buyer].Add(Tuple.Create(seller, dealSuccessful));
            if (dealSuccessful) sellersUnavailable[seller] = true;
        };

        //find a seller for each buyer, in random order
        var buyersR = buyers.OrderBy(x => rng.Next());
        var sellersR = sellers.OrderBy(x => rng.Next());
        foreach (var buyer in buyersR)
        {
            bool hasMadeDeal = false;
            foreach (var seller in sellersR)
            {
                if (hasMadeDeal) break;

                //attempt to make a deal. If successful, go to next buyer. Either way, log the result
                if (canMakeDeal(buyer, seller))
                {
                    bool dealSuccess = buyer.TryMakeDeal(seller);
                    pushDeal(buyer, seller, dealSuccess);
                    if (dealSuccess) hasMadeDeal = true;
                }
            }

            //if a buyer did not get to make any deals, adjust their expectations
            if (!hasMadeDeal) buyer.ExpectedPrize = Math.Min(buyer.ExpectedPrize * UnityEngine.Random.Range(1.0f, 1.25f), buyer.MaxPrize);
        }

        //if a seller did not make any deals, adjust their expectations too
        foreach (var seller in sellers)
            if (!sellersUnavailable.ContainsKey(seller)) seller.ExpectedPrize = Math.Max(seller.ExpectedPrize / UnityEngine.Random.Range(1.0f, 1.25f), seller.MinPrize);

        // Create graph
        if (BarGraph != null)
        {
            BarGraph barGraphScript = BarGraph.GetComponent<BarGraph>();
            barGraphScript.ResetBars();

            List<float> buyerBars = new List<float>();
            List<float> sellerBars = new List<float>();
            
            List<Buyer> buyersS = buyers.OrderBy(x => x.MaxPrize).ToList();
            foreach (var buyer in buyersS)
            {
                if (dealHistory.ContainsKey(buyer) && dealHistory[buyer].Last().Item2)
                {
                    var seller = dealHistory[buyer].Last().Item1;
                    buyerBars.Add(buyer.ExpectedPrize);
                    sellerBars.Add(seller.ExpectedPrize);
                }
            }

            barGraphScript.PushBars(buyerBars, Layer.Orange);
            barGraphScript.PushBars(sellerBars, Layer.Blue);
            barGraphScript.PrepareForRender();
        }

        dayCoroutine = null;
        yield break;
    }
}
