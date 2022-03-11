using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVCFlowerShop.Data;
using MVCFlowerShop.Models;

namespace MVCFlowerShop.Controllers
{
    public class FlowersController : Controller
    {
        private readonly MVCFlowerShopNewContext _context;

        public FlowersController(MVCFlowerShopNewContext context)
        {
            _context = context;
        }

        // GET: Flowers
        public async Task<IActionResult> Index(string SearchString, string FlowerType)
        {
            var flower = from m in _context.Flower
                         select m;
            IQueryable<string> dropdownlistquery = from m in _context.Flower
                                                   orderby m.Type
                                                   select m.Type;
            IEnumerable<SelectListItem> items = new SelectList(await dropdownlistquery.Distinct().ToListAsync());
            ViewBag.FlowerType = items;
            if (! string.IsNullOrEmpty(SearchString))
            {
                flower = flower.Where(s => s.FlowerName.Contains(SearchString));
            }
            if (!string.IsNullOrEmpty(FlowerType))
            {
                flower = flower.Where(s => s.Type.Equals(FlowerType));
            }
            return View(await flower.ToListAsync());
        }

        // GET: Flowers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flower = await _context.Flower
                .FirstOrDefaultAsync(m => m.ID == id);
            if (flower == null)
            {
                return NotFound();
            }

            return View(flower);
        }

        // GET: Flowers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Flowers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,FlowerName,FlowerProducedDate,Type,Price,Rating")] Flower flower)
        {
            if (ModelState.IsValid)
            {
                _context.Add(flower);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(flower);
        }

        // GET: Flowers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flower = await _context.Flower.FindAsync(id);
            if (flower == null)
            {
                return NotFound();
            }
            return View(flower);
        }

        // POST: Flowers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,FlowerName,FlowerProducedDate,Type,Price,Rating")] Flower flower)
        {
            if (id != flower.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(flower);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FlowerExists(flower.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(flower);
        }

        // GET: Flowers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flower = await _context.Flower
                .FirstOrDefaultAsync(m => m.ID == id);
            if (flower == null)
            {
                return NotFound();
            }

            return View(flower);
        }

        // POST: Flowers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var flower = await _context.Flower.FindAsync(id);
            _context.Flower.Remove(flower);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FlowerExists(int id)
        {
            return _context.Flower.Any(e => e.ID == id);
        }

        public async Task<IActionResult> SearchPage(string msg = "")
        {
            ViewBag.msg = msg;
            IQueryable<string> query = from m in _context.Flower
                                                   orderby m.Type
                                                   select m.Type;
            IEnumerable<SelectListItem> items = new SelectList(await query.Distinct().ToListAsync());
            ViewBag.FlowerType = items;
            return View();
        }

        public async Task<IActionResult> displaySearchResult(string SearchString, string FlowerType)
        {
            string message = "";
            if(string.IsNullOrEmpty(SearchString))
            {
                message = "Please enter a Search Keyword before proceed with the search function!";
                return RedirectToAction("SearchPage", new { msg = message });
            }

            var flower = from m in _context.Flower
                         select m;
            flower = flower.Where(s => s.FlowerName.Contains(SearchString));
            
            if (! string.IsNullOrEmpty(FlowerType))
            {
                flower = flower.Where(s => s.Type.Equals(FlowerType));
            }

            return View(await flower.ToListAsync());
        }
    }
}
