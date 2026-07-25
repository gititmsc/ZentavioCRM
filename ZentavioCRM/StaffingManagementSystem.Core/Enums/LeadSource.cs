namespace ZentavioCRM.Core.Enums
{
    /// <summary>
    /// Where a <see cref="Entities.Lead"/> originated from.
    /// </summary>
    public enum LeadSource
    {
        Website = 1,
        LandingPage = 2,
        Referral = 3,
        Exhibition = 4,
        WhatsApp = 5,
        Facebook = 6,
        LinkedIn = 7,
        EmailCampaign = 8,
        GoogleAds = 9,
        ManualEntry = 10,
        ApiIntegration = 11
    }
}
