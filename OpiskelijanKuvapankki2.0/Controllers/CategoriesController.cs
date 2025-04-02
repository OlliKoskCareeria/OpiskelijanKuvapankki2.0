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

        [HttpGet("{id}")]
        [AllowAnonymous]
        public ActionResult GetOneCategory(int id)
        {
            try
            {
                var categ = db.Categories.Find(id);

                if (categ != null)
                {
                    return Ok(categ);
                }
                else
                {
                    return NotFound("Kategoriaa ei löydy");
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Jotain meni pieleen." + ex.InnerException);
            }
        }

        [HttpPost]
        public ActionResult AddNewCategory([FromBody] Category newcategory)
        {
            
                try
                {
                if(db.Categories.Count() > 15) {
                    return BadRequest("Kategorioita ei voi lisätä enempää!");//rajoitetaan kategorioiden määrää
                }
                    db.Categories.Add(newcategory);
                    db.SaveChanges();
                    return Ok($"Lisättiin uusi kategoria {newcategory.CategoryName}");
                }
                catch (Exception e)
                {
                    return BadRequest("Tapahtui virhe. Lue lisää: " + e.InnerException);
                }
            
        }

        [HttpDelete("{id}")]
        public ActionResult RemoveUnUsedCategory(int id)
        {
            try
            {

                var categ = db.Categories.Find(id);

                if (categ == null)
                {

                    return NotFound("Kategoriaa" + " ei löytynyt.");
                }

                var catState = db.Images.Where(k => k.CategoryId == categ.CategoryId).FirstOrDefault();
                
                if(catState != null)//Estää poistamisen jos kategoria on käytössä
                {
                    return BadRequest("Kategoria on käytössä!");
                }

                db.Categories.Remove(categ);
                db.SaveChanges();
                return Ok("Poistettiin kategoria" + categ.CategoryName);
                
                
            }
            catch (Exception e)
            {
                return BadRequest(e.InnerException);
            }
        }

        [HttpPut("{id}")]
        public ActionResult ChangeCategoryName(int id,string newname)
        {
            var renamedcategory = db.Categories.Find(id);
            if (renamedcategory != null)
            {
                renamedcategory.CategoryName = newname;
                db.SaveChanges();
                return Ok("Muutettu kategorian nimi muotoon " + renamedcategory.CategoryName);
                
            }
            return NotFound("Kategoriaa ei löytynyt id:llä " + id);
        }


    }

}
