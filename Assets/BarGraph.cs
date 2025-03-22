using System.Collections.Generic;
using UnityEngine;
// using static BarGraph;

public static class DealObserver
{
    public static List<Deal> dealHistory = new List<Deal>();
}

public enum Layer
{
    Blue,
    Orange,
}

public class BarGraph : MonoBehaviour
{
    // A simple class for what constitutes a "bar"
    public class Bar
    {
        public float Height;
        public GameObject BarObject;
        public Layer layer;

        public Bar(float height, Layer layer)
        {
            Height = height;
            this.layer = layer;
            BarObject = null;
        }
    }


    public GameObject BarPrefab;
    public GameObject Background;

    private Dictionary<Layer, List<Bar>> barDict = new Dictionary<Layer, List<Bar>>();
    private Dictionary<Layer, Color> layerColors = new Dictionary<Layer, Color>
    {
        { Layer.Blue, new Color(0.25f, 0.25f, 1.0f) },
        { Layer.Orange, new Color(1.0f, 1.0f, 0.25f) },
    };

    public int VisibleDealsCount = 50;
    public int AverageOver       = 1;

    /// <summary>
    /// Pushes a set of bars to the bar graph.
    /// </summary>
    /// <param name="barHeights"></param>
    public void PushBars(List<float> barHeights, Layer layer)
    {
        foreach (var barHeight in barHeights)
        {
            Bar bar = new Bar(barHeight, layer);
            if (barDict.ContainsKey(layer)) barDict[layer].Add(bar);
            else barDict[layer] = new List<Bar> { bar }; 
        }
    }

    public void FixedUpdate()
    {
        ResetBars();
        PushDeals();
        PrepareForRender();
    }

    public void PushDeals()
    {
        List<Deal> selectDeals = new List<Deal>();
        List<Deal> staticDeals = DealObserver.dealHistory;
        int staticDealsCount = staticDeals.Count;
        if ( staticDealsCount <= 0 ) return;

        for ( int i = 0; i < VisibleDealsCount; i++ )
        {
            if ( i >= staticDealsCount ) break;
            selectDeals.Add( staticDeals[staticDealsCount - i - 1 ] );
        }

        List<float> buys = new List<float>();
        List<float> sells = new List<float>();
        int   runningCounter = 0;
        float runningBuyAvg = 0.0f,
              runningSellAvg = 0.0f;
        foreach ( Deal deal in selectDeals )
        {
            runningBuyAvg += deal.buyerExpected;
            runningSellAvg += deal.sellerExpected;
            runningCounter++;
            if ( runningCounter % AverageOver == 0 )
            {
                buys.Add( runningBuyAvg / AverageOver );
                sells.Add( runningSellAvg / AverageOver );
                runningBuyAvg = 0.0f;
                runningSellAvg = 0.0f;
            }
        }

        PushBars(buys, Layer.Orange);
        PushBars(sells, Layer.Blue);
    }

    /// <summary>
    /// Clears the bar graph.
    /// </summary>
    public void ResetBars()
    {
        foreach (var barList in barDict.Values) foreach (var bar in barList) Destroy(bar.BarObject);
        barDict.Clear();
    }

    /// <summary>
    /// Prepares the bar graph for rendering.
    /// </summary>
    public void PrepareForRender()
    {
        // Set up width and height normalization factors for bars
        List<Bar> allBars = new List<Bar>();
        int     maxBarCount = -1;
        float   maxBarHeight = -1.0f;

        foreach (var barList in barDict.Values)
        {
            if (barList.Count > maxBarCount) maxBarCount = barList.Count;
            foreach (var bar in barList) if (maxBarHeight == -1.0f || bar.Height > maxBarHeight) maxBarHeight = bar.Height;
        }

        maxBarHeight = 4.0f; // DELETE
        float barWidth = 1.0f / maxBarCount;

        // Draw each layer in order
        foreach (var kvp in barDict)
        {
            // Get layer color
            Layer layer = kvp.Key;
            List<Bar> bars = kvp.Value;
            Color color = Color.red;
            if (layerColors.ContainsKey(layer)) color = layerColors[layer];

            // Create bar gameobjects for all bars in layer
            for (int i = 0; i < bars.Count; i++)
            {
                var bar = bars[i];
                if (bar.BarObject == null)
                {
                    GameObject barObject = Instantiate(BarPrefab, Background.transform);
                    barObject.transform.localScale = new Vector3(barWidth, bar.Height / maxBarHeight, 1.0f);
                    barObject.transform.localPosition = new Vector3(
                        -0.5f + barWidth * (i + 0.5f),
                        -0.5f + bar.Height / maxBarHeight / 2.0f,
                        -(1.0f + (int)layer));
                    barObject.SetActive(true);
                    barObject.GetComponent<SpriteRenderer>().color = color;
                    bar.BarObject = barObject;
                }

                bars[i] = bar;
            }

            // barDict[layer] = bars;
        }
    }
}
