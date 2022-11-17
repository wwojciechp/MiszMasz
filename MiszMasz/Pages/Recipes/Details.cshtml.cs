﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiszMasz.Entities;
using MiszMasz.Pages.Shared;

namespace MiszMasz.Pages.Recipes
{
    public class DetailsModel : AuthorizedPageModel
    {
        private readonly MiszMaszDbContext _context;

        public DetailsModel(MiszMaszDbContext context)
        {
            _context = context;
        }

        public Recipe Recipe { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Recipe = await _context.Recipes
                .Include(r => r.Author)
                .Include(r => r.Comments)
                .ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Recipe == null)
            {
                return NotFound();
            }

            try
            {
                var userId = Authorize();
                ViewData["IsAdded"] = await GetCoockbook(userId, Recipe.Id) != null;
            }
            catch { }

            return Page();
        }
        public async Task<IActionResult> OnPostComment([FromForm] int recipeId, [FromForm] string comment)
        {
            int userId = Authorize();
            if (!await _context.Recipes.AnyAsync(r => r.Id == recipeId))
                throw new Exception("Przepis nie istnieje.");

            _context.Comments.Add(new Comment
            {
                RecipeId = recipeId,
                AuthorId = userId,
                Text = comment
            });

            _context.SaveChanges();
            return Redirect($"/Recipes/Details?id={recipeId}");
        }

        public async Task<IActionResult> OnPostCookbook([FromForm] int recipeId)
        {
            int userId = Authorize();
            if (!await _context.Recipes.AnyAsync(r => r.Id == recipeId))
                throw new Exception("Przepis nie istnieje.");

            var cockbook = await GetCoockbook(userId, recipeId);

            if (cockbook == default)
            {
                _context.Cockbooks.Add(new Cockbook
                {
                    RecipeId = recipeId,
                    UserId = userId,
                });
            }
            else
            {
                _context.Cockbooks.Remove(cockbook);
            }
            _context.SaveChanges();
            return Redirect($"/Recipes/Details?id={recipeId}");
        }

        private async Task<Cockbook> GetCoockbook(int userId, int recipeId)
        {
            return await _context.Cockbooks.FirstOrDefaultAsync(c => c.RecipeId == recipeId && c.UserId == userId);
        }
    }
}
