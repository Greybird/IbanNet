﻿using System;
using System.Collections.Generic;
using FluentAssertions;
using IbanNet.Registry;
using IbanNet.Validation.Rules;
using Moq;
using NUnit.Framework;

namespace IbanNet
{
	[TestFixture]
	internal class IbanValidatorTests
	{
		private IbanValidator _validator;
		private ICountryValidationSupport _countryValidationSupport;
		private Mock<IIbanValidationRule> _customValidationRuleMock;

		[SetUp]
		public void SetUp()
		{
			_customValidationRuleMock = new Mock<IIbanValidationRule>();
			_validator = new IbanValidator(new IbanValidatorOptions
			{
				Rules =
				{
					_customValidationRuleMock.Object
				}
			});
			_countryValidationSupport = _validator;
		}

		[Test]
		public void When_validating_null_value_should_not_validate()
		{
			// Act
			ValidationResult actual = _validator.Validate(null);

			// Assert
			actual.Should().BeEquivalentTo(new ValidationResult
			{
				Result = IbanValidationResult.InvalidLength,
				ValidationRuleType = typeof(NotNullRule)
			});
		}

		[TestCase("NL91ABNA041716430!")]
		[TestCase("NL91ABNA^417164300")]
		public void When_validating_iban_with_illegal_characters_should_not_validate(string ibanWithIllegalChars)
		{
			// Act
			ValidationResult actual = _validator.Validate(ibanWithIllegalChars);

			// Assert
			actual.Should().BeEquivalentTo(new ValidationResult
			{
				Value = ibanWithIllegalChars,
				Result = IbanValidationResult.IllegalCharacters,
				Country = _countryValidationSupport.SupportedCountries[ibanWithIllegalChars.Substring(0, 2)],
				ValidationRuleType = typeof(NoIllegalCharactersRule)
			});
		}

		[TestCase("0091ABNA0417164300")]
		[TestCase("4591ABNA0417164300")]
		[TestCase("#L91ABNA0417164300")]
		public void When_validating_iban_with_illegal_country_code_should_not_validate(string ibanWithIllegalCountryCode)
		{
			// Act
			ValidationResult actual = _validator.Validate(ibanWithIllegalCountryCode);

			// Assert
			actual.Should().BeEquivalentTo(new ValidationResult
			{
				Value = ibanWithIllegalCountryCode,
				Result = IbanValidationResult.IllegalCharacters
			}, opts => opts.Excluding(vr => vr.ValidationRuleType));
		}

		[TestCase("NL00ABNA0417164300")]
		[TestCase("NL01ABNA0417164300")]
		[TestCase("NL99ABNA0417164300")]
		public void When_validating_iban_with_invalid_checksum_should_not_validate(string ibanWithInvalidChecksum)
		{
			// Act
			ValidationResult actual = _validator.Validate(ibanWithInvalidChecksum);

			// Assert
			actual.Should().BeEquivalentTo(new ValidationResult
			{
				Value = ibanWithInvalidChecksum,
				Result = IbanValidationResult.IllegalCharacters,
				Country = _countryValidationSupport.SupportedCountries[ibanWithInvalidChecksum.Substring(0, 2)],
				ValidationRuleType = typeof(HasIbanChecksumRule)
			});
		}


		[TestCase("NL91ABNA04171643000")]
		[TestCase("NL91ABNA041716430")]
		[TestCase("NO938601111794")]
		[TestCase("NO93860111179470")]
		public void When_validating_iban_with_incorrect_length_should_not_validate(string ibanWithIncorrectLength)
		{
			// Act
			ValidationResult actual = _validator.Validate(ibanWithIncorrectLength);

			// Assert
			actual.Should().BeEquivalentTo(new ValidationResult
			{
				Value = ibanWithIncorrectLength,
				Result = IbanValidationResult.InvalidLength,
				Country = _countryValidationSupport.SupportedCountries[ibanWithIncorrectLength.Substring(0, 2)],
				ValidationRuleType = typeof(IsValidLengthRule)
			});
		}

		[TestCase("AA91ABNA0417164300")]
		[TestCase("ZZ93860111179470")]
		public void When_validating_iban_with_unknown_country_code_should_not_validate(string ibanWithUnknownCountryCode)
		{
			// Act
			ValidationResult actual = _validator.Validate(ibanWithUnknownCountryCode);

			// Assert
			actual.Should().BeEquivalentTo(new ValidationResult
			{
				Value = ibanWithUnknownCountryCode,
				Result = IbanValidationResult.UnknownCountryCode,
				ValidationRuleType = typeof(IsValidCountryCodeRule)
			});
		}

		[TestCase("NL91ABNA041716430A")]
		[TestCase("NO938601111794A")]
		public void When_validating_iban_with_invalid_structure_should_not_validate(string ibanWithInvalidStructure)
		{
			// Act
			ValidationResult actual = _validator.Validate(ibanWithInvalidStructure);

			// Assert
			actual.Should().BeEquivalentTo(new ValidationResult
			{
				Value = ibanWithInvalidStructure,
				Result = IbanValidationResult.InvalidStructure,
				Country = _countryValidationSupport.SupportedCountries[ibanWithInvalidStructure.Substring(0, 2)],
				ValidationRuleType = typeof(IsMatchingStructureRule)
			});
		}

		[TestCase("NL92ABNA0417164300")]
		[TestCase("NO9486011117947")]
		public void When_validating_tampered_iban_should_not_validate(string tamperedIban)
		{
			// Act
			ValidationResult actual = _validator.Validate(tamperedIban);

			// Assert
			actual.Should().BeEquivalentTo(new ValidationResult
			{
				Value = tamperedIban,
				Result = IbanValidationResult.InvalidCheckDigits,
				Country = _countryValidationSupport.SupportedCountries[tamperedIban.Substring(0, 2)],
				ValidationRuleType = typeof(Mod97Rule)
			});
		}

		[TestCase("NL91 ABNA 0417 1643 00")]
		[TestCase("NL91\tABNA\t0417\t1643\t00")]
		[TestCase(" NL91 ABNA041 716 4300 ")]
		public void When_iban_contains_whitespace_should_validate(string ibanWithWhitespace)
		{
			// Act
			ValidationResult actual = _validator.Validate(ibanWithWhitespace);

			// Assert
			actual.Should().BeEquivalentTo(new ValidationResult
			{
				Value = Iban.Normalize(ibanWithWhitespace),
				Result = IbanValidationResult.Valid,
				Country = _countryValidationSupport.SupportedCountries["NL"]
			});
		}

		[Test]
		public void When_validating_should_call_custom_rule()
		{
			// Act
			_validator.Validate("NL91ABNA0417164300");

			// Assert
			_customValidationRuleMock.Verify(m => m.Validate(It.IsAny<ValidationRuleContext>()), Times.Once);
		}

		[Test]
		public void Given_custom_rule_fails_when_validating_should_not_validate()
		{
			const string iban = "NL91ABNA0417164300";
			const string errorMessage = "My custom error";

			_customValidationRuleMock
				.Setup(m => m.Validate(It.IsAny<ValidationRuleContext>()))
				.Callback<ValidationRuleContext>(ctx => ctx.Fail(errorMessage));

			// Act
			ValidationResult actual = _validator.Validate(iban);

			// Assert
			actual.Should().BeEquivalentTo(new ValidationResult
			{
				Value = iban,
				Result = IbanValidationResult.Custom,
				Country = _countryValidationSupport.SupportedCountries["NL"],
				ValidationRuleType = _customValidationRuleMock.Object.GetType(),
				ErrorMessage = errorMessage
			});
		}

		[Test]
		public void Given_custom_rule_fails_with_exception_when_validating_should_not_validate()
		{
			const string iban = "NL91ABNA0417164300";
			Exception exception = new InvalidOperationException("My custom error");

			_customValidationRuleMock
				.Setup(m => m.Validate(It.IsAny<ValidationRuleContext>()))
				.Callback<ValidationRuleContext>(ctx => ctx.Fail(exception));

			// Act
			ValidationResult actual = _validator.Validate(iban);

			// Assert
			actual.Should().BeEquivalentTo(new ValidationResult
			{
				Value = iban,
				Result = IbanValidationResult.Custom,
				Country = _countryValidationSupport.SupportedCountries["NL"],
				ValidationRuleType = _customValidationRuleMock.Object.GetType(),
				Exception = exception
			});
		}

		[TestCaseSource(typeof(IbanTestCaseData), nameof(IbanTestCaseData.GetValidIbanPerCountry))]
		public void When_validating_good_iban_should_validate(string countryCode, string iban)
		{
			var expectedResult = new ValidationResult
			{
				Value = iban,
				Result = IbanValidationResult.Valid,
				Country = _countryValidationSupport.SupportedCountries[iban.Substring(0, 2)]
			};

			// Act
			ValidationResult actual = _validator.Validate(iban);

			// Assert
			actual.Should().BeEquivalentTo(expectedResult);
		}

		[Test]
		public void When_getting_supported_countries_should_match_default_registry()
		{
			_validator.SupportedCountries
				.Should()
				.BeEquivalentTo(new IbanRegistry());
		}

		[Test]
		public void When_casting_readonly_countries_dictionary_should_not_be_able_to_add()
		{
			var countries = (IDictionary<string, CountryInfo>)((ICountryValidationSupport)_validator).SupportedCountries;

			// Act
			Action act = () => countries.Add("key", new CountryInfo());

			// Assert
			act.Should().Throw<NotSupportedException>()
				.WithMessage("Collection is read-only.");
		}

		[TestCase("NL91abna0417164300", IbanValidationResult.InvalidStructure, Description = "A region that requires bank details to be upper case")]
		[TestCase("MT84MALT011000012345mtlcast001S", IbanValidationResult.Valid, Description = "A region that allows bank details to be lower case")]
		public void When_validating_iban_with_invalid_case_should_not_be_valid(string lowerIban, IbanValidationResult expectedResult)
		{
			// Act
			ValidationResult actual = _validator.Validate(lowerIban);

			// Assert
			actual.Result.Should().Be(expectedResult);
		}		
	}
}
