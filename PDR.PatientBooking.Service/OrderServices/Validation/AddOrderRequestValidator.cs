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

            if (PatientNotFound(request, ref result))
                return result;

            if (DoctorNotFound(request, ref result))
                return result;

            return result;
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
    }
}
