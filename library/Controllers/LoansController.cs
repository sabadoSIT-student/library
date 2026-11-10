
using library.Data;
using library.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace library.Controllers;

public class LoansController : Controller
{
    private readonly LoanRepository _loans;
    private readonly MembersRepository _members;
    private readonly BookRepository _books;

    public LoansController(
        LoanRepository loans,
        MembersRepository members,
        BookRepository books)
    {
        _loans = loans;
        _members = members;
        _books = books;
    }

    public async Task<IActionResult> Index()
    {
        var loans = await _loans.GetAllAsync();
        return View(loans);
    }

    // Fills the Borrow form's two dropdowns from the database.
    private async Task FillDropdownsAsync()
    {
        ViewBag.Members = new SelectList(
            await _members.GetAllAsync(), "MemberId", "FullName");

        ViewBag.Books = new SelectList(
            await _books.GetAllAsync(), "BookId", "Title");
    }

    // GET /Loans/Borrow
    public async Task<IActionResult> Borrow()
    {
        await FillDropdownsAsync();
        return View(new BorrowForm());
    }

    // POST /Loans/Borrow
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Borrow(BorrowForm form)
    {
        if (ModelState.IsValid)
        {
            bool saved = await _loans.BorrowAsync(
                form.MemberId!.Value,
                form.BookId!.Value,
                form.DaysAllowed);

            if (saved)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(
                "",
                $"This member already has {LoanRepository.MaxOpenLoans} books out. Nothing was saved.");
        }

        await FillDropdownsAsync();
        return View(form);
    }
}
