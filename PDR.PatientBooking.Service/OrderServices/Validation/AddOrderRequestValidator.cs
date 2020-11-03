using System;
using System.Collections.Generic;
using System.Linq;
using PDR.PatientBooking.Data;
using PDR.PatientBooking.Service.OrderServices.Requests;
using PDR.PatientBooking.Service.Validation;

namespace PDR.PatientBooking.Service.OrderServices.Validation
{
    public class AddOrderRequestValidator : IAddOrderRequestValidator
    {
        private readonly PatientBookingContext _context;

        public AddOrderRequestValidator(PatientBookingContext context)
        {
            _context = context;
        }

        public PdrValidationResult ValidateRequest(AddOrderRequest request)
        {
            var result = new PdrValidationResult(true);

            if (MissingRequiredFields(request, ref result))
                return result;

            if (PatientNotFound(request, ref result))
                return result;

            if (DoctorNotFound(request, ref result))
                return result;

            if (DoctorIsAlreadyScheduled(request, ref result))
                return result;

            return result;
        }

        private bool MissingRequiredFields(AddOrderRequest request, ref PdrValidationResult result)
        {
            var errors = new List<string>();

            if (request.StartTime > request.EndTime)
                errors.Add("EndTime should be greater than StartTime");

            if (request.StartTime < DateTime.UtcNow)
                errors.Add("StartTime should be greater than current time");

            if (errors.Any())
            {
                result.PassedValidation = false;
                result.Errors.AddRange(errors);
                return true;
            }

            return false;
        }

        private bool PatientNotFound(AddOrderRequest request, ref PdrValidationResult result)
        {
            if (!_context.Patient.Any(x => x.Id == request.PatientId))
            {
                result.PassedValidation = false;
                result.Errors.Add("A patient with that ID could not be found");
                return true;
            }

            return false;
        }

        private bool DoctorNotFound(AddOrderRequest request, ref PdrValidationResult result)
        {
            if (!_context.Doctor.Any(x => x.Id == request.DoctorId))
            {
                result.PassedValidation = false;
                result.Errors.Add("A doctor with that ID could not be found");
                return true;
            }

            return false;
        }

        private bool DoctorIsAlreadyScheduled(AddOrderRequest request, ref PdrValidationResult result)
        {
            if (_context.Order.Any(x => x.DoctorId == request.DoctorId
                                        && !((request.StartTime < x.StartTime && request.EndTime < x.StartTime) ||
                                             (request.StartTime > x.EndTime && request.EndTime > x.EndTime))))
            {
                result.PassedValidation = false;
                result.Errors.Add("A doctor is already scheduled for this time");
                return true;
            }

            return false;
        }
    }
}
