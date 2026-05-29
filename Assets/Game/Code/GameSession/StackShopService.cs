public class StackShopService
{
    private readonly StackProgression progression;

    public StackShopService(StackProgression progression)
    {
        this.progression = progression;
    }

    public StackPurchaseResult BuyOrSelectSkin(StackShopProduct product)
    {
        if (product == null || product.Kind != StackShopProductKind.Skin)
        {
            return StackPurchaseResult.Fail("Invalid skin");
        }

        if (progression.IsSkinUnlocked(product.SkinId))
        {
            progression.SelectSkin(product.SkinId);
            return StackPurchaseResult.Ok("Skin selected");
        }

        if (!progression.TryUnlockSkin(product.SkinId, product.CoinCost))
        {
            return StackPurchaseResult.Fail("Not enough coins");
        }

        progression.SelectSkin(product.SkinId);
        return StackPurchaseResult.Ok("Skin unlocked");
    }

    public StackPurchaseResult GrantRealMoneyProduct(string realMoneyProductId)
    {
        StackShopProduct product = StackShopCatalog.FindByRealMoneyProductId(realMoneyProductId);

        if (product == null)
        {
            return StackPurchaseResult.Fail("Unknown store product");
        }

        if (product.Kind == StackShopProductKind.CoinPack)
        {
            progression.AddCoins(product.CoinGrant);
            return StackPurchaseResult.Ok($"{product.CoinGrant} coins granted");
        }

        if (product.Kind == StackShopProductKind.Skin)
        {
            progression.GrantSkin(product.SkinId);
            progression.SelectSkin(product.SkinId);
            return StackPurchaseResult.Ok("Skin granted");
        }

        return StackPurchaseResult.Fail("Unsupported product");
    }
}
