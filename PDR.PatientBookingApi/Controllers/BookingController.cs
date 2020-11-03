using Microsoft.AspNetCore.Mvc;
using PDR.PatientBooking.Data;
using System;
using PDR.PatientBooking.Service.OrderServices;
using PDR.PatientBooking.Service.OrderServices.Requests;

namespace PDR.PatientBookingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly PatientBookingContext _context;
        private readonly IOrderService _orderService;

        public BookingController(PatientBookingContext context, IOrderService orderService)
        {
            _context = context;
            _orderService = orderService;
        }

        [HttpGet("patient/{identificationNumber}/next")]
        public IActionResult GetPatientNextAppointment(long identificationNumber)
        {
            try
            {
                var nextAppointment = _orderService.GetPatientNextAppointment(identificationNumber);
                if (nextAppointment is null)
                {
                    return NoContent();
                }

                return Ok(nextAppointment);
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
    }
}