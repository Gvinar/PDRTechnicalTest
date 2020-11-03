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
