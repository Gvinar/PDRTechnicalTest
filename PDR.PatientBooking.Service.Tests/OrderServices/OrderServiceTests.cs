using System;
using System.Linq;
using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using PDR.PatientBooking.Data;
using PDR.PatientBooking.Data.Models;
using PDR.PatientBooking.Service.OrderServices;
using PDR.PatientBooking.Service.OrderServices.Requests;
using PDR.PatientBooking.Service.OrderServices.Responses;
using PDR.PatientBooking.Service.OrderServices.Validation;
using PDR.PatientBooking.Service.Validation;

namespace PDR.PatientBooking.Service.Tests.OrderServices
{
    [TestFixture]
    public class OrderServiceTests
    {
        private MockRepository _mockRepository;
        private IFixture _fixture;

        private PatientBookingContext _context;
        private Mock<IAddOrderRequestValidator> _validator;

        private OrderService _orderService;

        [SetUp]
        public void SetUp()
        {
            // Boilerplate
            _mockRepository = new MockRepository(MockBehavior.Strict);
            _fixture = new Fixture();

            //Prevent fixture from generating circular references
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));

            // Mock setup
            _context = new PatientBookingContext(new DbContextOptionsBuilder<PatientBookingContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            _validator = _mockRepository.Create<IAddOrderRequestValidator>();

            // Mock default
            SetupMockDefaults();

            // Common DB Data
            SetupTestData();

            // Sut instantiation
            _orderService = new OrderService(
                _context,
                _validator.Object
            );
        }

        private void SetupMockDefaults()
        {
            _validator.Setup(x => x.ValidateRequest(It.IsAny<AddOrderRequest>()))
                .Returns(new PdrValidationResult(true));
        }

        private void SetupTestData()
        {
            var clinic = _fixture
                .Build<Clinic>()
                .Without(x => x.Patients)
                .Create();
            _context.Clinic.Add(clinic);

            var patient = _fixture
                .Build<Patient>()
                .With(x => x.ClinicId, clinic.Id)
                .Without(x => x.Clinic)
                .Create();
            _context.Patient.Add(patient);
            _context.SaveChanges();
        }

        [Test]
        public void AddOrder_ValidatesRequest()
        {
            //arrange
            var request = GetValidRequest();

            //act
            _orderService.AddOrder(request);

            //assert
            _validator.Verify(x => x.ValidateRequest(request), Times.Once);
        }

        [Test]
        public void AddOrder_ValidatorFails_ThrowsArgumentException()
        {
            //arrange
            var failedValidationResult = new PdrValidationResult(false, _fixture.Create<string>());

            _validator.Setup(x => x.ValidateRequest(It.IsAny<AddOrderRequest>())).Returns(failedValidationResult);

            //act
            var exception = Assert.Throws<ArgumentException>(() => _orderService.AddOrder(_fixture.Create<AddOrderRequest>()));

            //assert
            exception.Message.Should().Be(failedValidationResult.Errors.First());
        }

        [Test]
        public void AddOrder_AddsOrderToContextWithGeneratedId()
        {
            //arrange
            var request = GetValidRequest();
            var clinic = _context.Clinic.First();

            var expected = new Order
            {
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                PatientId = request.PatientId,
                DoctorId = request.DoctorId
            };

            //act
            _orderService.AddOrder(request);

            //assert
            _context.Order
                .Should().ContainEquivalentOf(expected, 
                    options => options
                        .Excluding(order => order.Id)
                        .Excluding(order => order.Doctor)
                        .Excluding(order => order.Patient))
                .And.Match(orders => 
                    orders.Any(order => order.SurgeryType == (int)clinic.SurgeryType && !order.IsCancelled));
        }

        [Test]
        public void CancelOrder_UpdateOrderWithIsCancelled()
        {
            //arrange
            var existingOrder = _fixture.Create<Order>();
            _context.Order.Add(existingOrder);
            _context.SaveChanges();

            var expected = new Order
            {
                Id = existingOrder.Id,
                StartTime = existingOrder.StartTime,
                EndTime = existingOrder.EndTime,
                PatientId = existingOrder.PatientId,
                DoctorId = existingOrder.DoctorId,
                SurgeryType = existingOrder.SurgeryType,
                IsCancelled = true
            };

            //act
            _orderService.CancelOrder(existingOrder.Id);

            //assert
            _context.Order.Should().ContainEquivalentOf(expected, 
                options => options
                    .Excluding(order => order.Doctor)
                    .Excluding(order => order.Patient));
        }

        [Test]
        public void GetPatientNextOrder_NoNonCancelledOrder_ReturnsNull()
        {
            //arrange
            var patientId = _fixture.Create<long>();

            //act
            var res = _orderService.GetPatientNextOrder(patientId);

            //assert
            res.Should().BeNull();
        }

        [Test]
        public void GetPatientNextOrder_ReturnsNonCancelledOrder()
        {
            //arrange
            var utcNow = DateTime.UtcNow;

            var patient = _fixture
                .Build<Patient>()
                .Without(x => x.Orders)
                .Create();

            var cancelledOrder = _fixture
                .Build<Order>()
                .With(x => x.StartTime, utcNow.AddHours(1))
                .With(x => x.IsCancelled, true)
                .With(x => x.PatientId, patient.Id)
                .With(x => x.SurgeryType, (int)patient.Clinic.SurgeryType)
                .Without(x => x.Doctor)
                .Without(x => x.Patient)
                .Create();

            var nonCancelledOrder = _fixture
                .Build<Order>()
                .With(x => x.StartTime, utcNow.AddHours(2))
                .With(x => x.IsCancelled, false)
                .With(x => x.PatientId, patient.Id)
                .With(x => x.SurgeryType, (int)patient.Clinic.SurgeryType)
                .Without(x => x.Doctor)
                .Without(x => x.Patient)
                .Create();

            _context.Patient.Add(patient);
            _context.Order.Add(cancelledOrder);
            _context.Order.Add(nonCancelledOrder);
            _context.SaveChanges();

            var expected = new GetOrderResponse
            {
                Id = nonCancelledOrder.Id,
                StartTime = nonCancelledOrder.StartTime,
                EndTime = nonCancelledOrder.EndTime,
                DoctorId = nonCancelledOrder.DoctorId,
                PatientId = nonCancelledOrder.PatientId,
                SurgeryType = nonCancelledOrder.SurgeryType
            };

            //act
            var res = _orderService.GetPatientNextOrder(patient.Id);

            //assert
            res.Should().BeEquivalentTo(expected);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
        }

        private AddOrderRequest GetValidRequest()
        {
            var patient = _context.Patient.First();

            var request = _fixture
                .Build<AddOrderRequest>()
                .With(x => x.PatientId, patient.Id)
                .Create();

            return request;
        }
    }
}
