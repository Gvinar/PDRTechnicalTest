using System;
using System.Linq;
using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDR.PatientBooking.Data;
using PDR.PatientBooking.Data.Models;
using PDR.PatientBooking.Service.OrderServices.Requests;
using PDR.PatientBooking.Service.OrderServices.Validation;

namespace PDR.PatientBooking.Service.Tests.OrderServices.Validation
{
    [TestFixture]
    public class AddOrderRequestValidatorTests
    {
        private IFixture _fixture;

        private PatientBookingContext _context;

        private AddOrderRequestValidator _addOrderRequestValidator;

        [SetUp]
        public void SetUp()
        {
            // Boilerplate
            _fixture = new Fixture();

            //Prevent fixture from generating from entity circular references 
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));

            // Mock setup
            _context = new PatientBookingContext(new DbContextOptionsBuilder<PatientBookingContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

            // Common DB Data
            SetupTestData();

            // Sut instantiation
            _addOrderRequestValidator = new AddOrderRequestValidator(
                _context
            );
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

            var doctor = _fixture
                .Build<Doctor>()
                .Without(x => x.Orders)
                .Create();
            _context.Doctor.Add(doctor);

            _context.SaveChanges();
        }

        [Test]
        public void ValidateRequest_AllChecksPass_ReturnsPassedValidationResult()
        {
            //arrange
            var request = GetValidRequest();

            //act
            var res = _addOrderRequestValidator.ValidateRequest(request);

            //assert
            res.PassedValidation.Should().BeTrue();
        }

        [Test]
        public void ValidateRequest_StartTimeLessThanNow_ReturnsFailedValidationResult()
        {
            //arrange
            var request = GetValidRequest();
            request.StartTime = DateTime.UtcNow.Subtract(TimeSpan.FromHours(1));

            //act
            var res = _addOrderRequestValidator.ValidateRequest(request);

            //assert
            res.PassedValidation.Should().BeFalse();
            res.Errors.Should().Contain("StartTime should be greater than current time");
        }

        [Test]
        public void ValidateRequest_EndTimeLessThanStartTime_ReturnsFailedValidationResult()
        {
            //arrange
            var request = GetValidRequest();
            request.EndTime = DateTime.UtcNow.Subtract(TimeSpan.FromHours(1));

            //act
            var res = _addOrderRequestValidator.ValidateRequest(request);

            //assert
            res.PassedValidation.Should().BeFalse();
            res.Errors.Should().Contain("EndTime should be greater than StartTime");
        }

        [Test]
        public void ValidateRequest_PatientNotFound_ReturnsFailedValidationResult()
        {
            //arrange
            var request = GetValidRequest();
            request.PatientId = -1;

            //act
            var res = _addOrderRequestValidator.ValidateRequest(request);

            //assert
            res.PassedValidation.Should().BeFalse();
            res.Errors.Should().Contain("A patient with that ID could not be found");
        }

        [Test]
        public void ValidateRequest_DoctorNotFound_ReturnsFailedValidationResult()
        {
            //arrange
            var request = GetValidRequest();
            request.DoctorId = -1;

            //act
            var res = _addOrderRequestValidator.ValidateRequest(request);

            //assert
            res.PassedValidation.Should().BeFalse();
            res.Errors.Should().Contain("A doctor with that ID could not be found");
        }

        [Test]
        public void ValidateRequest_DoctorIsScheduled_ReturnsFailedValidationResult()
        {
            //arrange
            var request = GetValidRequest();
            var order = _fixture
                .Build<Order>()
                .With(x => x.StartTime, request.StartTime)
                .With(x => x.EndTime, request.EndTime)
                .With(x => x.DoctorId, request.DoctorId)
                .With(x => x.IsCancelled, false)
                .Without(x => x.Doctor)
                .Without(x => x.Patient)
                .Create();
            _context.Order.Add(order);
            _context.SaveChanges();

            //act
            var res = _addOrderRequestValidator.ValidateRequest(request);

            //assert
            res.PassedValidation.Should().BeFalse();
            res.Errors.Should().Contain("A doctor is already scheduled for this time");
        }

        private AddOrderRequest GetValidRequest()
        {
            var patient = _context.Patient.First();
            var doctor = _context.Doctor.First();
            var utcNow = DateTime.UtcNow;

            var request = _fixture
                .Build<AddOrderRequest>()
                .With(x => x.DoctorId, doctor.Id)
                .With(x => x.PatientId, patient.Id)
                .With(x => x.StartTime, utcNow.AddHours(1))
                .With(x => x.EndTime, utcNow.AddHours(2))
                .Create();
            return request;
        }
    }
}
