using System;

public static class StackShopCatalog
{
    public static readonly StackShopProduct[] SkinProducts =
    {
        new("skin_classic", "Classic", StackShopProductKind.Skin, 0, 0, "classic", string.Empty, StackRealMoneyProductType.NonConsumable),
        new("skin_neon", "Neon", StackShopProductKind.Skin, 150, 0, "neon", "stackblocks.skin.neon", StackRealMoneyProductType.NonConsumable),
        new("skin_glass", "Glass", StackShopProductKind.Skin, 350, 0, "glass", "stackblocks.skin.glass", StackRealMoneyProductType.NonConsumable),
        new("skin_gold", "Gold", StackShopProductKind.Skin, 700, 0, "gold", "stackblocks.skin.gold", StackRealMoneyProductType.NonConsumable)
    };

    public static readonly StackShopProduct[] CoinPackProducts =
    {
        new("coins_small", "Small Coin Pack", StackShopProductKind.CoinPack, 0, 500, string.Empty, "stackblocks.coins.small", StackRealMoneyProductType.Consumable),
        new("coins_big", "Big Coin Pack", StackShopProductKind.CoinPack, 0, 1500, string.Empty, "stackblocks.coins.big", StackRealMoneyProductType.Consumable)
    };

    public static StackShopProduct FindByRealMoneyProductId(string productId)
    {
        StackShopProduct product = FindIn(SkinProducts, productId);

        if (product != null)
        {
            return product;
        }

        return FindIn(CoinPackProducts, productId);
    }

    public static StackShopProduct FindSkinById(string skinId)
    {
        foreach (StackShopProduct product in SkinProducts)
        {
            if (product.SkinId == skinId)
            {
                return product;
            }
        }

        return null;
    }

    private static StackShopProduct FindIn(StackShopProduct[] products, string productId)
    {
        if (string.IsNullOrEmpty(productId))
        {
            return null;
        }

        foreach (StackShopProduct product in products)
        {
            if (string.Equals(product.RealMoneyProductId, productId, StringComparison.Ordinal))
            {
                return product;
            }
        }

        return null;
    }
}
