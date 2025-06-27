// Controllers/HomeController.cs
   using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // 检查是否已登录
        if (HttpContext.Session.GetString("IsLoggedIn") != "true")
        {
            return RedirectToAction("Index", "Login");
        }
        return View();
    }
}