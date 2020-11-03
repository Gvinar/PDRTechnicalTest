using System;

namespace PDR.PatientBooking.Service.OrderServices.Responses
{
    public class GetOrderResponse
    {
        public Guid Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long PatientId { get; set; }
        public long DoctorId { get; set; }
        public int SurgeryType { get; set; }
    }
}
