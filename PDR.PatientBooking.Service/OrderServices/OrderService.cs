using System;
using System.Linq;
using PDR.PatientBooking.Data;
using PDR.PatientBooking.Data.Models;
using PDR.PatientBooking.Service.OrderServices.Validation;
using PDR.PatientBooking.Service.OrderServices.Requests;

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

            var clinic = _context.Patient.FirstOrDefault(x => x.Id == request.PatientId).Clinic;

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
    }
}
