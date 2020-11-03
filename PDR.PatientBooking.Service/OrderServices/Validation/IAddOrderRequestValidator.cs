using PDR.PatientBooking.Service.OrderServices.Requests;
using PDR.PatientBooking.Service.Validation;

namespace PDR.PatientBooking.Service.OrderServices.Validation
{
    public interface IAddOrderRequestValidator
    {
        PdrValidationResult ValidateRequest(AddOrderRequest request);
    }
}