using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;
    private readonly string funcUrl = "https://funcstoretocosmo.azurewebsites.net/api/Function1?code=f8hwHt72l5kfUu3hGFWefQJbQw8MpK03d0ayZrB1amTeAzFugOImOw==";

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        var basketSpec = new BasketWithItemsSpecification(basketId);
        var basket = await _basketRepository.GetBySpecAsync(basketSpec);

        Guard.Against.NullBasket(basketId, basket);
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

        //var orderModel = new OrderModel(order.Id.ToString(), order.ShipToAddress, items, order.Total());
        //HttpClient client = new HttpClient();
        //var response = await client.PostAsJsonAsync(funcUrl, orderModel);       
    }

    private class OrderModel
    {
        public string Id { get; init; }
        public Address ShipToAddress { get; init; }
        public List<OrderItem> Items { get; init; }

        public decimal TotalPrice { get; init; }

        public OrderModel(string id, Address shipToAddress, List<OrderItem> items, decimal totalPrice) 
        {
            Id= id;
            ShipToAddress = shipToAddress;
            Items = items;
            TotalPrice = totalPrice;
        }
    }

}
