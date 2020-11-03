using PDR.PatientBooking.Service.OrderServices.Requests;
using PDR.PatientBooking.Service.OrderServices.Responses;

namespace PDR.PatientBooking.Service.OrderServices
{
    public interface IOrderService
    {
        void AddOrder(AddOrderRequest request);

        GetOrderResponse GetPatientNextAppointment(long patientId);
    }
}