using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Contracts.Domain.V1;


namespace OrderSimulatorApi.Controllers;

/*
 Controller base tiene Ok(), Accepted() NotFound(), BdRequest()
 public IActionResult reperesna las resupestas 
 */

public class OrdersControllers
{
	[ApiController]
	[Route("orders")]
	[Produces("application/json")]
	public class OrdersController : ControllerBase
	{
		private readonly IOrderRepository _orderRepository;
		public OrdersController(IOrderRepository orderRepository)
		{
			_orderRepository = orderRepository;
		}

	}
	[HttpPost]
	public async Task<IActionResult> CreateOrder(
			[FromBody] CreateOrderRequest request)
	{
		var order = new Order
		{
			UserId = request.UserId,
			Symbol = request.Symbol,
			Type = request.Type,
			Quantity = request.Quantity,
			LimitPrice = request.LimitPrice,
			Status = OrderStatus.Pending,
			CreatedAtUtc = DateTime.UtcNow
		};
		await _orderRepository.InsertOrderAsync(order);
		return Acepted();
	}


	[HttpPost("burst")]
	public async Task<IActionResult> CreateOrderBurst(
			[FromBody] IEnumerable<CreateOrderRequest> requests)
	{
		var orders = new List<Order>();
		foreach(var request in requests)
		{
			var order = new Order
			{
				UserId = request.UserId,
				Symbol = request.Symbol,
				Type = request.Type,
				Quantity = request.Quantity,
				LimitPrice request.LimitPrice,
				Status = OrderStatus.Pending,
				CreatedAtUtc = DateTime.UtcNow
			};
			orders.Add(order);

			await _orderRepository.InserOrdersAsync(orders);
		}
	}
	[HttpGet("{orderId")]
	public async Task<IActionResult> GetOrder(int orderId)
	{
		Order? result = await _orderRepository.GetByIdAsync(orderId);
		if (result is null)
		{
			return NotFound();
		}
		

		return Ok(result);

	}
