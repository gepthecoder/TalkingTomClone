using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;

public class bannerAds : MonoBehaviour
{
    private bool testMode = true;

    IEnumerator Start()
    {
        Advertisement.Initialize(GameConstants.gameID, testMode);

        while (!Advertisement.IsReady(GameConstants.placementID))
            yield return null;

        Advertisement.Banner.SetPosition(BannerPosition.TOP_CENTER);
        Advertisement.Banner.Show(GameConstants.placementID);
    }
}
