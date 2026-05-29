public class StackShopProduct
{
    public StackShopProduct(
        string id,
        string title,
        StackShopProductKind kind,
        int coinCost,
        int coinGrant,
        string skinId,
        string realMoneyProductId,
        StackRealMoneyProductType realMoneyType)
    {
        Id = id;
        Title = title;
        Kind = kind;
        CoinCost = coinCost;
        CoinGrant = coinGrant;
        SkinId = skinId;
        RealMoneyProductId = realMoneyProductId;
        RealMoneyType = realMoneyType;
    }

    public string Id { get; }
    public string Title { get; }
    public StackShopProductKind Kind { get; }
    public int CoinCost { get; }
    public int CoinGrant { get; }
    public string SkinId { get; }
    public string RealMoneyProductId { get; }
    public StackRealMoneyProductType RealMoneyType { get; }
    public bool UsesCoins => CoinCost > 0 || Kind == StackShopProductKind.Skin;
    public bool UsesRealMoney => !string.IsNullOrEmpty(RealMoneyProductId);
}
