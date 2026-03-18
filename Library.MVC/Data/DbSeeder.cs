using Bogus;
using Library.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Library.MVC.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();

            // Seed Admin role + user
            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            if (await userManager.FindByEmailAsync("admin@library.com") == null)
            {
                var admin = new IdentityUser
                {
                    UserName = "admin@library.com",
                    Email = "admin@library.com",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // Only seed if empty
            if (await context.Books.AnyAsync()) return;

            var categories = new[] { "Fiction", "Science", "History", "Technology", "Biography", "Fantasy", "Mystery" };

            var bookFaker = new Faker<Book>()
                .RuleFor(b => b.Title, f => f.Lorem.Sentence(3).TrimEnd('.'))
                .RuleFor(b => b.Author, f => f.Name.FullName())
                .RuleFor(b => b.Isbn, f => f.Commerce.Ean13())
                .RuleFor(b => b.Category, f => f.PickRandom(categories))
                .RuleFor(b => b.IsAvailable, _ => true);

            var books = bookFaker.Generate(20);
            await context.Books.AddRangeAsync(books);
            await context.SaveChangesAsync();

            var memberFaker = new Faker<Member>()
                .RuleFor(m => m.FullName, f => f.Name.FullName())
                .RuleFor(m => m.Email, (f, m) => f.Internet.Email(m.FullName))
                .RuleFor(m => m.Phone, f => f.Phone.PhoneNumber("###-###-####"));

            var members = memberFaker.Generate(10);
            await context.Members.AddRangeAsync(members);
            await context.SaveChangesAsync();

            var random = new Random(42);
            var loans = new List<Loan>();
            var usedBookIds = new HashSet<int>();
            var today = DateTime.Today;

            // 5 returned loans
            for (int i = 0; i < 5; i++)
            {
                var book = books[i];
                var loanDate = today.AddDays(-random.Next(30, 60));
                loans.Add(new Loan
                {
                    BookId = book.Id,
                    MemberId = members[random.Next(members.Count)].Id,
                    LoanDate = loanDate,
                    DueDate = loanDate.AddDays(14),
                    ReturnedDate = loanDate.AddDays(random.Next(5, 13))
                });
            }

            // 5 active (not overdue) loans
            for (int i = 5; i < 10; i++)
            {
                var book = books[i];
                var loanDate = today.AddDays(-random.Next(1, 7));
                book.IsAvailable = false;
                usedBookIds.Add(book.Id);
                loans.Add(new Loan
                {
                    BookId = book.Id,
                    MemberId = members[random.Next(members.Count)].Id,
                    LoanDate = loanDate,
                    DueDate = today.AddDays(random.Next(3, 10)),
                    ReturnedDate = null
                });
            }

            // 5 overdue loans
            for (int i = 10; i < 15; i++)
            {
                var book = books[i];
                var loanDate = today.AddDays(-random.Next(20, 30));
                book.IsAvailable = false;
                usedBookIds.Add(book.Id);
                loans.Add(new Loan
                {
                    BookId = book.Id,
                    MemberId = members[random.Next(members.Count)].Id,
                    LoanDate = loanDate,
                    DueDate = loanDate.AddDays(14),
                    ReturnedDate = null
                });
            }

            await context.Loans.AddRangeAsync(loans);
            await context.SaveChangesAsync();
        }
    }
}
