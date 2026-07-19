using ClassBooking.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ClassBooking.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(AppDbContext context) : IUserRepository
{
  public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
      context.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

  public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
      context.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

  public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken) =>
      context.Users.AsNoTracking().AnyAsync(user => user.Email == email, cancellationToken);

  public void Add(User user) => context.Users.Add(user);
}
