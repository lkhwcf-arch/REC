using System;
using System.Collections;
using System.Collections.Generic;

public class Player
{
    public int currentDay = 1;

    public bool isDead;
    public bool isGameCleared;

    public List<int> clearedDays = new List<int>();

    public List<ResolvedPhenomenon> resolvedPhenomena =
        new List<ResolvedPhenomenon>();

    public bool CanAct => !isDead && !isGameCleared;

    public bool HasResolved(int day, int phenomenonIndex)
    {
        return resolvedPhenomena.Exists(record =>
            record.day == day &&
            record.phenomenonIndex == phenomenonIndex);
    }

    public void RecordResolved(int phenomenonIndex)
    {
        if (HasResolved(currentDay, phenomenonIndex))
            return;

        resolvedPhenomena.Add(new ResolvedPhenomenon
        {
            day = currentDay,
            phenomenonIndex = phenomenonIndex
        });
    }
}
[Serializable]
public class ResolvedPhenomenon
{
    public int day;
    public int phenomenonIndex;
}