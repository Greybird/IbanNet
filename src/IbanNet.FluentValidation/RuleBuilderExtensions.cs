﻿using FluentValidation;

namespace IbanNet.FluentValidation
{
	/// <summary>
	/// FluentValidation rule builder extensions.
	/// </summary>
	public static class RuleBuilderExtensions
	{
		/// <summary>
		/// Defines a regular expression validator on the current rule builder, but only for string properties.
		/// Validation will fail if the value returned by the lambda is not a valid email address.
		/// </summary>
		/// <typeparam name="T">Type of object being validated</typeparam>
		/// <param name="ruleBuilder">The rule builder on which the validator should be defined</param>
		/// <param name="ibanValidator">The <see cref="IIbanValidator"/> instance to use for validation.</param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, string> Iban<T>(
			this IRuleBuilder<T, string> ruleBuilder, IIbanValidator ibanValidator)
		{
			return ruleBuilder.SetValidator(new FluentIbanValidator(ibanValidator));
		}
	}
}