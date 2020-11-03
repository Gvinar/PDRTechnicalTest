using System;

namespace PDR.PatientBooking.Service.OrderServices.Requests
{
    public class AddOrderRequest
    {
        public Guid Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long PatientId { get; set; }
        public long DoctorId { get; set; }
    }
}
