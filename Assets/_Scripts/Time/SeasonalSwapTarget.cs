using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public struct SeasonTilePair {
    public TileBase spring, summer, fall, winter;
    public TileBase Get(SeasonManager.Season s) => s switch {
        SeasonManager.Season.Spring => spring,
        SeasonManager.Season.Summer => summer,
        SeasonManager.Season.Fall   => fall,
        _                           => winter
    };
}

public class SeasonalSwapTarget : MonoBehaviour {
    public Tilemap target;
    public List<SeasonTilePair> pairs;
    SeasonManager sm;
    SeasonManager.Season lastSeason; bool inited;

    void OnEnable(){ sm = FindFirstObjectByType<SeasonManager>(); if (sm) sm.OnSeasonChanged += OnSeasonChanged; }
    void OnDisable(){ if (sm) sm.OnSeasonChanged -= OnSeasonChanged; }

    void Start(){
        if (!target) target = GetComponent<Tilemap>();
        if (!sm) sm = FindFirstObjectByType<SeasonManager>();
        var now = sm ? sm.CurrentSeason : SeasonManager.Season.Spring;
        ApplyFull(now); lastSeason = now; inited = true;
    }
    void OnSeasonChanged(SeasonManager.Season newS){
        foreach (var p in pairs){
            var from = p.Get(lastSeason); var to = p.Get(newS);
            if (from && to && from != to) target.SwapTile(from, to);
        }
        target.RefreshAllTiles(); lastSeason = newS;
    }
    void ApplyFull(SeasonManager.Season newSeason)
    {
        if (!target) return;

        foreach (var p in pairs)
        {
            var to = p.Get(newSeason);
            if (!to) continue;

            void SwapIf(TileBase from)
            {
                if (from && from != to) target.SwapTile(from, to);
            }

            SwapIf(p.spring);
            SwapIf(p.summer);
            SwapIf(p.fall);
            SwapIf(p.winter);
        }
        target.RefreshAllTiles();
    }
}
