using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class HoverSphere
{
    public Vector3 WorldPosition;
    public float Radius;
    public int MaxResults;
    public LayerMask LayerMask;
    public QueryTriggerInteraction QueryTrigger = QueryTriggerInteraction.UseGlobal;
    public Collider[] HitResults;
    public int ResultCount = 0;

    public HoverResult[] Results;

    public struct HoverResult
    {
        public Collider collider;
        public float distanceToCenter;
        public Vector3 closestPointToCenter;

        public HoverResult(Collider collider, Vector3 worldPos)
        {
            this.collider = collider;
            closestPointToCenter = collider.ClosestPoint(worldPos);
            distanceToCenter = Vector3.Distance(closestPointToCenter, worldPos);
        }
    }

    public HoverSphere(Vector3 position, float radius, int maxResults, LayerMask layerMask)
    {
        WorldPosition = position;
        Radius = radius;
        MaxResults = maxResults;
        HitResults = new Collider[MaxResults];
        Results = new HoverResult[MaxResults];
        LayerMask = layerMask;
    }

    public void CheckSpehere()
    {
        if (MaxResults != HitResults.Length)
        {
            HitResults = new Collider[MaxResults];
            Results = new HoverResult[MaxResults];
        }

        ResultCount = Physics.OverlapSphereNonAlloc(WorldPosition, Radius, HitResults, LayerMask, QueryTrigger);
        HitsToSortedResults();
    }

    private void HitsToSortedResults()
    {
        // Calculate results
        for (int Index = 0; Index < ResultCount; Index++)
        {
            var col = HitResults[Index];
            HoverResult result;
            if (col == null)
                result = default;
            else
                result = new HoverResult(col, WorldPosition);
            Results[Index] = result;
        }
        // Sort by distance
        Array.Sort(Results[..ResultCount], (a, b) => a.distanceToCenter.CompareTo(b.distanceToCenter));
    }

    public HoverResult? ClosestResult()
    {
        if (ResultCount == 0 || MaxResults <= 0)
            return null;
        return Results[0];
    }
}