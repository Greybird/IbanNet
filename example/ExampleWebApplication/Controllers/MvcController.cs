﻿using ExampleWebApplication.Models;
using IbanNet;
using IbanNet.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ExampleWebApplication.Controllers
{
	/// <summary>
	/// MVC example, showing usage of <see cref="IbanAttribute"/>.
	/// </summary>
	public class MvcController : Controller
	{
		// GET
		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Save(InputModel model)
		{
			if (!ModelState.IsValid)
			{
				return View("Index");
			}

		    Iban iban = Iban.Parse(model.BankAccountNumber);
            // Do something with model...
		    ModelState.Clear();
		    model.BankAccountNumber = iban.ToString(Iban.Formats.Partitioned);

            return View("Index", model);
		}
	}
}