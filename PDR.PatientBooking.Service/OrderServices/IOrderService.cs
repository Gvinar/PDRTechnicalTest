using PDR.PatientBooking.Service.OrderServices.Requests;

namespace PDR.PatientBooking.Service.OrderServices
{
    public interface IOrderService
    {
        void AddOrder(AddOrderRequest request);
    }
}