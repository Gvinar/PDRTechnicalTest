using Microsoft.AspNetCore.Mvc;
using System;
using PDR.PatientBooking.Service.OrderServices;
using PDR.PatientBooking.Service.OrderServices.Requests;

namespace PDR.PatientBookingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public BookingController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("patient/{identificationNumber}/next")]
        public IActionResult GetPatientNextAppointment(long identificationNumber)
        {
            try
            {
                return Ok(_orderService.GetPatientNextOrder(identificationNumber));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex);
            }
        }

        [HttpPost()]
        public IActionResult AddBooking(AddOrderRequest newOrder)
        {
            try
            {
                _orderService.AddOrder(newOrder);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex);
            }
        }

        [HttpDelete("{appointmentId}")]
        public IActionResult CancelAppointment(Guid appointmentId)
        {
            try
            {
                _orderService.CancelOrder(appointmentId);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex);
            }
        }
    }
}