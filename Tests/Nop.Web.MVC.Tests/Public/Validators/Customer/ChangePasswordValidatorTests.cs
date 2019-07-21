﻿using FluentValidation.TestHelper;
using Nop.Core.Domain.Customers;
using Nop.Web.Models.Customer;
using Nop.Web.Validators.Customer;
using NUnit.Framework;

namespace Nop.Web.MVC.Tests.Public.Validators.Customer
{
    [TestFixture]
    public class ChangePasswordValidatorTests : BaseValidatorTests
    {
        private ChangePasswordValidator _validator;
        private CustomerSettings _customerSettings;
        
        [SetUp]
        public new void Setup()
        {
            _customerSettings = new CustomerSettings();
            _validator = new ChangePasswordValidator(_localizationService, _customerSettings);
        }
        
        [Test]
        public void Should_have_error_when_oldPassword_is_null_or_empty()
        {
            var model = new ChangePasswordModel
            {
                OldPassword = null
            };
            _validator.ShouldHaveValidationErrorFor(x => x.OldPassword, model);
            model.OldPassword = "";
            _validator.ShouldHaveValidationErrorFor(x => x.OldPassword, model);
        }

        [Test]
        public void Should_not_have_error_when_oldPassword_is_specified()
        {
            var model = new ChangePasswordModel
            {
                OldPassword = "old password"
            };
            _validator.ShouldNotHaveValidationErrorFor(x => x.OldPassword, model);
        }

        [Test]
        public void Should_have_error_when_newPassword_is_null_or_empty()
        {
            var model = new ChangePasswordModel
            {
                NewPassword = null
            };
            //we know that new password should equal confirmation password
            model.ConfirmNewPassword = model.NewPassword;
            _validator.ShouldHaveValidationErrorFor(x => x.NewPassword, model);
            model.NewPassword = "";
            //we know that new password should equal confirmation password
            model.ConfirmNewPassword = model.NewPassword;
            _validator.ShouldHaveValidationErrorFor(x => x.NewPassword, model);
        }

        [Test]
        public void Should_not_have_error_when_newPassword_is_specified()
        {
            var model = new ChangePasswordModel
            {
                NewPassword = "new password"
            };
            //we know that new password should equal confirmation password
            model.ConfirmNewPassword = model.NewPassword;
            _validator.ShouldNotHaveValidationErrorFor(x => x.NewPassword, model);
        }

        [Test]
        public void Should_have_error_when_confirmNewPassword_is_null_or_empty()
        {
            var model = new ChangePasswordModel
            {
                ConfirmNewPassword = null
            };
            _validator.ShouldHaveValidationErrorFor(x => x.ConfirmNewPassword, model);
            model.ConfirmNewPassword = "";
            _validator.ShouldHaveValidationErrorFor(x => x.ConfirmNewPassword, model);
        }

        [Test]
        public void Should_not_have_error_when_confirmNewPassword_is_specified()
        {
            var model = new ChangePasswordModel
            {
                ConfirmNewPassword = "some password"
            };
            //we know that new password should equal confirmation password
            model.NewPassword = model.ConfirmNewPassword;
            _validator.ShouldNotHaveValidationErrorFor(x => x.ConfirmNewPassword, model);
        }

        [Test]
        public void Should_have_error_when_newPassword_doesnot_equal_confirmationPassword()
        {
            var model = new ChangePasswordModel
            {
                NewPassword = "some password",
                ConfirmNewPassword = "another password"
            };
            _validator.ShouldHaveValidationErrorFor(x => x.ConfirmNewPassword, model);
        }

        [Test]
        public void Should_not_have_error_when_newPassword_equals_confirmationPassword()
        {
            var model = new ChangePasswordModel
            {
                NewPassword = "some password",
                ConfirmNewPassword = "some password"
            };
            _validator.ShouldNotHaveValidationErrorFor(x => x.NewPassword, model);
        }
    }
}
