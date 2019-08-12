using System.Collections.Generic;
using System.Reflection;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;
using Xunit;
using Xunit.Abstractions;
namespace CCTools.DevIdeas.UnitTest.ValidationAndBusinessRules
{
    public static class FluentValidationExtensions
    {
        static IDictionary<string, object> RulesMetaData { get; } = new Dictionary<string, object>(1000);

        public static void WithMetaData<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, object customData)
        {
            RuleBuilder<T, TProperty> rb = (RuleBuilder<T, TProperty>) rule;
            RulesMetaData.Add(rb.Rule.RuleMetaDataKeyBuild(), customData);
        }

        public static object RuleMetaDataGet(this PropertyRule rule) => RulesMetaData[rule.RuleMetaDataKeyBuild()];

        public static string RuleMetaDataKeyBuild(this PropertyRule rule)
        {
            MemberInfo memberInfo = rule.Member;
            return $"{memberInfo.ReflectedType.FullName}.{memberInfo.Name}";
        }
    }

    /// <summary>
    ///     To be used on Stack Exchange is a copy of <seealso cref="Test_FluentValidation" />
    /// </summary>
    public class Test_FluentValidation_MetaData
    {
        /// <inheritdoc />
        public Test_FluentValidation_MetaData(ITestOutputHelper logger) => Logger = logger;

        ITestOutputHelper Logger { get; }

        public class CustomerValidator : AbstractValidator<Customer>
        {
            public CustomerValidator()
            {
                UixEntityProperty uixInfo = UixEntityPropertyCreate();

                //RuleFor(customer => customer.Surname).NotNull();
                RuleFor(customer => customer.Discount)
                    .Must((customer, val, context) => !(customer.CreditLevel <= 5 && val > 20))
                    .WithMessage(customer => $"Customer with Level {customer.CreditLevel} (must be  >=5)  cannot have discount = {customer.Discount}, must be <= 20")
                    .WithErrorCode(uixInfo.Id.ToString())
                    .WithState(customer => uixInfo)
                    .WithName(uixInfo.FriendlyName)
                    .WithMetaData(uixInfo);
            }

            static UixEntityProperty UixEntityPropertyCreate() => new UixEntityProperty {Id = 101, FriendlyName = "Discount level defined by customer loyalty", Help = "Call the Sales supervisor to update."};
        }

        /// <summary>
        ///     Some of the fields used by the Uix, in reality there is lots more data stored.
        /// </summary>
        public class UixEntityProperty
        {
            /// <summary>
            ///     Some Id given to help desk by user if something goes wrong!
            /// </summary>
            public int Id { get; set; } = 20102;
            public string FriendlyName { get; set; } = "Friendly names usually longer ";
            public string Help { get; set; } = "some help here to tell you what this field is all about!";

            /// <inheritdoc />
            public override string ToString() => $"Id: {Id} | Name: {FriendlyName} | Help: {Help}";
        }

        public class Customer
        {
            public int Id { get; set; }
            public string Surname { get; set; }
            public string Forename { get; set; }
            public decimal Discount { get; set; }
            public string Address { get; set; }
            public int CreditLevel { get; set; }
        }

        static Customer CustomerCreate()
        {
            Customer t = new Customer {Discount = 25, CreditLevel = 5, Surname = "Smith"};
            return t;
        }

        
        /// <summary>
        /// Not working, need to create a unique Key per Rule instance.
        /// </summary>
        [Fact]
        public void Test_all_rules_display_on_uix_debug()
        {
            for (int i = 0; i < 10; i++)
            {
                Customer t = CustomerCreate();
                CustomerValidator v = new CustomerValidator();

                foreach (IValidationRule vr in v)
                {
                    PropertyRule pr = (PropertyRule) vr;
                    UixEntityProperty meta = (UixEntityProperty) pr.RuleMetaDataGet();
                    Logger.WriteLine(nameof(PropertyRule));
                    Logger.WriteLine(string.Join(" | ", nameof(pr.DisplayName), pr.DisplayName, meta.FriendlyName));
                }
            }
        }

        [Fact]
        public void Test_validate_then_display_errors_on_uix_debug()
        {
            Customer t = CustomerCreate();
            CustomerValidator v = new CustomerValidator();

            ValidationResult res = v.Validate(t);

            if (res.IsValid) return;
            foreach (ValidationFailure e in res.Errors)
                Logger.WriteLine(string.Join(" | ", nameof(e.PropertyName), e.PropertyName, nameof(e.ErrorCode), e.ErrorCode, nameof(e.ErrorMessage), e.ErrorMessage, nameof(e.CustomState), e.CustomState));
        }
    }
}
