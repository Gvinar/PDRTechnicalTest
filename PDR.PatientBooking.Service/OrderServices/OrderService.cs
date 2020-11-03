using System;
using System.Linq;
using PDR.PatientBooking.Data;
using PDR.PatientBooking.Data.Models;
using PDR.PatientBooking.Service.Extensions;
using PDR.PatientBooking.Service.OrderServices.Validation;
using PDR.PatientBooking.Service.OrderServices.Requests;
using PDR.PatientBooking.Service.OrderServices.Responses;

namespace PDR.PatientBooking.Service.OrderServices
{
    public class OrderService : IOrderService
    {
        private readonly PatientBookingContext _context;
        private readonly IAddOrderRequestValidator _validator;

        public OrderService(PatientBookingContext context, IAddOrderRequestValidator validator)
        {
            _context = context;
            _validator = validator;
        }

        public void AddOrder(AddOrderRequest request)
        {
            var validationResult = _validator.ValidateRequest(request);
            
            if (!validationResult.PassedValidation)
            {
                throw new ArgumentException(validationResult.Errors.First());
            }

            var clinic = _context.Patient.First(x => x.Id == request.PatientId).Clinic;

            var newOrder = new Order
            {
                Id = Guid.NewGuid(),
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                SurgeryType = (int)clinic.SurgeryType
            };

            _context.Order.Add(newOrder);

            _context.SaveChanges();
        }

        public GetOrderResponse GetPatientNextAppointment(long patientId)
        {
            var utcNow = DateTime.UtcNow;
            var order = _context.Order
                .Where(x => x.PatientId == patientId && x.StartTime > utcNow)
                .OrderBy(x => x.StartTime)
                .FirstOrDefault();

            if (order is null)
            {
                return null;
            }

            return new GetOrderResponse
            {
                Id = order.Id,
                StartTime = order.StartTime,
                EndTime = order.EndTime,
                PatientId = order.PatientId,
                DoctorId = order.DoctorId,
                SurgeryType = (int)order.GetSurgeryType()
            };
        }
    }
}
