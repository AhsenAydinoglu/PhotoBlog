using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using PhotoBlog.Areas.Admin.Models;
using PhotoBlog.Data;

namespace PhotoBlog.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PostsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Admin/Posts
        public async Task<IActionResult> Index()
        {
            return View(await _context.Posts.ToListAsync());
        }

        // GET: Admin/Posts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Posts == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .FirstOrDefaultAsync(m => m.Id == id);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // GET: Admin/Posts/Create
        public IActionResult Create()
        {
            LoadTags();
            return View();
        }

        private void LoadTags(HashSet<string>? postedTags = null)
        {

            HashSet<string> tags = _context.Tags.Select(x => x.Name).ToHashSet();

            if (postedTags != null)
            {
                tags.AddRange(postedTags);
            }

            ViewBag.Tags = tags.Select(x => new SelectListItem()
            {
                Text = x,
                Value = x
            }).OrderBy(x => x.Text);
        }

        // POST: Admin/Posts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateViewModel vm)
        {
            if (ModelState.IsValid)
            {
                List<Tag> tags = new List<Tag>();

                foreach (string tagName in vm.Tags!)
                {
                    var tag = _context.Tags.FirstOrDefault(x => x.Name == tagName);

                    if (tag == null)
                    {
                        tag = new Tag() { Name = tagName };
                    }

                    tags.Add(tag);
                }

                var post = new Post()
                {
                    Title = vm.Title,
                    Description = vm.Description,
                    Photo = SavePhoto(vm.Photo!),
                    Tags = tags
                };
                _context.Add(post);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            LoadTags(vm.Tags);
            return View(vm);
        }

        // https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-6.0
        private string SavePhoto(IFormFile photo)
        {
            var fileName = Guid.NewGuid() + Path.GetExtension(photo.FileName);
            string filePath = Path.Combine(_env.WebRootPath, "img", fileName);

            using (var stream = System.IO.File.Create(filePath))
            {
                photo.CopyTo(stream);
            }

            return fileName;
        }

        // GET: Admin/Posts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Posts == null)
            {
                return NotFound();
            }

            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound();
            }

            await _context.Entry(post).Collection(x=> x.Tags).LoadAsync();

            var vm = new EditViewModel()
            {
                Id = post.Id,
                Title = post.Title,
                Description = post.Description,
                Tags = post.Tags.Select(x => x.Name).ToHashSet()
            };
            
            LoadTags();
            return View(vm);
        }

        // POST: Admin/Posts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditViewModel vm)
        {
            if (ModelState.IsValid)
            {
                var post = await _context.Posts.FindAsync(vm.Id);

                if (post == null) return NotFound();

                post.Title = vm.Title;
                post.Description = vm.Description;
                if (vm.Photo != null)
                {
                    DeletePhoto(post.Photo);
                    post.Photo = SavePhoto(vm.Photo);
                }

                await _context.Entry(post).Collection(x=> x.Tags).LoadAsync();
                //post.Tags.RemoveRange(0, post.Tags.Count);
                post.Tags.RemoveAll(x => true);

                foreach (string tagName in vm.Tags!)
                {
                    var tag = await _context.Tags.FirstOrDefaultAsync(x => x.Name == tagName);

                    if (tag == null)
                    {
                        tag = new Tag() { Name = tagName };
                    }

                    post.Tags.Add(tag);
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            LoadTags(vm.Tags);
            return View(vm);
        }

        // GET: Admin/Posts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Posts == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .FirstOrDefaultAsync(m => m.Id == id);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // POST: Admin/Posts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Posts == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Posts'  is null.");
            }
            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                DeletePhoto(post.Photo);
                _context.Posts.Remove(post);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private void DeletePhoto(string photo)
        {
            if (string.IsNullOrEmpty(photo))
                return;

            string filePath = Path.Combine(_env.WebRootPath, "img", photo);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        private bool PostExists(int id)
        {
            return _context.Posts.Any(e => e.Id == id);
        }
    }
}
