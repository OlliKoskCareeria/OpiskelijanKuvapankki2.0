using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpiskelijanKuvapankki2_0.Models;

namespace OpiskelijanKuvapankki2_0.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController(OpiskelijanKuvapankki2_0Context _db) : ControllerBase
    {
        private readonly OpiskelijanKuvapankki2_0Context db = _db;

        [HttpGet]
        [AllowAnonymous]
        public ActionResult GetAllCategories()
        {
            var categories = db.Categories.ToList();
            return Ok(categories);
        }


    }

}
