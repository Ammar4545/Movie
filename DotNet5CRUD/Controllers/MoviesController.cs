using DotNet5CRUD.Models;
using DotNet5CRUD.ViewModels;
using Google.Rpc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
namespace DotNet5CRUD.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toasterNoti;
        private new List<string> _allowedExtention = new List<string> { ".png", ".jpg" };
        private long _maxAllowedPoster = 1048576;
        public MoviesController(ApplicationDbContext context , IToastNotification toasterNoti)
        {
            _context = context;
            _toasterNoti = toasterNoti;
        }
        public async Task<IActionResult> Index()
        {
            var movies = await _context.Movies.OrderByDescending(r=>r.Rate).ToListAsync();
            return View(movies);
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new MovieFormViewModel
            {
                Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync()
            };
            return View("MovieForm",viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                return View("MovieForm", model);
            }
            var files = Request.Form.Files;
            if (!files.Any())
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Please select movie poster");
                return View("MovieForm", model);
            }

            var poster = files.FirstOrDefault();
            //var allowedExtention = new List<string> { ".png", ".jpg" };
            if (! _allowedExtention.Contains(Path.GetExtension(poster.FileName).ToLower()))
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Please select .png and .jpg extention");
                return View("MovieForm", model);
            }

            if (poster.Length > _maxAllowedPoster)
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Poster can not be more than 1 MB");
                return View("MovieForm", model);
            }

            using var dataSteam = new MemoryStream();
            await poster.CopyToAsync(dataSteam);
            var movies = new Movie
            {
                Title = model.Title,
                GenreId = model.GenreId,
                Year = model.Year,
                Rate = model.Rate,
                StoreLine = model.StoreLine,
                Poster = dataSteam.ToArray()
            };
            _context.Movies.Add(movies);
            _context.SaveChanges();
            _toasterNoti.AddSuccessToastMessage("Movie Added Successfully");
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int ? id)
        {
            if (id==null)
                return BadRequest();
            
            var movie = await _context.Movies.FindAsync(id);

            if (movie==null)
                return NotFound();
            var viewModel = new MovieFormViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                GenreId=movie.GenreId,
                Rate=movie.Rate,
                Year=movie.Year,
                StoreLine=movie.StoreLine,
                Poster=movie.Poster,
                Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync()
            };


            return View("MovieForm", viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MovieFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                return View("MovieForm", model);
            }
            var movie = await _context.Movies.FindAsync(model.Id);
            if (movie == null)
                return NotFound();
            var files = Request.Form.Files;
            if (files.Any())
            {
                var poster = files.FirstOrDefault();
                using var dataStream = new MemoryStream();
                await poster.CopyToAsync(dataStream);

                model.Poster= dataStream.ToArray();

                if (!_allowedExtention.Contains(Path.GetExtension(poster.FileName).ToLower()))
                {
                    model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                    ModelState.AddModelError("Poster", "Please select .png and .jpg extention");
                    return View("MovieForm", model);
                }

                if (poster.Length > _maxAllowedPoster)
                {
                    model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                    ModelState.AddModelError("Poster", "Poster can not be more than 1 MB");
                    return View("MovieForm", model);
                }

                movie.Poster = model.Poster;
            }
            movie.Title = model.Title;
            movie.GenreId = model.GenreId;
            movie.Year = model.Year;
            movie.Rate = model.Rate;
            movie.StoreLine = model.StoreLine;

            _context.SaveChanges();
            _toasterNoti.AddSuccessToastMessage("Movie Updated Successfully");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int ? id)
        {
            if (id == null)
                return BadRequest();
            var movie = await _context.Movies.Include(g => g.Genre).SingleOrDefaultAsync(m => m.Id == id);

            if (movie==null)
                return NotFound();

            return View(movie);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return BadRequest();
            var movie = await _context.Movies.FindAsync(id);

            if (movie == null)
                return NotFound();

            _context.Movies.Remove(movie);

            _context.SaveChanges();

            return Ok();
        }

    }

}
