using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

public class StackIapManager : MonoBehaviour
{
    private StoreController storeController;
    private StackShopService shopService;
    private Action onPurchaseGranted;
    private Action<string> onStatusChanged;
    private bool isInitializing;
    private bool isReady;

    public bool IsReady => isReady;

    public static StackIapManager GetOrCreate()
    {
        StackIapManager manager = FindFirstObjectByType<StackIapManager>();

        if (manager != null)
        {
            return manager;
        }

        GameObject managerObject = new("StackIapManager");
        DontDestroyOnLoad(managerObject);

        return managerObject.AddComponent<StackIapManager>();
    }

    public void Initialize(StackProgression progression, Action purchaseGrantedCallback, Action<string> statusChangedCallback)
    {
        shopService = new StackShopService(progression);
        onPurchaseGranted = purchaseGrantedCallback;
        onStatusChanged = statusChangedCallback;

        if (isReady || isInitializing)
        {
            return;
        }

        InitializePurchasing();
    }

    public void Purchase(string productId)
    {
        if (!isReady || storeController == null)
        {
            SetStatus("Store is not ready yet");
            return;
        }

        Product product = storeController.GetProducts().FirstOrDefault(storeProduct => storeProduct.definition.id == productId);

        if (product == null)
        {
            SetStatus($"Product not loaded: {productId}");
            return;
        }

        SetStatus($"Opening store: {product.definition.id}");
        storeController.PurchaseProduct(product);
    }

    public string GetLocalizedPrice(string productId)
    {
        Product product = GetProduct(productId);

        if (product == null || string.IsNullOrEmpty(product.metadata.localizedPriceString))
        {
            return isReady ? "Unavailable" : "Loading";
        }

        return product.metadata.localizedPriceString;
    }

    public bool IsProductLoaded(string productId)
    {
        return GetProduct(productId) != null;
    }

    public void RestorePurchases()
    {
        if (!isReady || storeController == null)
        {
            SetStatus("Store is not ready yet");
            return;
        }

        storeController.RestoreTransactions((success, error) =>
        {
            SetStatus(success ? "Purchases restored" : $"Restore failed: {error}");
        });
    }

    private async void InitializePurchasing()
    {
        isInitializing = true;
        storeController = UnityIAPServices.StoreController();

        storeController.OnStoreConnected += OnStoreConnected;
        storeController.OnStoreDisconnected += OnStoreDisconnected;
        storeController.OnProductsFetched += OnProductsFetched;
        storeController.OnProductsFetchFailed += OnProductsFetchFailed;
        storeController.OnPurchasePending += OnPurchasePending;
        storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
        storeController.OnPurchaseFailed += OnPurchaseFailed;
        storeController.OnPurchaseDeferred += OnPurchaseDeferred;
        storeController.OnPurchasesFetched += OnPurchasesFetched;
        storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;

        SetStatus("Connecting store");

        try
        {
            await storeController.Connect();
        }
        catch (Exception exception)
        {
            isInitializing = false;
            SetStatus($"Store connection failed: {exception.Message}");
        }
    }

    private void OnStoreConnected()
    {
        SetStatus("Store connected");
        storeController.FetchProducts(BuildProductDefinitions());
    }

    private void OnStoreDisconnected(StoreConnectionFailureDescription description)
    {
        isReady = false;
        SetStatus($"Store disconnected: {description.message}");
    }

    private void OnProductsFetched(List<Product> products)
    {
        isReady = true;
        isInitializing = false;
        SetStatus($"Store ready: {products.Count} products");
        storeController.FetchPurchases();
    }

    private void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        isReady = false;
        isInitializing = false;
        SetStatus($"Products fetch failed: {failure.FailureReason}");
    }

    private void OnPurchasePending(PendingOrder order)
    {
        Product product = GetFirstProduct(order);

        if (product == null)
        {
            SetStatus("Purchase pending without product");
            return;
        }

        StackPurchaseResult result = shopService.GrantRealMoneyProduct(product.definition.id);
        SetStatus(result.Message);

        if (result.Success)
        {
            onPurchaseGranted?.Invoke();
            storeController.ConfirmPurchase(order);
        }
    }

    private void OnPurchaseConfirmed(Order order)
    {
        switch (order)
        {
            case ConfirmedOrder confirmedOrder:
                SetStatus($"Purchase confirmed: {GetProductId(confirmedOrder)}");
                break;
            case FailedOrder failedOrder:
                SetStatus($"Confirmation failed: {failedOrder.FailureReason}");
                break;
        }
    }

    private void OnPurchaseFailed(FailedOrder order)
    {
        SetStatus($"Purchase failed: {order.FailureReason}");
    }

    private void OnPurchaseDeferred(DeferredOrder order)
    {
        SetStatus($"Purchase deferred: {GetProductId(order)}");
    }

    private void OnPurchasesFetched(Orders orders)
    {
        foreach (ConfirmedOrder order in orders.ConfirmedOrders)
        {
            Product product = GetFirstProduct(order);

            if (product == null)
            {
                continue;
            }

            StackShopProduct shopProduct = StackShopCatalog.FindByRealMoneyProductId(product.definition.id);

            if (shopProduct != null && shopProduct.RealMoneyType == StackRealMoneyProductType.NonConsumable)
            {
                shopService.GrantRealMoneyProduct(product.definition.id);
                onPurchaseGranted?.Invoke();
            }
        }
    }

    private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
    {
        SetStatus($"Purchases fetch failed: {failure.message}");
    }

    private static List<ProductDefinition> BuildProductDefinitions()
    {
        List<ProductDefinition> definitions = new();
        AddDefinitions(definitions, StackShopCatalog.SkinProducts);
        AddDefinitions(definitions, StackShopCatalog.CoinPackProducts);

        return definitions;
    }

    private static void AddDefinitions(List<ProductDefinition> definitions, StackShopProduct[] products)
    {
        foreach (StackShopProduct product in products)
        {
            if (!product.UsesRealMoney)
            {
                continue;
            }

            definitions.Add(new ProductDefinition(product.RealMoneyProductId, GetUnityProductType(product.RealMoneyType)));
        }
    }

    private static ProductType GetUnityProductType(StackRealMoneyProductType productType)
    {
        return productType == StackRealMoneyProductType.Consumable ? ProductType.Consumable : ProductType.NonConsumable;
    }

    private static Product GetFirstProduct(Order order)
    {
        return order.CartOrdered.Items().FirstOrDefault()?.Product;
    }

    private Product GetProduct(string productId)
    {
        if (storeController == null)
        {
            return null;
        }

        return storeController.GetProducts().FirstOrDefault(storeProduct => storeProduct.definition.id == productId);
    }

    private static string GetProductId(Order order)
    {
        return GetFirstProduct(order)?.definition.id ?? "unknown";
    }

    private void SetStatus(string message)
    {
        Debug.Log($"IAP: {message}");
        onStatusChanged?.Invoke(message);
    }
}
