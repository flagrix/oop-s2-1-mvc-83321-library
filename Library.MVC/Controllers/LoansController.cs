using Library.Domain;
using Library.MVC.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Library.MVC.Controllers
{
    public class LoansController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoansController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Loans
        public async Task<IActionResult> Index()
        {
            var loans = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.Member)
                .OrderByDescending(l => l.LoanDate)
                .ToListAsync();
            return View(loans);
        }

        // GET: Loans/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var loan = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.Member)
                .FirstOrDefaultAsync(l => l.Id == id);
            if (loan == null) return NotFound();
            return View(loan);
        }

        // GET: Loans/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            // Only show available books
            var availableBooks = await _context.Books
                .Where(b => b.IsAvailable)
                .ToListAsync();

            ViewBag.BookId = new SelectList(availableBooks, "Id", "Title");
            ViewBag.MemberId = new SelectList(await _context.Members.ToListAsync(), "Id", "FullName");
            return View();
        }

        // POST: Loans/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("BookId,MemberId,LoanDate,DueDate")] Loan loan)
        {
            // Remove navigation properties from ModelState validation
            ModelState.Remove("Book");
            ModelState.Remove("Member");

            // Business rule: prevent duplicate active loan
            bool alreadyOnLoan = await _context.Loans
                .AnyAsync(l => l.BookId == loan.BookId && l.ReturnedDate == null);

            if (alreadyOnLoan)
            {
                ModelState.AddModelError("BookId", "This book is already on an active loan.");
            }

            if (ModelState.IsValid)
            {
                var book = await _context.Books.FindAsync(loan.BookId);
                if (book == null) return NotFound();

                book.IsAvailable = false;
                loan.ReturnedDate = null;

                _context.Add(loan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var availableBooks = await _context.Books.Where(b => b.IsAvailable).ToListAsync();
            ViewBag.BookId = new SelectList(availableBooks, "Id", "Title", loan.BookId);
            ViewBag.MemberId = new SelectList(await _context.Members.ToListAsync(), "Id", "FullName", loan.MemberId);
            return View(loan);
        }

        // POST: Loans/MarkReturned/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> MarkReturned(int id)
        {
            var loan = await _context.Loans
                .Include(l => l.Book)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null) return NotFound();

            if (loan.ReturnedDate == null)
            {
                loan.ReturnedDate = DateTime.Today;
                if (loan.Book != null)
                    loan.Book.IsAvailable = true;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Loans/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var loan = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.Member)
                .FirstOrDefaultAsync(l => l.Id == id);
            if (loan == null) return NotFound();
            return View(loan);
        }

        // POST: Loans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loan = await _context.Loans.Include(l => l.Book).FirstOrDefaultAsync(l => l.Id == id);
            if (loan != null)
            {
                // If the loan was still active, free the book
                if (loan.ReturnedDate == null && loan.Book != null)
                    loan.Book.IsAvailable = true;

                _context.Loans.Remove(loan);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
