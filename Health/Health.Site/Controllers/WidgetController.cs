﻿using System.Collections.Generic;
using System.Web.Mvc;
using Health.Core.API;
using Health.Site.Helpers.Classes;
using Health.Site.Models;

namespace Health.Site.Controllers
{
    /// <summary>
    /// Контроллер для работы с виджетами.
    /// </summary>
    public class WidgetController : CoreController
    {
        public WidgetController(IDIKernel di_kernel) : base(di_kernel)
        {
        }

        /// <summary>
        /// Отображение главного меню.
        /// </summary>
        public ActionResult MainMenu()
        {
            ViewBag.Login = CoreKernel.AuthServ.UserCredential.Login;
            List<MenuElement> elements = new MainMenu(CoreKernel).GetMainMenuElements();
            return View(elements);
        }
    }
}