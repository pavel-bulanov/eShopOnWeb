using System;
using System.Linq;
using System.Net.Http.Json;
using System.Net.Http;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Microsoft.Extensions.Options;
using Azure.Messaging.ServiceBus;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;
    private readonly TriggerSettings _triggerSettings;

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer,
        IOptions<TriggerSettings> triggerSettings)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
        _triggerSettings = triggerSettings.Value;
    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        var basketSpec = new BasketWithItemsSpecification(basketId);
        var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);

        Guard.Against.Null(basket, nameof(basket));
        Guard.Against.EmptyBasketOnCheckout(basket.Items);

        var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

        var items = basket.Items.Select(basketItem =>
        {
            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
            var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
            var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
            return orderItem;
        }).ToList();

        var order = new Order(basket.BuyerId, shippingAddress, items);

        await _orderRepository.AddAsync(order);

        using var storage = new HttpClient
        {
            BaseAddress = new Uri(_triggerSettings.FunctionUrl)
        };
        storage.DefaultRequestHeaders.Add("x-functions-key", _triggerSettings.FunctionKey);
        var result =  await storage.PostAsync("/api/Order", JsonContent.Create(order, options: new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        await using var client = new ServiceBusClient(_triggerSettings.ServiceBusConnection);

        // create the sender
        ServiceBusSender sender = client.CreateSender(_triggerSettings.ServiceBusQueue);

        // create a message that we can send. UTF-8 encoding is used when providing a string.
        ServiceBusMessage message = new ServiceBusMessage(JsonSerializer.Serialize(order, options: new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        // send the message
        await sender.SendMessageAsync(message);
    }
}
